﻿using BehaviorTree;
using HarmonyLib;
using JumpKing.Player;
using JumpKing.SaveThread;
using JumpKingModifiersMod.API;
using JumpKingModifiersMod.Patching.States;
using Logging.API;
using Microsoft.Xna.Framework;
using PBJKModBase.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JumpKingModifiersMod.Patching
{
    /// <summary>
    /// An implementation of <see cref="IManualPatch"/> and <see cref="IPlayerStateObserver"/> to keep track of the player state
    /// </summary>
    public class PlayerStateObserverManualPatch : IManualPatch, IPlayerStateObserver
    {
        private static ILogger logger;

        private static FieldInfo knockedField;
        private static FieldInfo leftField;
        private static FieldInfo rightField;
        private static FieldInfo jumpField;
        private static object bodyCompInstance;
        private static bool knockedOverrideIsActive;
        private static bool knockedOverrideValue;
        private static bool directionOverrideIsActive;
        private static int directionOverrideValue;
        private static bool isWalkingDisabled;
        private static bool isXVelocityDisabled;
        private static bool isInputInverted;
        private static bool isDrawDisabled;

        private static InputState prevInputState;

        /// <summary>
        /// Ctor for creating a <see cref="PlayerStateObserverManualPatch"/>
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> implementation to use</param>
        public PlayerStateObserverManualPatch(ILogger logger)
        {
            PlayerStateObserverManualPatch.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            knockedOverrideIsActive = false;
            knockedOverrideValue = false;
            isWalkingDisabled = false;
            isXVelocityDisabled = false;
            isInputInverted = false;
        }

        /// <inheritdoc/>
        public void SetUpManualPatch(Harmony harmony)
        {
            var bodyCompMethod = AccessTools.Method("JumpKing.Player.BodyComp:Update");
            var postfixBodyCompMethod = AccessTools.Method($"JumpKingModifiersMod.Patching.{this.GetType().Name}:PostfixBodyCompPatchMethod");
            harmony.Patch(bodyCompMethod, new HarmonyMethod(postfixBodyCompMethod));

            var playerEntitySetDirectionMethod = AccessTools.Method("JumpKing.Player.PlayerEntity:SetDirection");
            var prefixPlayerSetDirectionEntityMethod = AccessTools.Method($"JumpKingModifiersMod.Patching.{this.GetType().Name}:PrefixPlayerEntityPatchMethod");
            harmony.Patch(playerEntitySetDirectionMethod, prefix: new HarmonyMethod(prefixPlayerSetDirectionEntityMethod));

            var walkComponentMyRunMethod = AccessTools.Method("JumpKing.Player.Walk:MyRun");
            var prefixWalkComponentMyRunMethod = AccessTools.Method($"JumpKingModifiersMod.Patching.{this.GetType().Name}:PrefixWalkComponentMyRunMethod");
            harmony.Patch(walkComponentMyRunMethod, prefix: new HarmonyMethod(prefixWalkComponentMyRunMethod));

            var walkAnimComponentMyRunMethod = AccessTools.Method("JumpKing.Player.WalkAnim:MyRun");
            var prefixWalkAnimComponentMyRunMethod = AccessTools.Method($"JumpKingModifiersMod.Patching.{this.GetType().Name}:PrefixWalkAnimComponentMyRunMethod");
            harmony.Patch(walkAnimComponentMyRunMethod, prefix: new HarmonyMethod(prefixWalkAnimComponentMyRunMethod));

            var inputComponentGetStateMethod = AccessTools.Method("JumpKing.Player.InputComponent:GetState");
            var postfixInputComponentGetStateMethod = AccessTools.Method($"JumpKingModifiersMod.Patching.{this.GetType().Name}:PostfixInputComponentGetStateMethod");
            harmony.Patch(inputComponentGetStateMethod, postfix: new HarmonyMethod(postfixInputComponentGetStateMethod));

            var playerEntityDrawMethod = AccessTools.Method("JumpKing.Player.PlayerEntity:Draw");
            var prefixPlayerEntityDrawMethod = AccessTools.Method($"JumpKingModifiersMod.Patching.{this.GetType().Name}:PrefixPlayerEntityDrawMethod");
            harmony.Patch(playerEntityDrawMethod, prefix: new HarmonyMethod(prefixPlayerEntityDrawMethod));
        }

        /// <summary>
        /// Called before 'JumpKing.Player.Walk:MyRun' and optionally skips the method execution
        /// </summary>
        public static bool PrefixWalkComponentMyRunMethod(ref BTresult __result, TickData p_data)
        {
            // If we want to disable walking then exit early
            if (isWalkingDisabled)
            {
                if (JumpKing.GameManager.GameLoop.m_player?.m_body != null && isXVelocityDisabled)
                {
                    Vector2 curVelocity = JumpKing.GameManager.GameLoop.m_player.m_body.velocity;
                    JumpKing.GameManager.GameLoop.m_player.m_body.velocity = new Vector2(0, curVelocity.Y);
                }

                __result = BTresult.Success;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Called before 'JumpKing.Player.WalkAnim:MyRun' and optionally skips the method execution
        /// </summary>
        public static bool PrefixWalkAnimComponentMyRunMethod(ref BTresult __result, TickData p_data)
        {
            // If we want to disable walking then exit early
            if (isWalkingDisabled)
            {
                __result = BTresult.Success;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Called after 'JumpKing.Player.InputComponent:GetState' and records the last received input state
        /// </summary>
        public static void PostfixInputComponentGetStateMethod(ref object __result)
        {
            if (leftField == null)
            {
                leftField = AccessTools.Field(__result.GetType(), "left");
            }
            if (rightField == null)
            {
                rightField = AccessTools.Field(__result.GetType(), "right");
            }
            if (jumpField == null)
            {
                jumpField = AccessTools.Field(__result.GetType(), "jump");
            }
            bool left = (bool)leftField.GetValue(__result);
            bool right = (bool)rightField.GetValue(__result);
            bool jump = (bool)jumpField.GetValue(__result);

            // Invert the input in the result and also for our caching
            if (isInputInverted)
            {
                rightField.SetValue(__result, left);
                leftField.SetValue(__result, right);
                bool tempRight = right;
                right = left;
                left = tempRight;
            }

            prevInputState = new InputState(left, right, jump);
        }

        /// <summary>
        /// Runs after <see cref="JumpKing.Player.BodyComp.Update(float)"/> and allows us to track the state of the player
        /// </summary>
        public static void PostfixBodyCompPatchMethod(object __instance, float p_delta)
        {
            bodyCompInstance = __instance;
            if (knockedField == null)
            {
                knockedField = AccessTools.Field(bodyCompInstance.GetType(), "_knocked");
            }

            if (knockedOverrideIsActive)
            {
                knockedField.SetValue(bodyCompInstance, knockedOverrideValue);
            }
        }

        /// <summary>
        /// Runs before 'JumpKing.Player.PlayerEntity:SetDirection' and allows us to override the direction
        /// </summary>
        public static void PrefixPlayerEntityPatchMethod(object __instance, ref int p_direction)
        {
            if (directionOverrideIsActive)
            {
                p_direction = directionOverrideValue;
            }
        }

        /// <summary>
        /// Runs before 'JumpKing.Player.PlayerEntity:Draw
        /// </summary>
        public static bool PrefixPlayerEntityDrawMethod(object __instance)
        {
            if (isDrawDisabled)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the cached player state, or null if it hasnt been polled yet
        /// </summary>
        public PlayerState GetPlayerState()
        {
            if (JumpKing.GameManager.GameLoop.m_player?.m_body != null)
            {
                bool isOnGround = JumpKing.GameManager.GameLoop.m_player.m_body.IsOnGround;
                bool isOnSnow = JumpKing.GameManager.GameLoop.m_player.m_body.IsOnSnow;
                bool isInWater = JumpKing.GameManager.GameLoop.m_player.m_body.IsInWater;
                Vector2 velocity = JumpKing.GameManager.GameLoop.m_player.m_body.velocity;
                Vector2 position = JumpKing.GameManager.GameLoop.m_player.m_body.position;
                bool knocked = JumpKing.GameManager.GameLoop.m_player.m_body.IsKnocked;
                PlayerState state = new PlayerState(isOnGround, isOnSnow, isInWater, velocity, position, knocked);
                return state;
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public void SetKnockedStateOverride(bool isActive, bool newState)
        {
            knockedOverrideIsActive = isActive;
            knockedOverrideValue = newState;
        }

        /// <inheritdoc/>
        public void SetDirectionOverride(bool isActive, int newDirection)
        {
            directionOverrideIsActive = isActive;
            directionOverrideValue = newDirection;
        }

        /// <inheritdoc/>
        public void RestartPlayerPosition()
        {
            if (JumpKing.GameManager.GameLoop.m_player != null)
            {
                logger.Information($"Applying Default Save State!");
                SaveState defaultSaveState = SaveState.GetDefault();
                defaultSaveState.position -= new Vector2(0, 10); // Move the player up a tiny bit, fixes odd issues where we clip into the ground a tad
                JumpKing.GameManager.GameLoop.m_player.ApplySaveState(defaultSaveState);
            }
        }

        /// <inheritdoc/>
        public void DisablePlayerWalking(bool isWalkingDisabled, bool isXVelocityDisabled = false)
        {
            PlayerStateObserverManualPatch.isWalkingDisabled = isWalkingDisabled;
            if (isWalkingDisabled)
            {
                PlayerStateObserverManualPatch.isXVelocityDisabled = isXVelocityDisabled;
            }
            else
            {
                PlayerStateObserverManualPatch.isXVelocityDisabled = false;
            }
        }

        /// <inheritdoc/>
        public InputState GetInputState()
        {
            return prevInputState;
        }

        /// <inheritdoc/>
        public void SetInvertPlayerInputs(bool invertPlayerInputs)
        {
            isInputInverted = invertPlayerInputs;
        }

        /// <inheritdoc/>
        public bool GetInvertPlayerInputs()
        {
            return isInputInverted;
        }

        /// <inheritdoc/>
        public Rectangle GetPlayerHitbox()
        {
            return JumpKing.GameManager.GameLoop.m_player.m_body.GetHitbox();
        }

        /// <inheritdoc/>
        public void SetPosition(Vector2 position)
        {
            JumpKing.GameManager.GameLoop.m_player.m_body.position = position;
        }

        /// <inheritdoc/>
        public void DisablePlayerDrawing(bool isDrawDisabled)
        {
            PlayerStateObserverManualPatch.isDrawDisabled = isDrawDisabled;
        }
    }
}
