
using System;
using System.Collections.Generic;
using System.Numerics;
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
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using xTile.Layers;

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


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
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
                Log("Community center found");
                int areasComplete = CommunityCenter.areasComplete.Count(a => a);

                //If the community center isn't complete and the player doesn't have at least one bundle open,
                //say the steps the player currently needs to do in order to get in unlocked

                //Otherwise, if the community center isn't complete,
                if (areasComplete < 6)
                {
                    Log($"Community Center not complete. {areasComplete} area(s) completed");

                    //get all of the items the player to complete the Comunity Center.
                    List<Item> missingItems = new List<Item>();

                    foreach (KeyValuePair<string, List<List<int>>> pair in BundlesIngredientsInfo)
                    {
                        //Log($"Key: {pair.Key}");
                        //Log("Value");
                        List<List<int>> value = pair.Value;
                        foreach (List<int> list in value)
                        {
                            string str = string.Join(", ", list);
                            //Log(str);

                            Item itemCreated = ItemRegistry.Create(pair.Key, list[1], list[2]);

                            if (CommunityCenter.couldThisIngredienteBeUsedInABundle((StardewValley.Object)itemCreated))
                            {
                                missingItems.Add(itemCreated);
                            }

                        }
                    }

                    //Prioritize list by the stack of that of item(higher numbers first).
                    missingItems = missingItems.OrderByDescending(i => i.Stack).ToList();

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

                            if (item.QualifiedItemId == "(O)613")
                            {
                                Log("a");
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




                    

                    // When all items are given, send a message to say which items were added
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
    }
}
