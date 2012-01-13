using System;
using Contract;

namespace csharp_security
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var si = new SecureInstance(@"..\..\..\Evil\bin\Debug",
                                        "Evil",
                                        "Evil.MyCode",
                                        typeof (ITest));

            si.CallMethode<object>("MyMethode", null);

            si.Close();


            Console.WriteLine("[C#-Security] You're done.");
            Console.ReadKey();
        }
    }
}