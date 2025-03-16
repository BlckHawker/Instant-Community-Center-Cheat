using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley.Locations;

namespace Instant_Community_Center_Cheat
{
    /// <summary>
    /// All methods that are harmony patches
    /// </summary>
    internal class HarmonyPatches
    {
        private static IMonitor Monitor;

        internal static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }


        // patches need to be static!
        public static void Weather_Postfix(GameLocation location, string eventId, string[] args)
        {
            Log($"Gamelocation {location} | Event Id: {eventId} | args: {string.Join(", ", args)}");
        }


        private static void Log(string message, LogLevel logLevel = LogLevel.Debug)
        {
            Monitor.Log(message, logLevel);
        }
    }
}