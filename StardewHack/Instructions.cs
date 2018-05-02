using Harmony;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace StardewHack
{
    public static class Instructions
    {
        // B
        public static CodeInstruction Brfalse(CodeInstruction target) => new CodeInstruction(OpCodes.Brfalse, Hack.attachLabel(target));
        public static CodeInstruction Brtrue (CodeInstruction target) => new CodeInstruction(OpCodes.Brtrue,  Hack.attachLabel(target));

        // C
        public static CodeInstruction Call    (Type type, string method, params Type[] parameters) => new CodeInstruction(OpCodes.Call,     GetMethod(type, method, parameters));
        public static CodeInstruction Callvirt(Type type, string method, params Type[] parameters) => new CodeInstruction(OpCodes.Callvirt, GetMethod(type, method, parameters));

        // L
        public static CodeInstruction Ldarg_0() => new CodeInstruction(OpCodes.Ldarg_0);
        public static CodeInstruction Ldarg_1() => new CodeInstruction(OpCodes.Ldarg_1);
        public static CodeInstruction Ldarg_2() => new CodeInstruction(OpCodes.Ldarg_2);
        public static CodeInstruction Ldarg_3() => new CodeInstruction(OpCodes.Ldarg_3);
        public static CodeInstruction Ldarg_S(byte index) => new CodeInstruction(OpCodes.Ldarg_S, index);
        public static CodeInstruction Ldfld (Type type, string field) => new CodeInstruction(OpCodes.Ldfld,  GetField(type, field));
        public static CodeInstruction Ldsfld(Type type, string field) => new CodeInstruction(OpCodes.Ldsfld, GetField(type, field));
        public static CodeInstruction Ldstr(string text) => new CodeInstruction(OpCodes.Ldstr, text);

        // N
        public static CodeInstruction Nop() => new CodeInstruction(OpCodes.Nop);

        // R
        public static CodeInstruction Ret() => new CodeInstruction(OpCodes.Ret);


        /** Retrieves the field definition with the specified name. */
        internal static FieldInfo GetField(Type type, string field) {
            var res = AccessTools.Field(type, field);
            if (res == null) {
                throw new MissingFieldException($"ERROR: field {type}.{field} not found.");
            }
            return res;
        }

        /** Retrieves the property definition with the specified name. */
        internal static PropertyInfo GetProperty(Type type, string property) {
            var res = AccessTools.Property(type, property);
            if (res == null) {
                throw new MissingMemberException($"ERROR: property {type}.{property} not found.");
            }
            return res;
        }

        /** Retrieves the type definition with the specified name. */
        internal static MethodInfo GetMethod(Type type, string method, Type[] parameters) {
            var res = AccessTools.Method(type, method, parameters);
            if (res == null) {
                string args = String.Join(",", (object[])parameters);
                throw new MissingMemberException($"ERROR: member {type}.{method}({args}) not found.");
            }
            return res;
        }
    }
}

