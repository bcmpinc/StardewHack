using System.Collections.Generic;
using System.Reflection.Emit;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Menus;
using StardewValley;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Objects;
using StardewHack;

namespace BiggerBackpack
{
    public class Mod : StardewHack.Hack<Mod>
    {
        public static Mod instance;

        private Texture2D bigBackpack;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            base.Entry(helper);
            bigBackpack = Helper.Content.Load<Texture2D>("backpack.png");

            helper.Events.Display.MenuChanged += onMenuChanged;
            helper.Events.Display.RenderingHud += onRenderingHud;
            helper.Events.Input.ButtonPressed += onButtonPressed;

            Helper.ConsoleCommands.Add("player_setbackpacksize", "Set the size of the player's backpack.", command);
        }

        private void command( string cmd, string[] args )
        {
            if (args.Length != 1)
            {
                Monitor.Log("Must have one command argument", LogLevel.Info);
                return;
            }

            int newMax = int.Parse(args[0]);
            if (newMax < Game1.player.MaxItems)
            {
                for (int i = Game1.player.MaxItems - 1; i >= newMax; --i)
                    Game1.player.Items.RemoveAt(i);
            }
            else
            {
                for (int i = Game1.player.Items.Count; i < Game1.player.MaxItems; ++i)
                    Game1.player.Items.Add(null);
            }
            Game1.player.MaxItems = int.Parse(args[0]);
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void onRenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (Game1.currentLocation.Name == "SeedShop" && Game1.player.MaxItems == 36)
            {
                e.SpriteBatch.Draw(bigBackpack, Game1.GlobalToLocal(new Vector2(7 * Game1.tileSize + Game1.pixelZoom * 2, 17 * Game1.tileSize)), new Rectangle(0, 0, 12, 14), Color.White, 0.0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, (float)(19.25 * Game1.tileSize / 10000.0));
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void onButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button.IsActionButton() && !this.Helper.Input.IsSuppressed(e.Button))
            {
                if (Game1.player.MaxItems == 36 && Game1.currentLocation.Name == "SeedShop" && e.Cursor.Tile.X == 7 && (e.Cursor.Tile.Y == 17 || e.Cursor.Tile.Y == 18) )
                {
                    this.Helper.Input.Suppress(e.Button);
                    Response yes = new Response("Purchase", "Purchase (50,000g)");
                    Response no = new Response("Not", Game1.content.LoadString("Strings\\Locations:SeedShop_BuyBackpack_ResponseNo"));
                    Response[] resps = new Response[] { yes, no };
                    Game1.currentLocation.createQuestionDialogue("Backpack Upgrade -- 48 slots", resps, "spacechase0.BiggerBackpack");
                }
            }
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void onMenuChanged(object sender, MenuChangedEventArgs e)
        {
            // on closed
            if (Context.IsWorldReady && e.OldMenu is DialogueBox)
            {
                if (Game1.currentLocation.lastQuestionKey == "spacechase0.BiggerBackpack" && prevSelResponse == 0)
                {
                    if (Game1.player.Money >= 50000)
                    {
                        Game1.player.Money -= 50000;
                        Game1.player.MaxItems += 12;
                        for (int index = 0; index < Game1.player.MaxItems; ++index)
                        {
                            if (Game1.player.Items.Count <= index)
                                Game1.player.Items.Add((Item)null);
                        }
                        Game1.player.holdUpItemThenMessage((Item)new SpecialItem(99, "Premium Pack"), true);
                    }
                    else
                        Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney2"));
                }

                Helper.Events.GameLoop.UpdateTicked -= watchSelectedResponse;
                prevSelResponse = -1;
            }

            // on new menu
            switch (e.NewMenu)
            {
                case DialogueBox _:
                    Helper.Events.GameLoop.UpdateTicked += watchSelectedResponse;
                    break;
            }
        }

        int prevSelResponse = -1;

        /// <summary>Raised after the game state is updated (≈60 times per second), while waiting for a dialogue response.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void watchSelectedResponse(object sender, UpdateTickedEventArgs e)
        {
            if (Game1.activeClickableMenu is DialogueBox db)
            {
                int sel = Helper.Reflection.GetField<int>(db, "selectedResponse").GetValue();
                if (sel != -1)
                    prevSelResponse = sel;
            }
        }
        
        public static void shiftIconsDown(List<ClickableComponent> equipmentIcons){
            foreach (var icon in equipmentIcons) {
                icon.bounds.Y += Game1.tileSize;
            }
        }
        
        void resize_inventory() {
            // Change inventory size from default (36) to 48
            var inv = FindCode(
                OpCodes.Ldc_I4_M1,  // Size (-1 = default)
                OpCodes.Ldc_I4_3,   // Rows
                OpCodes.Ldc_I4_0,
                OpCodes.Ldc_I4_0,
                OpCodes.Ldc_I4_1
            );
            inv[0] = Instructions.Ldc_I4(48);
            inv[1] = Instructions.Ldc_I4_4();
        }
        
        [BytecodePatch("StardewValley.Menus.InventoryPage::.ctor")]
        void InventoryPage_ctor() {
            BeginCode().Prepend(
                // height += Game1.tileSize;
                Instructions.Ldarg_S(4),
                Instructions.Ldc_I4_S(Game1.tileSize),
                Instructions.Add(),
                Instructions.Starg_S(4)
            );
            
            resize_inventory();
            
            EndCode().Insert(-1,
                // Shift icons down by `Game1.tileSize` pixels
                Instructions.Ldarg_0(),
                Instructions.Ldfld(typeof(InventoryPage), "equipmentIcons"),
                Instructions.Call(GetType(), "shiftIconsDown", typeof(List<ClickableComponent>))
            );
            
            try {
                // Move portrait `Game1.tileSize` pixels down.
                // This only affects where the tooltip shows up.
                FindCode(
                    OpCodes.Ldarg_0,
                    Instructions.Ldfld(typeof(IClickableMenu), "yPositionOnScreen"),
                    Instructions.Ldsfld(typeof(IClickableMenu), "borderWidth"),
                    OpCodes.Add,
                    Instructions.Ldsfld(typeof(IClickableMenu), "spaceToClearTopBorder"),
                    OpCodes.Add,
                    Instructions.Ldc_I4(256),
                    OpCodes.Add,
                    Instructions.Ldc_I4_8(),
                    OpCodes.Sub,
                    Instructions.Ldc_I4_S(64),
                    OpCodes.Add
                )[6].operand = 256 + Game1.tileSize;
            } catch (System.Exception err) {
                Monitor.Log("Failed to fix portrait tooltip position.", LogLevel.Warn);
                LogException(err, LogLevel.Warn);
            }
        }

        [BytecodePatch("StardewValley.Menus.InventoryPage::draw")]
        void InventoryPage_draw() {
            var code = BeginCode();
            
            // var yoffset = yPositionOnScreen + borderWidth + spaceToClearTopBorder + Game1.tileSize
            var yoffset = generator.DeclareLocal(typeof(int));
            code.Prepend(
                Instructions.Ldarg_0(),
                Instructions.Ldfld(typeof(IClickableMenu), "yPositionOnScreen"),
                Instructions.Ldsfld(typeof(IClickableMenu), "borderWidth"),
                Instructions.Add(),
                Instructions.Ldsfld(typeof(IClickableMenu), "spaceToClearTopBorder"),
                Instructions.Add(),
                Instructions.Ldc_I4_S(Game1.tileSize),
                Instructions.Add(),
                Instructions.Stloc_S(yoffset)
            );
            
            // Replace all remaining `yPositionOnScreen + borderWidth + spaceToClearTopBorder` by `yoffset`.
            for (var i=0; i<12; i++) {
                code = code.FindNext(
                    OpCodes.Ldarg_0,
                    Instructions.Ldfld(typeof(IClickableMenu), "yPositionOnScreen"),
                    Instructions.Ldsfld(typeof(IClickableMenu), "borderWidth"),
                    OpCodes.Add,
                    Instructions.Ldsfld(typeof(IClickableMenu), "spaceToClearTopBorder"),
                    OpCodes.Add
                );
                code.Replace(
                    Instructions.Ldloc_S(yoffset)
                );
            }
        }

        [BytecodePatch("StardewValley.Menus.CraftingPage::.ctor")]
        void CraftingPage_ctor() {
            // Make the crafting page a bit higher too, to accomodate the bigger inventory.
            BeginCode().Prepend(
                // height += Game1.tileSize;
                Instructions.Ldarg_S(4),
                Instructions.Ldc_I4_S(Game1.tileSize),
                Instructions.Add(),
                Instructions.Starg_S(4)
            );
            
            resize_inventory();
        }
        
        [BytecodePatch("StardewValley.Menus.ShopMenu::.ctor(System.Collections.Generic.List<StardewValley.Item>,System.Int32,System.String)")]
        void ShopMenu_ctor() {
            resize_inventory();
            
            var code = BeginCode();
            for (int i=0; i<2; i++) {
                code = code.FindNext(
                    Instructions.Ldc_I4(600),
                    Instructions.Ldsfld(typeof(IClickableMenu), "borderWidth"),
                    Instructions.Ldc_I4_2(),
                    OpCodes.Mul,
                    OpCodes.Add
                );
                code[0].operand = 600 + Game1.tileSize;
            }
            
            // Fix the size of the shop buttons.
            // Replace `((height - 256) / 4)` with 106
            for (int i=0; i<2; i++) {
                code = FindCode(
                    OpCodes.Ldarg_0,
                    Instructions.Ldfld(typeof(IClickableMenu), "height"),
                    Instructions.Ldc_I4(256),
                    OpCodes.Sub,
                    Instructions.Ldc_I4_4(),
                    OpCodes.Div
                );
                code.Replace(
                    Instructions.Ldc_I4(106)
                );
            }
        }
        
        [BytecodePatch("StardewValley.Menus.ShopMenu::draw")]
        void ShopMenu_draw() {
            // Position the inventory background
            // Change `yPositionOnScreen + height - 256 + 40` to `yPositionOnScreen + 464`
            // Note: originally height = 680.
            FindCode(
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(IClickableMenu), "yPositionOnScreen"),
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(IClickableMenu), "height"),
                OpCodes.Add,
                Instructions.Ldc_I4(256),
                OpCodes.Sub,
                Instructions.Ldc_I4_S(40),
                OpCodes.Add
            ).SubRange(2,6).Replace(
                Instructions.Ldc_I4(464)
            );
            // Change `height - 448 + 20` to `inventory.height + 44`
            // Note: originally inventory.height = 3*64+16 = 208.
            FindCode(
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(IClickableMenu), "height"),
                Instructions.Ldc_I4(448),
                OpCodes.Sub,
                Instructions.Ldc_I4_S(20),
                OpCodes.Add
            ).Replace(
                Instructions.Ldarg_0(),
                Instructions.Ldfld(typeof(ShopMenu), "inventory"),
                Instructions.Ldfld(typeof(IClickableMenu), "height"),
                Instructions.Ldc_I4_S(44),
                Instructions.Add()
            );
            
            // Position the shop stock background
            // Change `height - 256 + 32 + 4` to 460.
            FindCode(
                OpCodes.Ldarg_0,
                Instructions.Ldfld(typeof(IClickableMenu), "height"),
                Instructions.Ldc_I4(256),
                OpCodes.Sub,
                Instructions.Ldc_I4_S(32),
                OpCodes.Add,
                Instructions.Ldc_I4_4(),
                OpCodes.Add
            ).Replace(
                Instructions.Ldc_I4(460)
            );
        }
        
        [BytecodePatch("StardewValley.Menus.MenuWithInventory::.ctor")]
        void ShippingMenu_ctor() {
            resize_inventory();
            
            var code = BeginCode();
            for (int i=0; i<2; i++) {
                code = code.FindNext(
                    Instructions.Ldc_I4(600),
                    Instructions.Ldsfld(typeof(IClickableMenu), "borderWidth"),
                    Instructions.Ldc_I4_2(),
                    OpCodes.Mul,
                    OpCodes.Add
                );
                code[0].operand = 600 + Game1.tileSize;
            }
        }
    }
}
