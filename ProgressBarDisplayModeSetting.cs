using SettingsExtender;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zorro.Core;
using Zorro.Core.CLI;
using Zorro.Settings;

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
        Right
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
}