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
    }
}