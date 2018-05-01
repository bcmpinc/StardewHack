using Harmony;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CraftCounter
{
    public class ModEntry : Mod
    {
        public static IMonitor Log;
        public override void Entry(IModHelper helper)
        {
            Log = this.Monitor;
            var harmony = HarmonyInstance.Create("bcmpinc.CraftCounter");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static IEnumerable<CodeInstruction> InjectInstructions(IEnumerable<CodeInstruction> instructions) {
            var codes = new List<CodeInstruction>(instructions);
            int pos = codes.FindIndex(i => i.opcode == OpCodes.Ldfld && AccessTools.Field(typeof(StardewValley.CraftingRecipe), "description") == (FieldInfo)i.operand);
            //Log.Log($"pos={pos}");
            codes.InsertRange(pos + 1, new CodeInstruction[] 
                {
                    new CodeInstruction(OpCodes.Ldstr, "\n\nTimes Crafted: "),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StardewValley.CraftingRecipe),"timesCrafted")),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Convert), "ToString", new Type[]{typeof(int)})),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), "Concat", new Type[]{typeof(String), typeof(String), typeof(String)}))
                }
            );
            return codes;
        }
    }

    [HarmonyPatch(typeof(StardewValley.CraftingRecipe))]
    [HarmonyPatch("getDescriptionHeight")]
    public static class CraftingRecipe_getDescriptionHeight
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ModEntry.InjectInstructions(instructions);
        }
    }

    [HarmonyPatch(typeof(StardewValley.CraftingRecipe))]
    [HarmonyPatch("drawRecipeDescription")]
    public static class CraftingRecipe_drawRecipeDescription
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ModEntry.InjectInstructions(instructions);
        }
    }
}

