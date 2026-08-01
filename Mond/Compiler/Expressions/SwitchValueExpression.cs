using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler.Expressions
{
    /// <summary>
    /// A C#-like switch expression: <c>subject switch { pattern -&gt; value, _ -&gt; value }</c>
    /// </summary>
    internal class SwitchValueExpression : Expression
    {
        public enum ArmKind
        {
            Values,
            Object,
            Array,
            Binding,
            Discard,
        }

        public class Arm
        {
            public ArmKind Kind { get; }

            /// <summary>Values compared against the subject with <c>==</c>. Only set for <see cref="ArmKind.Values"/>.</summary>
            public ReadOnlyCollection<Expression> Conditions { get; private set; }

            /// <summary>Destructuring pattern. Only set for <see cref="ArmKind.Object"/> and <see cref="ArmKind.Array"/>.</summary>
            public Expression Pattern { get; }

            /// <summary>Name the subject is bound to. Only set for <see cref="ArmKind.Binding"/>.</summary>
            public string BindingName { get; }

            public Expression Guard { get; private set; }

            public Expression Body { get; private set; }

            internal Scope Scope { get; set; }

            private Arm(ArmKind kind, List<Expression> conditions, Expression pattern, string bindingName, Expression guard, Expression body)
            {
                Kind = kind;
                Conditions = conditions?.AsReadOnly();
                Pattern = pattern;
                BindingName = bindingName;
                Guard = guard;
                Body = body;
            }

            public static Arm Values(List<Expression> conditions, Expression guard, Expression body) =>
                new Arm(ArmKind.Values, conditions, null, null, guard, body);

            public static Arm Object(DestructuredObjectExpression pattern, Expression guard, Expression body) =>
                new Arm(ArmKind.Object, null, pattern, null, guard, body);

            public static Arm Array(DestructuredArrayExpression pattern, Expression guard, Expression body) =>
                new Arm(ArmKind.Array, null, pattern, null, guard, body);

            public static Arm Binding(string name, Expression guard, Expression body) =>
                new Arm(ArmKind.Binding, null, null, name, guard, body);

            public static Arm Discard(Expression body) =>
                new Arm(ArmKind.Discard, null, null, null, null, body);

            internal static Arm Create(ArmKind kind, List<Expression> conditions, Expression pattern, string bindingName, Expression guard, Expression body) =>
                new Arm(kind, conditions, pattern, bindingName, guard, body);

            internal bool BindsIdentifiers => Kind == ArmKind.Object || Kind == ArmKind.Array || Kind == ArmKind.Binding;

            internal void Simplify(SimplifyContext context)
            {
                if (Conditions != null)
                {
                    Conditions = Conditions
                        .Select(c => c.Simplify(context))
                        .ToList()
                        .AsReadOnly();
                }

                Scope = context.PushScope();

                Pattern?.Simplify(context);

                if (BindingName != null && !context.DefineIdentifier(BindingName, true))
                    throw new MondCompilerException(Body, CompilerError.IdentifierAlreadyDefined, BindingName);

                Guard = Guard?.Simplify(context);
                Body = Body.Simplify(context);

                context.PopScope();
            }

            internal void SetParent(Expression parent)
            {
                if (Conditions != null)
                {
                    foreach (var condition in Conditions)
                    {
                        condition.SetParent(parent);
                    }
                }

                Pattern?.SetParent(parent);
                Guard?.SetParent(parent);
                Body.SetParent(parent);
            }
        }

        public Expression Subject { get; private set; }
        public ReadOnlyCollection<Arm> Arms { get; }

        public override Token StartToken => Subject.StartToken;

        public SwitchValueExpression(Token token, Expression subject, List<Arm> arms)
            : base(token)
        {
            Subject = subject;
            Arms = arms.AsReadOnly();
        }

        public override int Compile(FunctionContext context)
        {
            context.Position(Token);

            var end = context.MakeLabel("switchExprEnd");
            var discard = context.MakeLabel("switchExprDiscard");
            var discardArm = Arms[Arms.Count - 1];
            var valueArms = Arms.Take(Arms.Count - 1).ToList();

            var stack = 0;
            stack += Subject.Compile(context);

            if (CanUseJumpTable(valueArms))
            {
                stack += CompileJumpTable(context, valueArms, discard, out var armLabels);

                for (var i = 0; i < valueArms.Count; i++)
                {
                    stack += context.Bind(armLabels[i]);
                    CompileArmBody(context, valueArms[i], stack, null, end, true);
                }
            }
            else
            {
                foreach (var arm in valueArms)
                {
                    var next = context.MakeLabel("switchExprNextArm");

                    stack += CompileArmTest(context, arm, next);
                    CompileArmBody(context, arm, stack, next, end, true);
                    stack += context.Bind(next);
                }
            }

            stack += context.Bind(discard);
            CompileArmBody(context, discardArm, stack, null, end, false);

            stack += context.Bind(end);

            return 1;
        }

        private static bool CanUseJumpTable(List<Arm> arms)
        {
            return arms.Count > 0 && arms.All(a =>
                a.Kind == ArmKind.Values &&
                a.Guard == null &&
                a.Conditions.All(c => c is IConstantExpression));
        }

        private static int CompileJumpTable(FunctionContext context, List<Arm> arms, LabelOperand discardLabel, out List<LabelOperand> armLabels)
        {
            armLabels = new List<LabelOperand>(arms.Count);

            for (var i = 0; i < arms.Count; i++)
            {
                armLabels.Add(context.MakeLabel("switchExprArm_" + i));
            }

            var conditionGroups = arms
                .Select(a => (IReadOnlyList<Expression>)a.Conditions)
                .ToList();

            var flattened = SwitchJumpTable.FlattenBranches(conditionGroups, armLabels, discardLabel);
            SwitchJumpTable.BuildTables(flattened, discardLabel, out var tables, out var rest);

            var stack = 0;

            foreach (var table in tables)
            {
                var start = table.Entries[0].Value;
                var labels = table.Entries.Select(e => e.Label).ToList();

                stack += context.Dup();
                stack += context.JumpTable(start, labels);
            }

            foreach (var entry in rest)
            {
                stack += context.Dup();
                stack += entry.Condition.Compile(context);
                stack += context.BinaryOperation(TokenType.EqualTo);
                stack += context.JumpTrue(entry.Label);
            }

            stack += context.Jump(discardLabel);

            return stack;
        }

        /// <summary>
        /// Emits the pattern test and identifier bindings for an arm. Jumps to
        /// <paramref name="next"/> when the arm does not match. The subject remains on
        /// the stack either way, so this has no net stack effect.
        /// </summary>
        private static int CompileArmTest(FunctionContext context, Arm arm, LabelOperand next)
        {
            var stack = 0;

            switch (arm.Kind)
            {
                case ArmKind.Values:
                {
                    var matched = context.MakeLabel("switchExprArmMatch");

                    foreach (var condition in arm.Conditions)
                    {
                        stack += context.Dup();
                        stack += condition.Compile(context);
                        stack += context.BinaryOperation(TokenType.EqualTo);
                        stack += context.JumpTrue(matched);
                    }

                    stack += context.Jump(next);
                    stack += context.Bind(matched);
                    break;
                }

                case ArmKind.Object:
                {
                    var pattern = (DestructuredObjectExpression)arm.Pattern;

                    stack += CompileTypeTest(context, "object", next);

                    foreach (var field in pattern.Fields)
                    {
                        stack += context.Dup();
                        stack += context.Load(context.String(field.Name));
                        stack += context.Swap();
                        stack += context.BinaryOperation(TokenType.In);
                        stack += context.JumpFalse(next);
                    }

                    break;
                }

                case ArmKind.Array:
                {
                    var pattern = (DestructuredArrayExpression)arm.Pattern;
                    var hasSlice = pattern.Indices.Any(i => i.IsSlice);
                    var fixedSize = pattern.Indices.Count(i => !i.IsSlice);

                    stack += CompileTypeTest(context, "array", next);

                    stack += context.Dup();
                    stack += context.InstanceCall(context.String("length"), 0, new List<ImmediateOperand>());
                    stack += context.Load(context.Number(fixedSize));
                    stack += context.BinaryOperation(hasSlice ? TokenType.GreaterThanOrEqual : TokenType.EqualTo);
                    stack += context.JumpFalse(next);
                    break;
                }
            }

            return stack;
        }

        private static int CompileTypeTest(FunctionContext context, string typeName, LabelOperand next)
        {
            var stack = 0;

            stack += context.Dup();
            stack += context.InstanceCall(context.String("getType"), 0, new List<ImmediateOperand>());
            stack += context.Load(context.String(typeName));
            stack += context.BinaryOperation(TokenType.EqualTo);
            stack += context.JumpFalse(next);

            return stack;
        }

        /// <summary>
        /// Binds the arm's identifiers to a copy of the subject. Must be called with the
        /// arm's scope pushed. Has no net stack effect.
        /// </summary>
        private static int CompileArmBindings(FunctionContext context, Arm arm)
        {
            var stack = 0;

            switch (arm.Kind)
            {
                case ArmKind.Object:
                case ArmKind.Array:
                    stack += context.Dup();
                    stack += arm.Pattern.Compile(context); // consumes the duplicate
                    break;

                case ArmKind.Binding:
                    stack += context.Dup();
                    stack += context.Store(context.Identifier(arm.BindingName));
                    break;
            }

            return stack;
        }

        /// <summary>
        /// Emits the body of an arm. The subject is dropped and replaced with the arm's value.
        /// </summary>
        private void CompileArmBody(FunctionContext context, Arm arm, int stack, LabelOperand next, LabelOperand end, bool jumpToEnd)
        {
            context.PushScope(arm.Scope);

            stack += CompileArmBindings(context, arm);

            if (arm.Guard != null)
            {
                stack += arm.Guard.Compile(context);
                stack += context.JumpFalse(next);
            }

            var armStack = stack;
            armStack += context.Drop();
            armStack += arm.Body.Compile(context);

            if (jumpToEnd)
                armStack += context.Jump(end);

            context.PopScope();

            CheckStack(armStack, 1);
        }

        public override Expression Simplify(SimplifyContext context)
        {
            Subject = Subject.Simplify(context);

            foreach (var arm in Arms)
            {
                arm.Simplify(context);
            }

            return this;
        }

        public override void SetParent(Expression parent)
        {
            base.SetParent(parent);

            Subject.SetParent(this);

            foreach (var arm in Arms)
            {
                arm.SetParent(this);
            }
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
