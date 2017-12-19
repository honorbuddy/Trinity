﻿using Trinity.Framework;
using Trinity.Framework.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using Trinity.Components.Combat.Resources;
using Trinity.Framework.Actors.ActorTypes;
using Trinity.Framework.Objects;
using Trinity.Framework.Reference;
using Trinity.Settings;
using Trinity.UI;
using Zeta.Common;

namespace Trinity.Routines.Wizard
{
    public sealed class WizardChannelMeteor : WizardBase, IRoutine
    {
        #region Definition

        public string DisplayName => "Channel Meteor Wizard";
        public string Description => "Channeling immense powers to call forth destruction from the sky, the Meteor Wizard alternates" +
            " the might of two sets according to his whims and conjures fiery death. This bursty, hard hitting playstyle is available in " +
            "two Greater Rift solo and regular Rift farming variations, explained in that order.";
        public string Author => "jubisman";
        public string Version => "0.1.1";
        public string Url => "https://www.icy-veins.com/d3/wizard-channeling-meteor-firebird-build-patch-2-6-1-season-12";

        public Build BuildRequirements => new Build
        {
            Sets = new Dictionary<Set, SetBonus>
            {
                { Sets.TalRashasElements, SetBonus.Third },
                //{ Sets.FirebirdsFinery, SetBonus.Third }
            },
            Skills = new Dictionary<Skill, Rune>
            {
                { Skills.Wizard.Meteor, null },
            }
        };

        #endregion

        public override KiteMode KiteMode => KiteMode.Never;

        public TrinityPower GetOffensivePower()
        {
            Vector3 position;
            TrinityActor target;
            TrinityPower power;

            if (ShouldWalkToTarget(out target))
                return Walk(target);

            if (ShouldTeleport(out position))
                return Teleport(position);

            if (ShouldFrostNova())
                return FrostNova();

            if (ShouldFamiliar())
                return Familiar();

            if (ShouldMeteor(out target))
                return Meteor(target);

            if (TrySecondaryPower(out power))
                return power;

            if (TryPrimaryPower(out power))
                return power;

            return Walk(TargetUtil.GetSafeSpotPosition(20f));
        }

        private static bool ShouldWalkToTarget(out TrinityActor target)
        {
            target = null;

            if (CurrentTarget.Distance > 60f)
            {
                target = CurrentTarget;
                return target != null;
            }

            return false;
        }

        protected override bool ShouldFrostNova()
        {
            if (!Skills.Wizard.FrostNova.CanCast())
                return false;

            if (!TargetUtil.AnyMobsInRange(60f))
                return false;

            return true;
        }

        protected override bool ShouldTeleport(out Vector3 position)
        {
            position = Vector3.Zero;

            if (!Skills.Wizard.Teleport.CanCast())
                return false;

            if (Skills.Wizard.Teleport.TimeSinceUse < 200)
                return false;

            if (!AllowedToUse(Settings.Teleport, Skills.Wizard.Teleport))
                return false;

            Vector3 bestBuffedPosition;
            var bestSafeSpot = TargetUtil.GetSafeSpotPosition(60f);

            if (TargetUtil.BestBuffPosition(60f, bestSafeSpot, false, out bestBuffedPosition) &&
                Player.Position.Distance(bestBuffedPosition) > 10f && bestBuffedPosition != Vector3.Zero)
            {
                Core.Logger.Log($"Found buff position - distance: {Player.Position.Distance(bestBuffedPosition)} ({bestBuffedPosition})");
                position = bestBuffedPosition;
                return position != Vector3.Zero;
            }

            return false;
        }

        protected override bool ShouldMeteor(out TrinityActor target)
        {
            target = null;

            if (Skills.Wizard.Meteor.TimeSinceUse < 3000)
                return false;

            //Core.Logger.Log("Cast Meteor After {0} miliseconds", Skills.Wizard.Meteor.TimeSinceUse);
            return base.ShouldMeteor(out target);
        }

        public TrinityPower GetDefensivePower() => GetBuffPower();

        public TrinityPower GetBuffPower()
        {
            return DefaultBuffPower();
        }

        public TrinityPower GetDestructiblePower() => DefaultDestructiblePower();

        public TrinityPower GetMovementPower(Vector3 destination)
        {
            if (AllowedToUse(Settings.Teleport, Skills.Wizard.Teleport) && CanTeleportTo(destination))
                return Teleport(destination);

            return Walk(destination);
        }

        #region Settings

        public override int ClusterSize => Settings.ClusterSize;
        public override float EmergencyHealthPct => Settings.EmergencyHealthPct;

        IDynamicSetting IRoutine.RoutineSettings => Settings;
        public WizardChannelMeteorSettings Settings { get; } = new WizardChannelMeteorSettings();

        public sealed class WizardChannelMeteorSettings : NotifyBase, IDynamicSetting
        {
            private SkillSettings _teleport;
            private int _clusterSize;
            private float _emergencyHealthPct;

            [DefaultValue(6)]
            public int ClusterSize
            {
                get { return _clusterSize; }
                set { SetField(ref _clusterSize, value); }
            }

            [DefaultValue(0.4f)]
            public float EmergencyHealthPct
            {
                get { return _emergencyHealthPct; }
                set { SetField(ref _emergencyHealthPct, value); }
            }

            public SkillSettings Teleport
            {
                get { return _teleport; }
                set { SetField(ref _teleport, value); }
            }

            #region Skill Defaults

            private static readonly SkillSettings TeleportDefaults = new SkillSettings
            {
                UseMode = UseTime.Default,
                RecastDelayMs = 200,
                Reasons = UseReasons.Blocked
            };

            #endregion

            public override void LoadDefaults()
            {
                base.LoadDefaults();
                Teleport = TeleportDefaults.Clone();
            }

            #region IDynamicSetting

            public string GetName() => GetType().Name;
            public UserControl GetControl() => UILoader.LoadXamlByFileName<UserControl>(GetName() + ".xaml");
            public object GetDataContext() => this;
            public string GetCode() => JsonSerializer.Serialize(this);
            public void ApplyCode(string code) => JsonSerializer.Deserialize(code, this, true);
            public void Reset() => LoadDefaults();
            public void Save() { }

            #endregion
        }

        #endregion

    }
}