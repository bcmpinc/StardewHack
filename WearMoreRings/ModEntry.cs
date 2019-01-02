using Netcode;
using System.Collections.Generic;
using StardewModdingAPI;

namespace StardewHack.WearMoreRings
{
    using Ring = StardewValley.Objects.Ring;
    using ActualRingsDict = Dictionary<long, ActualRings>;
    using SaveRingsDict   = Dictionary<long, SaveRings>;

    /// <summary>
    /// Structure used to store the actual rings.
    /// </summary> 
    public class ActualRings {
        public NetRef<Ring> ring1;
        public NetRef<Ring> ring2;
        public NetRef<Ring> ring3;
        public NetRef<Ring> ring4;
    }
    
    /// <summary>
    /// Structure for save data.
    /// </summary>
    public class SaveRings {
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
        public int which1;
        public int which2;
        public int which3;
        public int which4;
    }

    public class ModEntry : Hack<ModEntry>
    {
        static ActualRingsDict playerdict = new ActualRingsDict();
        
        public override void Entry(IModHelper helper) {
            base.Entry(helper);
            
            helper.Events.GameLoop.SaveCreated += GameLoop_SaveCreated;
            helper.Events.GameLoop.Saved += GameLoop_Saved;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += GameLoop_ReturnedToTitle;;
            
        }

        void GameLoop_SaveCreated(object sender, StardewModdingAPI.Events.SaveCreatedEventArgs e) {
            
        }

        void GameLoop_Saved(object sender, StardewModdingAPI.Events.SavedEventArgs e) {
            var savedata = new SaveRingsDict();
            foreach(KeyValuePair<long, ActualRings> entry in playerdict) {
                savedata[entry.Key] = new SaveRings(entry.Value);
            }
            Helper.Data.WriteSaveData("extra-rings", playerdict);
        }

        void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e) {
            var savedata = Helper.Data.ReadSaveData<SaveRingsDict>("extra-rings");
        }

        /// <summary>
        /// Games the loop returned to title.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        void GameLoop_ReturnedToTitle(object sender, StardewModdingAPI.Events.ReturnedToTitleEventArgs e) {
            playerdict = new ActualRingsDict();
        }

    }
}

