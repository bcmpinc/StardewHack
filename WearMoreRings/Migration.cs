﻿using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;

namespace StardewHack.WearMoreRings
{
    using OldSaveRingsDict   = Dictionary<long, OldSaveRings>;

    /// <summary>
    /// Old structure for save data.
    /// Used to migrate to new chest based storage.
    /// </summary>
    public class OldSaveRings {
        public int which1;
        public int which2;
        public int which3;
        public int which4;
    }
    
    static class Migration {
#region extra-rings-data
        static private Ring MakeRing(int which) {
            if (which < 0) return null;
            try {
                return new Ring(which);
            } catch {
                // Ring no longer exists, so delete it.
                ModEntry.getInstance().Monitor.Log($"Failed to create ring with id {which}.", LogLevel.Warn);
                return null;
            }
        }
        static private void ImportExtraRingsData(IMonitor monitor, IModHelper helper) {
            if (!Game1.IsMasterGame) return;

            // Load data from mod's save file, if available.
            var savedata = helper.Data.ReadSaveData<OldSaveRingsDict>("extra-rings");
            if (savedata == null) {
                monitor.Log("Deprecated save data not available.");
                return;
            }
            
            monitor.Log("Migrating old save data");
            // Iterate through each farmer to load the extra equipped rings.
            foreach(Farmer f in Game1.getAllFarmers()) {
                if (savedata.ContainsKey(f.UniqueMultiplayerID)) {
                    var data = savedata[f.UniqueMultiplayerID];
                    Utility.CollectOrDrop(MakeRing(data.which1));
                    Utility.CollectOrDrop(MakeRing(data.which2));
                    Utility.CollectOrDrop(MakeRing(data.which3));
                    Utility.CollectOrDrop(MakeRing(data.which4));
                }
            }
            helper.Data.WriteSaveData<OldSaveRingsDict>("extra-rings", null);
        }
#endregion

#region Ring Chest
        private const string DATA_KEY = "bcmpinc.WearMoreRings/chest-id";
        private static Vector2 getPositionFromId(int id) {
            return new Vector2(id, -50);
        }
        /// <summary>
        /// Destroy Ring chest with given ID.
        /// </summary>
        /// <returns>Whether the chest was successfully destroyed.</returns>
        public static bool DestroyChest(IMonitor monitor, int id) {
            Farm farm = Game1.getFarm();
            if (!farm.objects.ContainsKey(getPositionFromId(id))) return false;

            var existing_chest = farm.objects[getPositionFromId(id)];
            if (!(existing_chest is Chest)) return false;

            monitor.Log(string.Format("Destroying ring chest with id {0}.", id), LogLevel.Trace);
            var inv = (existing_chest as Chest).items;
                        
            for (var slot = 0; slot < inv.Count; slot++) {
                Utility.CollectOrDrop(inv[slot]);
                inv[slot] = null;
            }

            farm.objects.Remove(getPositionFromId(id));
            return true;
        }

        private static void ImportRingChest(IMonitor monitor) {
            var farmer = Game1.player;
            
            // Check whether the farmer has a chest assigned.
            if (!farmer.modData.ContainsKey(DATA_KEY)) return;
            var id = int.Parse(farmer.modData[DATA_KEY]);
            monitor.Log($"Farmer {farmer.Name} has old ring chest {id}.");
            farmer.modData.Remove(DATA_KEY);

            ResetModifiers(monitor, farmer);

            // Try to destroy the chest.
            if (DestroyChest(monitor, id)) return;

            monitor.Log("Chest went missing!", LogLevel.Warn);
        }

        public static void ResetModifiers(IMonitor monitor, Farmer who) {
            monitor.Log("Resetting modifiers for " + who.Name);
            who.ClearBuffs();

            who.leftRing.Value?.onUnequip(who, who.currentLocation);
            who.rightRing.Value?.onUnequip(who, who.currentLocation);
            who.boots.Value?.onUnequip();
            
            who.MagneticRadius = 128;
            who.knockbackModifier = 0;
            who.weaponPrecisionModifier = 0;
            who.critChanceModifier = 0;
            who.critPowerModifier = 0;
            who.weaponSpeedModifier = 0;
            who.attackIncreaseModifier = 0;
            who.resilience = 0;
            who.addedLuckLevel.Value = 0;
            who.immunity = 0;

            who.leftRing.Value?.onEquip(who, who.currentLocation);
            who.rightRing.Value?.onEquip(who, who.currentLocation);
            who.boots.Value?.onEquip();
        }


        public static void DestroyRemainingChests(IMonitor monitor) {
            int maxid = 100;
            for (int id=0; id < maxid; id++) {
                if (DestroyChest(monitor, id)) {
                    maxid = id + 100;
                }
            }
        }
#endregion

        /// <summary>
        /// Migration entry point.
        /// </summary>
        static public void Import(IMonitor monitor, IModHelper helper) {
            ImportExtraRingsData(monitor, helper);
            ImportRingChest(monitor);
        }
    }
}
