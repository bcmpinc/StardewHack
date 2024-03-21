using System;

namespace StardewHack.WearMoreRings
{
    public interface IWearMoreRingsAPI_2 {
        /// <summary>
        /// Get the mod's config setting for how many rings can be equipped.
        /// 
        /// Note that this value is not synchronized in multiplayer, so its only valid for the current player (Game1.player).
        /// </summary>
        /// <returns>Config setting for how many rings the local player can wear.</returns>
        int RingSlotCount();

        /// <summary>
        /// Get the ring that the local player has equipped in the given slot. 
        /// </summary>
        /// <param name="slot">The ring equipment slot being queried. Ranging from 0 to RingSlotCount()-1.</param>
        /// <returns>The ring equipped in the given slot or null if its empty.</returns>
        StardewValley.Objects.Ring GetRing(int slot);

        /// <summary>
        /// Equip a new ring in the given slot. Note that this overwrites the previous ring in that slot. Use null to remove the ring.
        /// </summary>
        /// <param name="slot">The ring equipment slot being queried. Ranging from 0 to RingSlotCount()-1.</param>
        /// <param name="ring">The new ring being equipped. Can be null to unequip the current ring.</param>
        void SetRing(int slot, StardewValley.Objects.Ring ring);

    }
}
