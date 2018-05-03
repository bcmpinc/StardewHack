using Harmony;
using StardewModdingAPI;
using System;
using System.Reflection;
using System.Reflection.Emit;
using Newtonsoft.Json.Linq;

namespace StardewHack.MovementSpeed
{
    
    public class ModConfig {
        /** The movement speed is multiplied by this amount. The default is 1.5, meaning 50% faster movement. */
        public double MovementSpeedMultiplier = 1.5;
        /** Time required for charging the hoe or watering can in ms. Normally this is 600ms. The default is 600/1.5 = 400, meaning 50% faster charging. */
        public int ToolChargeDelay = 400;
    }

    public class ModEntry : Hack
    {
        ModConfig config;

        public override void Entry(IModHelper helper) {
            config = helper.ReadConfig<ModConfig>();
            base.Entry(helper);
        }

        // Add a multiplier to the movement speed.
        [BytecodePatch(typeof(StardewValley.Farmer), "getMovementSpeed")]
        void Farmer_getMovementSpeed() {
            if (config.MovementSpeedMultiplier == 1) return;
            FindCodeLast(
                OpCodes.Ret
            ).Prepend(
                Instructions.Ldc_R8(config.MovementSpeedMultiplier),
                Instructions.Mul()
            );
        }

        // Change (reduce) the time it takses to charge tools (hoe & water can).
        [BytecodePatch(typeof(StardewValley.Game1), "UpdateControlInput")]
        void Game1_UpdateControlInput() {
            // StardewModdingAPI changed this method and moved its original code into a delegate, hence the chain patching.
            MethodInfo method = (MethodInfo)FindCode(
                OpCodes.Ldftn
            )[0].operand;
            ChainPatch(method, AccessTools.Method(typeof(ModEntry), "Game1_UpdateControlInput_Chain"));
        }

        void Game1_UpdateControlInput_Chain() {
            if (config.ToolChargeDelay == 600) return;
            FindCode(
                Instructions.Ldc_I4(600),
                Instructions.Stfld(typeof(StardewValley.Farmer), "toolHold")
            )[0].operand = config.ToolChargeDelay;
        }
    }
}

