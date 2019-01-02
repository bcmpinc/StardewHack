using Netcode;
using StardewValley;
using StardewModdingAPI;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Ring = StardewValley.Objects.Ring;

namespace StardewHack.WearMoreRings
{
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
        static readonly ConditionalWeakTable<Farmer, ActualRings> actualdata = new ConditionalWeakTable<Farmer, ActualRings>();
        static IMonitor mon;
        
        public override void Entry(IModHelper helper) {
            base.Entry(helper);
            
            helper.Events.GameLoop.Saving += GameLoop_Saving;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            
            mon = Monitor;
        }
        
        static ActualRings FarmerNotFound(Farmer f) {
            throw new System.Exception("ERROR: A Farmer object was not correctly registered with the 'WearMoreRings' mod.");
        }
        
        /// <summary>
        /// Serializes the worn extra rings to disk.
        /// </summary>
        void GameLoop_Saving(object sender, StardewModdingAPI.Events.SavingEventArgs e) {
            var savedata = new SaveRingsDict();
            foreach(Farmer f in Game1.getAllFarmers()) {
                savedata[f.UniqueMultiplayerID] = new SaveRings(actualdata.GetValue(f, FarmerNotFound));
            }
            Helper.Data.WriteSaveData("extra-rings", savedata);
            Monitor.Log("Saved extra rings data.");
        }

        /// <summary>
        /// Reads the saved extra rings and creates them.
        /// </summary>
        void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e) {
            // Load data from mod's save file, if available.
            var savedata = Helper.Data.ReadSaveData<SaveRingsDict>("extra-rings");
            if (savedata == null) {
                Monitor.Log("Save data not available.");
                return;
            }
            // Iterate through each farmer to load the extra equipped rings.
            foreach(Farmer f in Game1.getAllFarmers()) {
                if (savedata.ContainsKey(f.UniqueMultiplayerID)) {
                    actualdata.GetValue(f, FarmerNotFound).LoadRings(savedata[f.UniqueMultiplayerID]);
                }
            }
            Monitor.Log("Loaded extra rings save data.");
        }
        
        /// <summary>
        /// Add the extra rings to the Netcode tree.
        /// </summary>
        public static void InitFarmer(Farmer f) {
            var actualrings = new ActualRings();
            f.NetFields.AddFields(
                actualrings.ring1,
                actualrings.ring2,
                actualrings.ring3,
                actualrings.ring4
            );
            actualdata.Add(f, actualrings);
        }

        [BytecodePatch("StardewValley.Farmer::farmerInit")]
        void Farmer_farmerInit() {
            var addfields = FindCode(
                OpCodes.Stelem_Ref,
                Instructions.Callvirt(typeof(NetFields), "AddFields", typeof(INetSerializable[]))
            );
            addfields.Append(
                Instructions.Ldarg_0(),
                Instructions.Call(typeof(ModEntry), "InitFarmer", typeof(Farmer))
            );
        }
        
        public static int CountWearingRing(Farmer f, int id) {
            bool IsRing(Ring r) {
                return r != null && r.parentSheetIndex == id;
            }
        
            ActualRings ar = actualdata.GetValue(f, FarmerNotFound);
            int res = 0;
            if (IsRing(f.leftRing)) res++;
            if (IsRing(f.rightRing)) res++;
            if (IsRing(ar.ring1)) res++;
            if (IsRing(ar.ring2)) res++;
            if (IsRing(ar.ring3)) res++;
            if (IsRing(ar.ring4)) res++;
            return res;
        }

        [BytecodePatch("StardewValley.Farmer::isWearingRing")]
        void Farmer_isWearingRing() {
            AllCode().Replace(
                Instructions.Ldarg_0(),
                Instructions.Ldarg_1(),
                Instructions.Call(typeof(ModEntry), "CountWearingRing", typeof(Farmer), typeof(int)),
                Instructions.Ret()
            );
        }

        public static void UpdateRings(Microsoft.Xna.Framework.GameTime time, GameLocation location, Farmer f) {
            void update(Ring r) { 
                if (r != null) r.update(time, location, f); 
            };
            
            ActualRings ar = actualdata.GetValue(f, FarmerNotFound);
            update(f.leftRing);
            update(f.rightRing);
            update(ar.ring1);
            update(ar.ring2);
            update(ar.ring3);
            update(ar.ring4);
        }
        
        [BytecodePatch("StardewValley.Farmer::updateCommon")]
        void Farmer_updateCommon() {
            FindCode(
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(Farmer), "rightRing"),
                OpCodes.Callvirt,
                OpCodes.Brfalse,
                
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(Farmer), "rightRing"),
                OpCodes.Callvirt,
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldarg_0,
                OpCodes.Callvirt,
                
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(Farmer), "leftRing"),
                OpCodes.Callvirt,
                OpCodes.Brfalse,
                
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(Farmer), "leftRing"),
                OpCodes.Callvirt,
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldarg_0,
                OpCodes.Callvirt
            ).Replace(
                Instructions.Ldarg_1(),
                Instructions.Ldarg_2(),
                Instructions.Ldarg_0(),
                Instructions.Call(typeof(ModEntry), "UpdateRings", typeof(Microsoft.Xna.Framework.GameTime), typeof(GameLocation), typeof(Farmer)),
                Instructions.Ret()
            );
        }

    }
}

