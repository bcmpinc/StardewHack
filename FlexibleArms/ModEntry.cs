using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace StardewHack.FlexibleArms
{
    public class ModConfig {
        public float MaxRange = 1.5f;
    }

    public class ModEntry : HackWithConfig<ModEntry, ModConfig>
    {
        public override void HackEntry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            Patch((Character c) => c.GetToolLocation(false), Character_GetToolLocation);
        }
        protected override void InitializeApi(IGenericModConfigMenuApi api) {
            api.AddNumberOption(mod: ModManifest, name: I18n.RangeName, tooltip: I18n.RangeTooltip, getValue: () => config.MaxRange, setValue: (float val) => config.MaxRange = val, min: 0, max: 10);
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

		public static bool withinRadiusOfPlayer(Vector2 point, float radius, Farmer f) {
			return (f.Position - point).LengthSquared() < radius * radius;
		}

		public static Vector2 GetToolLocation(Character c, Vector2 target_position, bool ignoreClick) {
			int direction = c.FacingDirection;
			if (!ignoreClick && !target_position.Equals(Vector2.Zero) && c.Name.Equals(Game1.player.Name)) {
				if (withinRadiusOfPlayer(target_position, getConfig().MaxRange * 64.0f, Game1.player)) {
					return target_position;
				}
				direction = Game1.player.getGeneralDirectionTowards(target_position);
			}
			Rectangle boundingBox = c.GetBoundingBox();
			return direction switch {
				0 => new Vector2(boundingBox.X + boundingBox.Width / 2, boundingBox.Y - 48), 
				1 => new Vector2(boundingBox.X + boundingBox.Width + 48, boundingBox.Y + boundingBox.Height / 2), 
				2 => new Vector2(boundingBox.X + boundingBox.Width / 2, boundingBox.Y + boundingBox.Height + 48), 
				3 => new Vector2(boundingBox.X - 48, boundingBox.Y + boundingBox.Height / 2), 
				_ => c.getStandingPosition(), 
			};
		}

    }
}

