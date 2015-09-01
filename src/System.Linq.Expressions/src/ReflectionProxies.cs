using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    internal static class ReflectionProxies
    {
        public static bool BinaryExpression_IsLiftedLogical(BinaryExpression ex)
        {
            Type type = ex.Left.Type;
            Type type2 = ex.Right.Type;
            var method = ex.Method;
            ExpressionType nodeType = ex.NodeType;
            return (nodeType == ExpressionType.AndAlso || nodeType == ExpressionType.OrElse) && TypeUtils_AreEquivalent(type2, type) && Type_IsNullableType(type) && method != null && TypeUtils_AreEquivalent(method.ReturnType, Type_GetNonNullableType(type));
        }

        public static Expression BinaryExpression_ReduceUserdefinedLifted(BinaryExpression b)
        {
            ParameterExpression parameterExpression = Expression.Parameter(b.Left.Type, "left");
            ParameterExpression parameterExpression2 = Expression.Parameter(b.Right.Type, "right");
            string name = (b.NodeType == ExpressionType.AndAlso) ? "op_False" : "op_True";
            var booleanOperator = TypeUtils_GetBooleanOperator(b.Method.DeclaringType, name);
            Debug.Assert(booleanOperator != null);
            return Expression.Block(new ParameterExpression[]
            {
        parameterExpression
            }, new Expression[]
            {
        Expression.Assign(parameterExpression, b.Left),
        Expression.Condition(Expression.Property(parameterExpression, "HasValue"), Expression.Condition(Expression.Call(booleanOperator, Expression.Call(parameterExpression, "GetValueOrDefault", null, ArrayEx.Empty<Expression>())), parameterExpression, Expression.Block(new ParameterExpression[]
        {
            parameterExpression2
        }, new Expression[]
        {
            Expression.Assign(parameterExpression2, b.Right),
            Expression.Condition(Expression.Property(parameterExpression2, "HasValue"), Expression.Convert(Expression.Call(b.Method, Expression.Call(parameterExpression, "GetValueOrDefault", null, ArrayEx.Empty<Expression>()), Expression.Call(parameterExpression2, "GetValueOrDefault", null, ArrayEx.Empty<Expression>())), b.Type), Expression.Constant(null, b.Type))
        })), Expression.Constant(null, b.Type))
            });
        }

        private static MethodInfo TypeUtils_GetBooleanOperator(Type type, string name)
        {
            MethodInfo methodValidated;
            while (true)
            {
                methodValidated = GetMethodValidated(type, name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[]
                {
                    type
                }, null);
                if (methodValidated != null && methodValidated.IsSpecialName && !methodValidated.ContainsGenericParameters)
                {
                    break;
                }
                type = type.BaseType;
                if (!(type != null))
                {
                    return null;
                }
            }
            return methodValidated;
        }

        // System.Dynamic.Utils.TypeExtensions
        internal static MethodInfo GetMethodValidated(Type type, string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
        {
            MethodInfo method = type.GetMethod(name, bindingAttr, binder, types, modifiers);
            if (!MethodInfo_MatchesArgumentTypes(method, types))
            {
                return null;
            }
            return method;
        }

        private static bool MethodInfo_MatchesArgumentTypes(this MethodInfo mi, Type[] argTypes)
        {
            if (mi == null || argTypes == null)
            {
                return false;
            }
            ParameterInfo[] parameters = mi.GetParameters();
            if (parameters.Length != argTypes.Length)
            {
                return false;
            }
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!TypeInfo_AreReferenceAssignable(parameters[i].ParameterType, argTypes[i]))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool TypeInfo_AreReferenceAssignable(Type dest, Type src)
        {
            return TypeUtils_AreEquivalent(dest, src) || (!dest.IsValueType && !src.IsValueType && dest.IsAssignableFrom(src));
        }


        private static bool Type_IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static Type Type_GetNonNullableType(Type type)
        {
            if (Type_IsNullableType(type))
            {
                return type.GetGenericArguments()[0];
            }
            return type;
        }

        private static bool TypeUtils_AreEquivalent(Type t1, Type t2)
        {
            return t1 == t2 || t1.IsEquivalentTo(t2);
        }
    }
}
