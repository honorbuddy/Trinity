using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Serilog;
using Trinity.Framework.Actors.Attributes;
using Trinity.Framework.Helpers;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals.Actors;

namespace Trinity.Components.Adventurer.Settings
{
    [DataContract]
    public class AdventurerGems : NotifyBase
    {
        private static readonly ILogger s_logger = Logger.GetLoggerInstanceForType();

        public void UpdateOrder(IList<AdventurerGemSetting> list)
        {
            foreach (var item in list)
            {
                item.Order = list.IndexOf(item);
            }
        }

        private FullyObservableCollection<AdventurerGemSetting> _gemSettings;

        /// <summary>
        /// A list of gem settings and fixed data distict by TYPE of gem
        /// </summary>
        [DataMember]
        public FullyObservableCollection<AdventurerGemSetting> GemSettings
        {
            get => _gemSettings ?? (_gemSettings = GetDefaultGemSettings());
            set => LoadGemSettings(value);
            // Called with value deserialized from XML
        }

        /// <summary>
        /// Update gem settings records with partial information from XML Save file.
        /// </summary>
        private void LoadGemSettings(IEnumerable<AdventurerGemSetting> value)
        {
            using (GemSettings.DeferRefresh)
            {
                foreach (var gem in _gemSettings)
                {
                    var adventurerGemSettings = value as AdventurerGemSetting[] ?? value.ToArray();
                    var setting = adventurerGemSettings.FirstOrDefault(g => g.Sno == gem.Sno);
                    if (setting != null)
                    {
                        gem.Order = setting.Order;
                        gem.IsLimited = setting.IsLimited;
                        gem.IsEnabled = setting.IsEnabled;
                        gem.Limit = setting.Limit;
                    }
                }
                _gemSettings = new FullyObservableCollection<AdventurerGemSetting>(GemSettings.OrderBy(b => b.Order));
            }
        }

        /// <summary>
        /// Populate gem settings with every possible gem type using the gem reference.
        /// </summary>
        private static FullyObservableCollection<AdventurerGemSetting> GetDefaultGemSettings()
        {
            var gems = Framework.Reference.Gems.ToList().OrderByDescending(o => o.Importance).Select(g => new AdventurerGemSetting(g));
            return new FullyObservableCollection<AdventurerGemSetting>(gems);
        }

        /// <summary>
        /// A list of actual gem instances in the players current game
        /// </summary>
        public List<AdventurerGem> Gems { get; set; } = new IndexedList<AdventurerGem>();

        private static void Swap<T>(IList<T> list, int indexA, int indexB)
        {
            var tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }

        /// <summary>
        /// Updates gem list to match the actual gems in players backpack/stash.
        /// </summary>
        public void UpdateGems(int greaterRiftLevel)
        {
            if (ZetaDia.Me == null)
            {
                return;
            }
            
            Gems = InventoryManager.AllItems
                    .Where(i => i.IsValid && i.GetItemType() == ItemType.LegendaryGem)
                    .Select(i => new AdventurerGem(i, greaterRiftLevel))
                    .Distinct(new AdventurerGemComparer())
                    .ToList();

            UpdateGemSettings(Gems);
        }

        /// <summary>
        /// Populate gem settings records with backpack/stash gems
        /// </summary>
        private void UpdateGemSettings(List<AdventurerGem> freshGemsList)
        {
            if (freshGemsList == null || !freshGemsList.Any())
            {
                return;
            }

            var gemsBySno = freshGemsList.ToLookup(k => k.SNO, v => v);
            foreach (var gemSetting in GemSettings)
            {
                IEnumerable<AdventurerGem> gems = gemsBySno[gemSetting.Sno].ToList();
                if (gems.Any())
                {
                    gemSetting.HighestRank = gems.Max(g => g.Rank);
                }
                gemSetting.GemCount = gems.Count();
                gemSetting.IsEquipped = gems.Any(g => g.IsEquiped);
                gemSetting.Gems = gems;
            }
        }

        public ACDItem GetUpgradeTarget()
        {
            var minChance = PluginSettings.Current.GreaterRiftGemUpgradeChance;
            var level = ZetaDia.Me.InTieredLootRunLevel + 1;
            var priority = PluginSettings.Current.GemUpgradePriority;
            var equipPriority = PluginSettings.Current.GreaterRiftPrioritizeEquipedGems;
            var chanceReq = PluginSettings.Current.GreaterRiftGemUpgradeChance;

            UpdateGems(level);

            s_logger.Information($"[{nameof(GetUpgradeTarget)}] ---- Gem Upgrade Summary ----");
            s_logger.Information($"[{nameof(GetUpgradeTarget)}] Current Rift Level: {level}");
            s_logger.Information($"[{nameof(GetUpgradeTarget)}] Gem Count: {Gems.Count}");

            if (Gems.Count > 0)
            {
                s_logger.Information($"[{nameof(GetUpgradeTarget)}] Highest Ranked Gem: {Gems.Max(g => g.Rank)}");
                s_logger.Information($"[{nameof(GetUpgradeTarget)}] Lowest Ranked Gem: {Gems.Min(g => g.Rank)}");
            }

            s_logger.Information($"[{nameof(GetUpgradeTarget)}] Upgrade Chance Setting: {minChance}%");
            s_logger.Information($"[{nameof(GetUpgradeTarget)}] Ordering Priority: {priority}");
            s_logger.Information($"[{nameof(GetUpgradeTarget)}] Prioritize Equipped: {equipPriority}");

            var gems = Gems.ToList();

            s_logger.Information($"[{nameof(GetUpgradeTarget)}] ---- Excluded: User Disabled Type ----");

            foreach (var gem in gems.ToList())
            {
                if (!gem.Settings.IsEnabled)
                {
                    s_logger.Information($"[{nameof(GetUpgradeTarget)}] {gem.Name} ({gem.SNO}) Id={gem.Guid} Rank={gem.Rank}");
                    gems.Remove(gem);
                }
            }

            s_logger.Information($"[{nameof(GetUpgradeTarget)}] ---- Excluded: By Max Rank ----");

            foreach (var gem in gems.ToList())
            {
                if (gem.Rank >= gem.Settings.MaxRank)
                {
                    s_logger.Information($"[{nameof(GetUpgradeTarget)}] {gem.Name} ({gem.SNO}) Id={gem.Guid} Rank={gem.Rank} MaxRank={gem.Settings.MaxRank}");
                    gems.Remove(gem);
                }
            }

            s_logger.Information($"[{nameof(GetUpgradeTarget)}] ---- Excluded: User Rank Limit ----");

            foreach (var gem in gems.ToList())
            {
                if (gem.Settings.IsLimited && gem.Rank >= gem.Settings.Limit)
                {
                    s_logger.Information($"[{nameof(GetUpgradeTarget)}] {gem.Name} ({gem.SNO}) Id={gem.Guid} Rank={gem.Rank} Limit={(!gem.Settings.IsLimited ? "None" : gem.Settings.Limit.ToString())}");
                    gems.Remove(gem);
                }
            }

            s_logger.Information($"[{nameof(GetUpgradeTarget)}] ---- Excluded: Below Chance ({minChance}%) ----");

            foreach (var gem in gems.ToList())
            {
                if (gem.UpgradeChance < chanceReq)
                {
                    s_logger.Information($"[{nameof(GetUpgradeTarget)}] {gem.Name} ({gem.SNO}) Id={gem.Guid} Rank={gem.Rank} Chance={gem.UpgradeChance}");
                    gems.Remove(gem);
                }
            }

            switch (priority)
            {
                case GemPriority.None:
                case GemPriority.Rank:
                    s_logger.Information($"[{nameof(GetUpgradeTarget)}] ---- 'Rank' Ordered Candidates ({gems.Count}), by {(equipPriority ? "Equipped, " : "")}Rank ----");
                    gems = gems.OrderBy(g => equipPriority && g.IsEquiped ? 0 : 1).ThenByDescending(g => g.Rank).ThenBy(g => g.Settings.Order).ToList();
                    break;
                case GemPriority.Order:
                    s_logger.Information($"[{nameof(GetUpgradeTarget)}] ---- 'Order' Ordered Candidates ({gems.Count}), by {(equipPriority ? "Equipped, " : "")}Order ----");
                    gems = gems.OrderBy(g => equipPriority && g.IsEquiped ? 0 : 1).ThenBy(g => g.Settings.Order).ToList();
                    break;
                case GemPriority.Chance:
                    s_logger.Information($"[{nameof(GetUpgradeTarget)}] ---- 'Chance' Ordered Candidates ({gems.Count}), by {(equipPriority ? "Equipped, " : "")}Chance, then Rank ----");
                    gems = gems.OrderBy(g => equipPriority && g.IsEquiped ? 0 : 1).ThenByDescending(g => g.UpgradeChance).ThenByDescending(g => g.Rank).ToList();
                    break;
            }

            //if (focus)
            //{
            //    Core.Logger.Log($"[UpgradeGems] ---- Ordered Candidates ({gems.Count}), by {(equipPriority ? "Equipped, " : "")}Order, Rank - Focus Mode ----");
            //    gems = gems.OrderBy(g => equipPriority && g.IsEquiped ? 0 : 1).ThenBy(g => g.Settings.Order).ToList();
            //}
            //else
            //{
            //    Core.Logger.Log($"[UpgradeGems] ---- Ordered Candidates ({gems.Count}), by {(equipPriority ? "Equipped, " : "")}Chance, Order, Rank ----");
            //    gems = gems.OrderBy(g => equipPriority && g.IsEquiped ? 0 : 1).ThenByDescending(g => g.UpgradeChance).ThenBy(g => g.Settings.Order).ThenBy(g => g.Rank).ToList();
            //}

            for (var i = 0; i < gems.Count; i++)
            {
                var gem = gems.ElementAtOrDefault(i);
                s_logger.Information($"[{nameof(GetUpgradeTarget)}] #{(i + 1)}: {gem.Name} ({gem.SNO}) Id={gem.Guid} Rank={gem.Rank} Chance={gem.UpgradeChance} @{level} Order={gem.Settings.Order} Limit={(gem.Settings.IsLimited ? "None" : gem.Settings.Limit.ToString())} Equipped={gem.IsEquiped}");
            }

            if (gems.Count == 0)
            {
                s_logger.Information($"[{nameof(GetUpgradeTarget)}] Couldn't find any gems over the minimum upgrade chance, upgrading the gem with highest upgrade chance");
                gems = Gems.Where(g => !g.IsMaxRank).OrderByDescending(g => g.UpgradeChance).ToList();
            }

            ACDItem acdGem = null;

            var gemToUpgrade = gems.FirstOrDefault();
            if (gemToUpgrade != null)
            {
                s_logger.Information($"[{nameof(GetUpgradeTarget)}] ---- Selection ----");
                s_logger.Information($"[{nameof(GetUpgradeTarget)}] Attempting to upgrade {gemToUpgrade.DisplayName} ({gemToUpgrade.SNO}) Rank={gemToUpgrade.Rank} Chance={gemToUpgrade.UpgradeChance}%");
                acdGem = ZetaDia.Actors.GetActorsOfType<ACDItem>().FirstOrDefault(i => gemToUpgrade.Guid == i.AnnId);
            }

            if (acdGem == null)
            {
                acdGem = ZetaDia.Actors.GetActorsOfType<ACDItem>().FirstOrDefault(i => i.GetItemType() == ItemType.LegendaryGem);
                s_logger.Information($"[{nameof(GetUpgradeTarget)}] AcdItem Not Found {gemToUpgrade?.DisplayName} - Using {acdGem?.Name} so the quest can be completed");
            }

            return acdGem;
        }

        private AdventurerGem GetMatchingInventoryGem(AdventurerGem gem, List<AdventurerGem> inventoryGems)
        {
            return inventoryGems.FirstOrDefault(g => g.SNO == gem.SNO && g.Rank >= gem.Rank);
        }

        public class AdventurerGemComparer : IEqualityComparer<AdventurerGem>
        {
            public bool Equals(AdventurerGem x, AdventurerGem y)
            {
                return x != null && y != null && x.Guid == y.Guid;
            }

            public int GetHashCode(AdventurerGem obj)
            {
                return obj.Guid;
            }
        }
    }
}
