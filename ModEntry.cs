using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Menus;
using HarmonyLib;
using SObject = StardewValley.Object;
using GenericModConfigMenu;
using StardewModdingAPI.Utilities;

namespace Instant_Community_Center_Cheat
{
    //todo verify the check for when the CC is complete is valid
    //todo make it so the button to trigger the code is customizable
    //todo make a configurable thing where the items auttomatically get added to the correct slots
    //todo make this mod compatible with the remixed versions
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        private CommunityCenter? communityCenter;
        private CommunityCenter CommunityCenter => communityCenter ??= (CommunityCenter)Game1.getLocationFromName("CommunityCenter");
        

        //key is the area id in the CC
        //the value is the list of ids corresponding the the bundles within that area
        private Dictionary<int, int[]> areaBundles;

        //lewis unlocking the community center
        private const string community_center_unlocked_event_id = "611439";

        //wizard sends mail for player to talk to them
        private const string wizard_jumino_mail_id = "wizardJunimoNote";

        //player goes to wizard tower to read jumino text
        private const string wizard_jumino_event_id = "canReadJunimoText";

        /// <summary>The mod configuration.</summary>
        public ModConfig Config { get; set;  }


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // read config
            this.Config = helper.ReadConfig<ModConfig>();

            //todo make this dynamic by reading from Bundles.json
            areaBundles = new Dictionary<int, int[]>()
            {
                { CommunityCenter.AREA_Pantry, new int[]{ 0, 1, 2, 3, 4, 5  } },
                { CommunityCenter.AREA_CraftsRoom, new int[] { 13, 14, 15, 16, 17, 19 } },
                { CommunityCenter.AREA_FishTank, new int[] { 6, 7, 8, 9, 10, 11 } },
                { CommunityCenter.AREA_BoilerRoom, new int[] { 20, 21, 22 } },
                { CommunityCenter.AREA_Bulletin, new int[] { 31, 32, 33, 34, 35 } },
                { CommunityCenter.AREA_Vault, new int[] { 23, 24, 25, 26 } },
                { CommunityCenter.AREA_AbandonedJojaMart, new int[] { 36 } }
            };

            HarmonyPatches.Initialize(Monitor);

            var harmony = new Harmony(this.ModManifest.UniqueID);

            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Preconditions), nameof(StardewValley.Preconditions.Weather)),
               postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Weather_Postfix))
            );

            harmony.PatchAll();

            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }


        /*********
        ** Private methods
        *********/

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod

            Action reset = () => this.Config.GiveCCItemKey = ModConfig.DefaultGiveCCItemKey;
            Action save = () => this.Helper.WriteConfig(Config);

            configMenu.Register(this.ModManifest, reset, save);

            configMenu.AddKeybind(
                    mod: this.ModManifest,
                    name: () => "Give item key",
                    tooltip: () => "The key to press to get all the items to complete the Community Center",
                    getValue: () => this.Config.GiveCCItemKey,
                    setValue: value => this.Config.GiveCCItemKey = value
                );


            
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet or if the player is in a cutscene or have a menu open
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;



            if (new KeybindList(this.Config.GiveCCItemKey).JustPressed())
            {
                LogTrace($"Received get items key ({this.Config.GiveCCItemKey.ToString()})");
                GetItems();
            }
        }


        private void GetItems()
        {
            if (CommunityCenter != null)
            {
                //If the user cannot donate items to the community center yet,
                //say the steps the player currently needs to do in order to get in unlocked
                if (!Game1.player.hasOrWillReceiveMail(wizard_jumino_event_id))
                {
                    LogTrace("The player doesn't have the ability to donate items to the CC yet");
                    //the player has not watched the CC cutscene with Lewis
                    string popUpText;
                    if (!Utility.HasAnyPlayerSeenEvent(community_center_unlocked_event_id))
                    {
                        //it is spring 5th year 1, or onwards
                        if (Game1.year == 1 && Game1.season == Season.Spring && Game1.dayOfMonth < 5)
                        {
                            popUpText = "Wait until it's Spring 5 or onwards";
                            LogTrace("The player needs to wait until Spring 5 or onwards");

                        }
                        //it is not raining in the town
                        else if (Game1.locations.First(l => l is Town).IsRainingHere())
                        {
                            popUpText = "Wait until it's not raining";
                            LogTrace("The player needs to wait until it's not raining");
                        }
                        //it is between 8:00 am and 1:00 pm
                        else if (Game1.timeOfDay < 800 || Game1.timeOfDay > 1300)
                        {
                            popUpText = "Wait until it's between 8am and 1pm";
                            LogTrace("The player needs to wait until between 8am and 1pm");

                        }

                        //todo make a check to make sure there aren't any festivals going on in the town

                        //the player must enter Pelican Town from the Bus Stop
                        else
                        {
                            popUpText = "Enter the town through the bus station";
                            LogTrace("The player needs to wait until between 8am and 1pm");

                        }

                        Game1.showGlobalMessage(popUpText);
                        return;
                    }

                    //the player needs to interact with the note inside the CC
                    if (!Game1.player.hasOrWillReceiveMail(wizard_jumino_mail_id))
                    {
                        popUpText = "Interact with the plaque inside the Community Center";
                        LogTrace("The player needs to interact with the plaque inside the Community Center");

                    }

                    //if the mail will be recieved tomorrow, tell the player to go to bed
                    else if (Game1.player.mailForTomorrow.Contains(wizard_jumino_mail_id))
                    {
                        popUpText = "Go to bed to recieve mail from the wizard";
                        LogTrace("The player needs to go to bed to recieve mail from the wizard");

                    }

                    //the player needs to read the letter from the wizard
                    else if (!Game1.player.mailReceived.Contains(wizard_jumino_mail_id))
                    {
                        popUpText = "Read the letter from the wizard";
                        LogTrace("The player needs to read the letter from the wizard");

                    }

                    //the player needs to go to wizards tower and watch the cutscene
                    else
                    { 
                        popUpText = "Go to the wizard's tower";
                        LogTrace("The player needs to go to the wizard's tower");

                    }

                    Game1.showGlobalMessage(popUpText);
                    return;
                }

                bool communityCenterComplete = CommunityCenter.areAllAreasComplete();

                //Otherwise, if the community center isn't complete,
                if (!communityCenterComplete)
                {
                    //Get all the bundles that are not complete
                    //priortize the bundles that are currently avaible to the player (where the plaque is visuble)
                    //ignore the abandanoned joja
                    List<BundleModel> incompleteBundles = GetBundles(this.Monitor).Where(b => !CommunityCenter.isBundleComplete(b.ID) && !areaBundles[CommunityCenter.AREA_AbandonedJojaMart].Contains(b.ID))
                                                         .OrderByDescending(b => BundleAvaiable(b.ID)).ToList();

                    //In the list of incomplete bundles, check which items have been donated or not
                    List<BundleIngredientModel> requiredIngredients = new List<BundleIngredientModel>();
                    foreach (BundleModel incompleteBundle in incompleteBundles)
                    {
                        BundleIngredientModel[] allIngredients = incompleteBundle.Ingredients;
                        //get the slots needed for an bundle to be completed
                        int bundleSlots = incompleteBundle.Slots;
                        requiredIngredients.AddRange(allIngredients.Where(i => IsIngredientNeeded(incompleteBundle, i)).Take(bundleSlots));
                    }

                    //get all of the items the player to complete the Comunity Center.
                    List<Item> missingItems = requiredIngredients.Select(item => ItemRegistry.Create(item.ItemId, item.Stack, (int)item.Quality)).ToList();

                    //if there are any intances of items that have the same name and quality, combine them
                    for (int i = missingItems.Count - 1; i > 0; i--)
                    {
                        Item targetItem = missingItems[i];

                        List<Item> duplicateItems = missingItems.Where(item => item.Name == targetItem.Name && item.Quality == targetItem.Quality).ToList();


                        //if more than one item was found, combine all instances into one item and delete the others from the list
                        if (duplicateItems.Count > 1)
                        {
                            int itemCount = duplicateItems.Sum(item => item.Stack);
                            Item newItem = ItemRegistry.Create(targetItem.ItemId, itemCount, targetItem.Quality);

                            foreach (Item duplicateItem in duplicateItems)
                            {
                                missingItems.Remove(duplicateItem);
                            }

                            missingItems.Add(newItem);
                        }
                    }

                    Log($"Missing item names: {Join(missingItems.Select(i => $"{i.Stack} {(ItemQuality)i.Quality} {i.Name}"))}");

                    Inventory playerItems = Game1.player.Items;

                    //all the items added to player's inventory
                    List<Item> addedItems = new List<Item>();


                    LogTrace("Adding items to player's inventory...");
                    // For each item in that list:
                    foreach (Item item in missingItems)
                    {
                        LogTrace($"Attempting to add {item.Stack} {(ItemQuality)item.Quality} {item.DisplayName}...");
                        //if the the item was found within the player's inventory
                        bool foundItem = false;

                        for (int i = 0; i < playerItems.Count; i++)
                        {
                            Item? playerItem = playerItems[i];

                            if (playerItem == null)
                            {
                                continue;
                            }

                            bool sameId = playerItem.QualifiedItemId == item.QualifiedItemId;
                            bool validQuality = playerItem.quality.Value >= item.quality.Value;
                            bool enoughStack = playerItem.Stack >= item.Stack;

                            // If the player has the item (of the same required quality or higher) and has at least the necessary stack requirement, move on to the next item
                            if (sameId && validQuality && enoughStack)
                            {
                                foundItem = true;
                                LogTrace("The player already has enough of this item in their inventory");
                                break;
                            }
                            // If the player has the item (of the same required quality or higher) and doesn't have the stack requirment, add the missing number to that stack
                            else if (sameId && validQuality)
                            {
                                int missingCount = Math.Abs(playerItem.Stack - item.Stack);
                                playerItem.Stack += missingCount;
                                foundItem = true;
                                addedItems.Add(ItemRegistry.Create(item.QualifiedItemId, missingCount, item.Quality));
                                LogTrace($"The player already has {item.Stack} of this item in their inventory. Added {missingCount} more");
                                break;
                            }
                        }

                        // If the player does not have this item in their inventory, and they have room for the item to be there,
                        // add the item with the correct stack quantity
                        if (!foundItem && playerItems.HasEmptySlots())
                        {
                            Game1.player.addItemsByMenuIfNecessary(new List<Item> { item });
                            addedItems.Add(ItemRegistry.Create(item.QualifiedItemId, item.Stack, item.Quality));
                            LogTrace($"Added the item to the player's inventory");
                        }

                        else
                        { 
                            LogTrace($"The player doesn't have enough space in their invetory to add this item");
                        }
                    }

                    //When all items are given, send a message to say which items were added
                    Game1.showGlobalMessage("Added items to inventory");
                    LogTrace("Finished adding items to player's inventory");

                    //Check if there are any money bundles
                    LogTrace("Checking if the player needs money...");

                    //Note: this is a bug where all of these ids are always considered incomplete (even when the vault area is considered complete)
                    List<bool> bundlesComplete = areaBundles[CommunityCenter.AREA_Vault].Select(id => CommunityCenter.isBundleComplete(id)).ToList();
                    Log($"Vault bundle ids: {Join(areaBundles[CommunityCenter.AREA_Vault].Select(id => id))}");
                    Log($"Complete money bundles {Join(bundlesComplete)}");
                    Log($"Vault complete: {Game1.player.hasOrWillReceiveMail("ccVault").ToString()}");
                    List<int> moneyBundles =  areaBundles[CommunityCenter.AREA_Vault].Where(id => !CommunityCenter.isBundleComplete(id)).Select(id => GetBundleMoneyValue(id)).ToList();

                    if (moneyBundles.Count > 0)
                    {
                        LogTrace($"Player needs to donate to the following bundles: {Join(moneyBundles)}");
                        LogTrace($"The player has {Game1.player.Money} gold");

                        int moneyRequired = moneyBundles.Sum();

                        if (moneyRequired > Game1.player.Money)
                        {
                            int moneyNeeded = moneyRequired - Game1.player.Money;
                            LogTrace($"Giving the {moneyNeeded} gold...");
                            Game1.player.Money += moneyNeeded;

                        }

                        else
                        {
                            LogTrace("The player has enough money to afford the bundles");
                        }

                    }

                    else
                    {
                        LogTrace("The player doesn't need to donate any money");
                    }

                }

                else
                { 
                    //todo If the community center is complete, send a message saying nothing else can be done with this mod
                    Game1.showGlobalMessage("Community Center is complete, no items to add");
                    LogTrace("Community Center is complete, no items to add");
                }
            }

            else
            {
                Log("Community center not found");
            }
        }

        //Todo make this dynamic by reading Bundles.json
        /// <summary>
        /// Get the money value of a specfic bundle
        /// </summary>
        /// <param name="bundleId">the id of the bundle</param>
        /// <returns></returns>
        private int GetBundleMoneyValue(int bundleId)
        {
            switch (bundleId)
            {
                case 23:
                    return 2500;
                case 24:
                    return 5000;
                case 25:
                    return 10000;
                case 26:
                    return 25000;
                default: 
                    return 0;
            }

            
        }

        private void Log(string message, LogLevel level = LogLevel.Debug)
        {
            this.Monitor.Log(message, level);
        }

        private void LogTrace(string message)
        {
            Log(message, LogLevel.Trace);
        }

        private string Join<T>(IEnumerable<T> collection, string separator = ", ")
        { 
            return string.Join(separator, collection);
        }


        //The following code below was taken straight from Pathoschild's Look Up Anything mod

        /// <summary>Read parsed data about the Community Center bundles.</summary>
        /// <param name="monitor">The monitor with which to log errors.</param>
        /// <remarks>Derived from the <see cref="StardewValley.Locations.CommunityCenter"/> constructor and <see cref="StardewValley.Menus.JunimoNoteMenu.openRewardsMenu"/>.</remarks>
        private IEnumerable<BundleModel> GetBundles(IMonitor monitor)
        {
            foreach ((string key, string? value) in Game1.netWorldState.Value.BundleData)
            {
                if (value is null)
                    continue;

                BundleModel bundle;
                try
                {
                    // parse key
                    string[] keyParts = key.Split('/');
                    string area = ArgUtility.Get(keyParts, 0);
                    int id = ArgUtility.GetInt(keyParts, 1);

                    // parse bundle info
                    string[] valueParts = value.Split('/');
                    string name = ArgUtility.Get(valueParts, Bundle.NameIndex);
                    string reward = ArgUtility.Get(valueParts, Bundle.RewardIndex);
                    string displayName = ArgUtility.Get(valueParts, Bundle.DisplayNameIndex);

                    // parse ingredients
                    List<BundleIngredientModel> ingredients = new List<BundleIngredientModel>();
                    string[] ingredientData = ArgUtility.SplitBySpace(ArgUtility.Get(valueParts, 2));
                    for (int i = 0; i < ingredientData.Length; i += 3)
                    {
                        int index = i / 3;
                        string itemID = ArgUtility.Get(ingredientData, i);
                        int stack = ArgUtility.GetInt(ingredientData, i + 1);
                        ItemQuality quality = ArgUtility.GetEnum<ItemQuality>(ingredientData, i + 2);
                        ingredients.Add(new BundleIngredientModel(index, itemID, stack, quality));
                    }

                    // create bundle
                    bundle = new BundleModel(
                        ID: id,
                        Name: name,
                        DisplayName: displayName,
                        Area: area,
                        RewardData: reward,
                        Ingredients: ingredients.ToArray(),
                        Slots: GetBundleSlotCount(id)
                    );
                }
                catch (Exception ex)
                {
                    monitor.LogOnce($"Couldn't parse community center bundle '{key}' due to an invalid format.\nRecipe data: '{value}'\nError: {ex}", LogLevel.Warn);
                    continue;
                }

                yield return bundle;
            }
        }

        //todo possibly replace this by looking at the function addJunimoNote
        /// <summary>
        /// Checks if the bundle is accessible to the player (regrldess if it's complete)
        /// </summary>
        /// <param name="bundleID"></param>
        /// <returns></returns>
        private bool BundleAvaiable(int bundleID)
        {
            //check which area the bundle belongs to
            int areaId = areaBundles.First(kv => kv.Value.Contains(bundleID)).Key;

            //check if the id is avaiable for the player
            return CommunityCenter.shouldNoteAppearInArea(areaId);
        }

        //todo try to make this dynamic with the game's code
        /// <summary>
        /// Get the number of items slots needed in order to complete the bundle
        /// </summary>
        /// <param name="id">the unique id of the bundle</param>
        /// <returns></returns>
        private int GetBundleSlotCount(int id)
        {
            switch (id)
            { 
                case 0: //Spring Crops
                    return 4;
                case 1: // Summer Crops
                    return 4;
                case 2: //Fall Crops
                    return 4;
                case 3: //Quality Crops
                    return 3;
                case 4: //Animal
                    return 5;
                case 5: //Artisan
                    return 6;
                case 13: //Spring Foraging
                    return 4;
                case 14: //Summer Foraging
                    return 3;
                case 15: //Fall Foraging
                    return 4;
                case 16: //Winter Foraging
                    return 4;
                case 17: //Construction
                    return 4;
                case 19: //Exotic Foraging
                    return 5;
                case 6: //River Fish
                    return 4;
                case 7: //Lake Fish
                    return 4;
                case 8: //Ocean Fish
                    return 4;
                case 9: //Night Fishing
                    return 3;
                case 10: //Specialty Fish
                    return 4;
                case 11: //Crab Pot
                    return 5;
                case 20: //Blacksmith's
                    return 3;
                case 21: //Geologist's
                    return 4;
                case 22: //Adventurer's
                    return 2;
                case 31: //Chef's
                    return 6;
                case 32: //Field Research
                    return 4;
                case 33: //Enchanter's
                    return 3;
                case 34: //Dye
                    return 6;
                case 35: //Fodder
                    return 3;
                case 36: //The Missing
                    return 5;
            }
            return int.MinValue;
        }

        /// <summary>Get whether an ingredient is still needed for a bundle.</summary>
        /// <param name="bundle">The bundle to check.</param>
        /// <param name="ingredient">The ingredient to check.</param>
        private bool IsIngredientNeeded(BundleModel bundle, BundleIngredientModel ingredient)
        {
            CommunityCenter communityCenter = Game1.locations.OfType<CommunityCenter>().First();

            // handle rare edge case where item is required in the bundle data, but it's not
            // present in the community center data. This seems to be caused by some mods like
            // Challenging Community Center Bundles in some cases.
            if (!communityCenter.bundles.TryGetValue(bundle.ID, out bool[] items) || ingredient.Index >= items.Length)
                return true;

            return !items[ingredient.Index];
        }

        /// <summary>A bundle entry parsed from the game's data files.</summary>
        /// <param name="ID">The unique bundle ID.</param>
        /// <param name="Name">The bundle name.</param>
        /// <param name="DisplayName">The translated bundle name.</param>
        /// <param name="Area">The community center area containing the bundle.</param>
        /// <param name="RewardData">The unparsed reward description, which can be parsed with <see cref="StardewValley.Utility.getItemFromStandardTextDescription"/>.</param>
        /// <param name="Ingredients">The required item ingredients.</param>
        /// <param name="Slots">The amount of items needed in order to complete the bundle</param>
        private record BundleModel(int ID, string Name, string DisplayName, string Area, string RewardData, BundleIngredientModel[] Ingredients, int Slots);


        /// <summary>An item slot for a bundle.</summary>
        /// <param name="Index">The ingredient's index in the bundle.</param>
        /// <param name="ItemId">The required item's qualified or unqualified item ID, or category ID, or -1 for a monetary bundle.</param>
        /// <param name="Stack">The number of items required.</param>
        /// <param name="Quality">The required item quality.</param>
        private record BundleIngredientModel(int Index, string ItemId, int Stack, ItemQuality Quality);

        private enum ItemQuality
        {
            Normal = SObject.lowQuality,
            Silver = SObject.medQuality,
            Gold = SObject.highQuality,
            Iridium = SObject.bestQuality
        }
    }
}
