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
using System.Xml.Serialization;

namespace SecureInstanceRunner
{
    class SecureInstance : MarshalByRefObject
    {
        private AppDomain domain;

        private String path;
        private String assemblyName;
        private String typeName;

        private String methode;
        private object[] parameters;

        private Type iface;

        private long maxMethodeTime;

        private object o;
        private ObjectHandle handle;


        private bool fail = false;
        private object r;

        public SecureInstance(String path, String assemblyName, String typeName, Type iface, long maxMethodeTime)
        {
            this.maxMethodeTime = maxMethodeTime;
  
            this.Construct(path, assemblyName, typeName, iface);
        }



        private void Construct(String path, String assemblyName, String typeName, Type iface)
        {

            this.path = Path.GetFullPath(path);
            this.assemblyName = assemblyName;
            this.typeName = typeName;
            this.iface = iface;


            this.CreateAppDomain();

            Thread t = new Thread(this.ProcessConstructor);

            t.Start();
            t.Join((int)this.maxMethodeTime);

            if (t.IsAlive || this.fail)
            {
                t.Abort();
                throw new SecurityException("Thread timed out");
            }


            if (this.handle != null)
            {
                this.o = this.handle.Unwrap();
            }
        }


        public string CallMethod(String methode, object[] parameters)
        {

            this.methode = methode;
            this.parameters = parameters;

            Thread t = new Thread(this.ProcessMethod);

            t.Start();
            t.Join((int) this.maxMethodeTime);

            if (t.IsAlive || this.fail)
            {
                while (t.IsAlive)
                {
                    t.Interrupt();
                    t.Abort(); // killing spree!
                }


                throw new SecurityException("Thread timed out");
            }

            string r = "NULL";
            if(this.r != null)
            {
                r = this.SerializeToString(this.r);
            }

            return r;
        }

        private void ProcessMethod()
        {
            try
            {
                this.r = this.iface.InvokeMember(this.methode, BindingFlags.InvokeMethod, Type.DefaultBinder, this.o, this.parameters);
            }
            catch (Exception e)
            {
                this.fail = true;
            }
        }

        private void ProcessConstructor()
        {
            try
            {
                this.handle = this.domain.CreateInstanceFrom(this.path + @"\" + this.assemblyName + @".dll", this.typeName);
            }
            catch (Exception e)
            {
                this.fail = true;
            }
        }

        private void CreateAppDomain()
        {
            PermissionSet set = new PermissionSet(PermissionState.None);
            set.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            set.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read |
                                                   FileIOPermissionAccess.PathDiscovery,
                                                   this.path));

            AppDomainSetup info = new AppDomainSetup { ApplicationBase = this.path };

            // StrongName fullTrustAssembly = typeof(SecureInstance).Assembly.Evidence.GetHostEvidence<StrongName>();
            StrongName fullTrustAssembly = this.GetType().Assembly.Evidence.GetHostEvidence<StrongName>();

            //Console.WriteLine(this.GetType().Assembly.Evidence.GetHostEvidence<StrongName>());

            this.domain = AppDomain.CreateDomain("Sandbox", null, info, set, null);


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
