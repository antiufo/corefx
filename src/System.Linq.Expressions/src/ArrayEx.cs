using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    internal static class ArrayEx
    {
  
        public static T[] Empty<T>()
        {
            return ArrayEx<T>.instance;
        }
    }
    internal static class ArrayEx<T>
    {
        internal static T[] instance = new T[0];
    }
}
