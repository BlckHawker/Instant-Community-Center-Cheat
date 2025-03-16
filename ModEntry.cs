
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using xTile.Layers;
using HarmonyLib;
using SObject = StardewValley.Object;

namespace Instant_Community_Center_Cheat
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        //the key is the QualifiedItemId of the desired item 
          //each list is a different instance of that item being needed in a bundle
            //the first element is ???
            //the second element is the number of that desired item to complete the bundle
            //the third element is the minimum quality of that item (normal, silver, gold, iridium)
        private Dictionary<string, List<List<int>>>? bundlesIngredientsInfo;
        private Dictionary<string, List<List<int>>> BundlesIngredientsInfo => bundlesIngredientsInfo ??= this.Helper.Reflection.GetField<Dictionary<string, List<List<int>>>>(obj: CommunityCenter, name: "bundlesIngredientsInfo").GetValue();

        private CommunityCenter? communityCenter;
        private CommunityCenter CommunityCenter => communityCenter ??= (CommunityCenter)Game1.getLocationFromName("CommunityCenter");
        

        //key is the area id in the CC
        //the value is the list of ids corresponding the the bundles within that area
        private Dictionary<int, int[]> areaBundles;

        //lewis unlocking the community center
        private const string community_center_unlock_id = "611439";

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
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
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet or if the player is in a cutscene or have a menu open
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;

            const char desiredButton = 'L';

            if (e.Button.ToString() == desiredButton.ToString())
            {
                GetItems();
            }
        }

        private void GetItems()
        {


            Log($"Get items called");
            if (CommunityCenter != null)
            {
                //the player has not watched the CC cutscene with Lewis

                string popUpText;
                if (!Utility.HasAnyPlayerSeenEvent(community_center_unlock_id))
                {
                    //it is spring 5th year 1, or onwards
                    if (Game1.year == 1 && Game1.season == Season.Spring && Game1.dayOfMonth < 5)
                    {
                        popUpText = "Wait until it's Spring 5 or onwards";
                    }
                    //it is not raining in the town
                    else if (Game1.locations.First(l => l is Town).IsRainingHere())
                    {
                        popUpText = "Wait until it's not raining";
                    }
                    //it is between 8:00 am and 1:00 pm
                    else if (Game1.timeOfDay < 800 || Game1.timeOfDay > 1300)
                    {
                        popUpText = "Wait until it's between 8am and 1pm";
                    }

                    //the player must enter Pelican Town from the Bus Stop
                    else
                    {
                        popUpText = "Enter the town through the bus station";
                    }

                    Game1.showGlobalMessage(popUpText);
                    return;
                }

                bool communityCenterComplete = CommunityCenter.areAllAreasComplete();

                //todo If the community center isn't complete and the player doesn't have at least one bundle open,
                //say the steps the player currently needs to do in order to get in unlocked
                List<int> allBundlesIDs = areaBundles.Values.SelectMany(x => x).ToList();


                //todo the following reasons are why the CC is not un

                //Otherwise, if the community center isn't complete,
                if (!communityCenterComplete)
                {
                    Log($"Community Center not complete");

                    //Get all the bundles that are not complete
                    //priortize the bundles that are currently avaible to the player (where the plaque is visuble)
                    List<BundleModel> incompleteBundles = GetBundles(this.Monitor).Where(b => !CommunityCenter.isBundleComplete(b.ID)).OrderByDescending(b => BundleAvaiable(b.ID)).ToList();

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

                    // For each item in that list:
                    foreach (Item item in missingItems)
                    {
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
                                Log($"The player has enough {item.DisplayName} in their inventory");
                                foundItem = true;
                                break;
                            }
                            // If the player has the item (of the same required quality or higher) and doesn't have the stack requirment, add the missing number to that stack
                            else if (sameId && validQuality)
                            {
                                int missingCount = Math.Abs(playerItem.Stack - item.Stack);
                                playerItem.Stack += missingCount;
                                Log($"The player doesn't have enough {item.DisplayName} in their inventory (but has some missing {missingCount})");
                                foundItem = true;
                                break;
                            }
                        }

                        // If the player does not have this item in their inventory, and they have room for the item to be there, add the item with the correct stack quantity
                        if (!foundItem && playerItems.HasEmptySlots())
                        {
                            Game1.player.addItemsByMenuIfNecessary(new List<Item> { item });
                            Log($"The player doesn't have the {item.DisplayName} in their invetory");
                        }
                    }
                    
                    //todo When all items are given, send a message to say which items were added
                }

                // If the community center is complete, send a message saying nothing else can be done with this mod
            }

            else
            {
                Log("Community center not found");
            }
        }

        private void Log(string message, LogLevel level = LogLevel.Debug)
        {
            this.Monitor.Log(message, level);
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
                case 4: //Animal
                case 5: //Artisan
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
        internal record BundleModel(int ID, string Name, string DisplayName, string Area, string RewardData, BundleIngredientModel[] Ingredients, int Slots);


        /// <summary>An item slot for a bundle.</summary>
        /// <param name="Index">The ingredient's index in the bundle.</param>
        /// <param name="ItemId">The required item's qualified or unqualified item ID, or category ID, or -1 for a monetary bundle.</param>
        /// <param name="Stack">The number of items required.</param>
        /// <param name="Quality">The required item quality.</param>
        internal record BundleIngredientModel(int Index, string ItemId, int Stack, ItemQuality Quality);

        internal enum ItemQuality
        {
            Normal = SObject.lowQuality,
            Silver = SObject.medQuality,
            Gold = SObject.highQuality,
            Iridium = SObject.bestQuality
        }
    }
}
