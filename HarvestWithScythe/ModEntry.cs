using System;
using System.Reflection.Emit;
using StardewModdingAPI;

namespace StardewHack.HarvestWithScythe
{
    public class ModConfig {
        /** Should the game be patched to allow harvesting forage with the scythe? */
        public bool HarvestForage = true;
    }

    public class ModEntry : HackWithConfig<ModEntry, ModConfig>
    {
        [BytecodePatch("StardewValley.Crop::harvest")]
        void Crop_harvest_onions() {
            // Find the line:
            //   if (Game1.player.addItemToInventoryBool (@object, false)) {
            var AddItem = FindCode(
                Instructions.Call_get(typeof(StardewValley.Game1), "player"),
                OpCodes.Ldloc_0,
                OpCodes.Ldc_I4_0,
                Instructions.Callvirt(typeof(StardewValley.Farmer), "addItemToInventoryBool", typeof(StardewValley.Item), typeof(bool)),
                OpCodes.Brfalse
            );
            // Insert check for harvesting with scythe and act accordingly.
            AddItem.Prepend(
                // if (this.harvestMethod != 0) {
                Instructions.Ldarg_0(),
                Instructions.Ldfld(typeof(StardewValley.Crop), "harvestMethod"),
                Instructions.Brfalse(AttachLabel(AddItem[0])),
                // Game1.createObjectDebris (this.indexOfHarvest, xTile, yTile, -1, @object.Quality, 1, null);
                Instructions.Ldarg_0(),
                Instructions.Ldfld(typeof(StardewValley.Crop), "indexOfHarvest"),
                Instructions.Call_get(typeof(Netcode.NetInt), "Value"), // this.indexOfHarvest
                Instructions.Ldarg_1(), // xTile
                Instructions.Ldarg_2(), // yTile
                Instructions.Ldc_I4_M1(), // -1
                Instructions.Ldloc_0(),
                Instructions.Call_get(typeof(StardewValley.Object), "Quality"), // @object.Quality
                Instructions.Ldc_I4_1(), // 1
                Instructions.Ldc_I4_0(), // null
                Instructions.Call(typeof(StardewValley.Game1), "createObjectDebris", typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(StardewValley.GameLocation)),
                // Game1.player.gainExperience (2, howMuch);
                Instructions.Call_get(typeof(StardewValley.Game1), "player"),
                Instructions.Ldc_I4_2(),
                Instructions.Ldloc_1(),
                Instructions.Callvirt(typeof(StardewValley.Farmer), "gainExperience", typeof(int), typeof(int)),
                // return true
                Instructions.Ldc_I4_1(),
                Instructions.Ret()
                // }
            );
        }

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

        public bool HarvestForageEnabled() {
            return config.HarvestForage;
        }


        [BytecodePatch("StardewValley.Object::performToolAction", "HarvestForageEnabled")]
        void Object_performToolAction() {
            var code = BeginCode();
            Label begin = AttachLabel(code[0]);
            code.Prepend(
                // Check if Tool is scythe.
                Instructions.Ldarg_1(),
                Instructions.Isinst(typeof(StardewValley.Tools.MeleeWeapon)),
                Instructions.Brfalse(begin),
                Instructions.Ldarg_1(),
                Instructions.Isinst(typeof(StardewValley.Tools.MeleeWeapon)),
                Instructions.Callvirt_get(typeof(StardewValley.Tool), "BaseName"),
                Instructions.Ldstr("Scythe"),
                Instructions.Callvirt(typeof(System.String), "Equals", typeof(string)),
                Instructions.Brfalse(begin),
                // Hook
                Instructions.Ldarg_0(),
                Instructions.Ldarg_1(),
                Instructions.Ldarg_2(),
                Instructions.Call(typeof(ModEntry), "ScytheForage", typeof(StardewValley.Object), typeof(StardewValley.Tool), typeof(StardewValley.GameLocation)),
                Instructions.Brfalse(begin),
                Instructions.Ldc_I4_1(),
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
            } else {
                return false;
            }
        }
    }
}

