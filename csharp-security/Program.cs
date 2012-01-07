using System;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Diagnostics;
using System.IO.Pipes;
using System.IO;
using System.Xml.Serialization;
using System.Security.Permissions;

namespace csharp_security
{
    class Program
    {
        static void Main(string[] args)
        {
            SecureInstance si = new SecureInstance(@"..\..\..\Evil\bin\Debug",
                                                        "Evil",
                                                        "Evil.MyCode",
                                                        typeof(Contract.ITest));

            si.CallMethode<object>("MyMethode", null);

            si.Close();


            Console.WriteLine("[C#-Security] You're done.");
            Console.ReadKey();

        }
        
    }
}
