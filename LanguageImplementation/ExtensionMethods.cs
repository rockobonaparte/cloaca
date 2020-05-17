using System.Reflection;
using System.Runtime.CompilerServices;

namespace CloacaInterpreter
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Determine if a MethodBase is an extension method.
        /// 
        /// Note that it's only coincidental--if not ironic--that this call is itself an extension method!
        /// </summary>
        /// <param name="methodBase">MethodBase to test to see if it's an extension method.</param>
        public static bool IsExtensionMethod(this MethodBase methodBase)
        {
            return methodBase.IsDefined(typeof(ExtensionAttribute));
        }
    }
}
