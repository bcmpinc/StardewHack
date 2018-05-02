using Harmony;
using System;
using System.Reflection.Emit;

namespace StardewHack
{
    public static class Instructions
    {
        public static CodeInstruction Ldsfld(Type type, string field) {
            return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(type, field));
        }
        public static CodeInstruction Ldfld(Type type, string field) {
            return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(type, field));
        }
        public static CodeInstruction Ldstr(string text) {
            return new CodeInstruction(OpCodes.Ldstr, text);
        }
        public static CodeInstruction Ldarg_0() {
            return new CodeInstruction(OpCodes.Ldarg_0);
        }
        public static CodeInstruction Call(Type type, string method, params Type[] parameters) {
            return new CodeInstruction(OpCodes.Call, AccessTools.Method(type, method, parameters));
        }
    }
}

