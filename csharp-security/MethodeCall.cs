using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Contract
{
    public class MethodeCall
    {
        public string methode;
        public object[] arguments;
        public string return_type;

        public MethodeCall()
        {
        }

        public MethodeCall(String methode, object[] arguments, String return_type)
        {
            this.methode = methode;
            this.arguments = arguments;
            this.return_type = return_type;
        }

        public string GetMethode() {
            return this.methode;
        }

        public object[] GetArguments()
        {
            return this.arguments;
        }

        public Type GetTheReturnType()
        {
            return Type.GetType(this.return_type);
        }
    }
}
