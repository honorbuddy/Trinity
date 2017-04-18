﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using Trinity.Components.Adventurer;
using Trinity.Components.Adventurer.Game.Actors;
using Trinity.Components.Adventurer.Game.Exploration;
using Trinity.Framework;
using Trinity.Framework.Actors.ActorTypes;
using Trinity.Framework.Helpers;
using Trinity.Framework.Objects.Enums;
using Trinity.Framework.Objects.Memory;
using Trinity.Modules;
using Zeta.Bot.Profile;
using Zeta.Common;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.Game.Internals.Actors;
using Zeta.Game.Internals.Actors.Gizmos;
using Zeta.Game.Internals.SNO;
using Zeta.XmlEngine;

namespace Trinity.Components.QuestTools
{
    public class ProfileTagLogger
    {
        public class StateSnapshot
        {
            public static StateSnapshot Create()
            {
                ZetaDia.Actors.Update();
                if (!ZetaDia.IsInGame || ZetaDia.Me == null)
                    return null;

                var s = new StateSnapshot();
                using (ZetaDia.Memory.AcquireFrame())
                {
                    Core.Scenes.Update();
                    Core.Update();

                    var position = ZetaDia.Me.Position;
                    var currentQuest = ZetaDia.CurrentQuest;


                    s.QuestId = currentQuest?.QuestSnoId ?? 1;
                    s.QuestStep = currentQuest?.StepId ?? 1;
                    s.SceneSnoId = ZetaDia.Me.CurrentScene.SceneInfo.SNOId;
                    s.SceneName = ZetaDia.Me.CurrentScene.Name;
                    s.WorldSnoId = ZetaDia.Globals.WorldSnoId;
                    s.LevelAreaSnoId = ZetaDia.CurrentLevelAreaSnoId;

                    var waypoint = ZetaDia.Storage.ActManager.GetWaypointByLevelAreaSnoId(s.LevelAreaSnoId);
                    s.WaypointNumber = waypoint?.Number ?? 0;

                    s.UpdateForActor(Core.Actors.Me);

                }
                return s;
            }

            public void UpdateForActor(TrinityActor actor)
            {
                if (actor == null) return;
                ActorId = actor.ActorSnoId;
                ActorSno = (SNOActor)ActorId;
                StartAnimation = actor.Animation.ToString();
                SetPosition(actor.Position);
            }

            public void UpdateForMarker(TrinityMarker marker)
            {
                if (marker == null) return;
                MarkerHash = marker.NameHash;
                MarkerName = marker.Name;
                MarkerType = marker.MarkerType;

                var actor = Core.Actors.FirstOrDefault(a => !a.IsMe && !a.IsExcludedId && a.Position.Distance(a.Position) <= 3f);
                UpdateForActor(actor);
            }

            public void SetPosition(Vector3 position)
            {
                if (AdvDia.CurrentWorldScene == null)
                {
                    Core.Scenes.Update();
                }

                var relativePosition = AdvDia.CurrentWorldScene?.GetRelativePosition(position) ?? Vector3.Zero;
                X = position.X;
                Y = position.Y;
                Z = position.Z;
                SceneX = relativePosition.X;
                SceneY = relativePosition.Y;
                SceneZ = relativePosition.Z;
            }

            public string StartAnimation { get; set; }
            public WorldMarkerType MarkerType { get; set; }
            public int MarkerHash { get; set; }
            public string MarkerName { get; set; }
            public SNOActor ActorSno { get; set; }
            public int ActorId { get; set; }
            public int WaypointNumber { get; set; }
            public float SceneZ { get; set; }
            public float SceneY { get; set; }
            public float SceneX { get; set; }
            public float Z { get; set; }
            public float Y { get; set; }
            public float X { get; set; }
            public int QuestStep { get; set; }
            public int LevelAreaSnoId { get; set; }
            public int WorldSnoId { get; set; }
            public string SceneName { get; set; }
            public int SceneSnoId { get; set; }
            public int QuestId { get; set; }
        }

        public static string GenerateActorTags<T>(Func<TrinityActor, bool> actorSelector) where T : ProfileBehavior
        {
            var sb = new StringBuilder();
            var s = StateSnapshot.Create();
            foreach (var actor in Core.Actors.Actors.Where(a => !a.IsMe && actorSelector(a)).OrderBy(a => a.Distance))
            {
                s.UpdateForActor(actor);
                sb.AppendLine($"     <!-- {actor.Name} ({actor.ActorSnoId}) {(SNOActor)actor.ActorSnoId} Distance={actor.Distance} Type={actor.Type} Anim={actor.Animation} -->");
                sb.AppendLine(GenerateTag<T>(s));
                sb.AppendLine(Environment.NewLine);
            }
            return sb.ToString();
        }

        public static string GenerateMarkerTags<T>(Func<TrinityMarker, bool> markerSelector = null) where T : ProfileBehavior
        {
            var sb = new StringBuilder();
            var s = StateSnapshot.Create();
            foreach (var marker in Core.Markers.Where(a => markerSelector?.Invoke(a) ?? true))
            {
                s.UpdateForMarker(marker);
                sb.AppendLine($"     <!-- {marker.Name} {marker.NameHash} {marker.MarkerType} Distance={marker.Distance} TextureId={marker.TextureId} WorldSnoId={marker.WorldSnoId} -->");
                sb.AppendLine(GenerateTag<T>(s));
                sb.AppendLine(Environment.NewLine);
            }
            return sb.ToString();
        }


        public static string GenerateTag<T>(StateSnapshot snapshot = null) where T : ProfileBehavior
        {
            var result = string.Empty;
            var s = snapshot ?? StateSnapshot.Create();
            if (s == null) return result;

            var stateDict = s.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(k => k.Name, v => v.GetValue(s));

            result += $@"     <{typeof(T).Name.TrimEnd("Tag")} questId=""{s.QuestId}"" stepId=""{s.QuestStep}"" ";

            foreach (var propertyInfo in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var xmlAttributes = propertyInfo.GetCustomAttributes<XmlAttributeAttribute>();
                var attName = xmlAttributes.LastOrDefault()?.Name ?? string.Empty;

                if (string.IsNullOrEmpty(attName) || IgnoreXmlAttributeNames.Contains(attName))
                    continue;

                var valueMatch = stateDict.FirstOrDefault(i
                    => string.Equals(propertyInfo.Name.ToLowerInvariant(), i.Key.ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(attName, i.Key.ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase));

                var value = valueMatch.Value ?? GetDefaultValue(propertyInfo);
                if (value == null)
                {
                    continue;
                }

                if (IsNumericType(propertyInfo.PropertyType) && !propertyInfo.Name.Contains("Id"))
                {
                    value = Math.Round((decimal)Convert.ChangeType(value, typeof(decimal)), 3, MidpointRounding.AwayFromZero);
                    result += $@"{attName}=""{value:0.##}"" ";
                }
                else
                {
                    if (propertyInfo.PropertyType == typeof(bool))
                        value = value.ToString().ToLowerInvariant();

                    result += $@"{attName}=""{value}"" ";
                }
            }

            result += $@" worldSnoId=""{s.WorldSnoId}"" levelAreaSnoId=""{s.LevelAreaSnoId}"" />";
            return result;
        }

        private static object GetDefaultValue(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttribute<DefaultValueAttribute>()?.Value;
        }

        public static string GenerateQuestInfoComment()
        {
            var currentQuest = ZetaDia.Storage.Quests.ActiveQuests.FirstOrDefault();
            if (currentQuest == null) return null;
            var currentStep = currentQuest.QuestRecord.Steps.FirstOrDefault(q => q.StepId == currentQuest.QuestStep);
            var obj = currentStep?.QuestStepObjectiveSet?.QuestStepObjectives?.FirstOrDefault();

            return $"<!-- Quest: {currentQuest.Quest}: {currentQuest.DisplayName} ({currentQuest.QuestSNO}) Type:{currentQuest.QuestType} Step: {currentStep?.Name} ({currentQuest.QuestStep}) -->";
        }

        public static string GenerateWorldInfoComment()
        {
            var sceneSnoId = ZetaDia.CurrentLevelAreaSnoId;
            var sceneName = Core.World.CurrentLevelAreaName;
            var sceneSnoName = ZetaDia.SNO.LookupSNOName(SNOGroup.LevelArea, sceneSnoId);
            var worldSnoId = ZetaDia.Globals.WorldSnoId;
            var world = ZetaDia.SNO[SNOGroup.Worlds].GetRecord<SNORecordWorld>(worldSnoId);
            return $"<!-- World: {world.Name} ({worldSnoId}) Scene: {sceneSnoName} {sceneName} ({sceneSnoId}) Generated={world.IsGenerated} -->";
        }

        private static HashSet<string> IgnoreXmlAttributeNames { get; } = new HashSet<string>
        {
            "questId",
            "stepId",
            "worldSnoId",
            "levelAreaSnoId",
            "ignoreReset",
            "statusText",
            "objectiveIndex",
            "questName",
        };

        public static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

    }
}