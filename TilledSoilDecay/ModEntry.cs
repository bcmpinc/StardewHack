using StardewModdingAPI;
using System.Reflection.Emit;
using Netcode;
using StardewValley;
using StardewValley.TerrainFeatures;
using Microsoft.Xna.Framework;
using GenericModConfigMenu;
using System.Reflection;
using HarmonyLib;

namespace StardewHack.TilledSoilDecay
{
    public class ModConfig
    {
        /** Chance that a patch of tilled soil will disappear during the night. */
        public double DryingRateMultiplier = 5;
        /** Number of days that the patch must have been without water, before it can disappear during the night. */
        public int    Delay = 2;
    }

    public class ModEntry : HackWithConfig<ModEntry, ModConfig>
    {
        public override void HackEntry(IModHelper helper) {
            I18n.Init(helper.Translation);

            Patch((GameLocation gl) => gl.HandleGrassGrowth(1), GameLocation_HandleGrassGrowth);
            Patch((HoeDirt hd) => hd.dayUpdate(), HoeDirt_dayUpdate);
            Patch((GameLocation gl) => gl.DayUpdate(0), GameLocation_DayUpdate);
        }

        protected override void InitializeApi(IGenericModConfigMenuApi api)
        {
            api.AddNumberOption(mod: ModManifest, name: I18n.DryingRateMultiplierName, tooltip: ()=>I18n.DryingRateMultiplierTooltip(), getValue: () => (float)config.DryingRateMultiplier,  setValue: (float val) => config.DryingRateMultiplier  = val, min: 0.0f, max: 10.0f);
            api.AddNumberOption(mod: ModManifest, name: I18n.DelayName,                tooltip: I18n.DelayTooltip,                      getValue: () =>        config.Delay,                 setValue: (int val)   => config.Delay                 = val, min: 0,    max: 4);
        }

        void GameLocation_HandleGrassGrowth() {
            var code = FindCode(
                // this.terrainFeatures.RemoveWhere( (..) => { .. } )
                // Note: the lambda is created lazily and stored in a static variable, hence the dup and branching.
                OpCodes.Ldsfld,
                OpCodes.Ldftn,
                OpCodes.Newobj,
                OpCodes.Dup,
                OpCodes.Stsfld,
                OpCodes.Callvirt
            );
            // We want to patch that lambda function.
            var method = (MethodInfo)code[1].operand;
            ChainPatch(method, AccessTools.Method(typeof(ModEntry), nameof(GameLocation_HandleGrassGrowth_Chain)));
        }
        void GameLocation_HandleGrassGrowth_Chain() {
            var hdv = generator.DeclareLocal(typeof(HoeDirt));
            var code = FindCode(
                // Game1.random.NextDouble() <= 0.8
                Instructions.Ldsfld(typeof(Game1), nameof(Game1.random)),
                OpCodes.Callvirt,
                OpCodes.Ldc_R8,
                OpCodes.Clt,
                OpCodes.Ret
            );
            code.Replace(
                // return 1-hoedirt.state.Value > getConfig().Delay
                Instructions.Ldc_I4_1(),
                Instructions.Ldloc_0(),
                Instructions.Ldfld(typeof(HoeDirt), nameof(HoeDirt.state)),
                Instructions.Callvirt_get(typeof(NetInt), nameof(NetInt.Value)),
                Instructions.Sub(),
                Instructions.Call(typeof(ModEntry), nameof(getConfig)),
                Instructions.Ldfld(typeof(ModConfig), nameof(ModConfig.Delay)),
                Instructions.Cgt(),
                Instructions.Ret()
            );
        }

        // To support the decay delay, we will use the HoeDirt.state variable to track how many days the patch has gone without being watered.
        // For example: 'state == -3' indicates that the tile hasn't been watered in the past 3 days. 
        void HoeDirt_dayUpdate() {
            var setcropvalue = FindCode(
                // state.Value = 0;
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(HoeDirt), nameof(HoeDirt.state)),
                OpCodes.Ldc_I4_0,
                OpCodes.Callvirt
            );
            // Replace with state.Value--;
            setcropvalue.Splice(2,1,
                Instructions.Dup(),
                Instructions.Callvirt_get(typeof(NetInt), nameof(NetInt.Value)),
                Instructions.Ldc_I4_1(),
                Instructions.Sub()
            );
        }
        
        void GameLocation_DayUpdate() {
            var code = FindCode(
                // this.terrainFeatures.RemoveWhere( (..) => { .. } )
                OpCodes.Ldarg_0,
                OpCodes.Ldfld,
                OpCodes.Ldarg_0,
                OpCodes.Ldftn,
                OpCodes.Newobj,
                OpCodes.Callvirt
            );
            // We want to patch that lambda function.
            var method = (MethodInfo)code[3].operand;
            ChainPatch(method, AccessTools.Method(typeof(ModEntry), nameof(GameLocation_DayUpdate_Chain)));
        }
        void GameLocation_DayUpdate_Chain() {
            var code = FindCode(
                // return Game1.random.NextBool(this.GetDirtDecayChance(pair.Key));
                Instructions.Ldsfld(typeof(Game1), nameof(Game1.random)),
                Instructions.Ldarg_0(),
                OpCodes.Ldarga_S,
                OpCodes.Call,
                Instructions.Callvirt(typeof(GameLocation), nameof(GameLocation.GetDirtDecayChance), typeof(Vector2)),
                OpCodes.Call,
                OpCodes.Ret
            );
            // Apply rate multiplier
            code.Insert(5,
                Instructions.Call(typeof(ModEntry), nameof(getConfig)),
                Instructions.Ldfld(typeof(ModConfig), nameof(ModConfig.DryingRateMultiplier)),
                Instructions.Mul()
            );

            var first = Instructions.Ldloc_0();
            code.ReplaceJump(0, first);

            // Add decay delay
            code.Prepend(
                // if (-hoedirt.state.Value < getConfig().Delay)
                first,
                Instructions.Ldfld(typeof(HoeDirt), nameof(HoeDirt.state)),
                Instructions.Callvirt_get(typeof(NetInt), nameof(NetInt.Value)),
                Instructions.Neg(),
                Instructions.Call(typeof(ModEntry), nameof(getConfig)),
                Instructions.Ldfld(typeof(ModConfig), nameof(ModConfig.Delay)),
                Instructions.Bge(AttachLabel(code[0])),
                //   return false;
                Instructions.Ldc_I4_0(),
                Instructions.Ret()
            );
        }
    }
}

