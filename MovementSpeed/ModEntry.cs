﻿using HarmonyLib;
using StardewModdingAPI;
using System.Reflection;
using System.Reflection.Emit;
using StardewValley;
using GenericModConfigMenu;

namespace StardewHack.MovementSpeed
{
    
    public class ModConfig {
        /** The movement speed is multiplied by this amount. The mod's default is 1.5, meaning 50% faster movement. Set this to 1 to disable the increase in movement speed. */
        public float MovementSpeedMultiplier = 1.5f;
        /** Time required for charging the hoe or watering can in ms. Normally this is 600ms. The default is 600/1.5 = 400, meaning 50% faster charging. Set this to 600 to disable faster tool charging. */
        public int ToolChargeDelay = 399;
    }
    
    public class ModEntry : HackWithConfig<ModEntry, ModConfig>
    {
        public override void HackEntry(IModHelper helper) {
            I18n.Init(helper.Translation);
            if (config.ToolChargeDelay == 400)
                config.ToolChargeDelay = 399; // For some reason using 400 causes the player to no longer be able to hop around while fully charged. I'm too lazy to figure out why.
            Patch((Farmer f)=>f.getMovementSpeed(), Farmer_getMovementSpeed);
            Patch(typeof(Game1), "UpdateControlInput", Game1_UpdateControlInput);
        }

        protected override void InitializeApi(IGenericModConfigMenuApi api)
        {
            api.AddNumberOption(mod: ModManifest, name: I18n.MovementSpeedMultiplierName, tooltip: I18n.MovementSpeedMultiplierTooltip, getValue: () => config.MovementSpeedMultiplier, setValue: (float val) => config.MovementSpeedMultiplier = val, min:   0, max:   5);
            api.AddNumberOption(mod: ModManifest, name: I18n.ToolChargeDelayName,         tooltip: I18n.ToolChargeDelayTooltip,         getValue: () => config.ToolChargeDelay,         setValue: (int val)   => config.ToolChargeDelay         = val, min: 100, max: 600);
        }

        static float getMovementSpeedMultiplier() => getInstance().config.MovementSpeedMultiplier;
        static float getToolChargeDelay() => getInstance().config.ToolChargeDelay;

        // Add a multiplier to the movement speed.
        void Farmer_getMovementSpeed() {
            var code = FindCode(
                // movementMultiplier = 0.066f;
                OpCodes.Ldarg_0,
                OpCodes.Ldc_R4,
                Instructions.Stfld(typeof(Farmer), nameof(Farmer.movementMultiplier))
            );
            code.Insert(2,
                Instructions.Call(GetType(), nameof(getMovementSpeedMultiplier)),
                Instructions.Mul()
            );
        }

        // Change (reduce) the time it takses to charge tools (hoe & water can).
        void Game1_UpdateControlInput() {
            try {
                Game1_UpdateControlInput_Chain();
            } catch (InstructionNotFoundException) {
                // This has been working fine for so long, we don't really need this debug info anymore.
                // LogException(err, LogLevel.Trace);
                Monitor.Log("Using chain patch");
                
                // The PC version of StardewModdingAPI changed this method and moved its original code into a delegate, hence the chain patching.
                MethodInfo method = (MethodInfo)FindCode(
                    OpCodes.Ldftn
                )[0].operand;
                ChainPatch(method, AccessTools.Method(typeof(ModEntry), nameof(Game1_UpdateControlInput_Chain)));
            }
        }

        void Game1_UpdateControlInput_Chain() {
            // Game1.player.toolHold = (int)(600f * num4);
            FindCode(
                Instructions.Call_get(typeof(Game1), nameof(Game1.player)),
                OpCodes.Ldfld,
                Instructions.Ldc_R4(600f),
                OpCodes.Ldloc_S,
                OpCodes.Mul,
                OpCodes.Conv_I4,
                OpCodes.Callvirt
            )[2] = Instructions.Call(GetType(), nameof(getToolChargeDelay));
        }
    }
}

