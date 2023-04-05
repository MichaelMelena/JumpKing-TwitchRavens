﻿using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpKingModifiersMod.Settings
{
    /// <summary>
    /// An aggregate of settings keys for use in <see cref="JumpKingModifiersMod"/>
    /// </summary>
    public abstract class JumpKingModifiersModSettingsContext
    {
        public const string SettingsFileName = "JumpKingModifiersMod.settings";
        public const char CommentCharacter = '#';

        // Fall Damage
        public const string FallDamageBloodSplatterFilePath = "Content/Mods/BloodSplatters.txt";
        public const string FallDamageSubtextsFilePath = "Content/Mods/FallDamageSubtexts.txt";
        public const string DebugTriggerFallDamageToggleKeyKey = "DebugTriggerFallDamageToggleKey";
        public const string FallDamageEnabledKey = "FallDamageEnabled";
        public const string FallDamageModifierKey = "FallDamageModifier";
        public const string FallDamageBloodEnabledKey = "FallDamageBloodEnabled";
        public const string FallDamageClearBloodKey = "FallDamageClearBlood";
        public const string FallDamagePreviousHealthKey = "FallDamagePreviousHealth";
        public const float DefaultFallDamageModifier = 0.05f;

        // Shrinking
        public const string DebugTriggerManualResizeToggleKey = "DebugTriggerManualResizeToggleKey";
        public const string ManualResizeEnabledKey = "ManualResizeEnabled";
        public const string ManualResizeGrowKeyKey = "ManualResizeGrowKey";
        public const string ManualResizeShrinkKeyKey = "ManualResizeShrinkKey";

        /// <summary>
        /// Gets the default state of the settings
        /// </summary>
        public static Dictionary<string, string> GetDefaultSettings()
        {
            return new Dictionary<string, string>()
            {
                { DebugTriggerFallDamageToggleKeyKey, Keys.F11.ToString() },
                { FallDamageEnabledKey, false.ToString() },
                { FallDamageBloodEnabledKey, true.ToString() },
                { FallDamageClearBloodKey, Keys.F10.ToString() },
                { FallDamageModifierKey, DefaultFallDamageModifier.ToString() },
                { FallDamagePreviousHealthKey, 100.ToString() },
                { DebugTriggerManualResizeToggleKey, Keys.F9.ToString() },
                { ManualResizeEnabledKey, false.ToString() },
                { ManualResizeGrowKeyKey, Keys.Up.ToString() },
                { ManualResizeShrinkKeyKey, Keys.Down.ToString() }
            };
        }

        /// <summary>
        /// Gets the default values for the Fall Damage Subtexts
        /// </summary>
        public static string[] GetDefaultFallDamageSubtexts()
        {
            return new string[]
            {
                "That's gotta be embarrassing...",
                "You're meant to be going up, you know",
                "Thanks for playing Fall Guys!",
                "It's like you don't even try",
                "Stop milking this for content",
                "Follow PhantomBadger_ on Twitter!",
                "Do you regret your choices yet?",
                "You'll get it next time for sure",
            };
        }
    }
}