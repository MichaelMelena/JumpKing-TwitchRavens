﻿using JumpKing;
using JumpKingModifiersMod.API;
using Logging.API;
using Microsoft.Xna.Framework;
using PBJKModBase.API;
using PBJKModBase.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpKingModifiersMod.Visuals
{
    public class ModifierNotifications : IModEntity, IDisposable
    {
        private class ModifierState
        {
            public IModifier Modifier { get; private set; }
            public bool IsEnabled { get; private set; }

            public ModifierState(IModifier modifier, bool isEnabled)
            {
                Modifier = modifier ?? throw new ArgumentNullException(nameof(modifier));
                IsEnabled = isEnabled;
            }  
        }

        private readonly ConcurrentQueue<ModifierState> modifierStatesToDisplay;
        private readonly ConcurrentQueue<UITextEntity> modifierNotifications;
        private readonly ModEntityManager modEntityManager;
        private readonly List<IModifierTrigger> triggers;
        private readonly ILogger logger;

        private float timeSinceLastUpdateCounter;
        private float fadeOutCounter;

        private const int MaxNumberOfNotificationsAtOnce = 5;
        private const float YPadding = 1;
        private const float InitialPositionXPadding = 8f;
        private const float TimeUntilFadeOutInSeconds = 1f;
        private const float FadeOutDurationInSeconds = 0.5f;
        private const float MaxAlpha = 102f;

        public ModifierNotifications(ModEntityManager modEntityManager, List<IModifierTrigger> triggers, ILogger logger)
        {
            this.modEntityManager = modEntityManager ?? throw new ArgumentNullException(nameof(modEntityManager));
            this.triggers = triggers ?? throw new ArgumentNullException(nameof(triggers));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            modifierStatesToDisplay = new ConcurrentQueue<ModifierState>();
            modifierNotifications = new ConcurrentQueue<UITextEntity>();

            for (int i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                trigger.OnModifierEnabled += OnModifierEnabled;
                trigger.OnModifierDisabled += OnModifierDisabled;
            }

            modEntityManager.AddEntity(this, zOrder: 0);
        }

        public void Dispose()
        {
            for (int i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                trigger.OnModifierEnabled -= OnModifierEnabled;
                trigger.OnModifierDisabled -= OnModifierDisabled;
            }

            modEntityManager.RemoveEntity(this);
        }

        private void OnModifierDisabled(IModifier modifier)
        {
            modifierStatesToDisplay.Enqueue(new ModifierState(modifier, isEnabled: false));
            timeSinceLastUpdateCounter = 0;
            fadeOutCounter = 0;
        }

        private void OnModifierEnabled(IModifier modifier)
        {
            modifierStatesToDisplay.Enqueue(new ModifierState(modifier, isEnabled: true));
            timeSinceLastUpdateCounter = 0;
            fadeOutCounter = 0;
        }

        public void Update(float p_delta)
        {
            // Do we have any modifier states to display?
            if (modifierStatesToDisplay.TryDequeue(out ModifierState modifierState))
            {
                // Check if we already have too many notifications
                if (modifierNotifications.Count > MaxNumberOfNotificationsAtOnce)
                {
                    // If so remove the oldest one
                    if (modifierNotifications.TryDequeue(out UITextEntity notificationEntityToRemove))
                    {
                        notificationEntityToRemove?.Dispose();
                        notificationEntityToRemove = null;
                    }
                }

                // Make a UIEntity for the new update
                string notificationText = $"{(modifierState.IsEnabled ? "Enabled" : "Disabled")} '{modifierState.Modifier.DisplayName}' Modifier";
                Vector2 notificationPosition = JumpGame.GAME_RECT.Size.ToVector2();
                notificationPosition.X -= InitialPositionXPadding;
                var notificationEntity = new UITextEntity(modEntityManager, notificationPosition, notificationText, Color.White, 
                    UIEntityAnchor.BottomRight, JKContentManager.Font.MenuFontSmall, zOrder: 2);
                float ySize = notificationEntity.Size.Y;

                // Move up all the other notifications
                foreach (UITextEntity modifierNotification in modifierNotifications)
                {
                    modifierNotification.ScreenSpacePosition = modifierNotification.ScreenSpacePosition - new Vector2(0, ySize + YPadding);
                }

                // Add the new notification
                modifierNotifications.Enqueue(notificationEntity);
            }

            // Update timeout counter
            if ((timeSinceLastUpdateCounter += p_delta) > TimeUntilFadeOutInSeconds)
            {
                if ((fadeOutCounter += p_delta) > FadeOutDurationInSeconds)
                {
                    fadeOutCounter = FadeOutDurationInSeconds;
                    while (modifierNotifications.TryDequeue(out UITextEntity textEntityToDispose))
                    {
                        textEntityToDispose?.Dispose();
                    }
                }
            }

            // Update alpha of all entries based on fadeOutCounter
            foreach (UITextEntity modifierNotification in modifierNotifications)
            {
                Color curCol = modifierNotification.TextColor;
                curCol.A = (byte)(MaxAlpha * (1 - (fadeOutCounter / FadeOutDurationInSeconds)));
                modifierNotification.TextColor = curCol;
            }
        }

        public void Draw()
        {
            // Do nothing
        }
    }
}