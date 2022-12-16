using StardewValley;
using StardewValley.Objects;
using System;

namespace StardewHack.WearMoreRings
{
    /// <summary>
    /// CombinedRing Wrapper which allows it to be used as a container that accepts empty slots (= null values).
    /// </summary>
    public class RingMap {
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
                container = new CombinedRing(880);
                this[0] = who.leftRing.Value;
                this[1] = who.rightRing.Value;
                who.leftRing.Value = container;
                who.rightRing.Value = null;
            }
        }

        /// <summary>
        /// Drop all rings in slots numbered `capacity` or above.
        /// </summary>
        /// <param name="capacity"></param>
        public void limitSize(int capacity) {
            for (int i=capacity; i<slot_map.Length; i++) {
                if (slot_map[i] >= 0) { 
                    this[i].onUnequip(Game1.player, Game1.player.currentLocation);
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
                var pos = slot_map[index];
                if (pos < 0) {
                    if (value == null) return;
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
            }
        }

        public bool AddRing(Ring r) {
            var new_pos = Array.FindIndex(slot_map, val => val < 0);
            if (new_pos < 0) return false;
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
