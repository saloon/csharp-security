using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Threading;
using System.Xml.Serialization;

namespace SecureInstanceRunner
{
    internal class SecureInstance : MarshalByRefObject
    {
        private readonly long _maxMethodeTime;
        private String _assemblyName;
        private AppDomain _domain;
        private bool _fail;
        private ObjectHandle _handle;

        private Type _iface;
        private String _methode;

        private object[] _parameters;
        private String _path;
        private object _r;
        private String _typeName;
        private object _o;

        public SecureInstance(String path, String assemblyName, String typeName, Type iface, long maxMethodeTime)
        {
            _maxMethodeTime = maxMethodeTime;

            Construct(path, assemblyName, typeName, iface);
        }


        private void Construct(String path, String assemblyName, String typeName, Type iface)
        {
            _path = Path.GetFullPath(path);
            _assemblyName = assemblyName;
            _typeName = typeName;
            _iface = iface;


            CreateAppDomain();

            var t = new Thread(ProcessConstructor);

            t.Start();
            t.Join((int) _maxMethodeTime);

            if (t.IsAlive || _fail)
            {
                t.Abort();
                throw new SecurityException("Thread timed out");
            }


            if (_handle != null)
            {
                _o = _handle.Unwrap();
            }
        }


        public string CallMethod(String methode, object[] parameters)
        {
            _methode = methode;
            _parameters = parameters;

            var t = new Thread(ProcessMethod);

            t.Start();
            t.Join((int) _maxMethodeTime);

            if (t.IsAlive || _fail)
            {
                while (t.IsAlive)
                {
                    t.Interrupt();
                    t.Abort(); // killing spree!
                }


                throw new SecurityException("Thread timed out");
            }

            string r = "NULL";
            if (_r != null)
            {
                r = SerializeToString(_r);
            }

            return r;
        }

        private void ProcessMethod()
        {
            try
            {
                _r = _iface.InvokeMember(_methode, BindingFlags.InvokeMethod, Type.DefaultBinder, _o, _parameters);
            }
            catch (Exception)
            {
                _fail = true;
            }
        }

        private void ProcessConstructor()
        {
            try
            {
                _handle = _domain.CreateInstanceFrom(_path + @"\" + _assemblyName + @".dll", _typeName);
            }
            catch (Exception)
            {
                _fail = true;
            }
        }

        private void CreateAppDomain()
        {
            var set = new PermissionSet(PermissionState.None);
            set.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            set.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read |
                                                   FileIOPermissionAccess.PathDiscovery,
                                                   _path));

            var info = new AppDomainSetup {ApplicationBase = _path};

            // StrongName fullTrustAssembly = typeof(SecureInstance).Assembly.Evidence.GetHostEvidence<StrongName>();
            GetType().Assembly.Evidence.GetHostEvidence<StrongName>();

            //Console.WriteLine(this.GetType().Assembly.Evidence.GetHostEvidence<StrongName>());

            _domain = AppDomain.CreateDomain("Sandbox", null, info, set, null);
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