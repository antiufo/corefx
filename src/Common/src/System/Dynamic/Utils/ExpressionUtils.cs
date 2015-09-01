// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Dynamic.Utils
{
    internal static class ExpressionUtils
    {
        public static ReadOnlyCollection<T> ReturnReadOnly<T>(ref IList<T> collection)
        {
            IList<T> value = collection;

            // if it's already read-only just return it.
            ReadOnlyCollection<T> res = value as ReadOnlyCollection<T>;
            if (res != null)
            {
                return res;
            }

            // otherwise make sure only readonly collection every gets exposed
            Interlocked.CompareExchange<IList<T>>(
                ref collection,
                value.ToReadOnly(),
                value
            );

            // and return it
            return (ReadOnlyCollection<T>)collection;
        }

#if false
        /// <summary>
        /// Helper used for ensuring we only return 1 instance of a ReadOnlyCollection of T.
        /// 
        /// This is similar to the ReturnReadOnly of T. This version supports nodes which hold 
        /// onto multiple Expressions where one is typed to object.  That object field holds either
        /// an expression or a ReadOnlyCollection of Expressions.  When it holds a ReadOnlyCollection
        /// the IList which backs it is a ListArgumentProvider which uses the Expression which
        /// implements IArgumentProvider to get 2nd and additional values.  The ListArgumentProvider 
        /// continues to hold onto the 1st expression.  
        /// 
        /// This enables users to get the ReadOnlyCollection w/o it consuming more memory than if 
        /// it was just an array.  Meanwhile The DLR internally avoids accessing  which would force 
        /// the readonly collection to be created resulting in a typical memory savings.
        /// </summary>
        public static ReadOnlyCollection<Expression> ReturnReadOnly(IArgumentProvider provider, ref object collection)
        {
            Expression tObj = collection as Expression;
            if (tObj != null)
            {
                // otherwise make sure only one readonly collection ever gets exposed
                Interlocked.CompareExchange(
                    ref collection,
                    new ReadOnlyCollection<Expression>(new ListArgumentProvider(provider, tObj)),
                    tObj
                );
            }

            // and return what is not guaranteed to be a readonly collection
            return (ReadOnlyCollection<Expression>)collection;
        }

#endif
        /// <summary>
        /// Helper which is used for specialized subtypes which use ReturnReadOnly(ref object, ...). 
        /// This is the reverse version of ReturnReadOnly which takes an IArgumentProvider.
        /// 
        /// This is used to return the 1st argument.  The 1st argument is typed as object and either
        /// contains a ReadOnlyCollection or the Expression.  We check for the Expression and if it's
        /// present we return that, otherwise we return the 1st element of the ReadOnlyCollection.
        /// </summary>
        public static T ReturnObject<T>(object collectionOrT) where T : class
        {
            T t = collectionOrT as T;
            if (t != null)
            {
                return t;
            }

            return ((ReadOnlyCollection<T>)collectionOrT)[0];
        }

        public static void ValidateArgumentTypes(MethodBase method, ExpressionType nodeKind, ref ReadOnlyCollection<Expression> arguments)
        {
            Debug.Assert(nodeKind == ExpressionType.Invoke || nodeKind == ExpressionType.Call || nodeKind == ExpressionType.Dynamic || nodeKind == ExpressionType.New);

            ParameterInfo[] pis = GetParametersForValidation(method, nodeKind);

            ValidateArgumentCount(method, nodeKind, arguments.Count, pis);

            Expression[] newArgs = null;
            for (int i = 0, n = pis.Length; i < n; i++)
            {
                Expression arg = arguments[i];
                ParameterInfo pi = pis[i];
                arg = ValidateOneArgument(method, nodeKind, arg, pi);

                if (newArgs == null && arg != arguments[i])
                {
                    newArgs = new Expression[arguments.Count];
                    for (int j = 0; j < i; j++)
                    {
                        newArgs[j] = arguments[j];
                    }
                }
                if (newArgs != null)
                {
                    newArgs[i] = arg;
                }
            }
            if (newArgs != null)
            {
                arguments = new TrueReadOnlyCollection<Expression>(newArgs);
            }
        }

        public static void ValidateArgumentCount(MethodBase method, ExpressionType nodeKind, int count, ParameterInfo[] pis)
        {
            if (pis.Length != count)
            {
                // Throw the right error for the node we were given
                switch (nodeKind)
                {
                    case ExpressionType.New:
                        throw Error.IncorrectNumberOfConstructorArguments();
                    case ExpressionType.Invoke:
                        throw Error.IncorrectNumberOfLambdaArguments();
                    case ExpressionType.Dynamic:
                    case ExpressionType.Call:
                        throw Error.IncorrectNumberOfMethodCallArguments(method);
                    default:
                        throw ContractUtils.Unreachable;
                }
            }
        }

        public static Expression ValidateOneArgument(MethodBase method, ExpressionType nodeKind, Expression arg, ParameterInfo pi)
        {
            RequiresCanRead(arg, "arguments");
            Type pType = pi.ParameterType;
            if (pType.IsByRef)
            {
                pType = pType.GetElementType();
            }
            TypeUtils.ValidateType(pType);
            if (!TypeUtils.AreReferenceAssignable(pType, arg.Type))
            {
                if (!TryQuote(pType, ref arg))
                {
                    // Throw the right error for the node we were given
                    switch (nodeKind)
                    {
                        case ExpressionType.New:
                            throw Error.ExpressionTypeDoesNotMatchConstructorParameter(arg.Type, pType);
                        case ExpressionType.Invoke:
                            throw Error.ExpressionTypeDoesNotMatchParameter(arg.Type, pType);
                        case ExpressionType.Dynamic:
                        case ExpressionType.Call:
                            throw Error.ExpressionTypeDoesNotMatchMethodParameter(arg.Type, pType, method);
                        default:
                            throw ContractUtils.Unreachable;
                    }
                }
            }
            return arg;
        }

        public static void RequiresCanRead(Expression expression, string paramName)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(paramName);
            }

            // validate that we can read the node
            switch (expression.NodeType)
            {
                case ExpressionType.Index:
                    IndexExpression index = (IndexExpression)expression;
                    if (index.Indexer != null && !index.Indexer.CanRead)
                    {
                        throw new ArgumentException(Strings.ExpressionMustBeReadable, paramName);
                    }
                    break;
                case ExpressionType.MemberAccess:
                    MemberExpression member = (MemberExpression)expression;
                    PropertyInfo prop = member.Member as PropertyInfo;
                    if (prop != null)
                    {
                        if (!prop.CanRead)
                        {
                            throw new ArgumentException(Strings.ExpressionMustBeReadable, paramName);
                        }
                    }
                    break;
            }
        }

        // Attempts to auto-quote the expression tree. Returns true if it succeeded, false otherwise.
        public static bool TryQuote(Type parameterType, ref Expression argument)
        {
            // We used to allow quoting of any expression, but the behavior of
            // quote (produce a new tree closed over parameter values), only
            // works consistently for lambdas
            Type quoteable = typeof(LambdaExpression);

            if (TypeUtils.IsSameOrSubclass(quoteable, parameterType) &&
                parameterType.IsAssignableFrom(argument.GetType()))
            {
                argument = Expression.Quote(argument);
                return true;
            }
            return false;
        }

        internal static ParameterInfo[] GetParametersForValidation(MethodBase method, ExpressionType nodeKind)
        {
            ParameterInfo[] pis = method.GetParametersCached();

            if (nodeKind == ExpressionType.Dynamic)
            {
                pis = pis.RemoveFirst(); // ignore CallSite argument
            }
            return pis;
        }
    }
}
