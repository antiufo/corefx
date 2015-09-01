using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace InterpreterSandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            var k = new System.Linq.Expressions.Interpreter.LightCompiler();

            var par = Expression.Parameter(typeof(int), "x");
            var ex = Expression.Lambda(Expression.Add(par, Expression.Constant(5)), par);
            var zz = k.CompileTop(ex);

            Console.WriteLine(zz.CreateDelegate().DynamicInvoke(4));
            
            
        }
    }
}
