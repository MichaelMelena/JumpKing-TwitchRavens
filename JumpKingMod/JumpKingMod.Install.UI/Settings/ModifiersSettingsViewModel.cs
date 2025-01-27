﻿using JumpKingMod.Install.UI.API;
using JumpKingModifiersMod.Settings;
using JumpKingModifiersMod.Settings.ViewModels;
using JumpKingRavensMod.Install.UI;
using Logging.API;
using Microsoft.Xna.Framework.Input;
using Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace JumpKingMod.Install.UI.Settings
{
    /// <summary>
    /// An aggregate of all settings for the Modifiers Mod
    /// </summary>
    public class ModifiersSettingsViewModel : INotifyPropertyChanged, IInstallerSettingsViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly DelegateCommand updateSettingsCommand;
        private readonly DelegateCommand loadSettingsCommand;
        private readonly ILogger logger;
        private readonly List<ConfigurableModifierViewModel> modifierViewModels;
        private readonly StackPanel modifiersStackPanel;

        #region Properties
        /// <summary>
        /// Whether the Resizing mod will be enabled or not
        /// </summary>
        public bool ManualResizingEnabled
        {
            get
            {
                return resizingEnabled;
            }
            set
            {
                if (resizingEnabled != value)
                {
                    resizingEnabled = value;
                    RaisePropertyChanged(nameof(ManualResizingEnabled));
                }
            }
        }
        private bool resizingEnabled;

        /// <summary>
        /// The key to press to toggle the Resize Mod
        /// </summary>
        public Keys ManualResizingToggleKey
        {
            get
            {
                return manualResizingToggleKey;
            }
            set
            {
                if (manualResizingToggleKey != value)
                {
                    manualResizingToggleKey = value;
                    RaisePropertyChanged(nameof(ManualResizingToggleKey));
                }
            }
        }
        private Keys manualResizingToggleKey;

        /// <summary>
        /// The key to press to grow the screen in the Resize Mod
        /// </summary>
        public Keys ManualResizingGrowKey
        {
            get
            {
                return manualResizingGrowKey;
            }
            set
            {
                if (manualResizingGrowKey != value)
                {
                    manualResizingGrowKey = value;
                    RaisePropertyChanged(nameof(ManualResizingGrowKey));
                }
            }
        }
        private Keys manualResizingGrowKey;

        /// <summary>
        /// The key to press to shrink the screen in the Resize Mod
        /// </summary>
        public Keys ManualResizingShrinkKey
        {
            get
            {
                return manualResizingShrinkKey;
            }
            set
            {
                if (manualResizingShrinkKey != value)
                {
                    manualResizingShrinkKey = value;
                    RaisePropertyChanged(nameof(ManualResizingShrinkKey));
                }
            }
        }
        private Keys manualResizingShrinkKey;

        /// <summary>
        /// The type of trigger to use
        /// </summary>
        public ModifierTriggerTypes TriggerType
        {
            get
            {
                return triggerType;
            }
            set
            {
                if (triggerType != value)
                {
                    triggerType = value;
                    RaisePropertyChanged(nameof(TriggerType));
                    RaisePropertyChanged(nameof(ShouldShowToggleKeys));
                    RaisePropertyChanged(nameof(ShowAllModifierSettings));
                    RaisePropertyChanged(nameof(IsChatPoll));
                }
            }
        }
        private ModifierTriggerTypes triggerType;

        /// <summary>
        /// The duration of the poll in seconds
        /// </summary>
        public string PollDurationInSeconds
        {
            get
            {
                return pollDurationInSeconds.ToString(CultureInfo.InvariantCulture);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    pollDurationInSeconds = JumpKingModifiersModSettingsContext.DefaultBasePollTimeInSeconds;
                }
                else
                {
                    if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float newVal))
                    {
                        if (newVal < 0)
                        {
                            newVal = Math.Abs(newVal);
                        }
                        pollDurationInSeconds = newVal;
                    }
                }
                RaisePropertyChanged(nameof(PollDurationInSeconds));
            }
        }
        private float pollDurationInSeconds;

        /// <summary>
        /// The time the poll is closed in seconds
        /// </summary>
        public string PollClosedDurationInSeconds
        {
            get
            {
                return pollClosedDurationInSeconds.ToString(CultureInfo.InvariantCulture);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    pollClosedDurationInSeconds = JumpKingModifiersModSettingsContext.DefaultPollClosedTimeInSeconds;
                }
                else
                {
                    if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float newVal))
                    {
                        if (newVal < 0)
                        {
                            newVal = Math.Abs(newVal);
                        }
                        pollClosedDurationInSeconds = newVal;
                    }
                }
                RaisePropertyChanged(nameof(PollClosedDurationInSeconds));
            }
        }
        private float pollClosedDurationInSeconds;

        /// <summary>
        /// The time between polls in seconds
        /// </summary>
        public string TimeBetweenPollsInSeconds
        {
            get
            {
                return timeBetweenPollsInSeconds.ToString(CultureInfo.InvariantCulture);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    timeBetweenPollsInSeconds = JumpKingModifiersModSettingsContext.DefaultTimeBetweenPollsInSeconds;
                }
                else
                {
                    if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float newVal))
                    {
                        if (newVal < 0)
                        {
                            newVal = Math.Abs(newVal);
                        }
                        timeBetweenPollsInSeconds = newVal;
                    }
                }
                RaisePropertyChanged(nameof(TimeBetweenPollsInSeconds));
            }
        }
        private float timeBetweenPollsInSeconds;

        /// <summary>
        /// The time between polls in seconds
        /// </summary>
        public string ModifierDurationInSeconds
        {
            get
            {
                return modifierDurationInSeconds.ToString(CultureInfo.InvariantCulture);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    modifierDurationInSeconds = JumpKingModifiersModSettingsContext.DefaultBaseActiveModifierDurationInSeconds;
                }
                else
                {
                    if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float newVal))
                    {
                        if (newVal < 0)
                        {
                            newVal = Math.Abs(newVal);
                        }
                        modifierDurationInSeconds = newVal;
                    }
                }
                RaisePropertyChanged(nameof(ModifierDurationInSeconds));
            }
        }
        private float modifierDurationInSeconds;

        /// <summary>
        /// Whether we should show the configurable toggle keys
        /// </summary>
        public bool ShouldShowToggleKeys
        {
            get
            {
                return triggerType == ModifierTriggerTypes.Toggle;
            }
        }

        /// <summary>
        /// Whether we should hide all modifier settings
        /// </summary>
        public bool ShowAllModifierSettings
        {
            get
            {
                return triggerType != ModifierTriggerTypes.None;
            }
        }

        /// <summary>
        /// Whether the current trigger is a twitch pol
        /// </summary>
        public bool IsChatPoll
        {
            get
            {
                return triggerType == ModifierTriggerTypes.ChatPoll;
            }
        }

        /// <summary>
        /// Returns whether the fall damage mod settings are currently populated
        /// </summary>
        public bool AreFallDamageModSettingsLoaded
        {
            get
            {
                return ModifiersModSettings != null;
            }
        }

        /// <summary>
        /// The <see cref="UserSettings"/> object for the Fall Damage Mod settings
        /// </summary>
        public UserSettings ModifiersModSettings
        {
            get
            {
                return fallDamageModSettings;
            }
            private set
            {
                fallDamageModSettings = value;
                RaisePropertyChanged(nameof(ModifiersModSettings));
                RaisePropertyChanged(nameof(AreFallDamageModSettingsLoaded));
                updateSettingsCommand.RaiseCanExecuteChanged();
                loadSettingsCommand.RaiseCanExecuteChanged();
            }
        }
        private UserSettings fallDamageModSettings;

        #endregion

        /// <summary>
        /// Ctor for creating a <see cref="ModifiersSettingsViewModel"/>
        /// </summary>
        /// <param name="updateSettingsCommand">A <see cref="DelegateCommand"/> in the UI for handling the updating of settings</param>
        /// <param name="loadSettingsCommand">A <see cref="DelegateCommand"/> in the UI for handling the loading of settings</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ModifiersSettingsViewModel(DelegateCommand updateSettingsCommand, DelegateCommand loadSettingsCommand, ILogger logger,
            StackPanel modifiersStackPanel)
        {
            this.updateSettingsCommand = updateSettingsCommand ?? throw new ArgumentNullException(nameof(updateSettingsCommand));
            this.loadSettingsCommand = loadSettingsCommand ?? throw new ArgumentNullException(nameof(loadSettingsCommand));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.modifierViewModels = new List<ConfigurableModifierViewModel>();
            this.modifiersStackPanel = modifiersStackPanel ?? throw new ArgumentNullException(nameof(modifiersStackPanel));
        }

        /// <inheritdoc/>
        public bool LoadSettings(string gameDirectory, string modFolder, bool createIfDoesntExist)
        {
            if (string.IsNullOrWhiteSpace(gameDirectory))
            {
                logger.Error("Failed to load Modifier settings as provided Game Directory was empty!");
                return false;
            }

            // Create dynamic controls for modifiers
            string modifiersDLLPath = Path.Combine(modFolder, "JumpKingModifiersMod.dll");
            CreateModifiersUIControls(modifiersStackPanel, modifiersDLLPath);

            // Load in the settings
            string expectedSettingsFilePath = Path.Combine(gameDirectory, JumpKingModifiersModSettingsContext.SettingsFileName);
            if (File.Exists(expectedSettingsFilePath) || createIfDoesntExist)
            {
                if (ModifiersModSettings == null)
                {
                    ModifiersModSettings = new UserSettings(expectedSettingsFilePath, JumpKingModifiersModSettingsContext.GetDefaultSettings(), logger);
                }

                // Load the initial data
                TriggerType = ModifiersModSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.TriggerTypeKey, ModifierTriggerTypes.Toggle);
                PollDurationInSeconds = ModifiersModSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.PollDurationInSecondsKey, JumpKingModifiersModSettingsContext.DefaultBasePollTimeInSeconds.ToString(CultureInfo.InvariantCulture));
                PollClosedDurationInSeconds = ModifiersModSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.PollClosedDurationInSecondsKey, JumpKingModifiersModSettingsContext.DefaultPollClosedTimeInSeconds.ToString(CultureInfo.InvariantCulture));
                TimeBetweenPollsInSeconds = ModifiersModSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.TimeBetweenPollsInSecondsKey, JumpKingModifiersModSettingsContext.DefaultTimeBetweenPollsInSeconds.ToString(CultureInfo.InvariantCulture));
                ModifierDurationInSeconds = ModifiersModSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.ModifierDurationInSecondsKey, JumpKingModifiersModSettingsContext.DefaultBaseActiveModifierDurationInSeconds.ToString(CultureInfo.InvariantCulture));

                ManualResizingEnabled = ModifiersModSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.ManualResizeEnabledKey, false);
                ManualResizingToggleKey = ModifiersModSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.DebugTriggerManualResizeToggleKey, Keys.F9);
                ManualResizingGrowKey = ModifiersModSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.ManualResizeGrowKeyKey, Keys.Up);
                ManualResizingShrinkKey = ModifiersModSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.ManualResizeShrinkKeyKey, Keys.Down);

                // Parse the enabled setting
                string rawEnabledModifiers = ModifiersModSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.EnabledModifiersKey, "");
                HashSet<string> enabledModifiers = JumpKingModifiersModSettingsContext.ParseEnabledModifiers(rawEnabledModifiers);

                // Parse the toggle key setting
                string rawModifierToggleKeys = ModifiersModSettings.GetSettingOrDefault(JumpKingModifiersModSettingsContext.ModifierToggleKeysKey, "");
                Dictionary<string, Keys> toggleKeys = JumpKingModifiersModSettingsContext.ParseToggleKeys(rawModifierToggleKeys);

                // Go through all our known modifiers and populate them from our settings
                for (int i = 0; i < modifierViewModels.Count; i++)
                {
                    ConfigurableModifierViewModel configurableModifier = modifierViewModels[i];
                    configurableModifier.ModifierEnabled = enabledModifiers.Contains(configurableModifier.ModifierType.ToString());

                    if (toggleKeys.ContainsKey(configurableModifier.ModifierType.ToString()))
                    {
                        configurableModifier.ToggleKey = toggleKeys[configurableModifier.ModifierType.ToString()];
                    }

                    for (int j = 0; j < configurableModifier.ModifierSettings.Count; j++)
                    {
                        ModifierSettingViewModel modifierSetting = configurableModifier.ModifierSettings[j];
                        switch (modifierSetting.SettingType)
                        {
                            case ModifierSettingType.Bool:
                                modifierSetting.BoolSettingValue = ModifiersModSettings.GetSettingOrDefault(modifierSetting.SettingKey, (bool)modifierSetting.DefaultSettingValue);
                                break;
                            case ModifierSettingType.String:
                                modifierSetting.StringSettingValue = ModifiersModSettings.GetSettingOrDefault(modifierSetting.SettingKey, (string)modifierSetting.DefaultSettingValue);
                                break;
                            case ModifierSettingType.Float:
                                modifierSetting.FloatSettingValue = ModifiersModSettings.GetSettingOrDefault(modifierSetting.SettingKey, ((float)modifierSetting.DefaultSettingValue).ToString());
                                break;
                            case ModifierSettingType.Int:
                                modifierSetting.IntSettingValue = ModifiersModSettings.GetSettingOrDefault(modifierSetting.SettingKey, ((int)modifierSetting.DefaultSettingValue).ToString());
                                break;
                            case ModifierSettingType.Enum:
                                modifierSetting.EnumSettingValue = ModifiersModSettings.GetSettingOrDefault(modifierSetting.SettingKey, (Enum)modifierSetting.DefaultSettingValue, modifierSetting.EnumType).ToString();
                                break;
                        }
                    }
                }

                return true;
            }
            else
            {
                logger.Error($"Failed to load Modifier settings as the settings file couldnt be found at '{expectedSettingsFilePath}'");
                return false;
            }
        }

        /// <inheritdoc/>
        public bool SaveSettings(string gameDirectory, string modFolder)
        {
            if (ModifiersModSettings == null || string.IsNullOrWhiteSpace(gameDirectory))
            {
                logger.Error($"Failed to save modifier settings, either internal settings object ({ModifiersModSettings}) is null, or no game directory was provided ({gameDirectory})");
                return false;
            }

            ModifiersModSettings.SetOrCreateSetting(JumpKingModifiersModSettingsContext.TriggerTypeKey, TriggerType.ToString());
            ModifiersModSettings.SetOrCreateSetting(JumpKingModifiersModSettingsContext.PollDurationInSecondsKey, PollDurationInSeconds.ToString(CultureInfo.InvariantCulture));
            ModifiersModSettings.SetOrCreateSetting(JumpKingModifiersModSettingsContext.PollClosedDurationInSecondsKey, PollClosedDurationInSeconds.ToString(CultureInfo.InvariantCulture));
            ModifiersModSettings.SetOrCreateSetting(JumpKingModifiersModSettingsContext.TimeBetweenPollsInSecondsKey, TimeBetweenPollsInSeconds.ToString(CultureInfo.InvariantCulture));
            ModifiersModSettings.SetOrCreateSetting(JumpKingModifiersModSettingsContext.ModifierDurationInSecondsKey, ModifierDurationInSeconds.ToString(CultureInfo.InvariantCulture));

            ModifiersModSettings.SetOrCreateSetting(JumpKingModifiersModSettingsContext.ManualResizeEnabledKey, ManualResizingEnabled.ToString());
            ModifiersModSettings.SetOrCreateSetting(JumpKingModifiersModSettingsContext.DebugTriggerManualResizeToggleKey, ManualResizingToggleKey.ToString());
            ModifiersModSettings.SetOrCreateSetting(JumpKingModifiersModSettingsContext.ManualResizeGrowKeyKey, ManualResizingGrowKey.ToString());
            ModifiersModSettings.SetOrCreateSetting(JumpKingModifiersModSettingsContext.ManualResizeShrinkKeyKey, manualResizingShrinkKey.ToString());

            // Save all the modifier settings
            StringBuilder enabledStringBuilder = new StringBuilder();
            StringBuilder toggleKeyStringBuilder = new StringBuilder();
            for (int i = 0; i < modifierViewModels.Count; i++)
            {
                ConfigurableModifierViewModel configurableModifier = modifierViewModels[i];

                // Build up the enabled setting
                if (configurableModifier.ModifierEnabled)
                {
                    enabledStringBuilder.Append($"{configurableModifier.ModifierType.ToString()}");
                    if (i < (modifierViewModels.Count - 1))
                    {
                        enabledStringBuilder.Append(",");
                    }
                }

                // Build up the toggle key setting
                toggleKeyStringBuilder.Append($"{configurableModifier.ModifierType.ToString()}:{configurableModifier.ToggleKey.ToString()}");
                if (i < (modifierViewModels.Count - 1))
                {
                    toggleKeyStringBuilder.Append(",");
                }

                // Save each setting within the modifier
                for (int j = 0; j < configurableModifier.ModifierSettings.Count; j++)
                {
                    ModifierSettingViewModel modifierSetting = configurableModifier.ModifierSettings[j];
                    switch (modifierSetting.SettingType)
                    {
                        case ModifierSettingType.Bool:
                            ModifiersModSettings.SetOrCreateSetting(modifierSetting.SettingKey, modifierSetting.BoolSettingValue.ToString());
                            break;
                        case ModifierSettingType.String:
                            ModifiersModSettings.SetOrCreateSetting(modifierSetting.SettingKey, modifierSetting.StringSettingValue.ToString());
                            break;
                        case ModifierSettingType.Float:
                            ModifiersModSettings.SetOrCreateSetting(modifierSetting.SettingKey, modifierSetting.FloatSettingValue);
                            break;
                        case ModifierSettingType.Int:
                            ModifiersModSettings.SetOrCreateSetting(modifierSetting.SettingKey, modifierSetting.IntSettingValue);
                            break;
                        case ModifierSettingType.Enum:
                            ModifiersModSettings.SetOrCreateSetting(modifierSetting.SettingKey, modifierSetting.EnumSettingValue);
                            break;
                    }
                }
            }
            ModifiersModSettings.SetOrCreateSetting(JumpKingModifiersModSettingsContext.EnabledModifiersKey, enabledStringBuilder.ToString());
            ModifiersModSettings.SetOrCreateSetting(JumpKingModifiersModSettingsContext.ModifierToggleKeysKey, toggleKeyStringBuilder.ToString());

            return true;
        }

        /// <inheritdoc/>
        public bool AreSettingsLoaded()
        {
            return AreFallDamageModSettingsLoaded;
        }

        /// <summary>
        /// Invokes the <see cref="PropertyChanged"/> event
        /// </summary>
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Loads all possible modifiers from the modifier DLL and creates UI controls for each,
        /// which are then registered with the provided stack
        /// </summary>
        /// <remarks>This should really be user controls and data templates but I hate myself</remarks>
        private void CreateModifiersUIControls(StackPanel modifiersStack, string modifierDLLPath)
        {
            modifiersStack.Children.Clear();

            // Modifiers stack visibility
            Binding modifierStackVisibilityBinding = new Binding(nameof(ShowAllModifierSettings));
            modifierStackVisibilityBinding.Source = this;
            modifierStackVisibilityBinding.Converter = new BooleanToVisibilityConverter();
            modifierStackVisibilityBinding.Mode = BindingMode.OneWay;
            modifiersStack.SetBinding(StackPanel.VisibilityProperty, modifierStackVisibilityBinding);

            Assembly assembly = Assembly.LoadFrom(modifierDLLPath);
            List<Tuple<Type, ConfigurableModifierAttribute>> modifiers = assembly.GetTypes().Select((Type t) =>
            {
                ConfigurableModifierAttribute attribute = t.GetCustomAttribute<ConfigurableModifierAttribute>();
                if (attribute != null)
                {
                    return new Tuple<Type, ConfigurableModifierAttribute>(t, attribute);
                }
                return null;
            })
            .Where((Tuple<Type, ConfigurableModifierAttribute> tuple) => tuple != null)
            .OrderBy((Tuple<Type, ConfigurableModifierAttribute> tuple) => tuple.Item2.ConfigurableModifierName)
            .ToList();

            // A Master grid enforces column spacing between all options
            // we will dynamically create all the row definitions as we need them
            Grid modifierSettingGrid = new Grid();
            modifierSettingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            modifierSettingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            modifierSettingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            modifierSettingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            modifiersStack.Children.Add(modifierSettingGrid);
            int numberOfRowsNeeded = 0;

            // Make some headers to explain what the presented options are
            Label modifierNameHeader = new Label();
            modifierNameHeader.Content = "Modifier Name";
            modifierNameHeader.FontWeight = FontWeights.Bold;
            modifierNameHeader.Margin = new Thickness(5, 0, 10, 0);
            modifierNameHeader.VerticalAlignment = VerticalAlignment.Center;
            modifierNameHeader.HorizontalAlignment = HorizontalAlignment.Left;
            modifierNameHeader.SetValue(Grid.ColumnProperty, 0);
            modifierNameHeader.SetValue(Grid.RowProperty, numberOfRowsNeeded);

            Label toggleHeader = new Label();
            toggleHeader.Content = "Toggle Key";
            toggleHeader.FontWeight = FontWeights.Bold;
            toggleHeader.Margin = new Thickness(5, 0, 10, 0);
            toggleHeader.VerticalAlignment = VerticalAlignment.Center;
            toggleHeader.HorizontalAlignment = HorizontalAlignment.Right;
            toggleHeader.SetValue(Grid.ColumnProperty, 2);
            toggleHeader.SetValue(Grid.RowProperty, numberOfRowsNeeded);

            Binding toggleHeaderVisibilityBinding = new Binding(nameof(ShouldShowToggleKeys));
            toggleHeaderVisibilityBinding.Source = this;
            toggleHeaderVisibilityBinding.Converter = new BooleanToVisibilityConverter();
            toggleHeaderVisibilityBinding.Mode = BindingMode.OneWay;
            toggleHeader.SetBinding(Label.VisibilityProperty, toggleHeaderVisibilityBinding);

            Label enabledHeader = new Label();
            enabledHeader.Content = "Enabled";
            enabledHeader.FontWeight = FontWeights.Bold;
            enabledHeader.Margin = new Thickness(5, 0, 10, 0);
            enabledHeader.VerticalAlignment = VerticalAlignment.Center;
            enabledHeader.HorizontalAlignment = HorizontalAlignment.Right;
            enabledHeader.SetValue(Grid.ColumnProperty, 3);
            enabledHeader.SetValue(Grid.RowProperty, numberOfRowsNeeded);

            // Add to the grid
            modifierSettingGrid.Children.Add(modifierNameHeader);
            modifierSettingGrid.Children.Add(toggleHeader);
            modifierSettingGrid.Children.Add(enabledHeader);

            // Create the row needed to support this
            modifierSettingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            numberOfRowsNeeded++;

            // Go through all Modifiers
            modifierViewModels.Clear();
            for (int i = 0; i < modifiers.Count; i++)
            {
                // Collect any other settings for this modifier
                List<ModifierSettingAttribute> modifierSettings = modifiers[i].Item1.GetFields()
                    .Select((FieldInfo f) => f.GetCustomAttribute<ModifierSettingAttribute>()).Where((ModifierSettingAttribute a) => a != null).ToList();

                ConfigurableModifierViewModel configurableModifierViewModel =
                    new ConfigurableModifierViewModel(modifiers[i].Item1, modifiers[i].Item2.ConfigurableModifierName, modifiers[i].Item2.DefaultToggleKey);

                // Make the modifier name
                Label modifierName = new Label();
                modifierName.Content = modifiers[i].Item2.ConfigurableModifierName;
                modifierName.FontWeight = FontWeights.Medium;
                modifierName.Margin = new Thickness(5, 0, 10, 0);
                modifierName.VerticalAlignment = VerticalAlignment.Center;
                modifierName.SetValue(Grid.ColumnProperty, 0);
                modifierName.SetValue(Grid.RowProperty, numberOfRowsNeeded);
                if (!string.IsNullOrWhiteSpace(modifiers[i].Item2.ModifierDescription))
                {
                    ToolTip tooltip = new ToolTip();
                    tooltip.Content = modifiers[i].Item2.ModifierDescription;
                    modifierName.ToolTip = tooltip;
                }

                // Make the ComboBox for toggle keys
                ComboBox toggleKeyComboBox = new ComboBox();
                toggleKeyComboBox.ItemsSource = Enum.GetValues(typeof(Keys));
                toggleKeyComboBox.VerticalAlignment = VerticalAlignment.Center;
                toggleKeyComboBox.SetValue(Grid.ColumnProperty, 2);
                toggleKeyComboBox.SetValue(Grid.RowProperty, numberOfRowsNeeded);

                Binding toggleKeyBinding = new Binding(nameof(configurableModifierViewModel.ToggleKey));
                toggleKeyBinding.Source = configurableModifierViewModel;
                toggleKeyComboBox.SetBinding(ComboBox.SelectedItemProperty, toggleKeyBinding);

                Binding toggleKeyVisibilityBinding = new Binding(nameof(ShouldShowToggleKeys));
                toggleKeyVisibilityBinding.Source = this;
                toggleKeyVisibilityBinding.Converter = new BooleanToVisibilityConverter();
                toggleKeyVisibilityBinding.Mode = BindingMode.OneWay;
                toggleKeyComboBox.SetBinding(ComboBox.VisibilityProperty, toggleKeyVisibilityBinding);

                // Make the enabled toggle
                CheckBox enabledBox = new CheckBox();
                enabledBox.Margin = new Thickness(5, 0, 10, 0);
                enabledBox.VerticalAlignment = VerticalAlignment.Center;
                enabledBox.HorizontalAlignment = HorizontalAlignment.Right;
                enabledBox.SetValue(Grid.ColumnProperty, 3);
                enabledBox.SetValue(Grid.RowProperty, numberOfRowsNeeded);

                // Make the binding and assign the default value
                Binding enabledBinding = new Binding(nameof(configurableModifierViewModel.ModifierEnabled));
                enabledBinding.Source = configurableModifierViewModel;
                enabledBox.SetBinding(CheckBox.IsCheckedProperty, enabledBinding);
                enabledBox.IsChecked = true;

                // Add to the grid
                modifierSettingGrid.Children.Add(modifierName);
                modifierSettingGrid.Children.Add(toggleKeyComboBox);
                modifierSettingGrid.Children.Add(enabledBox);

                // Create the grid row needed to support this entry
                modifierSettingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                numberOfRowsNeeded++;

                // Cache the ViewModel
                modifierViewModels.Add(configurableModifierViewModel);

                // Make options for every defined setting
                for (int j = 0; j < modifierSettings.Count; j++)
                {
                    // Make the setting name
                    Label settingName = new Label();
                    settingName.Content = modifierSettings[j].DisplayName;
                    settingName.Margin = new Thickness(32, 0, 10, 0);
                    settingName.VerticalAlignment = VerticalAlignment.Center;
                    settingName.SetValue(Grid.ColumnProperty, 0);
                    settingName.SetValue(Grid.RowProperty, numberOfRowsNeeded);
                    modifierSettingGrid.Children.Add(settingName);

                    // Set the visibility binding
                    Binding settingNameVisibleBinding = new Binding(nameof(configurableModifierViewModel.ModifierEnabled));
                    settingNameVisibleBinding.Source = configurableModifierViewModel;
                    settingNameVisibleBinding.Converter = new BooleanToVisibilityConverter();
                    settingName.SetBinding(Label.VisibilityProperty, settingNameVisibleBinding);

                    // Make the value editor
                    switch (modifierSettings[j].SettingType)
                    {
                        case ModifierSettingType.Bool:
                            {
                                CheckBox settingBox = new CheckBox();
                                settingBox.Margin = new Thickness(5, 0, 10, 0);
                                settingBox.VerticalAlignment = VerticalAlignment.Center;
                                settingBox.HorizontalAlignment = HorizontalAlignment.Right;
                                settingBox.SetValue(Grid.ColumnProperty, 2);
                                settingBox.SetValue(Grid.ColumnSpanProperty, 2);
                                settingBox.SetValue(Grid.RowProperty, numberOfRowsNeeded);

                                ModifierSettingViewModel modifierSettingViewModel =
                                    new ModifierSettingViewModel(modifierSettings[j].SettingKey, modifierSettings[j].DefaultSetting, modifierSettings[j].SettingType);

                                // Make the binding and assign the default value
                                Binding boolBinding = new Binding(nameof(modifierSettingViewModel.BoolSettingValue));
                                boolBinding.Source = modifierSettingViewModel;
                                settingBox.SetBinding(CheckBox.IsCheckedProperty, boolBinding);
                                settingBox.IsChecked = (bool)modifierSettings[j].DefaultSetting;

                                // Set the visibility binding
                                Binding settingVisibleBinding = new Binding(nameof(configurableModifierViewModel.ModifierEnabled));
                                settingVisibleBinding.Source = configurableModifierViewModel;
                                settingVisibleBinding.Converter = new BooleanToVisibilityConverter();
                                settingBox.SetBinding(CheckBox.VisibilityProperty, settingVisibleBinding);

                                // Cache the ViewModel
                                configurableModifierViewModel.ModifierSettings.Add(modifierSettingViewModel);

                                // Add to the grid
                                modifierSettingGrid.Children.Add(settingBox);
                                break;
                            }
                        case ModifierSettingType.String:
                            {
                                TextBox textBox = new TextBox();
                                textBox.Margin = new Thickness(5, 0, 10, 0);
                                textBox.MinWidth = 100;
                                textBox.VerticalAlignment = VerticalAlignment.Center;
                                textBox.HorizontalAlignment = HorizontalAlignment.Right;
                                textBox.SetValue(Grid.ColumnProperty, 2);
                                textBox.SetValue(Grid.ColumnSpanProperty, 2);
                                textBox.SetValue(Grid.RowProperty, numberOfRowsNeeded);

                                ModifierSettingViewModel modifierSettingViewModel =
                                    new ModifierSettingViewModel(modifierSettings[j].SettingKey, modifierSettings[j].DefaultSetting, modifierSettings[j].SettingType);

                                // Make the binding and assign the default value
                                Binding stringBinding = new Binding(nameof(modifierSettingViewModel.StringSettingValue));
                                stringBinding.Source = modifierSettingViewModel;
                                textBox.SetBinding(TextBox.TextProperty, stringBinding);
                                textBox.Text = (string)modifierSettings[j].DefaultSetting;

                                // Set the visibility binding
                                Binding settingVisibleBinding = new Binding(nameof(configurableModifierViewModel.ModifierEnabled));
                                settingVisibleBinding.Source = configurableModifierViewModel;
                                settingVisibleBinding.Converter = new BooleanToVisibilityConverter();
                                textBox.SetBinding(TextBox.VisibilityProperty, settingVisibleBinding);

                                // Cache the ViewModel
                                configurableModifierViewModel.ModifierSettings.Add(modifierSettingViewModel);

                                // Add to the grid
                                modifierSettingGrid.Children.Add(textBox);
                                break;
                            }
                        case ModifierSettingType.Float:
                            {
                                TextBox textBox = new TextBox();
                                textBox.Margin = new Thickness(5, 0, 10, 0);
                                textBox.MinWidth = 100;
                                textBox.VerticalAlignment = VerticalAlignment.Center;
                                textBox.HorizontalAlignment = HorizontalAlignment.Right;
                                textBox.SetValue(Grid.ColumnProperty, 2);
                                textBox.SetValue(Grid.ColumnSpanProperty, 2);
                                textBox.SetValue(Grid.RowProperty, numberOfRowsNeeded);

                                ModifierSettingViewModel modifierSettingViewModel =
                                    new ModifierSettingViewModel(modifierSettings[j].SettingKey, modifierSettings[j].DefaultSetting, modifierSettings[j].SettingType);

                                // Make the binding and assign the default value
                                Binding floatBinding = new Binding(nameof(modifierSettingViewModel.FloatSettingValue));
                                floatBinding.Source = modifierSettingViewModel;
                                floatBinding.UpdateSourceTrigger = UpdateSourceTrigger.Default;
                                textBox.SetBinding(TextBox.TextProperty, floatBinding);
                                textBox.Text = ((float)modifierSettings[j].DefaultSetting).ToString();

                                // Set the visibility binding
                                Binding settingVisibleBinding = new Binding(nameof(configurableModifierViewModel.ModifierEnabled));
                                settingVisibleBinding.Source = configurableModifierViewModel;
                                settingVisibleBinding.Converter = new BooleanToVisibilityConverter();
                                textBox.SetBinding(TextBox.VisibilityProperty, settingVisibleBinding);

                                // Cache the ViewModel
                                configurableModifierViewModel.ModifierSettings.Add(modifierSettingViewModel);

                                // Add to the grid
                                modifierSettingGrid.Children.Add(textBox);
                                break;
                            }
                        case ModifierSettingType.Int:
                            {
                                TextBox textBox = new TextBox();
                                textBox.Margin = new Thickness(5, 0, 10, 0);
                                textBox.MinWidth = 100;
                                textBox.VerticalAlignment = VerticalAlignment.Center;
                                textBox.HorizontalAlignment = HorizontalAlignment.Right;
                                textBox.SetValue(Grid.ColumnProperty, 2);
                                textBox.SetValue(Grid.ColumnSpanProperty, 2);
                                textBox.SetValue(Grid.RowProperty, numberOfRowsNeeded);

                                ModifierSettingViewModel modifierSettingViewModel =
                                    new ModifierSettingViewModel(modifierSettings[j].SettingKey, modifierSettings[j].DefaultSetting, modifierSettings[j].SettingType);

                                // Make the binding and assign the default value
                                Binding intBinding = new Binding(nameof(modifierSettingViewModel.IntSettingValue));
                                intBinding.Source = modifierSettingViewModel;
                                intBinding.UpdateSourceTrigger = UpdateSourceTrigger.Default;
                                textBox.SetBinding(TextBox.TextProperty, intBinding);
                                textBox.Text = ((int)modifierSettings[j].DefaultSetting).ToString();

                                // Set the visibility binding
                                Binding settingVisibleBinding = new Binding(nameof(configurableModifierViewModel.ModifierEnabled));
                                settingVisibleBinding.Source = configurableModifierViewModel;
                                settingVisibleBinding.Converter = new BooleanToVisibilityConverter();
                                textBox.SetBinding(TextBox.VisibilityProperty, settingVisibleBinding);

                                // Cache the ViewModel
                                configurableModifierViewModel.ModifierSettings.Add(modifierSettingViewModel);

                                // Add to the grid
                                modifierSettingGrid.Children.Add(textBox);
                                break;
                            }
                        case ModifierSettingType.Enum:
                            {
                                ComboBox comboBox = new ComboBox();
                                comboBox.Margin = new Thickness(5, 0, 10, 0);
                                comboBox.ItemsSource = Enum.GetNames(modifierSettings[j].EnumType);
                                comboBox.VerticalAlignment = VerticalAlignment.Center;
                                comboBox.HorizontalAlignment = HorizontalAlignment.Right;
                                comboBox.SetValue(Grid.ColumnProperty, 2);
                                comboBox.SetValue(Grid.ColumnSpanProperty, 2);
                                comboBox.SetValue(Grid.RowProperty, numberOfRowsNeeded);

                                ModifierSettingViewModel modifierSettingViewModel =
                                    new ModifierSettingViewModel(modifierSettings[j].SettingKey, modifierSettings[j].DefaultSetting, modifierSettings[j].SettingType, modifierSettings[j].EnumType);

                                // Make the binding and assign the default value
                                Binding enumBinding = new Binding(nameof(modifierSettingViewModel.EnumSettingValue));
                                enumBinding.Source = modifierSettingViewModel;
                                comboBox.SetBinding(ComboBox.SelectedValueProperty, enumBinding);
                                comboBox.SelectedValue = modifierSettingViewModel.DefaultSettingValue.ToString();

                                // Set the visibility binding
                                Binding settingVisibleBinding = new Binding(nameof(configurableModifierViewModel.ModifierEnabled));
                                settingVisibleBinding.Source = configurableModifierViewModel;
                                settingVisibleBinding.Converter = new BooleanToVisibilityConverter();
                                comboBox.SetBinding(ComboBox.VisibilityProperty, settingVisibleBinding);

                                // Cache the ViewModel
                                configurableModifierViewModel.ModifierSettings.Add(modifierSettingViewModel);

                                // Add to the grid
                                modifierSettingGrid.Children.Add(comboBox);
                                break;
                            }
                    }

                    // Create the row needed to support this
                    modifierSettingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                    numberOfRowsNeeded++;
                }
            }
        }

    }
}
