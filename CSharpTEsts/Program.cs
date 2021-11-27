using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CSharpTEsts {
    public delegate int ContextValueDelegate(int val);

    public static class State {
        public static int Value = 0;
        public static int Eval(this ContextValueDelegate d) => d(State.Value);
    }

    class Program
    {
        public static void Log(string str) => Console.WriteLine(str);
        static void Main(string[] args)
        {
            Console.Read();
            ContextValueDelegate a = floor => floor / 2;
        }

    }
}
