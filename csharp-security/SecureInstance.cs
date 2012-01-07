using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Permissions;
using System.Security;
using System.Security.Policy;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Threading;
using System.Diagnostics;
using System.IO.Pipes;
using System.Xml.Serialization;
using System.Collections.Specialized;
using Contract;

namespace csharp_security
{
    class SecureInstance
    {
        private String secureInstanceRunnerPath = @"..\..\..\SecureInstanceRunner\bin\Debug\SecureInstanceRunner.exe";
        private String path;
        private String assemblyName;
        private String typeName;

        private Type iface;
        private long maxMethodeTime = 5000;
        private long maxMemoryUsage = 1024 * 1024 * 10; //in bytes = 10 MB
        Process secureProcess;

        NamedPipeServerStream pipeStream;

        StreamReader inStream;
        StreamWriter outStream;

        public SecureInstance(String path, String assemblyName, String typeName, Type iface, long maxMethodeTime, long maxMemoryUsage)
        {
            this.maxMethodeTime = maxMethodeTime;
            this.maxMemoryUsage = maxMemoryUsage;

            this.Construct(path, assemblyName, typeName, iface);
        }
        public SecureInstance(String path, String assemblyName, String typeName, Type iface)
        {
            this.Construct(path, assemblyName, typeName, iface);
        }

        public void Close()
        {
            this.outStream.WriteLine("QUIT");
            this.outStream.Flush();


            this.pipeStream.Close();
        }

        private void Construct(String path, String assemblyName, String typeName, Type iface)
        {

            this.path = path;
            this.assemblyName = assemblyName;
            this.typeName = typeName;
            this.iface = iface;

            this.secureProcess = new Process();
            this.secureProcess.StartInfo.FileName = secureInstanceRunnerPath;

            /**
             * Generate random pipe name
             */
            Random random = new Random();
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < 8; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }


            this.pipeStream = new NamedPipeServerStream(builder.ToString(), PipeDirection.InOut);

            this.secureProcess.StartInfo.UseShellExecute = false;

            string[] args = { 
                                builder.ToString(), 
                                "\"" + this.path + "\"", 
                                "\"" + this.assemblyName + "\"", 
                                "\"" + this.typeName + "\"", 
                                "\"" + this.iface.AssemblyQualifiedName + "\"",
                                this.maxMethodeTime.ToString(), 
                                this.maxMemoryUsage.ToString() 
                            };
            this.secureProcess.StartInfo.Arguments = String.Join(" ", args);
            this.secureProcess.Start();
            this.secureProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

            this.pipeStream.WaitForConnection();

            this.inStream = new StreamReader(this.pipeStream);
            this.outStream = new StreamWriter(this.pipeStream);
            this.outStream.AutoFlush = true;

            String temp;

            while ((temp = inStream.ReadLine()) != null)
            {
                if (temp.Equals("READY"))
                {
                    Console.WriteLine("[C#-Security] Got Constructor call.");
                    break;
                }
                else if (temp.Equals("FAIL") || !this.pipeStream.IsConnected)
                {
                    Console.WriteLine("[C#-Security] Got a fail.");
                    throw new SecurityException();
                }
            }
        }

        public T CallMethode<T>(String methode, object[] parameters)
        {
            MethodeCall call = new MethodeCall(methode, parameters, typeof(T).AssemblyQualifiedName);


            this.outStream.WriteLine(this.SerializeToString(call));
            this.outStream.WriteLine("SYNC");
            Console.WriteLine("[C#-Security] MethodeCall sent");

            //outStream.WriteLine("READY");

            string temp;
            string command = "";

            while ((temp = inStream.ReadLine()) != null)
            {
                if (temp == "SYNC")
                {
                    break;
                } else {
                    command = command + temp + "\n";
                }
            }

            T r;

            if (command.Equals("NULL\n"))
            {
                r = default(T);
            }
            else if (command.Equals("FAIL\n") || !this.pipeStream.IsConnected)
            {
                throw new SecurityException();
            }
            else
            {
                Console.WriteLine(command);
                r = this.SerializeFromString<T>(command);
            }

            return r;
        }

        public string SerializeToString(object obj)
        {
            XmlSerializer serializer = new XmlSerializer(obj.GetType());

            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, obj);

                return writer.ToString();
            }
        }

        public T SerializeFromString<T>(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StringReader reader = new StringReader(xml))
            {
                return (T)serializer.Deserialize(reader);
            }
        }


    }
}
