using System;
using Harmony;
using System.Reflection;

namespace StardewHack
{
    /** Indicates that this is a transpiler for the given method.
     * Can be used multiple times to patch multiple methods.
     */
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]  
    public class BytecodePatch : System.Attribute  
    {
        Type type;
        string name;
        Type[] parameters;

        public BytecodePatch(Type type, string name, Type[] parameters = null)
        {
            this.type = type;
            this.name = name;
            this.parameters = parameters;
        }

        public MethodInfo GetMethod() 
        {
            var method = AccessTools.Method(type, name, parameters);
            if (method == null) {
                if (parameters == null) {
                    throw new Exception("Failed to find method {type}.{name}.");
                } else {
                    string pars = String.Join(",", (object[])parameters);
                    throw new Exception($"Failed to find method {type}.{name}({pars})");
                }
            }
            return method;
        }
    }
}

