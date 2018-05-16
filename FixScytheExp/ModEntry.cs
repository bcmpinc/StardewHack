using System;
using System.Reflection.Emit;

namespace StardewHack.FixScytheExp
{
    public class ModEntry : Hack<ModEntry>
    {
        // Note: in StardewValley.Crop.harvest(...) method, the branch
        //   if (this.harvestMethod == 1) {
        // does not contain code to reward Exp points.

        [BytecodePatch("StardewValley.Crop::harvest")]
        void Crop_harvest() {
            // Find tail of harvestMethod==1 branch
            var ScytheBranchTail = FindCode(
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(StardewValley.Crop), "harvestMethod"),
                OpCodes.Call, // Netcode
                OpCodes.Ldc_I4_1,
                OpCodes.Bne_Un
            ).Follow(4);
            ScytheBranchTail.ExtendBackwards(
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(StardewValley.Crop), "regrowAfterHarvest"),
                OpCodes.Call, // Netcode
                OpCodes.Ldc_I4_M1,
                OpCodes.Bne_Un_S
            );
            // Monitor.Log(ScytheBranchTail.ToString());
            if (ScytheBranchTail.length > 30) throw new Exception("Too many operations in tail of harvestMethod branch");

            // Find the start of the 'drop sunflower seeds' part.
            var DropSunflowerSeeds = FindCode(
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(StardewValley.Crop), "indexOfHarvest"),
                OpCodes.Call, // Netcode
                Instructions.Ldc_I4(421), // 421 = Item ID of Sunflower.
                OpCodes.Bne_Un_S
            );

            // Find the local variable that stores the amount being dropped.
            var DropAmount = DropSunflowerSeeds.FindNext(
                Instructions.Callvirt(typeof(System.Random), "Next", typeof(int), typeof(int)),
                OpCodes.Stloc_S
            );
            // Rewrite the tail of the Scythe harvest branch. 
            ScytheBranchTail.Replace(
                // Set num2 = 0.
                Instructions.Ldc_I4_0(),
                Instructions.Stloc_S((LocalBuilder)DropAmount[1].operand),
                // Jump to the 'drop subflower seeds' part.
                Instructions.Br(AttachLabel(DropSunflowerSeeds[0]))
            );
        }
    }
}

