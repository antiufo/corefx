using System.Collections.ObjectModel;

namespace System.Linq.Expressions
{
    internal static class InterpreterCompatibilityExtensions
    {
        public static Expression GetArgument(this IndexExpression e, int index)
        {
            return e.Arguments[index];
        }
        public static Expression GetArgument(this NewExpression e, int index)
        {
            return e.Arguments[index];
        }
        public static Expression GetExpression(this BlockExpression e, int index)
        {
            return e.Expressions[index];
        }
        public static BlockExpression Rewrite(this BlockExpression e, ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
        {
            return Expression.Block(args);
        }
    }
}