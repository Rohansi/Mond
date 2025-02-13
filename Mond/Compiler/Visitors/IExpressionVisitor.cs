using Mond.Compiler.Expressions;
using Mond.Compiler.Expressions.Statements;

namespace Mond.Compiler
{
    internal interface IExpressionVisitor<out T>
    {
        T Visit(BreakExpression expression);
        T Visit(ContinueExpression expression);
        T Visit(DebuggerExpression expression);
        T Visit(DoWhileExpression expression);
        T Visit(ForeachExpression expression);
        T Visit(ForExpression expression);
        T Visit(FunctionExpression expression);
        T Visit(IfExpression expression);
        T Visit(ReturnExpression expression);
        T Visit(SequenceExpression expression);
        T Visit(SwitchExpression expression);
        T Visit(VarExpression expression);
        T Visit(WhileExpression expression);
        T Visit(YieldExpression expression);

        T Visit(ArrayExpression expression);
        T Visit(BinaryOperatorExpression expression);
        T Visit(BlockExpression expression);
        T Visit(BoolExpression expression);
        T Visit(CallExpression expression);
        T Visit(EmptyExpression expression);
        T Visit(FieldExpression expression);
        T Visit(GlobalExpression expression);
        T Visit(IdentifierExpression expression);
        T Visit(IndexerExpression expression);
        T Visit(NullExpression expression);
        T Visit(NumberExpression expression);
        T Visit(ObjectExpression expression);
        T Visit(PipelineExpression expression);
        T Visit(PostfixOperatorExpression expression);
        T Visit(PrefixOperatorExpression expression);
        T Visit(ScopeExpression expression);
        T Visit(SliceExpression expression);
        T Visit(StringExpression expression);
        T Visit(TernaryExpression expression);
        T Visit(UndefinedExpression expression);
        T Visit(UnpackExpression expression);
        T Visit(DestructuredObjectExpression expression);
        T Visit(DestructuredArrayExpression expression);
        T Visit(ExportExpression expression);
        T Visit(ImportExpression expression);
        T Visit(ExportAllExpression expression);
    }
}
