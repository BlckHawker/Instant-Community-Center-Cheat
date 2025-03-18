using StardewModdingAPI.Utilities;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant_Community_Center_Cheat
{
    /// <summary>The mod settings.</summary>
    internal class ModConfig
    {

        /// <summary>The default key for giving a player an item</summary>
        public const SButton DefaultGiveCCItemKey = SButton.L;

        /// <summary>The key which gives the player the desired itemss.</summary>
        public SButton GiveCCItemKey { get; set; } = DefaultGiveCCItemKey;
    }
}
