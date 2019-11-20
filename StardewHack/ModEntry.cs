using StardewModdingAPI;
using Harmony;
using System.Collections.Generic;

namespace StardewHack.Library
{
    public class ModEntry : Mod
    {
        /// <summary>
        /// During startup mods that are broken are added to this list. Used to produce an error message during startup.
        /// </summary>
        static public List<string> broken_mods = new List<string>();
    
        public override void Entry(IModHelper helper) {
            // Check versions
            var harmony_version = typeof(HarmonyInstance).Assembly.GetName().Version;
            Monitor.Log($"Loaded StardewHack library v{ModManifest.Version} using Harmony v{harmony_version}.", LogLevel.Info);
            if (harmony_version < new System.Version(1,2,0,1)) {
                Monitor.Log($"Expected Harmony v1.2.0.1 or later. Mods that depend on StardewHack might not work correctly.", LogLevel.Warn);
            }
            
            // Check incompatible mods.
            CheckIncompatible(helper, "bcmpinc.AlwaysScrollMap",   new SemanticVersion(1,0,0));
            CheckIncompatible(helper, "bcmpinc.CraftCounter",      new SemanticVersion(1,0,0));
            CheckIncompatible(helper, "bcmpinc.FixAnimalTools",    new SemanticVersion(1,0,0));
            CheckIncompatible(helper, "bcmpinc.GrassGrowth",       new SemanticVersion(1,0,0));
            CheckIncompatible(helper, "bcmpinc.HarvestWithScythe", new SemanticVersion(1,1,0));
            CheckIncompatible(helper, "bcmpinc.MovementSpeed",     new SemanticVersion(1,0,0));
            CheckIncompatible(helper, "bcmpinc.TilledSoilDecay",   new SemanticVersion(1,0,0));
            CheckIncompatible(helper, "bcmpinc.TreeSpread",        new SemanticVersion(1,0,0));
            CheckIncompatible(helper, "bcmpinc.WearMoreRings",     new SemanticVersion(1,4,0));
            
            Helper.Events.Display.MenuChanged += Display_MenuChanged;
        }
        
        public void CheckIncompatible(IModHelper helper, string uniqueID, SemanticVersion version) {
            var mod = helper.ModRegistry.Get(uniqueID);
            if (mod != null && mod.Manifest.Version.IsOlderThan(version)) {
                this.Monitor.Log($"Mod '{mod.Manifest.Name}' v{mod.Manifest.Version} is outdated. This will likely cause problems. Please update '{mod.Manifest.Name}' to at least v{version}.", LogLevel.Error);
            }
        }

        void Display_MenuChanged(object sender, StardewModdingAPI.Events.MenuChangedEventArgs e)
        {
            // Fire the first time a menu is being loaded, but only fire once.
            Helper.Events.Display.MenuChanged -= Display_MenuChanged;
            
            // Create a warning message if patches failed to apply cleanly.
            if (broken_mods.Count==0) return;
            
            var mod_list = new List<string>();
            foreach (var i in broken_mods) {
                var mod = Helper.ModRegistry.Get(i).Manifest;
                mod_list.Add($"{mod.Name} (v{mod.Version})");
            }

            // The message is a list containing a single string.
            var dialogue = new List<string>() {
                "StardewHack failed to apply some bytecode patches. The following mods won't work correctly or at all: " +
                mod_list.Join() +
                ". Check your console for further instructions."
            };
            
            // Create the dialogue box. We can't pass a string directly as the signature differs between the PC and android version.
            var box = new StardewValley.Menus.DialogueBox(dialogue);
            StardewValley.Game1.activeClickableMenu = box;
            StardewValley.Game1.dialogueUp = true;
            box.finishTyping();
        }
    }
}

