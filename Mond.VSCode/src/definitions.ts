import { readFileSync } from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import { errorMessage, resolveVariables } from './utility';

export type MondSymbolKind = 'keyword' | 'constant' | 'function' | 'method' | 'property' | 'module' | 'class';

export interface MondSymbol {
	readonly name: string;
	readonly kind: MondSymbolKind;
	/** Call signatures without the owner, eg. `atan2(y: number, x: number): number`. */
	readonly signatures?: readonly string[];
	readonly documentation?: string;
}

export interface MondContainer extends MondSymbol {
	readonly kind: 'module' | 'class';
	readonly members: readonly MondSymbol[];
}

export interface MondInstanceMember extends MondSymbol {
	/** Prototypes and classes that declare a member with this name. */
	readonly owners: readonly string[];
}

/**
 * A resolved view of every definition file that is currently enabled. Providers hold on to the
 * whole object rather than individual lists so `revision` can be used to invalidate derived caches.
 */
export interface MondLibrary {
	readonly revision: number;
	readonly globals: readonly MondSymbol[];
	readonly modules: readonly MondContainer[];
	readonly classes: readonly MondContainer[];
	readonly prototypes: readonly MondContainer[];
	readonly instanceMembers: readonly MondInstanceMember[];
}

/** The current file format. Files claiming a newer version are ignored rather than misread. */
const supportedVersion = 1;

/** Definition files that ship with the extension, relative to its root. */
const builtInFiles = ['definitions/mond-stdlib.json', 'definitions/mond-extras.json'];

let extensionPath: string | undefined;
let cached: MondLibrary | undefined;
let revision = 0;

export function activateDefinitions(context: vscode.ExtensionContext) {
	extensionPath = context.extensionUri.fsPath;
	cached = undefined;

	context.subscriptions.push(
		vscode.workspace.onDidChangeConfiguration(e => {
			if (e.affectsConfiguration('mond.definitions')) {
				cached = undefined;
			}
		})
	);
}

export function getLibrary(): MondLibrary {
	return cached ??= build();
}

export function findModule(name: string): MondContainer | undefined {
	return getLibrary().modules.find(m => m.name === name);
}

export function findClass(name: string): MondContainer | undefined {
	return getLibrary().classes.find(c => c.name === name);
}

export function findGlobal(name: string): MondSymbol | undefined {
	return getLibrary().globals.find(g => g.name === name);
}

function build(): MondLibrary {
	const config = vscode.workspace.getConfiguration('mond.definitions');
	const files: string[] = [];

	if (config.get<boolean>('includeStandardLibrary') ?? true) {
		if (extensionPath) {
			files.push(...builtInFiles.map(f => path.join(extensionPath!, f)));
		}
	}

	for (const configured of config.get<string[]>('paths') ?? []) {
		if (typeof configured !== 'string' || configured.trim().length === 0) {
			continue;
		}

		const resolved = path.normalize(resolveVariables(configured.trim()));
		files.push(path.isAbsolute(resolved)
			? resolved
			: path.join(vscode.workspace.workspaceFolders?.[0]?.uri.fsPath ?? '', resolved));
	}

	const globals = new Map<string, MutableSymbol>();
	const prototypes = new Map<string, MutableSymbol>();

	for (const file of files) {
		const parsed = read(file);
		if (!parsed) {
			continue;
		}

		// later files win on conflicts, which lets a workspace correct anything the built-in
		// definitions get wrong for a customized build of Mond
		for (const entry of parsed.globals) {
			merge(globals, entry);
		}

		for (const entry of parsed.prototypes) {
			merge(prototypes, entry);
		}
	}

	const globalList = [...globals.values()].map(freeze).sort(byName);
	const prototypeList = [...prototypes.values()]
		.map(s => freeze({ ...s, kind: 'class' }) as MondContainer)
		.sort(byName);

	const modules = globalList.filter(isContainer).filter(g => g.kind === 'module');
	const classes = globalList.filter(isContainer).filter(g => g.kind === 'class');

	return {
		revision: ++revision,
		globals: globalList,
		modules,
		classes,
		prototypes: prototypeList,
		instanceMembers: collectInstanceMembers([...prototypeList, ...classes]),
	};
}

/**
 * Members that could exist on an unknown receiver - prototype methods plus the instance methods of
 * the standard library classes, deduplicated by name.
 */
function collectInstanceMembers(containers: readonly MondContainer[]): MondInstanceMember[] {
	const members = new Map<string, MondSymbol & { owners: string[] }>();

	for (const container of containers) {
		for (const member of container.members) {
			const existing = members.get(member.name);
			if (existing) {
				if (!existing.owners.includes(container.name)) {
					existing.owners.push(container.name);
				}
				continue;
			}

			members.set(member.name, { ...member, owners: [container.name] });
		}
	}

	return [...members.values()].sort(byName);
}

function byName(a: { name: string }, b: { name: string }): number {
	return a.name.localeCompare(b.name);
}

function isContainer(symbol: MondSymbol): symbol is MondContainer {
	return symbol.kind === 'module' || symbol.kind === 'class';
}

interface MutableSymbol {
	name: string;
	kind: MondSymbolKind;
	signatures?: string[];
	documentation?: string;
	members?: MondSymbol[];
}

function merge(target: Map<string, MutableSymbol>, entry: MutableSymbol) {
	const existing = target.get(entry.name);
	if (!existing) {
		target.set(entry.name, entry);
		return;
	}

	existing.kind = entry.kind;
	existing.signatures = entry.signatures ?? existing.signatures;
	existing.documentation = entry.documentation ?? existing.documentation;

	if (entry.members) {
		const members = new Map((existing.members ?? []).map(m => [m.name, m]));
		for (const member of entry.members) {
			members.set(member.name, member);
		}
		existing.members = [...members.values()].sort(byName);
	}
}

function freeze(symbol: MutableSymbol): MondSymbol {
	if (symbol.kind !== 'module' && symbol.kind !== 'class') {
		return { ...symbol };
	}

	// containers always expose a member list so consumers never have to check for one
	const container: MondContainer = {
		...symbol,
		kind: symbol.kind,
		members: [...(symbol.members ?? [])].sort(byName),
	};

	return container;
}

function read(file: string): { globals: MutableSymbol[]; prototypes: MutableSymbol[] } | undefined {
	let raw: string;

	try {
		raw = readFileSync(file, 'utf8');
	} catch (e) {
		// a missing built-in file means a broken install, a missing configured file means a typo -
		// both are worth telling the user about, but neither should break the other definitions
		void vscode.window.showWarningMessage(`Mond: could not read definitions from ${file}. ${errorMessage(e)}`);
		return undefined;
	}

	let parsed: unknown;

	try {
		parsed = JSON.parse(raw);
	} catch (e) {
		void vscode.window.showWarningMessage(`Mond: ${file} is not valid JSON. ${errorMessage(e)}`);
		return undefined;
	}

	if (typeof parsed !== 'object' || parsed === null) {
		return undefined;
	}

	const document = parsed as Record<string, unknown>;
	if (typeof document.version === 'number' && document.version > supportedVersion) {
		void vscode.window.showWarningMessage(
			`Mond: ${file} uses definitions format ${document.version}, but this extension only understands ${supportedVersion}.`);
		return undefined;
	}

	return {
		globals: readSymbols(document.globals, 'function'),
		prototypes: readSymbols(document.prototypes, 'class'),
	};
}

const knownKinds: readonly MondSymbolKind[] = ['keyword', 'constant', 'function', 'method', 'property', 'module', 'class'];

/** Definition files are user supplied, so nothing from them is trusted without checking. */
function readSymbols(value: unknown, defaultKind: MondSymbolKind): MutableSymbol[] {
	if (!Array.isArray(value)) {
		return [];
	}

	const result: MutableSymbol[] = [];

	for (const item of value) {
		if (typeof item !== 'object' || item === null) {
			continue;
		}

		const entry = item as Record<string, unknown>;
		if (typeof entry.name !== 'string' || entry.name.length === 0) {
			continue;
		}

		const kind = knownKinds.find(k => k === entry.kind) ?? defaultKind;
		const symbol: MutableSymbol = { name: entry.name, kind };

		if (Array.isArray(entry.signatures)) {
			const signatures = entry.signatures.filter((s): s is string => typeof s === 'string');
			if (signatures.length > 0) {
				symbol.signatures = signatures;
			}
		}

		if (typeof entry.documentation === 'string' && entry.documentation.length > 0) {
			symbol.documentation = entry.documentation;
		}

		if (Array.isArray(entry.members)) {
			symbol.members = readSymbols(entry.members, 'method');

			// only containers carry members, so anything with them has to be one
			if (symbol.kind !== 'module' && symbol.kind !== 'class') {
				symbol.kind = 'class';
			}
		}

		result.push(symbol);
	}

	return result;
}
