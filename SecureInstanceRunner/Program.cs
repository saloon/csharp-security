using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security;
using System.Threading;
using Contract;

namespace SecureInstanceRunner
{
    internal class Program
    {
        private static bool _watchdogAlive = true;

        private static string _path;
        private static string _assemblyName;
        private static string _typeName;
        private static Type _iface;
        private static long _maxMethodeTime;
        private static long _maxMemoryUsage;

        private static SecureInstance _secureInstance;

        private static StreamWriter _outStream;
        private static StreamReader _inStream;

        private static void Main(string[] args)
        {
            if (args.Length > 6)
            {
                _path = args[1];
                _assemblyName = args[2];
                _typeName = args[3];
                _iface = Type.GetType(args[4]);
                _maxMethodeTime = Int64.Parse(args[5]);
                _maxMemoryUsage = Int64.Parse(args[6]);

                /*
                Console.WriteLine("path = " + path);
                Console.WriteLine("assemblyName = " + assemblyName);
                Console.WriteLine("typeName = " + typeName);
                Console.WriteLine("iface = " + iface);
                Console.WriteLine("maxMethodeTime = " + maxMethodeTime);
                Console.WriteLine("maxMemoryUsage = " + maxMemoryUsage);
                */

                using (var pipeStream = new NamedPipeClientStream(".", args[0], PipeDirection.InOut))
                {
                    try
                    {
                        pipeStream.Connect();

                        var t = new Thread(WatchDog);
                        t.Start();

                        Console.WriteLine("[SecureInstanceRunner] Pipe connection established");

                        _outStream = new StreamWriter(pipeStream);
                        _inStream = new StreamReader(pipeStream);
                        _outStream.AutoFlush = true;


                        string msg = "READY";

                        try
                        {
                            _secureInstance = new SecureInstance(_path, _assemblyName, _typeName, _iface,
                                                                 _maxMethodeTime);
                        }
                        catch (SecurityException)
                        {
                            msg = "FAIL";
                        }

                        CheckThreads();
                        _outStream.WriteLine(msg);

                        Console.WriteLine("[SecureInstanceRunner] Constructor called. Waiting for methode calls.");

                        string temp;
                        string command = "";
                        while ((temp = _inStream.ReadLine()) != null)
                        {
                            if (temp == "QUIT")
                            {
                                break;
                            }
                            if (temp == "SYNC")
                            {
                                ProcessCommand(command);
                                command = "";


                                Console.WriteLine("[SecureInstanceRunner] Methode called. Waiting for methode calls.");
                            }
                            else
                            {
                                command = command + temp + "\n";
                            }
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                        _outStream.WriteLine("FAIL");
                    }
                    _watchdogAlive = false;
                    Console.WriteLine("[SecureInstanceRunner] Bye!");
                }
                Process.GetCurrentProcess().Kill();
                Environment.Exit(-1);
            }
        }


        private static void ProcessCommand(string command)
        {
            var call = _secureInstance.SerializeFromString<MethodeCall>(command);

            string r;
            try
            {
                r = _secureInstance.CallMethod(call.GetMethode(), call.GetArguments());
            }
            catch (SecurityException)
            {
                r = "FAIL";
            }


            CheckThreads();


            _outStream.WriteLine(r);
            _outStream.WriteLine("SYNC");
        }

        private static void WatchDog()
        {
            while (_watchdogAlive)
            {
                //Console.WriteLine("[WatchDog] PrivateMemorySize64 = " + System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64);
                //Console.WriteLine("[WatchDog] GC.GetTotalMemory = " + System.GC.GetTotalMemory(true));

                if (GC.GetTotalMemory(true) > _maxMemoryUsage)
                {
                    Console.WriteLine("[SecureInstanceRunner][WatchDog] Memory overflow!");
                    Process.GetCurrentProcess().Kill();
                    Environment.Exit(-1);
                }

                Thread.Sleep(50);
            }
        }

        private static void CheckThreads()
        {
            if (Process.GetCurrentProcess().Threads.Count > 11)
            {
                Console.WriteLine("[SecureInstanceRunner][WatchDog] Thread overflow!");
                Process.GetCurrentProcess().Kill();
                Environment.Exit(-1);
            }
        }
    }
}