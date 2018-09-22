using System;
using System.Reflection.Emit;

namespace StardewHack.GrassGrowth
{
    public class ModConfig {
        /** Whether grass spreading should be suppressed entirely. */
        public bool DisableGrowth = false;
    }

    public class ModEntry : HackWithConfig<ModEntry, ModConfig>
    {
        // Change the milk pail such that it doesn't do anything while no animal is in range. 
        [BytecodePatch("StardewValley.GameLocation::growWeedGrass")]
        void GameLocation_growWeedGrass() {
            if (config.DisableGrowth) {
                // Stop grass from spreading.
                AllCode().Replace(
                    Instructions.Ret()
                );
            } else {
                // Change grass growth to spread mostly everywhere.
                var growWeedGrass = BeginCode();
                // For each ofthe 4 directions
                for (int i=0; i<4; i++) {
                    growWeedGrass = growWeedGrass.FindNext(
                        OpCodes.Ldarg_0,
                        null,
                        null,
                        null,
                        null,
                        Instructions.Ldstr("Diggable"),
                        Instructions.Ldstr("Back"),
                        Instructions.Call(typeof(StardewValley.GameLocation), "doesTileHaveProperty", typeof(int), typeof(int), typeof(string), typeof(string)),
                        OpCodes.Brfalse
                    );
                    growWeedGrass.Remove();
                }
            }
        }
    }
}

