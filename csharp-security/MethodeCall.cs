using System;

namespace Contract
{
    public class MethodeCall
    {
        public object[] Arguments;
        public string Methode;
        public string ReturnType;

        public MethodeCall()
        {
        }

        public MethodeCall(String methode, object[] arguments, String returnType)
        {
            Methode = methode;
            Arguments = arguments;
            ReturnType = returnType;
        }

        public string GetMethode()
        {
            return Methode;
        }

        public object[] GetArguments()
        {
            return Arguments;
        }

        public Type GetTheReturnType()
        {
            return Type.GetType(ReturnType);
        }
    }
}