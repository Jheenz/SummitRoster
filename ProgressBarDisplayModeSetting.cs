using SettingsExtender;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zorro.Core;
using Zorro.Core.CLI;
using Zorro.Settings;
using Unity.Mathematics;

namespace ProgressMap
{
    enum ProgressBarDisplayMode
    {
        Full, Centered
    }

    class ProgressBarDisplayModeSetting : EnumSetting<ProgressBarDisplayMode>, IExposedSetting
    {
        public string GetDisplayName()
        {
            return "Summit Roster display mode";
        }

        public string GetCategory()
        {
            return SettingsRegistry.GetPageId("Roster");
        }

        protected override ProgressBarDisplayMode GetDefaultValue()
        {
            return ProgressBarDisplayMode.Full;
        }

        public override List<UnityEngine.Localization.LocalizedString> GetLocalizedChoices()
        {
            return null;
        }

        public override void ApplyValue()
        {
            //
        }
    }

    public enum RosterSide
    {
        Left,
        Right,
        Top
    }

    public class RosterSideSetting : EnumSetting<RosterSide>, IExposedSetting
    {
        public string GetDisplayName()
        {
            return "Roster Side";
        }

        public string GetCategory()
        {
            return SettingsRegistry.GetPageId("Roster");
        }

        protected override RosterSide GetDefaultValue()
        {
            return RosterSide.Left; // default to Left
        }

        public override List<UnityEngine.Localization.LocalizedString> GetLocalizedChoices()
        {
            return null; // no localization provided
        }

        public override void ApplyValue()
        {
            // nothing special to apply immediately
        }
        public bool IsAvailableInGame()
        {
            return false; // not available during gameplay
        }
    }

    public class ProgressBarDisplayRangeSetting : FloatSetting, IExposedSetting
    {
        public string GetDisplayName()
        {
            return "Centered Mode Display Range";
        }

        public string GetCategory()
        {
            return SettingsRegistry.GetPageId("Roster");
        }

        protected override float GetDefaultValue()
        {
            return 100f; // default range in meters
        }

        // Correct type: float2 instead of Vector2
        protected override float2 GetMinMaxValue()
        {
            return new float2(10f, 1000f); // min, max
        }

        public override void ApplyValue()
        {
            // Optional: trigger refresh logic here
        }

        public bool IsAvailableInGame()
        {
            return true; // allow adjusting during gameplay
        }
    }
}