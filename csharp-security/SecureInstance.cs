using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Security;
using System.Text;
using System.Xml.Serialization;
using Contract;

namespace csharp_security
{
    internal class SecureInstance
    {
        private const String SecureInstanceRunnerPath =
            @"..\..\..\SecureInstanceRunner\bin\Debug\SecureInstanceRunner.exe";

        private readonly long _maxMemoryUsage = 1024*1024*10; //in bytes = 10 MB
        private readonly long _maxMethodeTime = 5000;
        private String _assemblyName;

        private StreamReader _inStream;
        private StreamWriter _outStream;
        private String _path;
        private NamedPipeServerStream _pipeStream;
        private Process _secureProcess;
        private String _typeName;

        private Type _iface;

        public SecureInstance(String path, String assemblyName, String typeName, Type iface, long maxMethodeTime,
                              long maxMemoryUsage)
        {
            _maxMethodeTime = maxMethodeTime;
            _maxMemoryUsage = maxMemoryUsage;

            Construct(path, assemblyName, typeName, iface);
        }

        public SecureInstance(String path, String assemblyName, String typeName, Type iface)
        {
            Construct(path, assemblyName, typeName, iface);
        }

        public void Close()
        {
            _outStream.WriteLine("QUIT");
            _outStream.Flush();


            _pipeStream.Close();
        }

        private void Construct(String path, String assemblyName, String typeName, Type iface)
        {
            _path = path;
            _assemblyName = assemblyName;
            _typeName = typeName;
            _iface = iface;

            _secureProcess = new Process {StartInfo = {FileName = SecureInstanceRunnerPath}};

            /**
             * Generate random pipe name
             */
            var random = new Random();
            var builder = new StringBuilder();
            for (int i = 0; i < 8; i++)
            {
                char ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26*random.NextDouble() + 65)));
                builder.Append(ch);
            }


            _pipeStream = new NamedPipeServerStream(builder.ToString(), PipeDirection.InOut);

            _secureProcess.StartInfo.UseShellExecute = false;

            string[] args = {
                                builder.ToString(),
                                "\"" + _path + "\"",
                                "\"" + _assemblyName + "\"",
                                "\"" + _typeName + "\"",
                                "\"" + _iface.AssemblyQualifiedName + "\"",
                                _maxMethodeTime.ToString(CultureInfo.InvariantCulture),
                                _maxMemoryUsage.ToString(CultureInfo.InvariantCulture)
                            };
            _secureProcess.StartInfo.Arguments = String.Join(" ", args);
            _secureProcess.Start();
            _secureProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

            _pipeStream.WaitForConnection();

            _inStream = new StreamReader(_pipeStream);
            _outStream = new StreamWriter(_pipeStream) {AutoFlush = true};

            String temp;

            while ((temp = _inStream.ReadLine()) != null)
            {
                if (temp.Equals("READY"))
                {
                    Console.WriteLine("[C#-Security] Got Constructor call.");
                    break;
                }
                if (temp.Equals("FAIL") || !_pipeStream.IsConnected)
                {
                    Console.WriteLine("[C#-Security] Got a fail.");
                    throw new SecurityException();
                }
            }
        }

        public T CallMethode<T>(String methode, object[] parameters)
        {
            var call = new MethodeCall(methode, parameters, typeof (T).AssemblyQualifiedName);


            _outStream.WriteLine(SerializeToString(call));
            _outStream.WriteLine("SYNC");
            Console.WriteLine("[C#-Security] MethodeCall sent");

            //outStream.WriteLine("READY");

            string temp;
            string command = "";

            while ((temp = _inStream.ReadLine()) != null)
            {
                if (temp == "SYNC")
                {
                    break;
                }
                command = command + temp + "\n";
            }

            T r;

            if (command.Equals("NULL\n"))
            {
                r = default(T);
            }
            else if (command.Equals("FAIL\n") || !_pipeStream.IsConnected)
            {
                throw new SecurityException();
            }
            else
            {
                Console.WriteLine(command);
                r = SerializeFromString<T>(command);
            }

            return r;
        }

        public string SerializeToString(object obj)
        {
            var serializer = new XmlSerializer(obj.GetType());

            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, obj);

                return writer.ToString();
            }
        }

        public T SerializeFromString<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof (T));

            using (var reader = new StringReader(xml))
            {
                return (T) serializer.Deserialize(reader);
            }
        }
    }
}