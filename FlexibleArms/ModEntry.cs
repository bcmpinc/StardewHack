using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace StardewHack.FlexibleArms
{
    public class ModConfig {
        public float MaxRange = 1.4f;
    }

    public class ModEntry : HackWithConfig<ModEntry, ModConfig>
    {
        public override void HackEntry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            Patch((Character c) => c.GetToolLocation(Vector2.Zero, false), Character_GetToolLocation);
        }
        protected override void InitializeApi(IGenericModConfigMenuApi api) {
            api.AddNumberOption(mod: ModManifest, name: I18n.RangeName, tooltip: I18n.RangeTooltip, getValue: () => config.MaxRange, setValue: (float val) => config.MaxRange = val, min: 1, max: 5);
        }

        void Character_GetToolLocation() {
            AllCode().Replace(
                Instructions.Ldarg_0(),
                Instructions.Ldarg_1(),
                Instructions.Ldarg_2(),
                Instructions.Call(typeof(ModEntry), nameof(GetToolLocation), typeof(Character), typeof(Vector2), typeof(bool)),
                Instructions.Ret()
            );
        }

		public static Vector2 GetToolLocation(Character c, Vector2 target_position, bool ignoreClick) {
			if (!ignoreClick && !target_position.Equals(Vector2.Zero) && c.Name.Equals(Game1.player.Name)) {
                var player_position = Game1.player.getStandingPosition();
                var delta = target_position - player_position;
                var range = getConfig().MaxRange * 64.0f;
				if (delta.LengthSquared() <= range * range) {
					return target_position;
				} else { 
                    delta.Normalize();
				    return player_position + delta * range;
                }
			}
			Rectangle boundingBox = c.GetBoundingBox();
			return c.FacingDirection switch {
				0 => new Vector2(boundingBox.X + boundingBox.Width / 2, boundingBox.Y - 48), 
				1 => new Vector2(boundingBox.X + boundingBox.Width + 48, boundingBox.Y + boundingBox.Height / 2), 
				2 => new Vector2(boundingBox.X + boundingBox.Width / 2, boundingBox.Y + boundingBox.Height + 48), 
				3 => new Vector2(boundingBox.X - 48, boundingBox.Y + boundingBox.Height / 2), 
				_ => c.getStandingPosition(), 
			};
		}

    }
}

