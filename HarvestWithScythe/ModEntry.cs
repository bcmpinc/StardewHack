using System;
using System.Reflection.Emit;
using StardewModdingAPI;

namespace StardewHack.HarvestWithScythe
{
    public class ModEntry : Hack
    {
        // Note: the branch
        //   if (this.forageCrop)
        // refers mainly to the crop spring union.
        // Harvesting those with scythe behaves a bit odd.

        [BytecodePatch("StardewValley.TerrainFeatures.HoeDirt::performToolAction")]
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
                Instructions.Call_set(typeof(Netcode.NetInt), "Value")
            );

            // Set harvestMethod=damage after the following crop!=null check.
            HarvestMethodCheck.FindNext(
                OpCodes.Ldarg_0,
                Instructions.Call_get(typeof(StardewValley.TerrainFeatures.HoeDirt), "crop"),
                Instructions.Ldfld(typeof(StardewValley.Crop), "dead"),
                OpCodes.Call, // Netcode
                OpCodes.Brfalse_S
            ).Prepend(
                HarvestMethodCheck[0],
                HarvestMethodCheck[1],
                HarvestMethodCheck[2],
                Instructions.Ldarg_2(), // damage
                Instructions.Call_set(typeof(Netcode.NetInt), "Value")
            );
        }

        [BytecodePatch("StardewValley.Object::performToolAction")]
        void Object_performToolAction() {
            var code = BeginCode();
            code.Prepend(
                // Check if Tool is scythe.
                Instructions.Ldarg_1(),
                Instructions.Isinst(typeof(StardewValley.Tools.MeleeWeapon)),
                Instructions.Brfalse(code[0]),
                Instructions.Ldarg_1(),
                Instructions.Isinst(typeof(StardewValley.Tools.MeleeWeapon)),
                Instructions.Callvirt_get(typeof(StardewValley.Tool), "BaseName"),
                Instructions.Ldstr("Scythe"),
                Instructions.Callvirt(typeof(System.String), "Equals", typeof(string)),
                Instructions.Brfalse(code[0]),
                // Hook
                Instructions.Ldarg_0(),
                Instructions.Ldarg_1(),
                Instructions.Ldarg_2(),
                Instructions.Call(typeof(ModEntry), "ScytheForage", typeof(StardewValley.Object), typeof(StardewValley.Tool), typeof(StardewValley.GameLocation)),
                Instructions.Ret()
            );
        }

        public static bool ScytheForage(StardewValley.Object o, StardewValley.Tool t, StardewValley.GameLocation loc) {
            if (o.isSpawnedObject && !o.questItem && o.isForage(loc)) {
                var who = t.getLastFarmerToUse();
                var vector = o.TileLocation; 
                int quality = o.quality;
                Random random = new Random((int)StardewValley.Game1.uniqueIDForThisGame / 2 + (int)StardewValley.Game1.stats.DaysPlayed + (int)vector.X + (int)vector.Y * 777);
                if (who.professions.Contains(16)) {
                    quality = 4;
                } else if (random.NextDouble() < (double)((float)who.ForagingLevel / 30)) {
                    quality = 2;
                } else if (random.NextDouble() < (double)((float)who.ForagingLevel / 15)) {
                    quality = 1;
                }
                who.gainExperience(2, 7);
                StardewValley.Game1.createObjectDebris(o.ParentSheetIndex, (int)vector.X, (int)vector.Y, -1, quality, 1, loc);
                StardewValley.Game1.stats.ItemsForaged += 1;
                if (who.professions.Contains(13) && random.NextDouble() < 0.2) {
                    StardewValley.Game1.createObjectDebris(o.ParentSheetIndex, (int)vector.X, (int)vector.Y, -1, quality, 1, loc);
                    who.gainExperience(2, 7);
                }
                return true;
            }
            return false;
        }
    }
}

