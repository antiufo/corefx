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
            var type = ex.Left.Type;
            var type2 = ex.Right.Type;
            var method = ex.Method;
            ExpressionType nodeType = ex.NodeType;
            return (nodeType == ExpressionType.AndAlso || nodeType == ExpressionType.OrElse) && TypeUtils_AreEquivalent(type2, type) && Type_IsNullableType(type.GetTypeInfo()) && method != null && TypeUtils_AreEquivalent(method.ReturnType, Type_GetNonNullableType(type.GetTypeInfo()));
        }

        public static Expression BinaryExpression_ReduceUserdefinedLifted(BinaryExpression b)
        {
            ParameterExpression parameterExpression = Expression.Parameter(b.Left.Type, "left");
            ParameterExpression parameterExpression2 = Expression.Parameter(b.Right.Type, "right");
            string name = (b.NodeType == ExpressionType.AndAlso) ? "op_False" : "op_True";
            var booleanOperator = TypeUtils_GetBooleanOperator(b.Method.DeclaringType.GetTypeInfo(), name);
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

        private static MethodInfo TypeUtils_GetBooleanOperator(TypeInfo type, string name)
        {
            MethodInfo methodValidated;
            while (true)
            {
                methodValidated = GetMethodValidated(type.AsType(), name, new Type[]
                {
                    type.AsType()
                });
                if (methodValidated != null && methodValidated.IsSpecialName && !methodValidated.ContainsGenericParameters)
                {
                    break;
                }
                type = type.BaseType.GetTypeInfo();
                if (!(type != null))
                {
                    return null;
                }
            }
            return methodValidated;
        }

        // System.Dynamic.Utils.TypeExtensions
        internal static MethodInfo GetMethodValidated(Type type, string name, Type[] types)
        {
            MethodInfo method = type.GetMethod(name, types);
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
                if (!TypeInfo_AreReferenceAssignable(parameters[i].ParameterType.GetTypeInfo(), argTypes[i].GetTypeInfo()))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool TypeInfo_AreReferenceAssignable(TypeInfo dest, TypeInfo src)
        {
            return TypeUtils_AreEquivalent(dest.AsType(), src.AsType()) || (!dest.IsValueType && !src.IsValueType && dest.IsAssignableFrom(src));
        }


        private static bool Type_IsNullableType(TypeInfo type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Type Type_GetNonNullableType(Type type)
        {
            return Type_GetNonNullableType(type.GetTypeInfo());
        }

        public static Type Type_GetNonNullableType(TypeInfo type)
        {
            if (Type_IsNullableType(type))
            {
                return type.GenericTypeArguments.First();
            }
            return type.AsType();
        }

        private static bool TypeUtils_AreEquivalent(Type t1, Type t2)
        {
            return t1 == t2;//|| t1.IsEquivalentTo(t2);
        }
    }
}
