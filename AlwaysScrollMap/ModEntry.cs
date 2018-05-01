using Harmony;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AlwaysScrollMap
{
    public class ModEntry : Mod
    {
        public static IMonitor Log;
        public override void Entry(IModHelper helper)
        {
            Log = this.Monitor;
            var harmony = HarmonyInstance.Create("bcmpinc.AlwaysScrollMap");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(StardewValley.Game1))]
    [HarmonyPatch("UpdateViewPort")]
    public static class Game1_UpdateViewPort
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int start = codes.FindIndex(i => i.opcode == OpCodes.Ldsfld && AccessTools.Field(typeof(StardewValley.Game1), "viewportFreeze") == (FieldInfo)i.operand);
            int end = codes.FindIndex(i => i.opcode == OpCodes.Ldfld && AccessTools.Field(typeof(StardewValley.GameLocation), "forceViewportPlayerFollow") == (FieldInfo)i.operand);
            //ModEntry.Log.Log($"start={start} end={end}");
            codes.RemoveRange(start, end-start+2);
            return codes;
        }
    }
}

