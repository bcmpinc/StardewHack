using StardewValley;
using StardewValley.Objects;
using System;
using System.Runtime.CompilerServices;

namespace StardewHack.WearMoreRings
{
    /// <summary>
    /// CombinedRing Wrapper which allows it to be used as a container that accepts empty slots (= null values).
    /// </summary>
    public class RingMap {
        private static readonly ConditionalWeakTable<Farmer,RingMap> player_table = new ConditionalWeakTable<Farmer, RingMap>();
        public static RingMap player_ringmap {
            get { 
                RingMap who = null;
                player_table.TryGetValue(Game1.player, out who);
                return who;                
            }
            set { 
                player_table.Add(Game1.player, value); 
            }
        }


        public const string RING_NAME = "Wear More Rings container ring (do not touch!)";
        public const string DATA_KEY = "bcmpinc.WearMoreRings/slot-map";
        public static int MAX_RINGS = 20;
        readonly Farmer who;
        readonly int[] slot_map = new int[MAX_RINGS];
        public readonly CombinedRing container;

        public RingMap(Farmer _who) {
            who = _who;
            for (int i=0; i<slot_map.Length; i++) slot_map[i] = -1;
            if (who.leftRing.Value is CombinedRing && who.rightRing.Value == null && Load()) {
                container = who.leftRing.Value as CombinedRing;
                int equipped = container.combinedRings.Count;

                // Reset any indices that are out of bounds
                for (int i=0; i<slot_map.Length; i++) {
                    if (slot_map[i] >= equipped) { 
                        ModEntry.getInstance().Monitor.Log("Slot_map contains invalid index "+i, StardewModdingAPI.LogLevel.Warn);
                        slot_map[i] = -1;
                    }
                }

                // Add any missing indices
                for (int i=0; i<equipped; i++) {
                    if (!Array.Exists(slot_map, val => val == i)) {
                        ModEntry.getInstance().Monitor.Log("Ring "+i+" missing from slot_map", StardewModdingAPI.LogLevel.Warn);
                        var new_pos = Array.FindIndex(slot_map, val => val < 0);
                        if (new_pos >= 0) {
                            slot_map[new_pos] = i;
                        } else {
                            ModEntry.getInstance().Monitor.Log("Failed to insert ring "+i, StardewModdingAPI.LogLevel.Error);
                        }
                    }
                }
            } else {
                // Create a new combined ring for storage.
                container = new CombinedRing(880);
                this[0] = who.leftRing.Value;
                this[1] = who.rightRing.Value;
                who.leftRing.Value = container;
                who.rightRing.Value = null;
            }
            container.DisplayName = RING_NAME;
            Save();
        }

        /// <summary>
        /// Drop all rings in slots numbered `capacity` or above.
        /// </summary>
        /// <param name="capacity"></param>
        public void limitSize(int capacity) {
            for (int i=capacity; i<slot_map.Length; i++) {
                if (slot_map[i] >= 0) { 
                    this[i].onUnequip(who, who.currentLocation);
                    Utility.CollectOrDrop(this[i]);
                    this[i] = null;
                }
            }
        }

        public Ring this[int index] {
            get {
                var pos = slot_map[index];
                if (pos < 0)
                    return null;
                return container.combinedRings[pos];
            }
            set {
                // Prevent recursion.
                if (value == container) {
                    throw new Exception("Really, don't touch the WMR container ring please!");
                }

                // Equip the ring
                var pos = slot_map[index];
                if (pos < 0) {
                    if (value == null) return; // Nothing changed.
                    slot_map[index] = container.combinedRings.Count;
                    container.combinedRings.Add(value);
                } else {
                    if (value == null) {
                        container.combinedRings.RemoveAt(pos);
                        slot_map[index] = -1;
                        for (int i=0; i<slot_map.Length; i++) {
                            if (slot_map[i] > pos) slot_map[i]--;
                        }
                    } else {
                        container.combinedRings[pos] = value;
                    }
                }
                Save();
            }
        }

        public bool AddRing(int slot_hint, Ring r) {
            var new_pos = slot_hint;
            if (slot_map[slot_hint] >= 0) {
                // Hint already occupied, find empty slot.
                new_pos = Array.FindIndex(slot_map, val => val < 0);
            }
            if (new_pos < 0 || new_pos >= ModEntry.getConfig().Rings) return false;
            r.onEquip(who, who.currentLocation);
            this[new_pos] = r;
            return true;
        }

        public bool Load() {
            if (who.modData.ContainsKey(DATA_KEY)) {
                var data = who.modData[DATA_KEY].Split(",");
                for (int i=0; i<Math.Min(MAX_RINGS, data.Length); i++) {
                    slot_map[i] = int.Parse(data[i]);
                }
                return true;
            } else {
                return false;
            }
        }

        public void Save() {
            who.modData[DATA_KEY] = string.Join(",", slot_map);
        }
    }
}
