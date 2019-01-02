using Netcode;
using StardewModdingAPI;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace StardewHack.WearMoreRings
{
    using Farmer = StardewValley.Farmer;
    using Ring = StardewValley.Objects.Ring;
    using ActualRingsDict = Dictionary<long, ActualRings>;
    using SaveRingsDict   = Dictionary<long, SaveRings>;

    /// <summary>
    /// Structure used to store the actual rings.
    /// </summary> 
    public class ActualRings {
        public readonly NetRef<Ring> ring1 = new NetRef<Ring>(null);
        public readonly NetRef<Ring> ring2 = new NetRef<Ring>(null);
        public readonly NetRef<Ring> ring3 = new NetRef<Ring>(null);
        public readonly NetRef<Ring> ring4 = new NetRef<Ring>(null);
        
        public void LoadRings(SaveRings sr) {
            ring1.Set(MakeRing(sr.which1));
            ring2.Set(MakeRing(sr.which2));
            ring3.Set(MakeRing(sr.which3));
            ring4.Set(MakeRing(sr.which4));
        }
        
        private Ring MakeRing(int which) {
            if (which < 0) return null;
            return new Ring(which);
        }
    }
    
    /// <summary>
    /// Structure for save data.
    /// </summary>
    public class SaveRings {
        public int which1;
        public int which2;
        public int which3;
        public int which4;

        public SaveRings() { }
        
        public SaveRings(ActualRings er) {
            which1 = getWhich(er.ring1);
            which2 = getWhich(er.ring2);
            which3 = getWhich(er.ring3);
            which4 = getWhich(er.ring4);
        }
        
        private int getWhich(Ring r) {
            if (r==null) return -1;
            return r.ParentSheetIndex;
        }
    }

    public class ModEntry : Hack<ModEntry>
    {
        static ActualRingsDict actualdata = new ActualRingsDict();
        
        public override void Entry(IModHelper helper) {
            base.Entry(helper);
            
            helper.Events.GameLoop.Saved += GameLoop_Saved;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += GameLoop_ReturnedToTitle;
            
        }

        /// <summary>
        /// Serializes the worn extra rings to disk.
        /// </summary>
        void GameLoop_Saved(object sender, StardewModdingAPI.Events.SavedEventArgs e) {
            var savedata = new SaveRingsDict();
            foreach(KeyValuePair<long, ActualRings> entry in actualdata) {
                savedata[entry.Key] = new SaveRings(entry.Value);
            }
            Helper.Data.WriteSaveData("extra-rings", savedata);
            Monitor.Log("Saved extra rings data.");
        }

        /// <summary>
        /// Reads the saved extra rings and creates them.
        /// </summary>
        void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e) {
            var savedata = Helper.Data.ReadSaveData<SaveRingsDict>("extra-rings");
            if (savedata == null) {
                Monitor.Log("No save data loaded. Mod was probably added since last save.");
                return;
            }
            foreach(KeyValuePair<long, SaveRings> entry in savedata) {
                actualdata[entry.Key].LoadRings(entry.Value);
            }
            Monitor.Log("Loaded extra rings save data.");
        }

        /// <summary>
        /// Clears the actual rings dictionary to prevent memory leaking.
        /// </summary>
        void GameLoop_ReturnedToTitle(object sender, StardewModdingAPI.Events.ReturnedToTitleEventArgs e) {
            actualdata = new ActualRingsDict();
        }
        
        /// <summary>
        /// Add the extra rings to the Netcode tree.
        /// </summary>
        public static void InitFarmer(Farmer f) {
            var actualrings = new ActualRings();
            f.NetFields.AddFields(actualrings.ring1,actualrings.ring2,actualrings.ring3,actualrings.ring4);
            actualdata[f.UniqueMultiplayerID] = actualrings;
        }

        [BytecodePatch("StardewValley.Farmer::farmerInit")]
        void Tree_DayUpdate() {
            var addfields = FindCode(
                OpCodes.Stelem_Ref,
                Instructions.Callvirt(typeof(NetFields), "AddFields", typeof(INetSerializable[]))
            );
            addfields.Append(
                Instructions.Ldarg_0(),
                Instructions.Call(typeof(ModEntry), "InitFarmer", typeof(Farmer))
            );
        }
    }
}

