using Entoarox.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;

namespace StardewHack.FlexibleArms
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper) {
            helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            plate = helper.Content.Load<Texture2D>("switch.png");
        }

        void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e) {
            Game1.player.addItemToInventory(new PressurePlate());
        }
    }
}

