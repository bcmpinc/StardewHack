using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace StardewHack.HarvestWithScythe
{
    public enum HarvestModeEnum {
        NONE, // crop cannot be harvested at all.
        HAND, // prevent scythe harvesting.
        IRID, // vanilla default.
        GOLD, // golden scythe can be used.
        BOTH, // determined by whether a scythe is equipped.
        SCYTHE, // cannot be harvested by hand.
    }

    public class ModConfig {
        /** Whether a sword can be used instead of a normal scythe. */
        public bool HarvestWithSword = false;

        /** Whether you can still pluck plants with a valid scythe equipped. */
        public bool PluckingScythe = false;

        /** Whether the scythe should work too on forage not above tilled soil. */
        public bool HarvestAllForage = false;
    
        /** How should flowers be harvested? 
         * Any object whose harvest has Object.flowersCategory. */
        public HarvestModeEnum Flowers = HarvestModeEnum.BOTH;

        /** How should forage be harvested? 
         * Any Object where `isForage() && isSpawnedObject && !questItem` evaluates to true is considered forage. */
        public HarvestModeEnum Forage = HarvestModeEnum.BOTH;

        /** How should pluckable crops & forage be harvested? 
         * Any Crop that has `GetHarvestMethod() == HarvestMethod.Hand` is considered a pluckable crop.
         * Any object that sits on top of HoeDirt is considered forage. */
        public HarvestModeEnum PluckableCrops = HarvestModeEnum.BOTH;

        /** How should scythable crops be harvested?
         * Any Crop that has `GetHarvestMethod() == HarvestMethod.Scythe` is considered a scythable crop. */
        public HarvestModeEnum ScythableCrops = HarvestModeEnum.SCYTHE;
    }

    /**
     * This is the core of the Harvest With Scythe mod.
     *
     * Crops are either harvested by hand, which is initiated by HoeDirt.PerformUseAction(), 
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
     * Forage are plain Objects that exist on top of HoeDirt.
     */
    public class ModEntry : HackWithConfig<ModEntry, ModConfig> {
        public override void HackEntry(IModHelper helper) {
            I18n.Init(helper.Translation);
            Patch((HoeDirt hd) => hd.performUseAction(Vector2.Zero), HoeDirt_performUseAction);
            Patch((HoeDirt hd) => hd.performToolAction(null, 0, Vector2.Zero), HoeDirt_performToolAction);
        }

        #region ModConfig
        protected override void InitializeApi(IGenericModConfigMenuApi api) {
            api.AddBoolOption(mod: ModManifest, name: I18n.HarvestWithSwordName, tooltip: I18n.HarvestWithSwordTooltip, getValue: () => config.HarvestWithSword, setValue: (bool val) => config.HarvestWithSword = val);
            api.AddBoolOption(mod: ModManifest, name: I18n.PluckingScytheName,   tooltip: I18n.PluckingScytheTooltip,   getValue: () => config.PluckingScythe,   setValue: (bool val) => config.PluckingScythe   = val);
            api.AddBoolOption(mod: ModManifest, name: I18n.AllForageName,        tooltip: I18n.AllForageTooltip,        getValue: () => config.HarvestAllForage, setValue: (bool val) => config.HarvestAllForage = val);

            var options_dict = new Dictionary<HarvestModeEnum, string>()
            {
                {HarvestModeEnum.HAND,   I18n.Hand()},
                {HarvestModeEnum.IRID,   I18n.Irid()},
                {HarvestModeEnum.GOLD,   I18n.Gold()},
                {HarvestModeEnum.BOTH,   I18n.Both()},
                {HarvestModeEnum.SCYTHE, I18n.Scythe()},
                {HarvestModeEnum.NONE,   I18n.None()},
            };
            var reverse_dict = options_dict.ToDictionary(x=>x.Value, x=>x.Key);
            string[] options = options_dict.Values.ToArray();

            api.AddSectionTitle(mod: ModManifest, text: I18n.HarvestModeSection);
            api.AddParagraph(mod: ModManifest, text: I18n.HarvestModeDescription);
            api.AddTextOption(mod: ModManifest, name: I18n.PluckableCropsName, tooltip: I18n.PluckableCropsTooltip, getValue: () => options_dict[config.PluckableCrops], setValue: (string val) => config.PluckableCrops = reverse_dict[val], allowedValues: options);
            api.AddTextOption(mod: ModManifest, name: I18n.ScythableCropsName, tooltip: I18n.ScythableCropsTooltip, getValue: () => options_dict[config.ScythableCrops], setValue: (string val) => config.ScythableCrops = reverse_dict[val], allowedValues: options);
            api.AddTextOption(mod: ModManifest, name: I18n.FlowersName,        tooltip: I18n.FlowersTooltip,        getValue: () => options_dict[config.Flowers       ], setValue: (string val) => config.Flowers        = reverse_dict[val], allowedValues: options);
            api.AddTextOption(mod: ModManifest, name: I18n.ForageName,         tooltip: I18n.ForageTooltip,         getValue: () => options_dict[config.Forage        ], setValue: (string val) => config.Forage         = reverse_dict[val], allowedValues: options);
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

        public static bool CheckMode(HarvestModeEnum mode, Tool tool) {
            switch (mode) {
                case HarvestModeEnum.SCYTHE: return true;
                case HarvestModeEnum.BOTH: return true;
                case HarvestModeEnum.GOLD: return tool.ItemId == MeleeWeapon.goldenScytheId || tool.ItemId == MeleeWeapon.iridiumScytheID;
                case HarvestModeEnum.IRID: return tool.ItemId == MeleeWeapon.iridiumScytheID;
                case HarvestModeEnum.HAND: return false;
                case HarvestModeEnum.NONE: return false;
                default:
                    throw new System.Exception("unreachable code");
            }
        }

        public static HarvestModeEnum GetHarvestSetting(Crop crop) {
            ModConfig config = getInstance().config;
            if (crop.GetHarvestMethod() == HarvestMethod.Scythe) return config.ScythableCrops;
            if (IsFlower(crop)) return config.Flowers;
            return config.PluckableCrops;
        }

        /** Determine whether the given crop can be harvested using a scythe. */
        public static bool CanScytheCrop(Crop crop, Tool tool) {
            if (crop == null) return false;
            return CheckMode(GetHarvestSetting(crop), tool);
        }

        public static bool CanScytheForage(Tool tool) {
            ModConfig config = getInstance().config;
            return CheckMode(config.Forage, tool);
        }
#endregion

#region Patch HoeDirt
        void HoeDirt_performUseAction() {
            // Change the code that checks and handles Iridium Scythe to allow any scythe that our mod accepts.
            var code = FindCode(
                // if (Game1.player.CurrentTool != null && Game1.player.CurrentTool.isScythe() && Game1.player.CurrentTool.ItemId == "66")
                Instructions.Call_get(typeof(Game1), nameof(Game1.player)),
                Instructions.Callvirt_get(typeof(Farmer), nameof(Farmer.CurrentTool)),
                OpCodes.Brfalse_S,
                Instructions.Call_get(typeof(Game1), nameof(Game1.player)),
                Instructions.Callvirt_get(typeof(Farmer), nameof(Farmer.CurrentTool)),
                Instructions.Callvirt(typeof(Tool), nameof(Tool.isScythe)),
                OpCodes.Brfalse_S,
                Instructions.Call_get(typeof(Game1), nameof(Game1.player)),
                Instructions.Callvirt_get(typeof(Farmer), nameof(Farmer.CurrentTool)),
                Instructions.Callvirt_get(typeof(Item), nameof(Item.ItemId)),
                Instructions.Ldstr("66"),
                OpCodes.Call,
                OpCodes.Brfalse_S
            );
            code.length--;
            code.Replace(
                Instructions.Ldarg_0(),
                code[0],
                code[1],
                Instructions.Call(typeof(ModEntry), nameof(is_force_scythe), typeof(HoeDirt), typeof(Tool))
            );

            // Replace the second Tool.isScythe call with our own.
            code = code.FindNext(
                Instructions.Callvirt(typeof(Tool), nameof(Tool.isScythe))
            );
            code[0] = Instructions.Call(typeof(ModEntry), nameof(IsScythe), typeof(Tool));
        }

        static bool is_force_scythe(HoeDirt dirt, Tool tool) {
            var crop = dirt.crop;

            // Always force scythe if this is set as a non-pluckable crop (SCYTHE & NONE).
            var mode = GetHarvestSetting(crop);
            if (mode == HarvestModeEnum.SCYTHE || mode == HarvestModeEnum.NONE) return true;

            // Never force scythe when plucking while wielding a scythe is enabled.
            if (getConfig().PluckingScythe) return false;

            // Return whether the current tool can be used to harvest the crop.
            return tool != null && IsScythe(tool) && CanScytheCrop(crop, tool);
        }

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
                // if (ModEntry.CanScytheCrop(this.crop, t))
                Instructions.Ldarg_0(),
                Instructions.Call_get(typeof(HoeDirt), nameof(HoeDirt.crop)),
                Instructions.Ldarg_1(),
                Instructions.Call(typeof(ModEntry), nameof(CanScytheCrop), typeof(Crop), typeof(Tool))
            );

            // Add fix for forage, including quality & xp
            var forage = crop.FindNext(
                // if (this.crop == null && 
                OpCodes.Ldarg_0,
                Instructions.Call_get(typeof(HoeDirt), nameof(HoeDirt.crop)),
                OpCodes.Brtrue_S,
                // t.ItemId == "66" &&
                OpCodes.Ldarg_1,
                Instructions.Callvirt_get(typeof(Item), nameof(Item.ItemId)),
                Instructions.Ldstr("66"),
                OpCodes.Call,
                OpCodes.Brfalse
            );
            forage.Extend(
	            // location.objects.Remove(tileLocation);
                OpCodes.Ldloc_0,
                Instructions.Ldfld(typeof(GameLocation), nameof(GameLocation.objects)),
                OpCodes.Ldarg_3,
                OpCodes.Callvirt,
                OpCodes.Pop
	        );
            forage.Replace(
                Instructions.Ldarg_0(),
                Instructions.Ldarg_1(),
                Instructions.Ldloc_0(),
                Instructions.Ldarg_3(),
                Instructions.Call(typeof(ModEntry), nameof(harvest_forage_with_xp), typeof(HoeDirt), typeof(Tool), typeof(GameLocation), typeof(Vector2))
            );
        }

        static void harvest_forage_with_xp(HoeDirt dirt, Tool t, GameLocation location, Vector2 tileLocation) {
            if (dirt.crop == null && CanScytheForage(t) && location.objects.ContainsKey(tileLocation) && location.objects[tileLocation].isForage()) {
				Object o = location.objects[tileLocation];
                var r = Game1.random;
                var who = t.getLastFarmerToUse();
				if (t.getLastFarmerToUse() != null && who.professions.Contains(16)) {
					o.Quality = 4;
				} else if (r.NextDouble() < (double)(who.ForagingLevel / 30f)) {
					o.Quality = 2;
				} else if (r.NextDouble() < (double)(who.ForagingLevel / 15f)) {
					o.Quality = 1;
				}
                who.gainExperience(2, 7);
                Game1.stats.ItemsForaged += 1;
                var vector = new Vector2(tileLocation.X * 64f + 32f, tileLocation.Y * 64f + 32f);
				Game1.createItemDebris(o, vector, -1);
                if (who.professions.Contains(13) && r.NextDouble() < 0.2) {
                    who.gainExperience(2, 7);
                    Game1.createItemDebris(o.getOne(), vector, -1, null, -1);
                }
				location.objects.Remove(tileLocation);
			}
        }
#endregion
    }
}

