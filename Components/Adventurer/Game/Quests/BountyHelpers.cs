﻿using System;
using System.Collections.Generic;
using System.Linq;
using Trinity.Framework;
using Trinity.Components.Adventurer.Game.Actors;
using Trinity.Components.Adventurer.Game.Events;
using Trinity.Components.Adventurer.Settings;
using Trinity.Components.Adventurer.Util;
using Trinity.Framework.Helpers;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;
using Trinity.Framework.Actors.ActorTypes;
using Trinity.Framework.Objects.Enums;
using Trinity.Modules;

namespace Trinity.Components.Adventurer.Game.Quests
{
    public static class BountyHelpers
    {
        //ActorId: 365020, Type: Item, Name: Khanduran Rune //A1
        //ActorId: 364281, Type: Item, Name: Caldeum Nightshade //A2
        //ActorId: 364290, Type: Item, Name: Arreat War Tapestry //A3
        //ActorId: 364305, Type: Item, Name: Corrupted Angel Flesh //A4
        //ActorId: 364975, Type: Item, Name: Westmarch Holy Water //A5

        //public static readonly int Act1BountyMatSNO = 365020;
        //public static readonly int Act2BountyMatSNO = 364281;
        //public static readonly int Act3BountyMatSNO = 364290;
        //public static readonly int Act4BountyMatSNO = 364305;
        //public static readonly int Act5BountyMatSNO = 364975;

        public static long GetActMatsCount(Act act)
        {
            var playerData = ZetaDia.Storage.PlayerDataManager.ActivePlayerData;
            switch (act)
            {
                case Act.A1:
                    return playerData.GetCurrencyAmount(CurrencyType.KhanduranRune);
                case Act.A2:
                    return playerData.GetCurrencyAmount(CurrencyType.CaldeumNightshade);
                case Act.A3:
                    return playerData.GetCurrencyAmount(CurrencyType.ArreatWarTapestry);
                case Act.A4:
                    return playerData.GetCurrencyAmount(CurrencyType.CorruptedAngelFlesh);
                case Act.A5:
                    return playerData.GetCurrencyAmount(CurrencyType.WestmarchHolyWater);
            }
            return 0;
        }

        public class ObjectiveActor
        {
            public int MarkerNameHash { get; set; }
            public SNOActor ActorId { get; set; }
            public SNOWorld DestWorldId { get; set; }
        }

        public static Dictionary<int, ObjectiveActor> DynamicBountyPortals = new Dictionary<int, ObjectiveActor>
        {
            //texture = 81058

            { -2005510577, new ObjectiveActor { MarkerNameHash = -2005510577, ActorId = SNOActor.P4_bounty_grounds_Crypt_Shrines, DestWorldId = SNOWorld.p4_bounty_grounds_Neph } },
            { 498366490, new ObjectiveActor { MarkerNameHash = 498366490, ActorId = SNOActor.P4_bounty_grounds_Leorics_Garden_Champ_A5, DestWorldId = SNOWorld.p4_A5_bounty_grounds_Leorics_Garden } },
            { -816816641, new ObjectiveActor { MarkerNameHash = -816816641, ActorId = SNOActor.P4_bounty_grounds_Swr_Burrowers_A5, DestWorldId = SNOWorld.p4_A5_bounty_grounds_Swr } },
            { -728782754, new ObjectiveActor { MarkerNameHash = -728782754, ActorId = SNOActor.P4_bounty_grounds_Keep_Shaman_A5, DestWorldId = SNOWorld.p4_A5_bounty_grounds_Keep } },
            { -2005068563, new ObjectiveActor { MarkerNameHash = -2005068563, ActorId = SNOActor.P4_bounty_grounds_Crypt_Shrines, DestWorldId = SNOWorld.p4_bounty_grounds_Zolt } },
            { 124027337, new ObjectiveActor { MarkerNameHash = 124027337, ActorId = SNOActor.P4_bounty_grounds_Keep_Shaman, DestWorldId = SNOWorld.p4_bounty_grounds_Keep } },
            { -1454464458, new ObjectiveActor { MarkerNameHash = -1454464458, ActorId = SNOActor.P4_bounty_grounds_Crypt_Shrines, DestWorldId = SNOWorld.p4_bounty_grounds_Crypt } },
            { 1012772770, new ObjectiveActor { MarkerNameHash = 1012772770, ActorId = SNOActor.P4_bounty_grounds_Zolt_A5, DestWorldId = SNOWorld.p4_A5_bounty_grounds_Zolt } },
            { 467573611, new ObjectiveActor { MarkerNameHash = 467573611, ActorId = SNOActor.P4_bounty_grounds_Crypt_Shrines_A5, DestWorldId = SNOWorld.p4_A5_bounty_grounds_Crypt } },
            { 1012330756, new ObjectiveActor { MarkerNameHash = 1012330756, ActorId = SNOActor.P4_bounty_grounds_Crypt_Shrines_A5, DestWorldId = SNOWorld.p4_A5_bounty_grounds_Neph } },
            { -2002131030, new ObjectiveActor { MarkerNameHash = -2002131030, ActorId = SNOActor.P4_bounty_grounds_Swr_Burrowers, DestWorldId = SNOWorld.p4_bounty_grounds_Swr } },
            { 37697317, new ObjectiveActor { MarkerNameHash = 37697317, ActorId = SNOActor.P4_bounty_grounds_Leorics_Garden_Champ, DestWorldId = SNOWorld.p4_bounty_grounds_Leorics_Garden } },
        };

        public static Vector3 ScanForMarkerLocation(int markerNameHash, int searchRadius)
        {
            var marker = Core.Markers.FirstOrDefault(m => m.NameHash == markerNameHash && m.Position.Distance(AdvDia.MyPosition) <= searchRadius && !EntryPortals.IsEntryPortal(AdvDia.CurrentWorldDynamicId, m.NameHash));
            return marker?.Position ?? Vector3.Zero;
        }

        public static Vector3 ScanForMarkerLocation(WorldMarkerType type, int searchRadius)
        {
            var marker = Core.Markers.FirstOrDefault(m => m.MarkerType == type && m.Position.Distance(AdvDia.MyPosition) <= searchRadius && !EntryPortals.IsEntryPortal(AdvDia.CurrentWorldDynamicId, m.NameHash));
            return marker?.Position ?? Vector3.Zero;
        }

        public static Vector3 ScanForRiftEntryMarkerLocation(int searchRadius = 2000)
        {
            var marker = Core.Markers.FirstOrDefault(m => m.MarkerType == WorldMarkerType.QuestPortalBack || m.MarkerType == WorldMarkerType.RiftPortalBack && m.Position.Distance(AdvDia.MyPosition) <= searchRadius && !EntryPortals.IsEntryPortal(AdvDia.CurrentWorldDynamicId, m.NameHash));
            return marker?.Position ?? Vector3.Zero;
        }

        public static Vector3 ScanForRiftExitMarkerLocation(int searchRadius = 2000)
        {
            var marker = Core.Markers.FirstOrDefault(m => m.MarkerType == WorldMarkerType.QuestSceneEntrance || m.MarkerType == WorldMarkerType.RiftPortalForward && m.Position.Distance(AdvDia.MyPosition) <= searchRadius && !EntryPortals.IsEntryPortal(AdvDia.CurrentWorldDynamicId, m.NameHash));
            return marker?.Position ?? Vector3.Zero;
        }

        public static TrinityMarker ScanForMarker(Vector3 markerPosition, int searchRadius = 5)
        {
            return Core.Markers.Where(m => m.Position.Distance2D(markerPosition) <= searchRadius).OrderBy(m => m.MarkerType == WorldMarkerType.Objective).FirstOrDefault();
        }

        public static TrinityMarker ScanForMarker(int markerNameHash, int searchRadius)
        {
            return Core.Markers.FirstOrDefault(m => m.NameHash == markerNameHash && m.Position.Distance(AdvDia.MyPosition) <= searchRadius && !EntryPortals.IsEntryPortal(AdvDia.CurrentWorldDynamicId, m.NameHash));
        }

        public static TrinityMarker ScanForMarker(int markerNameHash, WorldMarkerType type, int searchRadius)
        {
            return Core.Markers.Where(m => m.NameHash == markerNameHash && m.Position.Distance(AdvDia.MyPosition) <= searchRadius && !EntryPortals.IsEntryPortal(AdvDia.CurrentWorldDynamicId, m.NameHash)).OrderByDescending(a => a.MarkerType == type).FirstOrDefault();
        }

        public static TrinityMarker ScanForMarker(WorldMarkerType type, int searchRadius)
        {
            return Core.Markers.FirstOrDefault(m => m.MarkerType == type && m.Position.Distance(AdvDia.MyPosition) <= searchRadius && !EntryPortals.IsEntryPortal(AdvDia.CurrentWorldDynamicId, m.NameHash));
        }

        public static Vector3 TryFindObjectivePosition(IList<ObjectiveActor> objectives, int searchRadius, out ObjectiveActor foundObjective)
        {
            var objectiveMarkers = new HashSet<int>(objectives.Select(o => o.MarkerNameHash));
            var miniMapMarker = AdvDia.CurrentWorldMarkers
                .FirstOrDefault(m => objectiveMarkers.Contains(m.NameHash) && m.Position.Distance(AdvDia.MyPosition) <= searchRadius && !EntryPortals.IsEntryPortal(AdvDia.CurrentWorldDynamicId, m.NameHash));

            if (miniMapMarker != null)
            {
                foundObjective = objectives.FirstOrDefault(o => o.MarkerNameHash == miniMapMarker.Id);
                return miniMapMarker.Position;
            }
            foundObjective = null;
            return Vector3.Zero;
        }

        public static Vector3 ScanForActorLocation(SNOActor actorId, int searchRadius, Vector3 origin = default(Vector3), Func<TrinityActor, bool> func = null)
        {
            origin = origin != Vector3.Zero ? origin : Core.Player.Position;
            var actor = Core.Actors.Where(a => a.ActorSnoId == actorId && a.Position.Distance(origin) <= searchRadius && (func == null || func(a))).OrderBy(a => a.Distance).FirstOrDefault();
            return actor?.Position ?? Vector3.Zero;
        }

        public static TrinityActor GetActorNearMarker(int markerHash, float radius = 5, Func<TrinityActor, bool> func = null)
        {
            var marker = ScanForMarker(markerHash, 120);
            if (marker == null) return null;
            switch (marker.MarkerType)
            {
                case WorldMarkerType.Entrance:
                case WorldMarkerType.Exit:
                    return Core.Actors.Where(a => a.Position.Distance(marker.Position) < radius && a.IsPortal && (func == null || func(a))).OrderBy(a => a.Distance).FirstOrDefault();
            }
            return Core.Actors.Where(a => a.Position.Distance(marker.Position) < radius && !a.IsMe && !a.IsExcludedId && (func == null || func(a))).OrderBy(a => a.Distance).FirstOrDefault();
        }

        public static TrinityMarker GetMarkerNearActor(TrinityActor actor)
        {
            return ScanForMarker(actor.Position);
        }

        public static Vector3 ScanForActorLocation(string internalName, int searchRadius)
        {
            return ScanForActor(internalName, searchRadius)?.Position ?? Vector3.Zero;
        }

        public static TrinityActor ScanForActor(SNOActor actorSnoId, int markerHash, int searchRadius = 500, Func<TrinityActor, bool> func = null)
        {
            return Core.Actors.Where(a => a.ActorSnoId == actorSnoId && a.Distance <= searchRadius && (func == null || func(a))).Where(a => GetMarkerNearActor(a)?.NameHash == markerHash).OrderBy(a => a.Distance).FirstOrDefault();
        }

        public static TrinityActor ScanForActor(SNOActor actorSnoId, int searchRadius = 500, Func<TrinityActor, bool> func = null)
        {
            return Core.Actors.Where(a => a.ActorSnoId == actorSnoId && a.Distance <= searchRadius && (func == null || func(a))).OrderBy(a => a.Distance).FirstOrDefault();
        }

        public static TrinityActor ScanForActor(string internalName, int searchRadius = 500, Func<TrinityActor, bool> func = null)
        {
            return Core.Actors.Where(a => a.Distance <= searchRadius && a.InternalNameLowerCase.Contains(internalName.ToLowerInvariant()) && (func == null || func(a))).OrderBy(a => a.Distance).FirstOrDefault();
        }

        public static bool AreAllActBountiesCompleted(Act act)
        {
            return !ZetaDia.Storage.Quests.Bounties.ToList().Any(b => b.Act == act && b.Info.State != QuestState.Completed);
        }

        public static DiaGizmo GetPortalNearPosition(Vector3 position)
        {
            if (JustEnteredWorld)
            {
                return null;
            }
            using (new PerformanceLogger("GetPortalNearMarkerPosition"))
            {
                var gizmo = ZetaDia.Actors.GetActorsOfType<DiaGizmo>(true)
                    .Where(o => o != null && o.IsFullyValid() && o is GizmoPortal && o.Position.Distance(position) <= 10f)
                    .OrderBy(o => o.Distance)
                    .FirstOrDefault();
                return gizmo;
            }
        }    

        public static bool JustEnteredWorld
        {
            get
            {
                if (PluginTime.CurrentMillisecond - PluginEvents.WorldChangeTime < 5000)
                {
                    return true;
                }
                return false;
            }
        }

        public static bool AreAllActBountiesSupported(Act act)
        {
            int numSupported = ZetaDia.Storage.Quests.Bounties.Count(b => b.Act == act && BountyDataFactory.GetBountyData(b.Quest) != null);
            bool hasUnsupported = numSupported != 5;
            if (hasUnsupported)
            {
                Core.Logger.Debug("[Bounties] Unsupported bounties are detected in {0}", act);
                return false;
            }
            return true;
        }

        public static bool AnyUnsupportedBounties()
        {
            List<Act> _acts = new List<Act> { Act.A1, Act.A2, Act.A3, Act.A4, Act.A5 };
            for (int i = 0; i < 5; i++)
            {
                if (!AreAllActBountiesSupported(_acts[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsActEnabledOnSettings(Act act)
        {
            switch (act)
            {
                case Act.A1:
                    return PluginSettings.Current.BountyAct1;

                case Act.A2:
                    return PluginSettings.Current.BountyAct2;

                case Act.A3:
                    return PluginSettings.Current.BountyAct3;

                case Act.A4:
                    return PluginSettings.Current.BountyAct4;

                case Act.A5:
                    return PluginSettings.Current.BountyAct5;
            }
            return false;
        }

        public static readonly Dictionary<Act, SNOQuest> ActBountyFinishingQuests = new Dictionary<Act, SNOQuest>
        {
            {Act.A1,SNOQuest.x1_AdventureMode_BountyTurnin_A1},
            {Act.A2,SNOQuest.x1_AdventureMode_BountyTurnin_A2},
            {Act.A3,SNOQuest.x1_AdventureMode_BountyTurnin_A3},
            {Act.A4,SNOQuest.x1_AdventureMode_BountyTurnin_A4},
            {Act.A5,SNOQuest.x1_AdventureMode_BountyTurnin_A5},
        };

        public static bool IsActTurninInProgress(Act act)
        {
            return
                ZetaDia.Storage.Quests.AllQuests.Any(
                    q => q.QuestSNO == ActBountyFinishingQuests[act] && q.State == QuestState.InProgress);
        }

        public static bool IsAnyActTurninInProgress()
        {
            return
                ZetaDia.Storage.Quests.AllQuests.Any(
                    q => ActBountyFinishingQuests.Values.Contains(q.QuestSNO) && q.State == QuestState.InProgress);
        }

        public static bool IsActTurninCompleted(Act act)
        {
            return
                ZetaDia.Storage.Quests.AllQuests.Any(
                    q => q.QuestSNO == ActBountyFinishingQuests[act] && q.State == QuestState.Completed);
        }

        public static bool QuestNpcExistsNearMe(float radius)
        {
            return ZetaDia.Actors.GetActorsOfType<DiaObject>(true)
                .Any(o => o != null && o.IsFullyValid() && o.Position.Distance(ZetaDia.Me.Position) <= radius && (o.CommonData.MarkerType == MarkerType.Exclamation || o.CommonData.MarkerType == MarkerType.Question));
        }
    }
}