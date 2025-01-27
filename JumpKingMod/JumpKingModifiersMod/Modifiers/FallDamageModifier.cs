﻿using HarmonyLib;
using JumpKing;
using JumpKing.Player;
using JumpKingModifiersMod.API;
using JumpKingModifiersMod.Entities;
using JumpKingModifiersMod.Modifiers;
using JumpKingModifiersMod.Patching;
using JumpKingModifiersMod.Patching.States;
using JumpKingModifiersMod.Settings;
using Logging.API;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PBJKModBase.API;
using PBJKModBase.Entities;
using Settings;
using System;
using System.Collections.Generic;

namespace JumpKingModifiersMod.Modifiers
{
    /// <summary>
    /// An implementation of <see cref="IModifier"/> and <see cref="IDisposable"/> which gives a health bar to the player
    /// and loses health whenever they fall
    /// </summary>
    [ConfigurableModifier("Fall Damage", "The player has a health bar, a long fall will deal damage to it, when you hit zero you die and the map restarts")]
    public class FallDamageModifier : IModifier, IDisposable
    {
        public string DisplayName { get; } = "Fall Damage";

        [ModifierSetting(JumpKingModifiersModSettingsContext.FallDamageModifierKey, "Distance Damage Modifier", DefaultFallDamageDistanceModifier)]
        public readonly float DistanceDamageModifier;

        [ModifierSetting(JumpKingModifiersModSettingsContext.FallDamageBloodEnabledKey, "Is Blood Enabled", true)]
        public readonly bool IsBloodEnabled;

        [ModifierSetting(JumpKingModifiersModSettingsContext.FallDamageClearBloodKey, "Blood Clear Key", Keys.F10, typeof(Keys))]
        public readonly Keys ClearBloodKey;

        [ModifierSetting(JumpKingModifiersModSettingsContext.FallDamageNiceSpawnsKey, "Nice Spawns", true)]
        public readonly bool NiceSpawns;

        private enum FallDamageModifierState
        {
            Playing,
            DisplayingYouDied,
            DisplayingYouDiedSubtext,
            WaitingForInput
        }

        private readonly ModifierUpdatingEntity modifierUpdatingEntity;
        private readonly ModEntityManager modEntityManager;
        private readonly IPlayerStateObserver playerStateObserver;
        private readonly IGameStateObserver gameStateObserver;
        private readonly ILogger logger;
        private readonly Random random;
        private readonly UserSettings userSettings;
        private readonly IYouDiedSubtextGetter subtextGetter;
        private readonly BloodSplatterPersistence bloodSplatters;

        private UITextEntity healthTextEntity;
        private UIImageEntity healthBarFrontEntity;
        private UIImageEntity healthBarBackEntity;

        private UIImageEntity youDiedEntity;
        private UITextEntity youDiedSubtextEntity;
        private UITextEntity userPromptTextEntity;

        private int healthValue;
        private Vector2? lastOnGroundPosition;
        private bool processSplat;
        private bool reActivateModifier;
        private FallDamageModifierState fallModifierState;
        private bool clearBloodKeyReset;

        private float youDiedAlphaLerpCounter = 0;
        private float youDiedSubtextAlphaLerpCounter = 0;
        private float userPromptPulseCounter = 0;

        private const int MaxHealthValue = 100;
        private const float TimeTakenToShowYouDiedInSeconds = 2f;
        private const float TimeTakenToShowYouDiedSubtextInSeconds = 1f;
        private const float UserPromptPulseTimeInSeconds = 0.75f;

        private const float DefaultFallDamageDistanceModifier = 0.1f;

        /// <summary>
        /// Ctor for creating a <see cref="FallDamageModifier"/>
        /// </summary>
        /// <param name="modifierUpdatingEntity">A <see cref="ModifierUpdatingEntity"/> to hook our update method into</param>
        /// <param name="modEntityManager">A <see cref="ModEntityManager"/> to use for creating new entities</param>
        /// <param name="playerStateObserver">An implementation of <see cref="IPlayerStateObserver"/> for getting the current player state</param>
        /// <param name="logger">An implementation of <see cref="ILogger"/> to log to</param>
        public FallDamageModifier(ModifierUpdatingEntity modifierUpdatingEntity, ModEntityManager modEntityManager,
            IPlayerStateObserver playerStateObserver, IGameStateObserver gameStateObserver, IYouDiedSubtextGetter subtextGetter,
            UserSettings userSettings, ILogger logger)
        {
            this.modifierUpdatingEntity = modifierUpdatingEntity ?? throw new ArgumentNullException(nameof(modifierUpdatingEntity));
            this.modEntityManager = modEntityManager ?? throw new ArgumentNullException(nameof(modEntityManager));
            this.playerStateObserver = playerStateObserver ?? throw new ArgumentNullException(nameof(playerStateObserver));
            this.gameStateObserver = gameStateObserver ?? throw new ArgumentNullException(nameof(gameStateObserver));
            this.subtextGetter = subtextGetter ?? throw new ArgumentNullException(nameof(subtextGetter));
            this.userSettings = userSettings ?? throw new ArgumentNullException(nameof(userSettings));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            random = new Random(DateTime.Now.Second + DateTime.Now.Millisecond);

            DistanceDamageModifier = this.userSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.FallDamageModifierKey, DefaultFallDamageDistanceModifier);
            IsBloodEnabled = this.userSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.FallDamageBloodEnabledKey, true);
            ClearBloodKey = this.userSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.FallDamageClearBloodKey, Keys.F10);
            NiceSpawns = this.userSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.FallDamageNiceSpawnsKey, true);
            clearBloodKeyReset = true;

            bloodSplatters = new BloodSplatterPersistence(modEntityManager, userSettings, random, logger);
            fallModifierState = FallDamageModifierState.Playing;

            gameStateObserver.OnGameLoopNotRunning += OnGameLoopNotRunning;
            gameStateObserver.OnGameLoopRunning += OnGameLoopRunning;
            PlayerEntity.OnSplatCall =
                (PlayerEntity.OnSplat)Delegate.Combine(PlayerEntity.OnSplatCall,
                new PlayerEntity.OnSplat(this.OnSplat));

            modifierUpdatingEntity.RegisterModifier(this);
        }

        /// <summary>
        /// An implementation of <see cref="IDisposable.Dispose"/> which cleans up the entities
        /// </summary>
        public void Dispose()
        {
            healthTextEntity?.Dispose();
            healthBarFrontEntity?.Dispose();
            healthBarBackEntity?.Dispose();
            youDiedEntity?.Dispose();
            youDiedSubtextEntity?.Dispose();
            userPromptTextEntity?.Dispose();

            healthTextEntity = null;
            youDiedEntity = null;
            youDiedSubtextEntity = null;
            userPromptTextEntity = null;

            playerStateObserver?.DisablePlayerWalking(isWalkingDisabled: false);

            modifierUpdatingEntity.UnregisterModifier(this);
            gameStateObserver.OnGameLoopRunning -= OnGameLoopRunning;
            gameStateObserver.OnGameLoopNotRunning -= OnGameLoopNotRunning;
        }

        /// <summary>
        /// If the game loop stops running we cancel the modifier
        /// </summary>
        private void OnGameLoopNotRunning()
        {
            if (IsModifierEnabled())
            {
                DisableModifier();
                reActivateModifier = true;
            }
        }

        /// <summary>
        /// If the game loop restarts after we auto disable we re-enable
        /// </summary>
        private void OnGameLoopRunning()
        {
            if (reActivateModifier && !IsModifierEnabled())
            {
                EnableModifier();
            }
        }

        /// <inheritdoc/>
        public bool IsModifierEnabled()
        {
            return healthBarBackEntity != null;
        }

        /// <inheritdoc/>
        public bool EnableModifier()
        {
            if (healthBarBackEntity != null)
            {
                // Already active
                logger.Information($"Failed to Enable Fall Damage Modifier - Effect already active");
                return false;
            }

            if (!gameStateObserver.IsGameLoopRunning())
            {
                logger.Error($"Failed to Enable 'Fall Damage' Modifier, the game loop isn't running!");
                return false;
            }

            // Get the player state
            PlayerState playerState = playerStateObserver.GetPlayerState();
            if (playerState == null)
            {
                logger.Information($"Failed to Enable Fall Damage Modifier - Failed to get Player State");
                return false;
            }

            fallModifierState = FallDamageModifierState.Playing;

            // Get the last used health value
            healthValue = userSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.FallDamagePreviousHealthKey, MaxHealthValue);

            //Vector2? healthTextPosition = GetScreenSpacePositionForHealthTextEntity(playerState);
            //healthTextEntity = new UITextEntity(
            //    modEntityManager,
            //    healthPosition.Value,
            //    healthValue.ToString(),
            //    Color.Red,
            //    UIEntityAnchor.Center,
            //    JKContentManager.Font.MenuFontSmall);

            Vector2? healthBarPosition = GetScreenSpacePositionForHealthBarEntity(playerState);
            if (!healthBarPosition.HasValue)
            {
                logger.Information($"Failed to Enable Fall Damage Modifier - Unable to get a player position");
                return false;
            }
            Rectangle destRectangle = GetScreenSpaceRectangleForHealthBarFrontEntity(healthBarPosition.Value, healthValue, MaxHealthValue);
            healthBarBackEntity = new UIImageEntity(
                modEntityManager,
                healthBarPosition.Value,
                Sprite.CreateSprite(ModifiersModContentManager.HealthBarBackTexture),
                zOrder: 0);
            healthBarFrontEntity = new UIImageEntity(
                modEntityManager,
                destRectangle,
                Sprite.CreateSprite(ModifiersModContentManager.HealthBarFrontTexture),
                zOrder: 1);

            if (IsBloodEnabled)
            {
                bloodSplatters.LoadBloodSplatters();
            }
            logger.Information($"Enable Fall Damage Modifier");
            return true;
        }

        /// <inheritdoc/>
        public bool DisableModifier()
        {
            if (healthBarBackEntity == null)
            {
                // Already inactive
                logger.Information($"Failed to Disable Fall Damage Modifier");
                return false;
            }

            logger.Information($"Disable Fall Damage Modifier");

            // Save out health value
            userSettings.SetOrCreateSetting(JumpKingModifiersModSettingsContext.FallDamagePreviousHealthKey, healthValue.ToString());

            playerStateObserver?.DisablePlayerWalking(isWalkingDisabled: false);

            healthTextEntity?.Dispose();
            healthBarBackEntity?.Dispose();
            healthBarFrontEntity?.Dispose();

            youDiedEntity?.Dispose();
            youDiedSubtextEntity?.Dispose();
            userPromptTextEntity?.Dispose();

            healthTextEntity = null;
            healthBarBackEntity = null;
            healthBarFrontEntity = null;
            youDiedEntity = null;
            youDiedSubtextEntity = null;
            userPromptTextEntity = null;

            lastOnGroundPosition = null;
            processSplat = false;

            if (IsBloodEnabled)
            {
                bloodSplatters.SaveBloodSplatters();
                bloodSplatters.ClearAllBloodSplats();
            }
            return true;
        }

        /// <inheritdoc/>
        public void Update(float p_delta)
        {
            try
            {
                if (IsBloodEnabled)
                {
                    PollClearCommand();
                }

                switch (fallModifierState)
                {
                    case FallDamageModifierState.Playing:
                        {
                            PlayingUpdate(p_delta);
                            break;
                        }
                    case FallDamageModifierState.DisplayingYouDied:
                        {
                            DisplayingYouDiedUpdate(p_delta);
                            break;
                        }
                    case FallDamageModifierState.DisplayingYouDiedSubtext:
                        {
                            DisplayingYouDiedSubtextUpdate(p_delta);
                            break;
                        }
                    case FallDamageModifierState.WaitingForInput:
                        {
                            WaitingForInputUpdate(p_delta);
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                logger.Error($"Encountered exception when running FallDamageModifier: {e.ToString()}");
                DisableModifier();
            }
        }

        /// <summary>
        /// Polls the clear key to optionally clear the blood
        /// </summary>
        private void PollClearCommand()
        {
            KeyboardState kbState = Keyboard.GetState();
            if (kbState.IsKeyDown(ClearBloodKey))
            {
                if (clearBloodKeyReset)
                {
                    clearBloodKeyReset = false;
                    logger.Information($"Clearing all Blood Splats!");
                    bloodSplatters.ClearAllBloodSplats();
                }
            }
            else
            {
                clearBloodKeyReset = true;
            }
        }

        /// <summary>
        /// The update loop for the <see cref="FallDamageModifierState.Playing"/> state
        /// </summary>
        private void PlayingUpdate(float p_delta)
        {
            PlayerState playerState = playerStateObserver.GetPlayerState();
            if (playerState == null)
            {
                logger.Error($"Failed to get player state during FallDamageModifier Update!");
                return;
            }

            // Apply any damage thats pending
            if (processSplat && lastOnGroundPosition.HasValue)
            {
                processSplat = false;
                Vector2 newSplatPosition = playerState.Position;

                // Get the distance fallen and turn that into a damage value
                float yDiff = Math.Abs(lastOnGroundPosition.Value.Y - newSplatPosition.Y);
                float rawDamage = (yDiff * DistanceDamageModifier);
                int damage = Math.Max(1, (int)rawDamage);

                // If we landed on snow, deal no damage
                if (playerState.IsOnSnow)
                {
                    damage = 0;
                }

                // If we're on that one drop screen in GotB, deal no damage
                if (Camera.CurrentScreen == 160 && 
                    JumpKing.SaveThread.EventFlagsSave.ContainsFlag(JumpKing.SaveThread.StoryEventFlags.StartedGhost))
                {
                    damage = 0;
                }

                // Apply the damage to the health
                healthValue = Math.Max(0, healthValue - damage);
                logger.Information($"Dealing Damage of '{damage}' (Raw damage '{rawDamage}') - Remaining Health: {healthValue}");

                // Spawn the damage text
                SpawnDamageTextEntity(damage, Camera.TransformVector2(playerStateObserver.GetPlayerState().Position));

                if (damage > 0 && IsBloodEnabled)
                {
                    // Spawn a blood splat
                    SpawnBloodSplatEntity(playerState.Position);
                }
            }
            else if (playerState.IsOnGround)
            {
                lastOnGroundPosition = playerState.Position;
            }

            // Update the text
            if (healthTextEntity != null)
            {
                Vector2? textPosition = GetScreenSpacePositionForHealthTextEntity(playerState);
                if (textPosition != null)
                {
                    healthTextEntity.ScreenSpacePosition = textPosition.Value;
                    healthTextEntity.TextValue = healthValue.ToString();
                }
                else
                {
                    logger.Error($"Failed to update Health Text Position!");
                }
            }

            // Update the health bar
            Vector2? position = GetScreenSpacePositionForHealthBarEntity(playerState);
            if (!position.HasValue)
            {
                logger.Error($"Failed to update Health Bar Position!");
                return;
            }
            Rectangle destRectForHealth = GetScreenSpaceRectangleForHealthBarFrontEntity(position.Value, healthValue, MaxHealthValue);
            healthBarBackEntity.ScreenSpacePosition = position.Value;
            healthBarFrontEntity.DestinationRectangle = destRectForHealth;

            // We have died, enter the death state and restart
            if (healthValue <= 0)
            {
                // Disable walking so user can only jump + pause
                playerStateObserver?.DisablePlayerWalking(isWalkingDisabled: true, isXVelocityDisabled: true);

                Sprite youDiedSprite = Sprite.CreateSprite(ModifiersModContentManager.YouDiedTexture);
                youDiedEntity = new UIImageEntity(modEntityManager, new Vector2(0, 0), youDiedSprite, zOrder: 1);
                youDiedEntity.ImageValue.SetAlpha(0);
                youDiedAlphaLerpCounter = 0;

                fallModifierState = FallDamageModifierState.DisplayingYouDied;
                JKContentManager.Audio.RaymanSFX.Play();
                logger.Information($"Setting Modifier State to {fallModifierState.ToString()}!");
            }
        }

        /// <summary>
        /// The update loop for the <see cref="FallDamageModifierState.DisplayingYouDied"/> state
        /// </summary>
        private void DisplayingYouDiedUpdate(float p_delta)
        {
            // Lerp between 0 and 1 for the Alpha to fade in the texture
            youDiedAlphaLerpCounter += p_delta;
            float alphaValue = (youDiedAlphaLerpCounter / TimeTakenToShowYouDiedInSeconds);
            youDiedEntity.ImageValue.SetAlpha(alphaValue);

            // Once fully faded in we move to the Waiting for Input state
            if (alphaValue >= 1)
            {
                youDiedEntity.ImageValue.SetAlpha(1);

                string subtext = subtextGetter.GetYouDiedSubtext();
                youDiedSubtextEntity = new UITextEntity(modEntityManager,
                    new Vector2(240, 245),
                    subtext,
                    Color.White,
                    UIEntityAnchor.Center,
                    zOrder: 2);
                youDiedSubtextEntity.TextColor = new Color(Color.White, 0.0f);
                youDiedSubtextAlphaLerpCounter = 0;

                fallModifierState = FallDamageModifierState.DisplayingYouDiedSubtext;
                logger.Information($"Setting Modifier State to {fallModifierState.ToString()}!");
            }
        }

        /// <summary>
        /// The update loop for the <see cref="FallDamageModifierState.DisplayingYouDiedSubtext"/> state
        /// </summary>
        private void DisplayingYouDiedSubtextUpdate(float p_delta)
        {
            youDiedSubtextAlphaLerpCounter += p_delta;
            float alphaValue = (youDiedSubtextAlphaLerpCounter / TimeTakenToShowYouDiedSubtextInSeconds);
            youDiedSubtextEntity.TextColor = new Color(Color.White, alphaValue);

            if (alphaValue >= 1)
            {
                userPromptTextEntity = new UITextEntity(modEntityManager,
                    new Vector2(240, 260),
                    "Press Jump to restart!",
                    Color.White,
                    UIEntityAnchor.Center,
                    JKContentManager.Font.MenuFontSmall,
                    zOrder: 2);
                userPromptPulseCounter = 0;

                youDiedSubtextEntity.TextColor = Color.White;
                fallModifierState = FallDamageModifierState.WaitingForInput;
                logger.Information($"Setting Modifier State to {fallModifierState.ToString()}!");
            }
        }

        /// <summary>
        /// The update loop for the <see cref="FallDamageModifierState.WaitingForInput"/> state
        /// </summary>
        private void WaitingForInputUpdate(float p_delta)
        {
            // Pulse the user prompt
            userPromptPulseCounter += p_delta;
            if (userPromptPulseCounter > UserPromptPulseTimeInSeconds)
            {
                userPromptPulseCounter -= UserPromptPulseTimeInSeconds;
                if (userPromptTextEntity.TextColor.A > 0)
                {
                    userPromptTextEntity.TextColor = new Color(userPromptTextEntity.TextColor, 0f);
                }
                else
                {
                    userPromptTextEntity.TextColor = new Color(userPromptTextEntity.TextColor, 1f);
                }
            }

            // Use the game input to account for different controllers
            InputState inputState = playerStateObserver.GetInputState();
            if (inputState.Jump)
            {
                // Clear up the You Died & Text Entities
                youDiedEntity?.Dispose();
                youDiedEntity = null;

                youDiedSubtextEntity?.Dispose();
                youDiedSubtextEntity = null;

                userPromptTextEntity?.Dispose();
                userPromptTextEntity = null;

                // Re-enable user input
                playerStateObserver?.DisablePlayerWalking(isWalkingDisabled: false);

                // Restart the player position and reset the health
                playerStateObserver.RestartPlayerPosition(NiceSpawns, out _);
                healthValue = MaxHealthValue;
                fallModifierState = FallDamageModifierState.Playing;
                logger.Information($"Setting Modifier State to {fallModifierState.ToString()}!");

                JKContentManager.Audio.PressStart.Play();
            }
        }

        /// <summary>
        /// Called by JumpKing when the player splats on the ground
        /// </summary>
        private void OnSplat()
        {
            logger.Information($"Received Splat notification");
            processSplat = true;
        }

        /// <summary>
        /// Gets the current position for the health entity based on the player's state
        /// </summary>
        private Vector2? GetScreenSpacePositionForHealthBarEntity(PlayerState playerState)
        {
            if (playerState != null)
            {
                // Calculate the offset to center the (presumably larger) back health image above the player
                Rectangle sourceBackRect = ModifiersModContentManager.HealthBarBackTexture.Bounds;
                Vector2 healthPosition = playerState.Position + new Vector2(-(sourceBackRect.Width / 4), -20); // Honestly no idea why this is /4, it should be /2 but that is way too far?
                return Camera.TransformVector2(healthPosition);
            }
            return null;
        }

        /// <summary>
        /// Gets the current position for the health entity based on the player's state
        /// </summary>
        private Vector2? GetScreenSpacePositionForHealthTextEntity(PlayerState playerState)
        {
            if (playerState != null)
            {
                Vector2 healthPosition = playerState.Position + new Vector2(0, -20);
                return Camera.TransformVector2(healthPosition);
            }
            return null;
        }

        /// <summary>
        /// Gets a <see cref="Rectangle"/> representing the destination of the health bar front
        /// </summary>
        private Rectangle GetScreenSpaceRectangleForHealthBarFrontEntity(Vector2 healthPosition, float healthValue, float maxHealth)
        {
            // Calculate the offset to center the front rect in the middle of the back rect
            Rectangle sourceFrontRect = ModifiersModContentManager.HealthBarFrontTexture.Bounds;
            Rectangle sourceBackRect = ModifiersModContentManager.HealthBarBackTexture.Bounds;
            float xOffset = (sourceBackRect.Width - sourceFrontRect.Width) / 2f;
            float yOffset = (sourceBackRect.Height - sourceFrontRect.Height) / 2f;

            // Calculate the percentage of health so we can scale the X from left to right
            float percentage = (healthValue / maxHealth);
            float newWidth = sourceFrontRect.Width * percentage;
            Point newSize = new Point((int)newWidth, sourceFrontRect.Height);

            // As long as we have some health, make sure a little bit of the bar is being shown
            if (healthValue > 0)
            {
                newWidth = Math.Max(1, newWidth);
            }

            // Combine the offset and scale with the base position provided
            return new Rectangle(healthPosition.ToPoint() + new Point((int)xOffset, (int)yOffset), newSize);
        }

        /// <summary>
        /// Spawns a <see cref="DamageTextEntity"/> with the provided values. The entity will handle its own lifetime and movement
        /// </summary>
        private DamageTextEntity SpawnDamageTextEntity(int damageValue, Vector2 startingScreenSpacePosition)
        {
            return new DamageTextEntity(
                modEntityManager, 
                startingScreenSpacePosition, 
                $"-{damageValue.ToString()}", 
                damageValue > 0 ? Color.Red : Color.Aqua, 
                JKContentManager.Font.MenuFontSmall, 
                random);
        }

        /// <summary>
        /// Spawns a Blood Splat Entity
        /// </summary>
        /// <param name="playerPosition">The player position to base the blood splat entity on</param>
        private WorldspaceImageEntity SpawnBloodSplatEntity(Vector2 playerPosition)
        {
            if (ModifiersModContentManager.BloodSplatterSprites == null || ModifiersModContentManager.BloodSplatterSprites.Length == 0)
            {
                return null;
            }

            Vector2 playerFloor = playerPosition + new Vector2(0, 25);
            Sprite splatSprite = ModifiersModContentManager.BloodSplatterSprites[random.Next(ModifiersModContentManager.BloodSplatterSprites.Length)];
            
            var bloodSplatter = new WorldspaceImageEntity(modEntityManager, playerFloor, splatSprite, zOrder: 1);
            bloodSplatters.BloodSplatters.Add(bloodSplatter);
            return bloodSplatter;
        }
    }
}
