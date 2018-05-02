using Harmony;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace StardewHack.CraftCounter
{
    public class ModEntry : Hack
    {
        [BytecodePatch(typeof(StardewValley.CraftingRecipe),"getDescriptionHeight")]
        [BytecodePatch(typeof(StardewValley.CraftingRecipe),"drawRecipeDescription")]
        public void AddTimesCrafted() {
            var range = FindCode(
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(StardewValley.CraftingRecipe), "description")
            );
            range.Append(
                Instructions.Ldstr("\n\nTimes Crafted: "),
                Instructions.Ldarg_0(),
                Instructions.Ldfld(typeof(StardewValley.CraftingRecipe),"timesCrafted"),
                Instructions.Call(typeof(Convert), "ToString", typeof(int)),
                Instructions.Call(typeof(string), "Concat", typeof(String), typeof(String), typeof(String))
            );
        }
    }
}

