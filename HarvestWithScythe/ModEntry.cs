using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using static HarmonyLib.Code;

namespace StardewHack.HarvestWithScythe
{
    public enum HarvestModeEnum {
        HAND, 
        IRID, // I.e. vanilla default
        GOLD, // I.e. hand, unless the golden scythe is equipped.
        BOTH, // I.e. determined by whether the scythe is equipped.
    }

    public class ModConfig {
        /** Whether a sword can be used instead of a normal scythe. */
        public bool HarvestWithSword = false;
    
        /** How should flowers be harvested? 
            * Any object whose harvest has Object.flowersCategory. */
        public HarvestModeEnum Flowers = HarvestModeEnum.BOTH;
            
        /** How should forage be harvested? 
            * Any Object where `isForage() && isSpawnedObject && !questItem` evaluates to true is considered forage. */
        public HarvestModeEnum Forage = HarvestModeEnum.BOTH;
            
        /** How should pluckable crops be harvested? 
            * Any Crop that has `harvestMethod == 0` is considered a pluckable crop. */
        public HarvestModeEnum PluckableCrops = HarvestModeEnum.BOTH;
    }

    /**
     * This is the core of the Harvest With Scythe mod.
     *
     * Crops are either harvested by hand, which is initiatied by HoeDirt.PerformUseAction(), 
     * or harvested by scythe, which is initiated by HoeDirt.PerformToolAction().
     * These methods check whether the crop is allowed to be harvested by this method and 
     * then passes control to Crop.harvest() to perform the actual harvesting. 
     *
     * Crop.Harvest() can do further checks whether harvesting is possible. If not, it returns
     * false to indicate that harvesting failed.
     * 
     * The harvesting behavior, i.e. whether the item drops on the ground (scything) or 
     * is held above the head (plucking) is determined by the value of `harvestMethod`.
     * Hence HoeDirt.Perform*Action must set this field to the appropriate value and restore 
     * it afterwards.
     *
     * Flowers can have different colors, which is supported by the scythe harvesting code
     * in SDV 1.6. Whether a crop is a flower is detected using IsFlower, which is based on
     * Utility.findCloseFlower.
     *
     * Forage are plain Objects where `isForage() && isSpawnedObject && !questItem` evaluates to true.
     * Those are handled by GameLocation.checkAction() and Object.performToolAction(). As the 
     * game does not provide logic for scythe harvesting of forage, this is provided by this mod, 
     * see ScytheForage().
     *
     */
    public class ModEntry : HackWithConfig<ModEntry, ModConfig> {
        public override void HackEntry(IModHelper helper) {
            I18n.Init(helper.Translation);

            // Scythe harvesting
            Patch((HoeDirt hd) => hd.performToolAction(null, 0, new Vector2()), HoeDirt_performToolAction);

            // Interaction with HoeDirt
            //Patch((HoeDirt hd) => hd.performUseAction(new Vector2()), HoeDirt_performUseAction);

            // Sword harvesting grass
            Patch((Grass g) => g.performToolAction(null, 0, new Vector2()), Grass_performToolAction);
        }

#region ModConfig

        protected override void InitializeApi(IGenericModConfigMenuApi api) {
            api.AddBoolOption(mod: ModManifest, name: I18n.HarvestWithSwordName, tooltip: I18n.HarvestWithSwordTooltip, getValue: () => config.HarvestWithSword, setValue: (bool val) => config.HarvestWithSword = val);

            var options_dict = new Dictionary<HarvestModeEnum, string>()
            {
                {HarvestModeEnum.HAND, I18n.Hand()},
                {HarvestModeEnum.IRID, I18n.Irid()},
                {HarvestModeEnum.GOLD, I18n.Gold()},
                {HarvestModeEnum.BOTH, I18n.Both()},
            };
            var reverse_dict = options_dict.ToDictionary(x=>x.Value, x=>x.Key);
            string[] options = options_dict.Values.ToArray();

            api.AddSectionTitle(mod: ModManifest, text: I18n.HarvestModeSection);
            api.AddParagraph(mod: ModManifest, text: I18n.HarvestModeDescription);
            api.AddTextOption(mod: ModManifest, name: I18n.PluckableCropsName, tooltip: I18n.PluckableCropsTooltip, getValue: () => options_dict[config.PluckableCrops], setValue: (string val) => config.PluckableCrops = reverse_dict[val], allowedValues: options);
            api.AddTextOption(mod: ModManifest, name: I18n.FlowersName,        tooltip: I18n.FlowersTooltip,        getValue: () => options_dict[config.Flowers       ], setValue: (string val) => config.Flowers        = reverse_dict[val], allowedValues: options);
        } 
#endregion 

#region CanHarvest methods
        static public bool IsFlower(Crop crop)
        {
            var data = ItemRegistry.GetData(crop.indexOfHarvest.Value);
            return data?.Category == Object.flowersCategory;
        }

        static public bool IsScythe(Tool t) {
            if (t is MeleeWeapon) {
                return getInstance().config.HarvestWithSword || (t as MeleeWeapon).isScythe();
            }
            return false;
        }

        /** Determine whether the given crop can be harvested using a scythe. */
        public static bool CanScytheCrop(Crop crop, Tool tool) {
            getInstance().Monitor.Log($"{crop} {tool}");
            if (crop == null) return false;
            if (crop.GetHarvestMethod() == HarvestMethod.Scythe) return true;

            ModConfig config = getInstance().config;
            HarvestModeEnum mode = IsFlower(crop) ? config.Flowers : config.PluckableCrops;

            switch (mode) {
                case HarvestModeEnum.BOTH: return true;
                case HarvestModeEnum.GOLD: return tool.QualifiedItemId == MeleeWeapon.goldenScytheId || tool.QualifiedItemId == MeleeWeapon.iridiumScytheID;
                case HarvestModeEnum.IRID: return tool.QualifiedItemId == MeleeWeapon.iridiumScytheID;
                case HarvestModeEnum.HAND: return false;
                default:
                    throw new System.Exception("unreachable code");
            }
        }
#endregion

#region Patch HoeDirt

        static readonly InstructionMatcher HoeDirt_crop = Instructions.Call_get(typeof(HoeDirt), nameof(HoeDirt.crop));

        void HoeDirt_performToolAction() {
            // Replace Tool.isScythe call with our own method.
            var code = FindCode(
                Instructions.Ldarg_1(),
                Instructions.Callvirt(typeof(Tool), nameof(Tool.isScythe)),
                OpCodes.Brfalse
            );
            code[1] = Instructions.Call(typeof(ModEntry), nameof(IsScythe), typeof(Tool));

            // Replace the scythe check code with our own.
            var crop = FindCode(
                // if ((obj != null && obj.GetHarvestMethod() == HarvestMethod.Scythe) || (this.crop != null && t.ItemId == "66"))
                OpCodes.Ldarg_0,
                Instructions.Call_get(typeof(HoeDirt), nameof(HoeDirt.crop)),
                OpCodes.Dup,
                OpCodes.Brtrue_S,
                OpCodes.Pop,
                OpCodes.Ldc_I4_0,
                OpCodes.Br_S,
                Instructions.Call(typeof(Crop), nameof(Crop.GetHarvestMethod)),
                OpCodes.Ldc_I4_1,
                OpCodes.Ceq,
                OpCodes.Brtrue_S,
                OpCodes.Ldarg_0,
                Instructions.Call_get(typeof(HoeDirt), nameof(HoeDirt.crop)),
                OpCodes.Brfalse,
                OpCodes.Ldarg_1,
                Instructions.Callvirt_get(typeof(Item), nameof(Item.ItemId)),
                Instructions.Ldstr("66"),
                OpCodes.Call,
                OpCodes.Brfalse
            );
            crop.length--;
            crop.Replace(
                Instructions.Ldarg_0(),
                Instructions.Call_get(typeof(HoeDirt), nameof(HoeDirt.crop)),
                Instructions.Ldarg_1(),
                Instructions.Call(typeof(ModEntry), nameof(CanScytheCrop), typeof(Crop), typeof(Tool))
            );
        }

#if false
        private void HoeDirt_performUseAction_hand(LocalBuilder var_temp_harvestMethod) {
            // Do plucking logic
            var harvest_hand = FindCode(
                // if ((int)crop.harvestMethod == 0) {
                OpCodes.Ldarg_0,
                HoeDirt_crop,
                Instructions.Ldfld(typeof(Crop), nameof(Crop.harvestMethod)),
                OpCodes.Call, // NetCode implicit cast.
                OpCodes.Brtrue
            );
            harvest_hand.Replace(
                // var temp_harvestmethod = crop.harvestMethod;
                harvest_hand[0],
                harvest_hand[1],
                harvest_hand[2],
                harvest_hand[3],
                Instructions.Stloc_S(var_temp_harvestMethod),
                
                // if (ModEntry.CanHarvestCrop(crop, 0)) {
                Instructions.Ldarg_0(),
                harvest_hand[1],
                Instructions.Ldc_I4_0(),
                Instructions.Call(typeof(ModEntry), nameof(CanHarvestCrop), typeof(Crop), typeof(int)),
                Instructions.Brfalse((Label)harvest_hand[4].operand),
                
                // crop.harvestMethod = 0;
                Instructions.Ldarg_0(),
                harvest_hand[1],
                harvest_hand[2],
                Instructions.Ldc_I4_0(),
                Instructions.Call_set(typeof(NetInt), nameof(NetInt.Value))
            );
        }

        private void HoeDirt_performUseAction_scythe(LocalBuilder var_temp_harvestMethod) {
            // Do scything logic
            var harvest_scythe = FindCode(
                // if ((int)crop.harvestMethod == 1) {
                OpCodes.Ldarg_0,
                HoeDirt_crop,
                Instructions.Ldfld(typeof(Crop), nameof(Crop.harvestMethod)),
                OpCodes.Call, // NetCode implicit cast.
                OpCodes.Ldc_I4_1,
                OpCodes.Bne_Un
            );
            harvest_scythe.Replace(
                // crop.harvestMethod = temp_harvestmethod;
                harvest_scythe[0],
                harvest_scythe[1],
                harvest_scythe[2],
                Instructions.Ldloc_S(var_temp_harvestMethod),
                Instructions.Call_set(typeof(NetInt), nameof(NetInt.Value)),

                // if (ModEntry.CanHarvestCrop(crop, 1)) {
                Instructions.Ldarg_0(),
                harvest_scythe[1],
                Instructions.Ldc_I4_1(),
                Instructions.Call(typeof(ModEntry), nameof(CanHarvestCrop), typeof(Crop), typeof(int)),
                Instructions.Brfalse((Label)harvest_scythe[5].operand)
            );

            harvest_scythe = harvest_scythe.FindNext(
                // Game1.player.CurrentTool is MeleeWeapon &&
                Instructions.Call_get(typeof(Game1), nameof(Game1.player)),
                Instructions.Callvirt_get(typeof(Farmer), nameof(Farmer.CurrentTool)),
                Instructions.Isinst(typeof(MeleeWeapon)),
                OpCodes.Brfalse,

                // (Game1.player.CurrentTool as MeleeWeapon).isScythe()
                Instructions.Call_get(typeof(Game1), nameof(Game1.player)),
                Instructions.Callvirt_get(typeof(Farmer), nameof(Farmer.CurrentTool)),
                Instructions.Isinst(typeof(MeleeWeapon)),
                OpCodes.Ldc_I4_M1,
                Instructions.Callvirt(typeof(MeleeWeapon), nameof(MeleeWeapon.isScythe), typeof(int)),
                OpCodes.Brfalse
            );

            harvest_scythe.Replace(
                harvest_scythe[0],
                harvest_scythe[1],
                Instructions.Call(GetType(), nameof(IsScythe), typeof(Tool)),
                harvest_scythe[3]
            );
        }

        void HoeDirt_performUseAction() {
            LocalBuilder var_temp_harvestMethod = generator.DeclareLocal(typeof(int));
            HoeDirt_performUseAction_hand(var_temp_harvestMethod);
            HoeDirt_performUseAction_scythe(var_temp_harvestMethod);
        }

#endif
#endregion

#region Grass
        private void Grass_performToolAction() {
            /*
            var isScytheCode = FindCode(
                // if (t is MeleeWeapon && 
                OpCodes.Ldarg_1,
                Instructions.Isinst(typeof(MeleeWeapon)),
                OpCodes.Brfalse,

                // (t.Name.Contains("Scythe") || 
                OpCodes.Ldarg_1,
                Instructions.Callvirt_get(typeof(Item), nameof(Item.Name)),
                Instructions.Ldstr("Scythe"),
                OpCodes.Callvirt,
                OpCodes.Brtrue,

                // (t as MeleeWeapon).isScythe()))
                OpCodes.Ldarg_1,
                Instructions.Isinst(typeof(MeleeWeapon)),
                OpCodes.Ldc_I4_M1,
                Instructions.Callvirt(typeof(MeleeWeapon), nameof(MeleeWeapon.isScythe), typeof(int)),
                OpCodes.Brfalse
            );
            isScytheCode.Replace(
                isScytheCode[0],
                Instructions.Call(GetType(), nameof(IsScythe), typeof(Tool)),
                isScytheCode[2]
            );
            */
        }
#endregion
    }
}

