﻿using System;
using System.Reflection;

namespace UnderstandingGenericReflection
{
    public class SomeFunctions
    {
        public static int SimpleInt(int a)
        {
            return a + 1;
        }

        public static int SimpleIntGeneric<T>(T a)
        {
            return 100;
        }
    }

    class Program
    {
        public static void TryStuff(MethodInfo methodInfo)
        {
            Console.WriteLine("Name: " + methodInfo.Name);
            Console.WriteLine("ContainsGenerics: " + methodInfo.ContainsGenericParameters);
            methodInfo.Invoke(null, new object[] { 1 });
        }

        static void Main(string[] args)
        {
            var simpleInt = typeof(SomeFunctions).GetMethod("SimpleInt");
            var simpleIntGeneric = typeof(SomeFunctions).GetMethod("SimpleIntGeneric");

            TryStuff(simpleInt);
            TryStuff(simpleIntGeneric);

            Console.ReadLine();
        }
    }
}
