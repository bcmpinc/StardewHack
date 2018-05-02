using System;
using StardewHack;
using System.Reflection.Emit;
using System.Reflection;

namespace HarvestWithScythe
{
    public class ModEntry : Hack
    {
        [BytecodePatch(typeof(StardewValley.TerrainFeatures.HoeDirt), "performToolAction")]
        void HoeDirt_performToolAction() {
            // Find the first harvestMethod==1 check.
            var HarvestMethodCheck = FindCode(
                OpCodes.Ldarg_0,
                Instructions.Call_get(typeof(StardewValley.TerrainFeatures.HoeDirt), "crop"),
                Instructions.Ldfld(typeof(StardewValley.Crop), "harvestMethod"),
                OpCodes.Call, // Netcode
                OpCodes.Ldc_I4_1,
                OpCodes.Bne_Un_S
            );

            // Change the harvestMethod==1 check to damage=harvestMethod; harvestMethod=1
            HarvestMethodCheck.Replace(
                // damage = crop.harvestMethod.
                HarvestMethodCheck[0],
                HarvestMethodCheck[1],
                HarvestMethodCheck[2],
                HarvestMethodCheck[3],
                Instructions.Starg_S(2), // damage

                // crop.harvestMethod = 1
                HarvestMethodCheck[0],
                HarvestMethodCheck[1],
                HarvestMethodCheck[2],
                Instructions.Ldc_I4_1(),
                Instructions.Call_set(typeof(Netcode.NetFieldBase<int, Netcode.NetInt>), "Value")
            );

            // Set harvestMethod=damage after the following crop!=null check.
            HarvestMethodCheck.FindNext(
                OpCodes.Ldarg_0,
                Instructions.Call_get(typeof(StardewValley.TerrainFeatures.HoeDirt), "crop"),
                Instructions.Ldfld(typeof(StardewValley.Crop), "dead"),
                OpCodes.Call, // Netcode
                OpCodes.Brfalse_S
            ).Append(
                HarvestMethodCheck[0],
                HarvestMethodCheck[1],
                HarvestMethodCheck[2],
                Instructions.Ldarg_2(),
                Instructions.Call_set(typeof(Netcode.NetFieldBase<int, Netcode.NetInt>), "Value")
            );
        }

    }
}

