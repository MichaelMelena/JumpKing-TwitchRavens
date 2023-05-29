﻿using JumpKing;
using JumpKingModifiersMod.Triggers;
using Logging.API;
using Microsoft.Xna.Framework;
using PBJKModBase.API;
using PBJKModBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpKingModifiersMod.Visuals
{
    /// <summary>
    /// An implementation of <see cref="IModEntity"/> which acts as the visual component to a provided
    /// <see cref="TwitchPollTrigger"/>, creating and managing UI texts to display the poll state to the chat
    /// </summary>
    public class TwitchPollVisual : IModEntity, IDisposable
    {
        private readonly ModEntityManager modEntityManager;
        private readonly TwitchPollTrigger trigger;
        private readonly IGameStateObserver gameStateObserver;
        private readonly ILogger logger;

        private ModifierTwitchPoll currentPoll;
        private UITextEntity pollDescriptionEntity;
        private UITextEntity pollCountdownEntity;
        private List<Tuple<ModifierTwitchPollOption, UITextEntity>> pollOptionEntities;

        private const float YPadding = 1;
        private const float CountdownYPadding = 5;
        private const float InitialPositionXPadding = 8f;

        /// <summary>
        /// Ctor for creating a <see cref="TwitchPollVisual"/>
        /// </summary>
        /// <param name="modEntityManager">The <see cref="ModEntityManager"/> to register to</param>
        /// <param name="trigger">The <see cref="TwitchPollTrigger"/> to act as a visual for</param>
        /// <param name="gameStateObserver">An implementation of <see cref="IGameStateObserver"/> to determine when we should draw</param>
        /// <param name="logger">An implementation of <see cref="ILogger"/> to use for logging</param>
        public TwitchPollVisual(ModEntityManager modEntityManager, TwitchPollTrigger trigger, IGameStateObserver gameStateObserver, ILogger logger)
        {
            this.modEntityManager = modEntityManager ?? throw new ArgumentNullException(nameof(modEntityManager));
            this.trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
            this.gameStateObserver = gameStateObserver ?? throw new ArgumentNullException(nameof(gameStateObserver));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            pollOptionEntities = new List<Tuple<ModifierTwitchPollOption, UITextEntity>>();

            this.gameStateObserver.OnGameLoopNotRunning += OnGameLoopNotRunning;
            this.gameStateObserver.OnGameLoopRunning += OnGameLoopRunning;
            this.trigger.OnTwitchPollStarted += OnTwitchPollStarted;
            this.trigger.OnTwitchPollClosed += OnTwitchPollClosed;
            this.trigger.OnTwitchPollEnded += OnTwitchPollEnded;
            this.modEntityManager.AddEntity(this, zOrder: 0);
        }

        /// <summary>
        /// An implementation of <see cref="IDisposable.Dispose"/> to clean up events
        /// </summary>
        public void Dispose()
        {
            gameStateObserver.OnGameLoopNotRunning -= OnGameLoopNotRunning;
            gameStateObserver.OnGameLoopRunning -= OnGameLoopRunning;
            trigger.OnTwitchPollStarted -= OnTwitchPollStarted;
            trigger.OnTwitchPollClosed -= OnTwitchPollClosed;
            trigger.OnTwitchPollEnded -= OnTwitchPollEnded;
            modEntityManager.RemoveEntity(this);
            CleanUpUIEntities();
        }

        /// <summary>
        /// Called by <see cref="IGameStateObserver.OnGameLoopNotRunning"/> hides all active UI entities
        /// </summary>
        private void OnGameLoopNotRunning()
        {
            logger.Information($"Called OnGameLoopNotRunning in TwitchPollVisual - Hiding all UI");
            HideAllUI();
        }

        /// <summary>
        /// Called by <see cref="IGameStateObserver.OnGameLoopRunning"/> shows all active UI entities
        /// </summary>
        private void OnGameLoopRunning()
        {
            logger.Information($"Called OnGameLoopRunning in TwitchPollVisual - Showing all UI");
            ShowAllUI();
        }

        /// <summary>
        /// Hides all UI entities
        /// </summary>
        private void HideAllUI()
        {
            if (pollDescriptionEntity != null)
            {
                pollDescriptionEntity.IsEnabled = false;
            }
            if (pollCountdownEntity != null)
            {
                pollCountdownEntity.IsEnabled = false;
            }
            if (pollOptionEntities != null)
            {
                for (int i = 0; i < pollOptionEntities.Count; i++)
                {
                    if (pollOptionEntities[i].Item2 != null)
                    {
                        pollOptionEntities[i].Item2.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Shows all UI entities
        /// </summary>
        private void ShowAllUI()
        {
            if (pollDescriptionEntity != null)
            {
                pollDescriptionEntity.IsEnabled = true;
            }
            if (pollCountdownEntity != null)
            {
                pollCountdownEntity.IsEnabled = true;
            }
            if (pollOptionEntities != null)
            {
                for (int i = 0; i < pollOptionEntities.Count; i++)
                {
                    if (pollOptionEntities[i].Item2 != null)
                    {
                        pollOptionEntities[i].Item2.IsEnabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Called by <see cref="TwitchPollTrigger.OnTwitchPollStarted"/>
        /// </summary>
        private void OnTwitchPollStarted(ModifierTwitchPoll poll)
        {
            logger.Information($"Received OnTwitchPollStarted Event");
            if (poll == null)
            {
                logger.Error($"Unable to create a visual as the provided Twitch Poll is null!");
                return;
            }

            currentPoll = poll;

            // Make the description text
            string descriptionText = "Vote on the modifier to activate by typing in chat!";
            Vector2 descriptionPosition = JumpGame.GAME_RECT.Size.ToVector2();
            descriptionPosition.Y = 0;
            descriptionPosition.X -= InitialPositionXPadding;
            pollDescriptionEntity = new UITextEntity(modEntityManager, descriptionPosition, descriptionText,
                Color.White, UIEntityAnchor.TopRight, JKContentManager.Font.MenuFontSmall, zOrder: 2);
            float currentY = descriptionPosition.Y + pollDescriptionEntity.TextFont.MeasureString(descriptionText).Y;

            // Make the countdown text
            string countdownText = $"Time Remaining: {((int)currentPoll.TimeRemainingInSeconds).ToString().PadLeft(2, '0')}";
            Vector2 countdownPosition = JumpGame.GAME_RECT.Size.ToVector2();
            countdownPosition.Y = currentY;
            countdownPosition.X -= InitialPositionXPadding;
            pollCountdownEntity = new UITextEntity(modEntityManager, countdownPosition, countdownText, 
                Color.White, UIEntityAnchor.TopRight, JKContentManager.Font.MenuFontSmall, zOrder: 1);
            currentY += (CountdownYPadding + pollDescriptionEntity.TextFont.MeasureString(descriptionText).Y);

            // Make each of the choices
            var choicesList = currentPoll.Choices.Values.ToList();
            for (int i = 0; i < choicesList.Count; i++)
            {

                string pollOptionText = GetPollOptionText(choicesList[i]);
                Vector2 pollOptionPosition = JumpGame.GAME_RECT.Size.ToVector2();
                pollOptionPosition.Y = currentY;
                pollOptionPosition.X -= InitialPositionXPadding;
                var pollOptionEntity = new UITextEntity(modEntityManager, pollOptionPosition, pollOptionText,
                    Color.White, UIEntityAnchor.TopRight, JKContentManager.Font.MenuFontSmall, zOrder: 1);
                pollOptionEntities.Add(new Tuple<ModifierTwitchPollOption, UITextEntity>(choicesList[i], pollOptionEntity));
                
                currentY += (YPadding + pollOptionEntity.TextFont.MeasureString(pollOptionText).Y);
            }
        }

        /// <summary>
        /// Called by <see cref="TwitchPollTrigger.OnTwitchPollClosed"/>
        /// </summary>
        private void OnTwitchPollClosed(ModifierTwitchPoll poll)
        {
            // Get the winner
            ModifierTwitchPollOption winningOption = poll.FindWinningModifier();

            // Set the winning item to the right colour
            for (int i = 0; i < pollOptionEntities.Count; i++)
            {
                if (pollOptionEntities[i].Item1 == winningOption)
                {
                    pollOptionEntities[i].Item2.TextColor = new Color(100, 200, 100, 255);
                }
                else
                {
                    pollOptionEntities[i].Item2.TextColor = new Color(Color.Gray.R, Color.Gray.G, Color.Gray.B, (byte)(Color.Gray.A * 0.5f));
                }
            }
        }

        /// <summary>
        /// Called by <see cref="TwitchPollTrigger.OnTwitchPollEnded"/>
        /// </summary>
        private void OnTwitchPollEnded(ModifierTwitchPoll poll)
        {
            // Clean up the poll
            logger.Information($"Received OnTwitchPollEnded Event");
            currentPoll = null;
            CleanUpUIEntities();
        }

        /// <summary>
        /// Returns a string representing a poll option and it's current count
        /// </summary>
        private string GetPollOptionText(ModifierTwitchPollOption pollOption)
        {
            return $"{pollOption.ChoiceNumber}. {pollOption.Modifier.DisplayName} - {pollOption.Count}";
        }

        /// <summary>
        /// Disposes and cleans up all known UI entities for the visual
        /// </summary>
        private void CleanUpUIEntities()
        {
            pollDescriptionEntity?.Dispose();
            pollDescriptionEntity = null;
            pollCountdownEntity?.Dispose();
            pollCountdownEntity = null;
            for (int i = 0; i < pollOptionEntities.Count; i++)
            {
                pollOptionEntities[i].Item2?.Dispose();
            }
            pollOptionEntities.Clear();
        }

        /// <inheritdoc/>
        public void Draw()
        {
            // Do nothing
        }

        /// <inheritdoc/>
        public void Update(float p_delta)
        {
            if (currentPoll != null)
            {
                string countdownText = $"Time Remaining: {((int)currentPoll.TimeRemainingInSeconds).ToString().PadLeft(2, '0')}";
                pollCountdownEntity.TextValue = countdownText;

                // Update the current options
                for (int i = 0; i < pollOptionEntities.Count; i++)
                {
                    ModifierTwitchPollOption option = pollOptionEntities[i].Item1;
                    UITextEntity optionEntity = pollOptionEntities[i].Item2;
                    optionEntity.TextValue = GetPollOptionText(option);
                }
            }
        }
    }
}
