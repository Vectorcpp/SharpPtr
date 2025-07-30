using System;

using SharpPtr.Core;

namespace SharpPtr.Main
{
    class Program
    {
        /*
        static void Main()
        {
            Console.WriteLine("=== Testing PtrNode System ===");

            PtrNode.Register("myFunc", PtrKind.Custom, new MethodCall("obj", "myFunc"), "custom function");
            PtrNode.AddTag("myFunc", "utility");

            PtrNode.Register("coolCast", PtrKind.Custom, new SafeCast("int", "string"), "converts int to string");
            PtrNode.AddTag("coolCast", "conversion");

            Console.WriteLine("\nCustom stuff added!");
            PtrNode.ListAll();

            Console.WriteLine("\n=== Testing Parser ===");

            var compiler = new Compiler();

            Console.WriteLine("\nTesting method call:");
            compiler.Run("player->jump()");

            Console.WriteLine("\nTesting safe cast:");
            compiler.Run("num-?str");

            Console.WriteLine("\nTesting variable:");
            compiler.Run("health = 100");

            Console.WriteLine("\n=== Final Registry State ===");
            PtrNode.ListAll();
        }
        */

    }
}