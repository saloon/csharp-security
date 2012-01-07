using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using Contract;
using System.Security;
using System.Diagnostics;

namespace SecureInstanceRunner
{
    class Program
    {


        private static bool watchdog_alive = true;

        private static string path;
        private static string assemblyName;
        private static string typeName;
        private static Type iface;
        private static long maxMethodeTime;
        private static long maxMemoryUsage;

        private static SecureInstance secureInstance;

        private static StreamWriter outStream;
        private static StreamReader inStream;

        static void Main(string[] args)
        {

            if (args.Length > 6)
            {

                path = args[1];
                assemblyName = args[2];
                typeName = args[3];
                iface = Type.GetType(args[4]);
                maxMethodeTime = Int64.Parse(args[5]);
                maxMemoryUsage = Int64.Parse(args[6]);

                /*
                Console.WriteLine("path = " + path);
                Console.WriteLine("assemblyName = " + assemblyName);
                Console.WriteLine("typeName = " + typeName);
                Console.WriteLine("iface = " + iface);
                Console.WriteLine("maxMethodeTime = " + maxMethodeTime);
                Console.WriteLine("maxMemoryUsage = " + maxMemoryUsage);
                */

                using (NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", args[0], PipeDirection.InOut))
                {

                    try
                    {
                        pipeStream.Connect();

                        Thread t = new Thread(WatchDog);
                        t.Start();

                        Console.WriteLine("[SecureInstanceRunner] Pipe connection established");

                        outStream = new StreamWriter(pipeStream);
                        inStream = new StreamReader(pipeStream);
                        outStream.AutoFlush = true;


                        string msg = "READY";

                        try
                        {
                            secureInstance = new SecureInstance(path, assemblyName, typeName, iface, maxMethodeTime);
                        }
                        catch (SecurityException e)
                        {
                            msg = "FAIL";
                        }

                        CheckThreads();
                        outStream.WriteLine(msg);

                        Console.WriteLine("[SecureInstanceRunner] Constructor called. Waiting for methode calls.");

                        string temp;
                        string command = "";
                        while ((temp = inStream.ReadLine()) != null)
                        {
                            if (temp == "QUIT")
                            {
                                break;
                            }
                            else if (temp == "SYNC")
                            {
                                processCommand(command);
                                command = "";


                                Console.WriteLine("[SecureInstanceRunner] Methode called. Waiting for methode calls.");
                            }
                            else
                            {
                                command = command + temp + "\n";
                            }
                        }
                    }
                    catch (OutOfMemoryException e)
                    {
                        outStream.WriteLine("FAIL");
                    }
                    watchdog_alive = false;
                    Console.WriteLine("[SecureInstanceRunner] Bye!");
                }
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                Environment.Exit(-1);

            }
        }


        private static void processCommand(string command)
        {
            MethodeCall call = secureInstance.SerializeFromString<MethodeCall>(command);
            
            string r;
            try
            {
                r = secureInstance.CallMethod(call.GetMethode(), call.GetArguments());
            }
            catch (SecurityException e)
            {
                r = "FAIL";
            }


            CheckThreads();


            outStream.WriteLine(r);
            outStream.WriteLine("SYNC");

        }

        private static void WatchDog()
        {
            while (watchdog_alive)
            {
                //Console.WriteLine("[WatchDog] PrivateMemorySize64 = " + System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64);
                //Console.WriteLine("[WatchDog] GC.GetTotalMemory = " + System.GC.GetTotalMemory(true));

                if (System.GC.GetTotalMemory(true) > maxMemoryUsage)
                {
                    Console.WriteLine("[SecureInstanceRunner][WatchDog] Memory overflow!");
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                    Environment.Exit(-1);
                }

                Thread.Sleep(50);
            }
        }

        private static void CheckThreads()
        {
            if (System.Diagnostics.Process.GetCurrentProcess().Threads.Count > 11) 
            {
                Console.WriteLine("[SecureInstanceRunner][WatchDog] Thread overflow!");
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                Environment.Exit(-1);
            }
        }
    }
}
