// @ts-check

import eslint from '@eslint/js';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  {
    ignores: ['out/**', 'dist/**', 'build/**', '.vscode-test/**'],
  },
  eslint.configs.recommended,
  tseslint.configs.recommendedTypeChecked,
  {
    languageOptions: {
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
    rules: {
        '@typescript-eslint/no-unused-vars': ['error', {
            argsIgnorePattern: '^_',
            varsIgnorePattern: '^_',
            caughtErrorsIgnorePattern: '^_',
        }],
        '@typescript-eslint/no-explicit-any': 'warn',
        // DebugSession request handlers are declared as returning void but are legitimately async
        '@typescript-eslint/no-misused-promises': ['error', {
            checksVoidReturn: { inheritedMethods: false },
        }],
    },
  },
);
