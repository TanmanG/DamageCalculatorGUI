
using Pickings_For_Kurtulmak;
using ScottPlot.Drawing.Colormaps;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static DamageCalculatorGUI.DamageCalculator;


namespace DamageCalculatorGUI
{
    public partial class CalculatorWindow : Form
    {
        // Damage Stats Struct
        public DamageStats damageStats = new();
        public BatchResults damageStats_BATCH = new();
        public int[] batch_selected_variables = new[] { 0 };

        // Already Running a Sim
        private static bool computing_damage = false;

        // Batch Compute Variable
        private static bool hovering_entrybox = false;
        private static bool batch_mode_enabled = false;
        private static Control batch_mode_selected = null;
        private Dictionary<int, GroupBox> entrybox_groupBox_hashes = new();
        private Dictionary<int, EncounterSetting> control_hash_to_setting = new();
        private Dictionary<EncounterSetting, int> batched_variables_last_value = new();
        private Dictionary<EncounterSetting, BatchModeSettings> batched_variables = new();
        private Dictionary<EncounterSetting, Control> setting_to_control = new();
        private static readonly Dictionary<EncounterSetting, Tuple<DieField, bool, bool>> setting_to_info = new() // Conversions to get die type
        {
            { EncounterSetting.damage_dice_count, new (DieField.count, false, false) },
            { EncounterSetting.damage_dice_size, new (DieField.size, false, false) },
            { EncounterSetting.damage_dice_bonus, new (DieField.bonus, false, false) },

            { EncounterSetting.damage_dice_count_critical, new (DieField.count, false, true) },
            { EncounterSetting.damage_dice_size_critical, new (DieField.size, false, true) },
            { EncounterSetting.damage_dice_bonus_critical, new (DieField.bonus, false, true) },

            { EncounterSetting.damage_dice_DOT_count, new (DieField.count, true, false) },
            { EncounterSetting.damage_dice_DOT_size, new (DieField.size, true, false) },
            { EncounterSetting.damage_dice_DOT_bonus, new (DieField.bonus, true, false) },

            { EncounterSetting.damage_dice_DOT_count_critical, new (DieField.count, true, true) },
            { EncounterSetting.damage_dice_DOT_size_critical, new (DieField.size, true, true) },
            { EncounterSetting.damage_dice_DOT_bonus_critical, new (DieField.bonus, true, true) },
        };
        private static readonly Dictionary<EncounterSetting, string> setting_to_string = new()
        {
            { EncounterSetting.number_of_encounters, "Number of Encounters" },
            { EncounterSetting.rounds_per_encounter, "Rounds Per Encounter" },
            { EncounterSetting.actions_per_round_any, "Actions Per Round" },
            { EncounterSetting.actions_per_round_strike, "Extra Actions Per Round (Strike)" },
            { EncounterSetting.actions_per_round_draw, "Extra Actions Per Round (Draw)" },
            { EncounterSetting.actions_per_round_stride, "Extra Actions Per Round (Stride)" },
            { EncounterSetting.actions_per_round_reload, "Extra Actions Per Round (Reload)" },
            { EncounterSetting.actions_per_round_long_reload, "Extra Actions Per Round (Long Reload)" },
            { EncounterSetting.magazine_size, "Magazine Size" },
            { EncounterSetting.reload, "Reload" },
            { EncounterSetting.long_reload, "Long Reload" },
            { EncounterSetting.draw, "Draw" },
            { EncounterSetting.damage_dice_count, "Damage Die Count" },
            { EncounterSetting.damage_dice_size, "Damage Die Size" },
            { EncounterSetting.damage_dice_bonus, "Damage Die Bonus" },
            { EncounterSetting.damage_dice_count_critical, "Damage Die Count (Critical)" },
            { EncounterSetting.damage_dice_size_critical, "Damage Die Size (Critical)" },
            { EncounterSetting.damage_dice_bonus_critical, "Damage Die Bonus (Critical)" },
            { EncounterSetting.bonus_to_hit, "Bonus To Hit" },
            { EncounterSetting.AC, "AC" },
            { EncounterSetting.crit_threshhold, "Critical Hit Minimum" },
            { EncounterSetting.MAP_modifier, "MAP Modifier" },
            { EncounterSetting.engagement_range, "Engagement Start Range" },
            { EncounterSetting.move_speed, "Movement Speed" },
            { EncounterSetting.range, "Range Increment" },
            { EncounterSetting.volley, "Volley Increment" },
            { EncounterSetting.damage_dice_DOT_count, "Damage Die Count (Bleed)" },
            { EncounterSetting.damage_dice_DOT_size, "Damage Die Size (Bleed)" },
            { EncounterSetting.damage_dice_DOT_bonus, "Damage Die Bonus (Bleed)" },
            { EncounterSetting.damage_dice_DOT_count_critical, "Damage Die Count (Bleed, Critical)" },
            { EncounterSetting.damage_dice_DOT_size_critical, "Damage Die Size (Bleed, Critical)" },
            { EncounterSetting.damage_dice_DOT_bonus_critical, "Damage Die Bonus (Bleed, Critical)" }
        };
        private static double computationStepsTotal = 0;
        private static double computationStepsCurrent = 0;

        public enum EncounterSetting
        {
            number_of_encounters,
            rounds_per_encounter,
            actions_per_round_any,
            actions_per_round_strike,
            actions_per_round_draw,
            actions_per_round_stride,
            actions_per_round_reload,
            actions_per_round_long_reload,
            magazine_size,
            reload,
            long_reload,
            draw,
            bonus_to_hit,
            AC,
            crit_threshhold,
            MAP_modifier,
            engagement_range,
            move_speed,
            seek_favorable_range,
            range,
            volley,
            damage_dice_count,
            damage_dice_size,
            damage_dice_bonus,
            damage_dice_count_critical,
            damage_dice_size_critical,
            damage_dice_bonus_critical,
            damage_dice_DOT_count,
            damage_dice_DOT_size,
            damage_dice_DOT_bonus,
            damage_dice_DOT_count_critical,
            damage_dice_DOT_size_critical,
            damage_dice_DOT_bonus_critical,
        }

    // Batch Setting, represents the batch configuration for the given value.
        public struct BatchModeSettings
        {
            public int layer; // What layer to scale on. Equal means both iterate simultaneously, below means will iterate once every other layer finishes.
            public int start; // Initial value
            public int end; // End value 
            public List<int> steps; // Pattern of steps to be taken in total. Repeating list.
            public int number_of_steps; // Number of times to be stepped in total.
            public int step_direction; // Overall direction of steps
            public Dictionary<int, int> value_at_step; // Number of steps taken to reach the given value
            public bool initialized;

            public int index;
            public DieField die_field;
            public bool bleed;
            public bool critical;

            public BatchModeSettings(int layer, int start, int end, List<int> steps, bool bleed = false, int index = -1, DieField die_field = DieField.count, bool critical = false)
            {
                this.layer = layer;
                this.start = start;
                this.end = end;
                this.steps = steps;
                this.index = index;
                this.bleed = bleed;
                this.die_field = die_field;
                this.critical = critical;

                value_at_step = new();
                initialized = false;
                number_of_steps = 0;

                step_direction = Math.Sign(steps.Sum()); // Compute overall direction of the steps list
            }

            public int GetValueAtStep(int target_step = -1)
            {
                if (value_at_step.TryGetValue(target_step, out int value))
                    return value;
                // To-do: Maybe optimize this to get the key closest to the target step as the initial values?

                // Store the base value
                int valueAtTargetStep = start;

                // Store the current step
                int currentStep = 0;

                if (end > start)
                {
                    // Step until the target_step is reached
                    while ((target_step != -1 && currentStep != target_step)  // The target step is reached if provided
                            || (target_step == -1 && valueAtTargetStep < end))// End has been reached if provided
                    { // Iterate steps until the target step is reached

                        // Increase the current value by the step, clamped to not overflow the index 
                        valueAtTargetStep = Math.Clamp(value: valueAtTargetStep + steps[HelperFunctions.Mod(currentStep, steps.Count)],
                                                        min: int.MinValue, max: end);

                        // Cache the current step value
                        value_at_step.TryAdd(currentStep, valueAtTargetStep);

                        // Iterate the current step
                        currentStep++;
                    }
                }
                else
                {
                    // Step until the target_step is reached
                    while ((target_step != -1 && currentStep != target_step)  // The target step is reached if provided
                            || (target_step == -1 && valueAtTargetStep > end))// End has been reached if provided
                    { // Iterate steps until the target step is reached

                        // Increase the current value by the step, clamped to not overflow the index 
                        valueAtTargetStep = Math.Clamp(value: valueAtTargetStep + steps[HelperFunctions.Mod(currentStep, steps.Count)],
                                                        min: end, max: int.MaxValue);

                        // Cache the current step value
                        value_at_step.TryAdd(currentStep, valueAtTargetStep);

                        // Iterate the current step
                        currentStep++;
                    }
                }
                

                if (target_step == -1)
                    number_of_steps = currentStep;

                return valueAtTargetStep;
            }
        }
        // Help Mode Variable
        public static bool help_mode_enabled;
        Dictionary<int, string> label_hashes_help = new();
        public struct EncounterSettings
        {
            // Damage Computation Variables
            public int number_of_encounters = 10000;
            public int rounds_per_encounter = 6;
            public Actions actions_per_round = default;
            public int magazine_size = 0;
            public int reload = 0;
            public int long_reload = 0;
            public int draw = 0;
            public List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> damage_dice = new(); // List of <X, Y. Z> <M, N, O> where XDY+Z, on crit MDN+O
            public int bonus_to_hit = 0; // Bonus added to-hit.
            public int AC = 0; // AC to test against. Tie attack roll hits.
            public int crit_threshhold = 0; // Raw attack roll at and above this critically strike.
            public int MAP_modifier = 0; // Modifier applied to MAP. By default 0 and thus -5, -10.
            public int engagement_range = 0; // Range to start the combat encounter at.
            public int move_speed = 0; // Distance covered by Stride.
            public bool seek_favorable_range = false; // Whether to Stride to an optimal firing position before firing.
            public int range = 0; // Range increment. Past this and for every increment thereafter applies stacking -2 penalty to-hit.
            public int volley = 0; // Minimum range increment. Firing within this applies a -2 penalty to-hit.
            public List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> damage_dice_DOT = new(); // DOT damage to apply on a hit/crit.

            public EncounterSettings() {}
            public void ResetSettings()
            {
                // Reset Settings
                number_of_encounters = 10000;
                rounds_per_encounter = 6;
                actions_per_round = new();
                magazine_size = 0;
                reload = 1;
                long_reload = 0;
                draw = 1;
                damage_dice = new() { new(new(1, 6, 0), new(1, 8, 1)) };
                bonus_to_hit = 10;
                AC = 21;
                crit_threshhold = 20;
                MAP_modifier = 0;
                engagement_range = 30;
                move_speed = 25;
                seek_favorable_range = true;
                range = 100;
                volley = 0;
                damage_dice_DOT = new() { new(new(0, 0, 0), new(0, 0, 0)) };
            }
        }
        EncounterSettings currEncounterSettings = new();

        // BASE LOAD FUNCTIONS
        //
        public CalculatorWindow()
        {
            InitializeComponent();
        }
        private void CalculatorWindowLoad(object sender, EventArgs e)
        {
            // Set graph appearances to be something normal
            InitializeGraphStartAppearance();

            // Generate and store label hashes for the help mode
            StoreLabelHelpHashes();

            // Generate and store entry hashes for batch entry mode & entry locking
            StoreEntryBoxParentBatchHashes();

            // Generate and store Setting hashes and vice versa
            StoreSettingToControl();
            StoreControlToSetting();

            // Default Settings
            currEncounterSettings.ResetSettings();
            ResetVisuals(currEncounterSettings);

            // Lock Window Dimensions
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // Enable Carat Hiding
            AddCaretHidingEvents();

            CalculatorBatchComputeScottPlot.Render();
            CalculatorDamageDistributionScottPlot.Render();
        }

        private async void CalculateDamageStatsButton_MouseClick(object sender, MouseEventArgs e)
        {
            
            if (!HelperFunctions.IsControlDown())
            {
                if (computing_damage) // Prevent double-computing
                    return;

                // Update Data
                //try
                { // Check for exception
                    CheckComputeSafety();

                    // Safe!
                    currEncounterSettings = GetDamageSimulationVariables(currEncounterSettings);

                    // Compute Damage Stats
                    if (batch_mode_enabled)
                    {
                        var binned_compute_layers = ComputeBinnedLayersForBatch(batched_variables);
                        int[] maxSteps = ComputeMaxStepsArrayForBatch(binned_compute_layers);

                        // Track Progress Bar
                        Progress<int> progress = new();
                        progress.ProgressChanged += SetProgressBar;

                        damageStats_BATCH = await ComputeAverageDamage_BATCH(encounter_settings: currEncounterSettings,
                                                                            binned_compute_layers: binned_compute_layers,
                                                                            maxSteps: maxSteps,
                                                                            batched_variables: batched_variables,
                                                                            progress: progress);
                        
                        UpdateBatchGraph(damageStats_BATCH);
                    }
                    else
                    {
                        damageStats = await ComputeAverageDamage_SINGLE(number_of_encounters: currEncounterSettings.number_of_encounters,
                                                        rounds_per_encounter: currEncounterSettings.rounds_per_encounter,
                                                        actions_per_round: currEncounterSettings.actions_per_round,
                                                        reload_size: currEncounterSettings.magazine_size,
                                                        reload: currEncounterSettings.reload,
                                                        long_reload: currEncounterSettings.long_reload,
                                                        draw: currEncounterSettings.draw,
                                                        damage_dice: currEncounterSettings.damage_dice,
                                                        bonus_to_hit: currEncounterSettings.bonus_to_hit,
                                                        AC: currEncounterSettings.AC,
                                                        crit_threshhold: currEncounterSettings.crit_threshhold,
                                                        MAP_modifier: currEncounterSettings.MAP_modifier,
                                                        engagement_range: currEncounterSettings.engagement_range,
                                                        move_speed: currEncounterSettings.move_speed,
                                                        seek_favorable_range: currEncounterSettings.seek_favorable_range,
                                                        range: currEncounterSettings.range,
                                                        volley: currEncounterSettings.volley,
                                                        damage_dice_DOT: currEncounterSettings.damage_dice_DOT);
                        
                        // Compute 25th, 50th, and 75th Percentiles
                        Tuple<int, int, int> percentiles = HelperFunctions.ComputePercentiles(damageStats.damage_bins);
                        /// Update the GUI with the percentiles
                        UpdateStatisticsGUI(percentiles);

                        // Update Graph
                        UpdateBaseGraph(percentiles);
                    }

                    // Visually Update Graphs
                    CalculatorBatchComputeScottPlot.Render();
                    CalculatorDamageDistributionScottPlot.Render();
                }
                /* To-Do: REMOVE THIS WHEN TESTING IS FINISHED
                catch (Exception ex)
                { // Exception caught!
                    throw ex;
                    computing_damage = false;
                    PushErrorMessages(ex);
                    return;
                }*/
            }
            else
            {
                if (damageStats.damage_bins != null && damageStats.damage_bins.Count > 0)
                    // Store damage bins to the clipboard
                    Clipboard.SetText(string.Join(",", damageStats.damage_bins.OrderBy(x => x.Key).Select(x => x.Value)));
            }
        }

        private void AddCaretHidingEvents()
        {
            CalculatorEncounterStatisticsMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            CalculatorEncounterStatisticsMedianTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            CalculatorEncounterStatisticsUpperQuartileTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            CalculatorEncounterStatisticsLowerQuartileBoxTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            CalculatorMiscStatisticsRoundDamageMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            CalculatorMiscStatisticsAttackDamageMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            CalculatorMiscStatisticsAccuracyMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
        }

        // POPULATE DICTIONARIES/LISTS
        //
        private static void CollectionAddFromControlHash<T>(Dictionary<int, T> dictionary, Control control, T element)
        {
            dictionary.Add(control.GetHashCode(), element);
        }
        private void StoreLabelHelpHashes()
        {
            // Attack
            CollectionAddFromControlHash(label_hashes_help, CalculatorAttackBonusToHitLabel, "The total bonus to-hit. Around ~12 for a 4th level Gunslinger.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorAttackCriticalHitMinimumLabel, "Minimum die roll to-hit in order to critically strike. Only way to crit other than getting 10 over the AC.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorAttackACLabel, "Armor Class of the target to be tested against. Typically ~21 for a 4th level enemy.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorAttackMAPModifierLabel, "Modifier to the Multiple-Attack-Penalty. Typically 0 unless you are using an Agile weapon or have Agile Grace, in which this is equal to -1 and -2 respectively.");
            // Ammunition
            CollectionAddFromControlHash(label_hashes_help, CalculatorAmmunitionReloadLabel, "The number of Interact actions required to re-chamber a weapon after firing. I.e. a 3 action round would compose of Strike, Reload 1, Strike or Strike, Reload 2.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorAmmunitionMagazineSizeLabel, "The number of Strike actions that can be done before requiring a Long Reload.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorAmmunitionLongReloadLabel, "The number of Interact actions required to replenish the weapon's Magazine Size to max. Includes one complementary Reload. I.e. two 3 action rounds would compose of Strike, Reload 1, Strike then Long Reload 2, Strike");
            CollectionAddFromControlHash(label_hashes_help, CalculatorAmmunitionDrawLengthLabel, "The number of Interact actions required to draw the weapon. I.e. a 3 action round would compose of Draw 1, Strike, Reload 1.");
            // Damage
            CollectionAddFromControlHash(label_hashes_help, CalculatorDamageDieSizeLabel, "The base damage of this weapon on hit. Represented by [X]d[Y]+[Z], such that a Y sided die will be rolled X times with a flat Z added (or subtracted if negative).");
            CollectionAddFromControlHash(label_hashes_help, CalculatorDamageCriticalDieLabel, "The upgraded die quantity, size, and/or flat bonus from Critical Strikes. Represented in vanilla Pathfinder 2E as Brutal Critical.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorDamageBleedDieLabel, "The amount of Persistent Damage dealt on-hit by this weapon. Each applied instance of this is checked against a flat DC15 save every round to simulate a save.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorDamageCriticalBleedDieLabel, "The upgraded die quantity, size, and/or flat bonus to Persistent damage when Critically Striking.");
            // Reach
            CollectionAddFromControlHash(label_hashes_help, CalculatorReachRangeIncrementLabel, "The Range Increment of the weapon. Attacks attacks against targets beyond the range increment take a stacking -2 penalty to-hit for each increment away. I.e. Range Increment of 30ft will take a -4 at 70ft.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorReachVolleyIncrementLabel, "The Volley Increment of the weapon. Attacks made within this distance suffer a flat -2 to-hit.");
            // Encounter
            CollectionAddFromControlHash(label_hashes_help, CalculatorEncounterNumberOfEncountersLabel, "How many combat encounters to simulate. Default is around 100,000.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorEncounterRoundsPerEncounterLabel, "How many rounds to simulate per encounter. Higher simulates more drawn out encounters, shorter simulates more brief encounters.");
            // Actions
            CollectionAddFromControlHash(label_hashes_help, CalculatorActionActionsPerRoundLabel, "How many actions granted each round. Typically 3 unless affected by an effect such as Haste.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorActionExtraLimitedActionsLabel, "These actions can only be used for the specified action and are granted each round.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorActionExtraLimitedActionsDrawLabel, "How many Draw-only actions to be granted each round.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorActionExtraLimitedActionsReloadLabel, "How many Reload-only actions to be granted each round.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorActionExtraLimitedActionsStrideLabel, "How many Stride-only actions to be granted each round.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorActionExtraLimitedActionsStrikeLabel, "How many Strike-only actions to be granted each round.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorActionExtraLimitedActionsLongReloadLabel, "How many Long Reload-only actions to be granted each round.");
            // Distance
            CollectionAddFromControlHash(label_hashes_help, CalculatorEncounterEngagementRangeLabel, "Distance to begin the encounter at. Defaults to 30 if unchecked.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorReachMovementSpeedLabel, "Amount of distance covered with one Stride action. The simulated player will Stride into the ideal (within range and out of volley) firing position before firing.");
            // Encounter Damage Statistics
            CollectionAddFromControlHash(label_hashes_help, CalculatorEncounterStatisticsMeanLabel, "The 'true average' damage across all encounters. I.e. The sum of all encounters divded by the number of encounters.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorEncounterStatisticsUpperQuartileLabel, "The 75th percentile damage across all encounters. I.e. The average high-end/lucky damage performance of the weapon.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorEncounterStatisticsMedianLabel, "The 50th percentile damage across all encounters. I.e. The average center-most value and generally typical performance of the weapon.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorEncounterStatisticsLowerQuartileLabel, "The 25th percentile damage across all encounters. I.e. The aveerage lower-end/unlucky damage performance of the weapon.");
            // Misc Statistics
            CollectionAddFromControlHash(label_hashes_help, CalculatorMiscStatisticsRoundDamageMeanLabel, "The 'true average' round damage for each round of combat. Best captures rapid-fire weapons.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorMiscStatisticsAttackDamageMeanLabel, "The 'true average' attack damage for each attack landed. Best captures the typical per-hit performance of weapon.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorMiscStatisticsAccuracyMeanLabel, "The average accuracy of the weapon. I.e. What percentage of attacks hit.");
            // Buttons
            CollectionAddFromControlHash(label_hashes_help, CalculatorCalculateDamageStatsButton, "CTRL + Left-Click to copy a comma separated list of the data below.");
            // Batch Compute
            CollectionAddFromControlHash(label_hashes_help, CalculatorBatchComputeButton, "Enable to swap cursor to batch compute mode, allowing you to compute variables at different values rather than just one. Select a entry box with the crosshair to open the batch compute settings popup.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorBatchComputePopupStartLabel, "The value to start scaling this variable from for the given variable. This will be the first value of the selected variable, which will be increased by the value of \"step\" each computation iteration. I.e. Start 0, End 3, Step 1 will simulate the values: 0, 1, 2, 3.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorBatchComputePopupEndValueLabel, "The value to end scaling this variable up to for the given variable. The variable will be iterated by Step until it reaches or exceeds this value, in the latter case will just round down to this. I.e. Start 2, End 7, Step 2 will simulate the values: 2, 4, 6, 7.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorBatchComputePopupStepPatternLabel, "The value to increase the variable by each iteration. This will start at Start, then end rounding down to End. Can be negative. I.e. Start 5, End 0, Step -2 will simulate the values: 5, 3, 1, 0.");
            CollectionAddFromControlHash(label_hashes_help, CalculatorBatchComputePopupLayerLabel, "Lower values will be iterated after all layers above it have fully iterated from start to finish. Warning: This is extremely expensive to use, as computation increases exponentially with each new layer!");
        }
        private void StoreEntryBoxParentBatchHashes()
        {
            // Attack
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorAttackBonusToHitTextBox, CalculatorAttackGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorAttackCriticalHitMinimumTextBox, CalculatorAttackGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorAttackACTextBox, CalculatorAttackGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorAttackMAPModifierTextBox, CalculatorAttackGroupBox);

            // Ammunition
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorAmmunitionReloadTextBox, CalculatorAmmunitionGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorAmmunitionMagazineSizeCheckBox, CalculatorAmmunitionGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorAmmunitionMagazineSizeTextBox, CalculatorAmmunitionGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorAmmunitionLongReloadTextBox, CalculatorAmmunitionGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorAmmunitionDrawLengthTextBox, CalculatorAmmunitionGroupBox);

            // Damage

            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageEditButton, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageSaveButton, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageAddButton, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageDeleteButton, CalculatorDamageGroupBox);


            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageDieCountTextBox, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageDieSizeTextBox, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageDieBonusTextBox, CalculatorDamageGroupBox);

            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageCriticalDieCheckBox, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageCriticalDieCountTextBox, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageCriticalDieSizeTextBox, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageCriticalDieBonusTextBox, CalculatorDamageGroupBox);

            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageBleedDieCheckBox, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageBleedDieCountTextBox, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageBleedDieSizeTextBox, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageBleedDieBonusTextBox, CalculatorDamageGroupBox);

            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageCriticalBleedDieCheckBox, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageCriticalBleedDieCountTextBox, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageCriticalBleedDieSizeTextBox, CalculatorDamageGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorDamageCriticalBleedDieBonusTextBox, CalculatorDamageGroupBox);

            // Reach
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorReachRangeIncrementCheckBox, CalculatorReachGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorReachRangeIncrementTextBox, CalculatorReachGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorReachVolleyIncrementCheckBox, CalculatorReachGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorReachVolleyIncrementTextBox, CalculatorReachGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorReachMovementSpeedCheckBox, CalculatorReachGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorReachMovementSpeedTextBox, CalculatorReachGroupBox);

            // Encounter
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorEncounterNumberOfEncountersTextBox, CalculatorEncounterGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorEncounterRoundsPerEncounterTextBox, CalculatorEncounterGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorEncounterEngagementRangeCheckBox, CalculatorEncounterGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorEncounterEngagementRangeTextBox, CalculatorEncounterGroupBox);

            // Action
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorActionActionsPerRoundTextBox, CalculatorActionGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorActionExtraLimitedActionsStrikeNumericUpDown, CalculatorActionGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorActionExtraLimitedActionsDrawNumericUpDown, CalculatorActionGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorActionExtraLimitedActionsStrideNumericUpDown, CalculatorActionGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorActionExtraLimitedActionsReloadNumericUpDown, CalculatorActionGroupBox);
            CollectionAddFromControlHash(entrybox_groupBox_hashes, CalculatorActionExtraLimitedActionsLongReloadNumericUpDown, CalculatorActionGroupBox);
        }
        private void StoreControlToSetting()
        {
            // Attack
            control_hash_to_setting.Add(CalculatorAttackBonusToHitTextBox.GetHashCode(), EncounterSetting.bonus_to_hit);
            control_hash_to_setting.Add(CalculatorAttackCriticalHitMinimumTextBox.GetHashCode(), EncounterSetting.crit_threshhold);
            control_hash_to_setting.Add(CalculatorAttackACTextBox.GetHashCode(), EncounterSetting.AC);
            control_hash_to_setting.Add(CalculatorAttackMAPModifierTextBox.GetHashCode(), EncounterSetting.MAP_modifier);

            // Ammunition
            control_hash_to_setting.Add(CalculatorAmmunitionReloadTextBox.GetHashCode(), EncounterSetting.reload);
            control_hash_to_setting.Add(CalculatorAmmunitionMagazineSizeTextBox.GetHashCode(), EncounterSetting.magazine_size);
            control_hash_to_setting.Add(CalculatorAmmunitionLongReloadTextBox.GetHashCode(), EncounterSetting.long_reload);
            control_hash_to_setting.Add(CalculatorAmmunitionDrawLengthTextBox.GetHashCode(), EncounterSetting.draw);

            // Damage Die
            // Base
            control_hash_to_setting.Add(CalculatorDamageDieCountTextBox.GetHashCode(), EncounterSetting.damage_dice_count);
            control_hash_to_setting.Add(CalculatorDamageDieSizeTextBox.GetHashCode(), EncounterSetting.damage_dice_size);
            control_hash_to_setting.Add(CalculatorDamageDieBonusTextBox.GetHashCode(), EncounterSetting.damage_dice_bonus);
            // Critical
            control_hash_to_setting.Add(CalculatorDamageCriticalDieCountTextBox.GetHashCode(), EncounterSetting.damage_dice_count_critical);
            control_hash_to_setting.Add(CalculatorDamageCriticalDieSizeTextBox.GetHashCode(), EncounterSetting.damage_dice_size_critical);
            control_hash_to_setting.Add(CalculatorDamageCriticalDieBonusTextBox.GetHashCode(), EncounterSetting.damage_dice_bonus_critical);
            // Bleed
            control_hash_to_setting.Add(CalculatorDamageBleedDieCountTextBox.GetHashCode(), EncounterSetting.damage_dice_DOT_count);
            control_hash_to_setting.Add(CalculatorDamageBleedDieSizeTextBox.GetHashCode(), EncounterSetting.damage_dice_DOT_size);
            control_hash_to_setting.Add(CalculatorDamageBleedDieBonusTextBox.GetHashCode(), EncounterSetting.damage_dice_DOT_bonus);
            // Critical Bleed
            control_hash_to_setting.Add(CalculatorDamageCriticalBleedDieCountTextBox.GetHashCode(), EncounterSetting.damage_dice_DOT_count_critical);
            control_hash_to_setting.Add(CalculatorDamageCriticalBleedDieSizeTextBox.GetHashCode(), EncounterSetting.damage_dice_DOT_size_critical);
            control_hash_to_setting.Add(CalculatorDamageCriticalBleedDieBonusTextBox.GetHashCode(), EncounterSetting.damage_dice_DOT_bonus_critical);

            // Reach
            control_hash_to_setting.Add(CalculatorReachRangeIncrementTextBox.GetHashCode(), EncounterSetting.range);
            control_hash_to_setting.Add(CalculatorReachVolleyIncrementTextBox.GetHashCode(), EncounterSetting.volley);
            control_hash_to_setting.Add(CalculatorReachMovementSpeedTextBox.GetHashCode(), EncounterSetting.move_speed);

            // Reach
            control_hash_to_setting.Add(CalculatorEncounterNumberOfEncountersTextBox.GetHashCode(), EncounterSetting.number_of_encounters);
            control_hash_to_setting.Add(CalculatorEncounterRoundsPerEncounterTextBox.GetHashCode(), EncounterSetting.rounds_per_encounter);
            control_hash_to_setting.Add(CalculatorEncounterEngagementRangeTextBox.GetHashCode(), EncounterSetting.engagement_range);
            
            // Action
            control_hash_to_setting.Add(CalculatorActionActionsPerRoundTextBox.GetHashCode(), EncounterSetting.actions_per_round_any);
            control_hash_to_setting.Add(CalculatorActionExtraLimitedActionsStrikeNumericUpDown.GetHashCode(), EncounterSetting.actions_per_round_strike);
            control_hash_to_setting.Add(CalculatorActionExtraLimitedActionsDrawNumericUpDown.GetHashCode(), EncounterSetting.actions_per_round_draw);
            control_hash_to_setting.Add(CalculatorActionExtraLimitedActionsStrideNumericUpDown.GetHashCode(), EncounterSetting.actions_per_round_stride);
            control_hash_to_setting.Add(CalculatorActionExtraLimitedActionsReloadNumericUpDown.GetHashCode(), EncounterSetting.actions_per_round_reload);
            control_hash_to_setting.Add(CalculatorActionExtraLimitedActionsLongReloadNumericUpDown.GetHashCode(), EncounterSetting.actions_per_round_long_reload);
        }
        private void StoreSettingToControl()
        {
            // Attack
            setting_to_control.Add(EncounterSetting.bonus_to_hit, CalculatorAttackBonusToHitTextBox);
            setting_to_control.Add(EncounterSetting.crit_threshhold, CalculatorAttackCriticalHitMinimumTextBox);
            setting_to_control.Add(EncounterSetting.AC, CalculatorAttackACTextBox);
            setting_to_control.Add(EncounterSetting.MAP_modifier, CalculatorAttackMAPModifierTextBox);

            // Ammunition
            setting_to_control.Add(EncounterSetting.reload, CalculatorAmmunitionReloadTextBox);
            setting_to_control.Add(EncounterSetting.magazine_size, CalculatorAmmunitionMagazineSizeTextBox);
            setting_to_control.Add(EncounterSetting.long_reload, CalculatorAmmunitionLongReloadTextBox);
            setting_to_control.Add(EncounterSetting.draw, CalculatorAmmunitionDrawLengthTextBox);

            // Damage Die
            // Base
            setting_to_control.Add(EncounterSetting.damage_dice_count, CalculatorDamageDieCountTextBox);
            setting_to_control.Add(EncounterSetting.damage_dice_size, CalculatorDamageDieSizeTextBox);
            setting_to_control.Add(EncounterSetting.damage_dice_bonus, CalculatorDamageDieBonusTextBox);
            // Critical
            setting_to_control.Add(EncounterSetting.damage_dice_count_critical, CalculatorDamageCriticalDieCountTextBox);
            setting_to_control.Add(EncounterSetting.damage_dice_size_critical, CalculatorDamageCriticalDieSizeTextBox);
            setting_to_control.Add(EncounterSetting.damage_dice_bonus_critical, CalculatorDamageCriticalDieBonusTextBox);
            // Bleed
            setting_to_control.Add(EncounterSetting.damage_dice_DOT_count, CalculatorDamageBleedDieCountTextBox);
            setting_to_control.Add(EncounterSetting.damage_dice_DOT_size, CalculatorDamageBleedDieSizeTextBox);
            setting_to_control.Add(EncounterSetting.damage_dice_DOT_bonus, CalculatorDamageBleedDieBonusTextBox);
            // Critical Bleed
            setting_to_control.Add(EncounterSetting.damage_dice_DOT_count_critical, CalculatorDamageCriticalBleedDieCountTextBox);
            setting_to_control.Add(EncounterSetting.damage_dice_DOT_size_critical, CalculatorDamageCriticalBleedDieSizeTextBox);
            setting_to_control.Add(EncounterSetting.damage_dice_DOT_bonus_critical, CalculatorDamageCriticalBleedDieBonusTextBox);

            // Reach
            setting_to_control.Add(EncounterSetting.range, CalculatorReachRangeIncrementTextBox);
            setting_to_control.Add(EncounterSetting.volley, CalculatorReachVolleyIncrementTextBox);
            setting_to_control.Add(EncounterSetting.move_speed, CalculatorReachMovementSpeedTextBox);

            // Reach
            setting_to_control.Add(EncounterSetting.number_of_encounters, CalculatorEncounterNumberOfEncountersTextBox);
            setting_to_control.Add(EncounterSetting.rounds_per_encounter, CalculatorEncounterRoundsPerEncounterTextBox);
            setting_to_control.Add(EncounterSetting.engagement_range, CalculatorEncounterEngagementRangeTextBox);

            // Action
            setting_to_control.Add(EncounterSetting.actions_per_round_any, CalculatorActionActionsPerRoundTextBox);
            setting_to_control.Add(EncounterSetting.actions_per_round_strike, CalculatorActionExtraLimitedActionsStrikeNumericUpDown);
            setting_to_control.Add(EncounterSetting.actions_per_round_draw, CalculatorActionExtraLimitedActionsDrawNumericUpDown);
            setting_to_control.Add(EncounterSetting.actions_per_round_stride, CalculatorActionExtraLimitedActionsStrideNumericUpDown);
            setting_to_control.Add(EncounterSetting.actions_per_round_reload, CalculatorActionExtraLimitedActionsReloadNumericUpDown);
            setting_to_control.Add(EncounterSetting.actions_per_round_long_reload, CalculatorActionExtraLimitedActionsLongReloadNumericUpDown);
        }

        
        // SETTINGS
        // Set the given setting
        /// <summary>
        /// Sets the value of the variable corresponding to the given control and index.
        /// </summary>
        /// <param name="control">Control to set the value of.</param>
        /// <param name="value">Value to set the respective variable to.</param>
        /// <param name="index">Index to set, if using a list-type variable.</param>
        private void SetValueBySetting(EncounterSetting encounterSetting, int value, int index = 0)
        {
            switch (encounterSetting)
            {
            // Attack
                case EncounterSetting.bonus_to_hit:
                    currEncounterSettings.bonus_to_hit = value;
                    break;
                case EncounterSetting.crit_threshhold:
                    currEncounterSettings.crit_threshhold = value;
                    break;
                case EncounterSetting.AC:
                    currEncounterSettings.AC = value;
                    break;
                case EncounterSetting.MAP_modifier:
                    currEncounterSettings.MAP_modifier = value;
                    break;
            // Ammunition
                case EncounterSetting.reload:
                    currEncounterSettings.reload = value;
                    break;
                case EncounterSetting.magazine_size:
                    currEncounterSettings.magazine_size = value;
                    break;
                case EncounterSetting.long_reload:
                    currEncounterSettings.long_reload = value;
                    break;
                case EncounterSetting.draw:
                    currEncounterSettings.draw = value;
                    break;
            // Damage
                // Base
                case EncounterSetting.damage_dice_count:
                    EditDamageDie(currEncounterSettings.damage_dice, DieField.count, value, index);
                    break;
                case EncounterSetting.damage_dice_size:
                    EditDamageDie(currEncounterSettings.damage_dice, DieField.size, value, index);
                    break;
                case EncounterSetting.damage_dice_bonus:
                    EditDamageDie(currEncounterSettings.damage_dice, DieField.bonus, value, index);
                    break;
                // Critical
                case EncounterSetting.damage_dice_count_critical:
                    EditDamageDie(currEncounterSettings.damage_dice, DieField.count, value, index, edit_critical: true);
                    break;
                case EncounterSetting.damage_dice_size_critical:
                    EditDamageDie(currEncounterSettings.damage_dice, DieField.size, value, index, edit_critical: true);
                    break;
                case EncounterSetting.damage_dice_bonus_critical:
                    EditDamageDie(currEncounterSettings.damage_dice, DieField.bonus, value, index, edit_critical: true);
                    break;
                // Bleed
                case EncounterSetting.damage_dice_DOT_count:
                    EditDamageDie(currEncounterSettings.damage_dice_DOT, DieField.count, value, index);
                    break;
                case EncounterSetting.damage_dice_DOT_size:
                    EditDamageDie(currEncounterSettings.damage_dice_DOT, DieField.size, value, index);
                    break;
                case EncounterSetting.damage_dice_DOT_bonus:
                    EditDamageDie(currEncounterSettings.damage_dice_DOT, DieField.bonus, value, index);
                    break;
                // Critical
                case EncounterSetting.damage_dice_DOT_count_critical:
                    EditDamageDie(currEncounterSettings.damage_dice_DOT, DieField.count, value, index, edit_critical: true);
                    break;
                case EncounterSetting.damage_dice_DOT_size_critical:
                    EditDamageDie(currEncounterSettings.damage_dice_DOT, DieField.size, value, index, edit_critical: true);
                    break;
                case EncounterSetting.damage_dice_DOT_bonus_critical:
                    EditDamageDie(currEncounterSettings.damage_dice_DOT, DieField.bonus, value, index, edit_critical: true);
                    break;

                // Reach
                case EncounterSetting.range:
                    currEncounterSettings.range = value;
                    break;
                case EncounterSetting.volley:
                    currEncounterSettings.volley = value;
                    break;
                case EncounterSetting.move_speed:
                    currEncounterSettings.move_speed = value;
                    break;
            // Encounter
                case EncounterSetting.number_of_encounters:
                    currEncounterSettings.number_of_encounters = value;
                    break;
                case EncounterSetting.rounds_per_encounter:
                    currEncounterSettings.rounds_per_encounter = value;
                    break;
                case EncounterSetting.engagement_range:
                    currEncounterSettings.engagement_range = value;
                    break;
            // Action
                case EncounterSetting.actions_per_round_any:
                    currEncounterSettings.actions_per_round.any = value;
                    break;
                case EncounterSetting.actions_per_round_strike:
                    currEncounterSettings.actions_per_round.strike = value;
                    break;
                case EncounterSetting.actions_per_round_draw:
                    currEncounterSettings.actions_per_round.draw = value;
                    break;
                case EncounterSetting.actions_per_round_stride:
                    currEncounterSettings.actions_per_round.stride = value;
                    break;
                case EncounterSetting.actions_per_round_reload:
                    currEncounterSettings.actions_per_round.reload = value;
                    break;
                case EncounterSetting.actions_per_round_long_reload:
                    currEncounterSettings.actions_per_round.long_reload = value;
                    break;
            }
        }
        private int GetValueBySetting(EncounterSetting encounterSetting, int index = 0)
        {
            switch (encounterSetting)
            {
            // Attack
                case EncounterSetting.bonus_to_hit:
                    return currEncounterSettings.bonus_to_hit;
                case EncounterSetting.crit_threshhold:
                    return currEncounterSettings.crit_threshhold;
                case EncounterSetting.AC:
                    return currEncounterSettings.AC;
                case EncounterSetting.MAP_modifier:
                    return currEncounterSettings.MAP_modifier;
            // Ammunition
                case EncounterSetting.reload:
                    return currEncounterSettings.reload;
                case EncounterSetting.magazine_size:
                    return currEncounterSettings.magazine_size;
                case EncounterSetting.long_reload:
                    return currEncounterSettings.long_reload;
                case EncounterSetting.draw:
                    return currEncounterSettings.draw;
            // Damage
                // Base
                case EncounterSetting.damage_dice_count:
                    return currEncounterSettings.damage_dice[index].Item1.Item1;
                case EncounterSetting.damage_dice_size:
                    return currEncounterSettings.damage_dice[index].Item1.Item2;
                case EncounterSetting.damage_dice_bonus:
                    return currEncounterSettings.damage_dice[index].Item1.Item3;
                // Critical
                case EncounterSetting.damage_dice_count_critical:
                    return currEncounterSettings.damage_dice[index].Item2.Item1;
                case EncounterSetting.damage_dice_size_critical:
                    return currEncounterSettings.damage_dice[index].Item2.Item2;
                case EncounterSetting.damage_dice_bonus_critical:
                    return currEncounterSettings.damage_dice[index].Item2.Item3;
                // Bleed
                case EncounterSetting.damage_dice_DOT_count:
                    return currEncounterSettings.damage_dice_DOT[index].Item1.Item1;
                case EncounterSetting.damage_dice_DOT_size:
                    return currEncounterSettings.damage_dice_DOT[index].Item1.Item2;
                case EncounterSetting.damage_dice_DOT_bonus:
                    return currEncounterSettings.damage_dice_DOT[index].Item1.Item3;
                // Critical
                case EncounterSetting.damage_dice_DOT_count_critical:
                    return currEncounterSettings.damage_dice_DOT[index].Item2.Item1;
                case EncounterSetting.damage_dice_DOT_size_critical:
                    return currEncounterSettings.damage_dice_DOT[index].Item2.Item2;
                case EncounterSetting.damage_dice_DOT_bonus_critical:
                    return currEncounterSettings.damage_dice_DOT[index].Item2.Item3;

            // Reach
                case EncounterSetting.range:
                    return currEncounterSettings.range;
                case EncounterSetting.volley:
                    return currEncounterSettings.volley;
                case EncounterSetting.move_speed:
                    return currEncounterSettings.move_speed;
            // Encounter
                case EncounterSetting.number_of_encounters:
                    return currEncounterSettings.number_of_encounters;
                case EncounterSetting.rounds_per_encounter:
                    return currEncounterSettings.rounds_per_encounter;
                case EncounterSetting.engagement_range:
                    return currEncounterSettings.engagement_range;
            // Action
                case EncounterSetting.actions_per_round_any:
                    return currEncounterSettings.actions_per_round.any;
                case EncounterSetting.actions_per_round_strike:
                    return currEncounterSettings.actions_per_round.strike;
                case EncounterSetting.actions_per_round_draw:
                    return currEncounterSettings.actions_per_round.draw;
                case EncounterSetting.actions_per_round_stride:
                    return currEncounterSettings.actions_per_round.stride;
                case EncounterSetting.actions_per_round_reload:
                    return currEncounterSettings.actions_per_round.reload;
                case EncounterSetting.actions_per_round_long_reload:
                    return currEncounterSettings.actions_per_round.long_reload;
            }
            throw new Exception("No encounter setting given!");
        }
        private void ResetSettingByControl(Control control, int index = 0)
        {
            switch (control_hash_to_setting[control.GetHashCode()])
            {
            // Attack
                case EncounterSetting.bonus_to_hit:
                    currEncounterSettings.bonus_to_hit = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.crit_threshhold:
                    currEncounterSettings.crit_threshhold = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.AC:
                    currEncounterSettings.AC = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.MAP_modifier:
                    currEncounterSettings.MAP_modifier = GetDefaultSettingByControl(control);
                    break;
            // Ammunition
                case EncounterSetting.reload:
                    currEncounterSettings.reload = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.magazine_size:
                    currEncounterSettings.magazine_size = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.long_reload:
                    currEncounterSettings.long_reload = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.draw:
                    currEncounterSettings.draw = GetDefaultSettingByControl(control);
                    break;
            // Damage
                // Base
                case EncounterSetting.damage_dice_count:
                    EditDamageDie(currEncounterSettings.damage_dice, DieField.count, GetDefaultSettingByControl(control), index);
                    break;
                case EncounterSetting.damage_dice_size:
                    EditDamageDie(currEncounterSettings.damage_dice, DieField.size, GetDefaultSettingByControl(control), index);
                    break;
                case EncounterSetting.damage_dice_bonus:
                    EditDamageDie(currEncounterSettings.damage_dice, DieField.bonus, GetDefaultSettingByControl(control), index);
                    break;
                // Critical
                case EncounterSetting.damage_dice_count_critical:
                    EditDamageDie(currEncounterSettings.damage_dice, DieField.count, GetDefaultSettingByControl(control), index, edit_critical: true);
                    break;
                case EncounterSetting.damage_dice_size_critical:
                    EditDamageDie(currEncounterSettings.damage_dice, DieField.size, GetDefaultSettingByControl(control), index, edit_critical: true);
                    break;
                case EncounterSetting.damage_dice_bonus_critical:
                    EditDamageDie(currEncounterSettings.damage_dice, DieField.bonus, GetDefaultSettingByControl(control), index, edit_critical: true);
                    break;
                // Bleed
                case EncounterSetting.damage_dice_DOT_count:
                    EditDamageDie(currEncounterSettings.damage_dice_DOT, DieField.count, GetDefaultSettingByControl(control), index);
                    break;
                case EncounterSetting.damage_dice_DOT_size:
                    EditDamageDie(currEncounterSettings.damage_dice_DOT, DieField.size, GetDefaultSettingByControl(control), index);
                    break;
                case EncounterSetting.damage_dice_DOT_bonus:
                    EditDamageDie(currEncounterSettings.damage_dice_DOT, DieField.bonus, GetDefaultSettingByControl(control), index);
                    break;
                // Critical
                case EncounterSetting.damage_dice_DOT_count_critical:
                    EditDamageDie(currEncounterSettings.damage_dice_DOT, DieField.count, GetDefaultSettingByControl(control), index, edit_critical: true);
                    break;
                case EncounterSetting.damage_dice_DOT_size_critical:
                    EditDamageDie(currEncounterSettings.damage_dice_DOT, DieField.size, GetDefaultSettingByControl(control), index, edit_critical: true);
                    break;
                case EncounterSetting.damage_dice_DOT_bonus_critical:
                    EditDamageDie(currEncounterSettings.damage_dice_DOT, DieField.bonus, GetDefaultSettingByControl(control), index, edit_critical: true);
                    break;

            // Reach
                case EncounterSetting.range:
                    currEncounterSettings.range = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.volley:
                    currEncounterSettings.volley = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.move_speed:
                    currEncounterSettings.move_speed = GetDefaultSettingByControl(control);
                    break;
            // Encounter
                case EncounterSetting.number_of_encounters:
                    currEncounterSettings.number_of_encounters = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.rounds_per_encounter:
                    currEncounterSettings.rounds_per_encounter = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.engagement_range:
                    currEncounterSettings.engagement_range = GetDefaultSettingByControl(control);
                    break;
            // Action
                case EncounterSetting.actions_per_round_any:
                    currEncounterSettings.actions_per_round.any = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.actions_per_round_strike:
                    currEncounterSettings.actions_per_round.strike = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.actions_per_round_draw:
                    currEncounterSettings.actions_per_round.draw = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.actions_per_round_stride:
                    currEncounterSettings.actions_per_round.stride = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.actions_per_round_reload:
                    currEncounterSettings.actions_per_round.reload = GetDefaultSettingByControl(control);
                    break;
                case EncounterSetting.actions_per_round_long_reload:
                    currEncounterSettings.actions_per_round.long_reload = GetDefaultSettingByControl(control);
                    break;
            }
        }
        private int GetDefaultSettingByControl(Control control)
        {
            return control_hash_to_setting[control.GetHashCode()] switch
            {
                // Attack
                EncounterSetting.bonus_to_hit => 10,
                EncounterSetting.crit_threshhold => 20,
                EncounterSetting.AC => 21,
                EncounterSetting.MAP_modifier => 0,
                // Ammunition
                EncounterSetting.reload => 1,
                EncounterSetting.magazine_size => 0,
                EncounterSetting.long_reload => 0,
                EncounterSetting.draw => 1,
                // Damage
                // Base
                EncounterSetting.damage_dice_count => 1,
                EncounterSetting.damage_dice_size => 6,
                EncounterSetting.damage_dice_bonus => 0,
                // Critical
                EncounterSetting.damage_dice_count_critical => 1,
                EncounterSetting.damage_dice_size_critical => 8,
                EncounterSetting.damage_dice_bonus_critical => 1,
                // Bleed
                EncounterSetting.damage_dice_DOT_count => 0,
                EncounterSetting.damage_dice_DOT_size => 0,
                EncounterSetting.damage_dice_DOT_bonus => 0,
                // Critical
                EncounterSetting.damage_dice_DOT_count_critical => 0,
                EncounterSetting.damage_dice_DOT_size_critical => 0,
                EncounterSetting.damage_dice_DOT_bonus_critical => 0,
                // Reach
                EncounterSetting.range => 100,
                EncounterSetting.volley => 0,
                EncounterSetting.move_speed => 25,
                // Encounter
                EncounterSetting.number_of_encounters => 10000,
                EncounterSetting.rounds_per_encounter => 6,
                EncounterSetting.engagement_range => 30,
                // Action
                EncounterSetting.actions_per_round_any => 3,
                EncounterSetting.actions_per_round_strike => 0,
                EncounterSetting.actions_per_round_draw => 0,
                EncounterSetting.actions_per_round_stride => 0,
                EncounterSetting.actions_per_round_reload => 0,
                EncounterSetting.actions_per_round_long_reload => 0,
                _ => 0,
            };
        }
        private int GetSettingFromControl(Control control, int index = 0)
        {
            switch (control)
            {
                case TextBox textBox:
                    return int.TryParse(textBox.Text, out int result) ? result : GetDefaultSettingByControl(control);
                case NumericUpDown numericUpDown:
                    return (int)numericUpDown.Value;
                case ListBox listBox:
                    return listBox.Items.Count > index ? int.Parse((string)listBox.Items[index]) : GetDefaultSettingByControl(control);
                case CheckBox checkBox:
                    return checkBox.Checked ? 1 : 0;
            }

            Exception ex = new();
            ex.Data.Add(1001, "Given field cannot be parsed");
            throw ex;
        }

        // SETTING HELPERS
        //
        /// <summary>
        /// Edit the given damage die in the given damage_dice variable.
        /// </summary>
        /// <param name="setting">Which field to edit.</param>
        /// <param name="value">What to set the value of the damage die to.</param>
        /// <param name="index">What index to change the value of.</param>
        private static void EditDamageDie(List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> damage_dice, DieField setting, int value, int index, bool edit_critical = false)
        {// Store the relevant damage die
            Tuple<Tuple<int, int, int>, Tuple<int, int, int>> edited_damage_die = damage_dice[index];

            // Figure out whether edit_critical or base damage is being modified.
            Tuple<int, int, int> edited_die =
                (edit_critical)
                ? edited_damage_die.Item2 // Edit the edit_critical die
                : edited_damage_die.Item1;// Edit the base die

            Tuple<int, int, int> unedited_die =
                (edit_critical)
                ? edited_damage_die.Item1 // Storing the base die
                : edited_damage_die.Item2;// Storing the edit_critical die

            // Remove the old entry
            damage_dice.RemoveAt(index);

            switch (setting)
            {
                case DieField.count:
                    // Replace it with the new value
                    edited_die = new(value, edited_die.Item2, edited_die.Item3);
                    break;
                case DieField.size:
                    edited_die = new(edited_die.Item1, value, edited_die.Item3);
                    break;
                case DieField.bonus:
                    edited_die = new(edited_die.Item1, edited_die.Item2, value);
                    break;
            }

            damage_dice.Insert(index,new(edit_critical
                                                ? edited_die
                                                : unedited_die,
                                            edit_critical
                                                ? unedited_die
                                                : edited_die));
        }
        public enum DieField
        {
            count,
            size,
            bonus
        }

        // VISUAL UPDATES
        //
        private void InitializeGraphStartAppearance()
        {
            // Update the Batch Graph
            CalculatorBatchComputeScottPlot.Plot.SetAxisLimits(xMin: 0, xMax: 100, yMin: 0, yMax: 10);
            CalculatorBatchComputeScottPlot.Plot.SetOuterViewLimits(xMin: 0, xMax: 100, yMin: 0, yMax: 10);

            // Update the Base Graph
            CalculatorDamageDistributionScottPlot.Plot.SetAxisLimits(xMin: 0, xMax: 100, yMin: 0, yMax: 1000);
            CalculatorDamageDistributionScottPlot.Plot.SetOuterViewLimits(xMin: 0, xMax: 100, yMin: 0, yMax: 1000);
        }
        private void ResetVisuals(EncounterSettings encounterSettings)
        {
            // Reset Visuals
            CalculatorEncounterNumberOfEncountersTextBox.Text = encounterSettings.number_of_encounters.ToString();
            CalculatorEncounterRoundsPerEncounterTextBox.Text = encounterSettings.rounds_per_encounter.ToString();
            CalculatorActionActionsPerRoundTextBox.Text = encounterSettings.actions_per_round.any.ToString();
            CalculatorActionExtraLimitedActionsStrikeNumericUpDown.Value = encounterSettings.actions_per_round.strike;
            CalculatorActionExtraLimitedActionsDrawNumericUpDown.Value = encounterSettings.actions_per_round.draw;
            CalculatorActionExtraLimitedActionsStrideNumericUpDown.Value = encounterSettings.actions_per_round.stride;
            CalculatorActionExtraLimitedActionsReloadNumericUpDown.Value = encounterSettings.actions_per_round.reload;
            CalculatorActionExtraLimitedActionsLongReloadNumericUpDown.Value = encounterSettings.actions_per_round.long_reload;
            CalculatorAmmunitionReloadTextBox.Text = encounterSettings.reload.ToString();
            CalculatorAmmunitionDrawLengthTextBox.Text = encounterSettings.draw.ToString();
            CalculatorDamageListBox.Items.Clear();
            for (int dieIndex = 0; dieIndex < encounterSettings.damage_dice.Count; dieIndex++)
                CalculatorDamageListBox.Items.Add(CreateDamageListBoxString(encounterSettings.damage_dice[dieIndex], encounterSettings.damage_dice_DOT[dieIndex]));
            CalculatorAttackBonusToHitTextBox.Text = encounterSettings.bonus_to_hit.ToString();
            CalculatorAttackACTextBox.Text = encounterSettings.AC.ToString();
            CalculatorAttackCriticalHitMinimumTextBox.Text = encounterSettings.crit_threshhold.ToString();
            CalculatorAttackMAPModifierTextBox.Text = encounterSettings.MAP_modifier.ToString();

            CalculatorEncounterEngagementRangeCheckBox.Checked = true;
            CalculatorEncounterEngagementRangeTextBox.Text = encounterSettings.engagement_range.ToString();
            CalculatorReachMovementSpeedCheckBox.Checked = encounterSettings.seek_favorable_range;
            CalculatorReachMovementSpeedTextBox.Text = encounterSettings.move_speed.ToString();
            CalculatorReachRangeIncrementCheckBox.Checked = true;
            CalculatorReachRangeIncrementTextBox.Text = encounterSettings.range.ToString();
            CalculatorReachVolleyIncrementCheckBox.Checked = true;
            CalculatorReachVolleyIncrementTextBox.Text = encounterSettings.volley.ToString();
            ActiveControl = null;
        }
        private void UpdateStatisticsGUI(Tuple<int, int, int> percentiles)
        {
            // Update Encounter Statistics GUI
            CalculatorEncounterStatisticsMeanTextBox.Text = Math.Round(damageStats.average_encounter_damage, 2).ToString();
            CalculatorEncounterStatisticsMedianTextBox.Text = percentiles.Item2.ToString();
            CalculatorEncounterStatisticsUpperQuartileTextBox.Text = percentiles.Item3.ToString();
            CalculatorEncounterStatisticsLowerQuartileBoxTextBox.Text = percentiles.Item1.ToString();

            // Update Misc Statistics GUI
            CalculatorMiscStatisticsRoundDamageMeanTextBox.Text = Math.Round(damageStats.average_round_damage, 2).ToString();
            CalculatorMiscStatisticsAttackDamageMeanTextBox.Text = Math.Round(damageStats.average_hit_damage, 2).ToString();
            CalculatorMiscStatisticsAccuracyMeanTextBox.Text = (Math.Round(damageStats.average_accuracy * 100, 2)).ToString() + "%";
        }
        private void UpdateBaseGraph(Tuple<int, int, int> percentiles_int)
        {
            // Clear the Plot
            CalculatorDamageDistributionScottPlot.Plot.Clear();

            // Get the maximum value in the list
            int maxKey = damageStats.damage_bins.Keys.Max();
            // Convert the damage bins into an array
            double[] graphBins = Enumerable.Range(0, maxKey + 1)
                .Select(i => damageStats.damage_bins.TryGetValue(i, out int value) ? value : 0)
                .Select(i => (double)i)
                .ToArray();
            // Generate and store the edges of each bin
            double[] binEdges = Enumerable.Range(0, graphBins.Length).Select(x => (double)x).ToArray();

            // Render the Plot
            ScottPlot.Plottable.BarPlot plot = CalculatorDamageDistributionScottPlot.Plot.AddBar(values: graphBins, positions: binEdges);
            // Configure the Plot
            plot.BarWidth = 1;
            CalculatorDamageDistributionScottPlot.Plot.YAxis.Label("Occurances");
            CalculatorDamageDistributionScottPlot.Plot.XAxis.Label("Encounter Damage");
            CalculatorDamageDistributionScottPlot.Plot.SetAxisLimits(yMin: 0, xMin: 0, xMax: maxKey, yMax: damageStats.damage_bins.Values.Max() * 1.2);
            CalculatorDamageDistributionScottPlot.Plot.SetOuterViewLimits(yMin: 0, xMin: 0, xMax: maxKey, yMax: damageStats.damage_bins.Values.Max() * 1.2);
            CalculatorDamageDistributionScottPlot.Plot.Legend(location: ScottPlot.Alignment.UpperLeft);

            Tuple<double, double, double> percentiles = new(percentiles_int.Item1 + ((percentiles_int.Item1 == 0) ? 0.25 : 0),
                                                            percentiles_int.Item2 + ((percentiles_int.Item2 == 0) ? 0.25 : 0),
                                                            percentiles_int.Item3 + ((percentiles_int.Item3 == 0) ? 0.25 : 0));
            if (percentiles.Item1 == percentiles.Item2 && percentiles.Item2 == percentiles.Item3)
            { // Catch when all three are the same
                CalculatorDamageDistributionScottPlot.Plot.AddVerticalLine(x: percentiles.Item1, color: Color.Red, label: "Q1, Q2, Q3");
            }
            else if (percentiles.Item1 == percentiles.Item2)
            { // Catch when lower two percentiles are the same
                CalculatorDamageDistributionScottPlot.Plot.AddVerticalLine(x: percentiles.Item1, color: Color.Red, label: "Q1, Q2");
                CalculatorDamageDistributionScottPlot.Plot.AddVerticalLine(x: percentiles.Item3, color: Color.Orange, label: "Q3");
            }
            else if (percentiles.Item2 == percentiles.Item3)
            { // Catch when upper two percentiles are the same
                CalculatorDamageDistributionScottPlot.Plot.AddVerticalLine(x: percentiles.Item1, color: Color.Orange, label: "Q1");
                CalculatorDamageDistributionScottPlot.Plot.AddVerticalLine(x: percentiles.Item2, color: Color.Red, label: "Q2, Q3");
            }
            else
            { // Default to three separate lines
                CalculatorDamageDistributionScottPlot.Plot.AddVerticalLine(x: percentiles.Item1, color: Color.Orange, label: "Q1");
                CalculatorDamageDistributionScottPlot.Plot.AddVerticalLine(x: percentiles.Item2, color: Color.Red, label: "Q2");
                CalculatorDamageDistributionScottPlot.Plot.AddVerticalLine(x: percentiles.Item3, color: Color.Orange, label: "Q3");
            }
            CalculatorDamageDistributionScottPlot.Plot.Legend(location: ScottPlot.Alignment.UpperRight);
        }
        private void UpdateBatchGraph(BatchResults batch_results)
        {
            // Clear encounter tracking
            batch_results.highest_encounter_count = 0;

            // Clear the Plot
            CalculatorBatchComputeScottPlot.Plot.Clear();

            batch_results.processed_data = UpdateBatchGraph_PROCESS_DATA_ARR(batch_results, new int[1]);
            UpdateBatchGraph_RENDER_GRAPH(batch_results, batch_selected_variables);
        }
        private static double[,] UpdateBatchGraph_PROCESS_DATA_ARR(BatchResults batch_results, int[] batch_selected)
        {
            // To-Do: Add render dims for selecting the layer
            Array pulledLayer = HelperFunctions.GetSubArray(original_array: batch_results.raw_data, index_extracted: batch_selected);
            // Create the surface graph array as wide as the highest damage and as tall as the number of scaling variables
            double[,] processed_data = new double[batch_results.raw_data.GetLength(batch_selected.Length - 1), batch_results.max_width];

            int[] currIndex = new int[batch_results.raw_data.Rank];

            // Iterate to convert each horizonal entry
            for (int currRow = 0; currRow < processed_data.GetLength(0); currRow++)
            {
                // Set the current data column
                currIndex[^1] = currRow;
                // Store a copy of the cu
                // Store a reference to the current row being expanded
                Dictionary<int, int> currentStatsBlock = ((DamageStats)pulledLayer.GetValue(currIndex)).damage_bins;
                // Populate each row
                for (int currCol = 0; currCol < processed_data.GetLength(1); currCol++)
                {
                    // Store the current value, or a zero if it's not present
                    processed_data[currRow, currCol] = currentStatsBlock.TryGetValue(key: currCol, out int value)
                                                    ? value
                                                    : 0;

                    if (batch_results.highest_encounter_count < ((DamageStats)pulledLayer.GetValue(currIndex)).encounters_simulated)
                    {
                        batch_results.highest_encounter_count = ((DamageStats)pulledLayer.GetValue(currIndex)).encounters_simulated;
                    }
                }
            }

            return processed_data;
        }
        private void UpdateBatchGraph_RENDER_GRAPH(BatchResults batch_results, int[] batch_selected)
        {
            // Render the Plot
            var addedHeatmap = CalculatorBatchComputeScottPlot.Plot.AddHeatmap(batch_results.processed_data);

            // Configure the Plot
            addedHeatmap.FlipVertically = true;
            
            // To-do: Fix rendering with the new robust system
            // To-do: Generate/Create controls for selecting options
            // To-do: Add a timer hook to iterate the graph if it's selected

            // Generate a list of incrementing X ticks
            double[] xTicks = Enumerable.Range(0, batch_results.max_width)
                                        .Where(x => x % (batch_results.max_width / 8) == 0)
                                        .Select(Convert.ToDouble)
                                        .ToArray();
            CalculatorBatchComputeScottPlot.Plot.XTicks(positions: xTicks.Select(x => x + 0.5).ToArray(),
                                                        labels: xTicks.Select(x => x.ToString()).ToArray());

            // Label the X and Y Axes
            int highest_layer = batched_variables.Select(x => x.Value.layer).Max();

            // To-do: Investigate scuffed label shennaigans
            // Extract the dictionary of the currently selected layer
            List<Dictionary<EncounterSetting, int>> tick_values =
                                batch_results.tick_values.Rank > 1
                                ? HelperFunctions.GetSubArray(original_array: batch_results.tick_values,
                                                            index_extracted: batch_selected)
                                    .Cast<Dictionary<EncounterSetting, int>>()
                                    .ToList()
                                : batch_results.tick_values
                                    .Cast<Dictionary<EncounterSetting, int>>()
                                    .ToList();

            // Combine each batched variable into the Y-axis label
            // To-do: Group tick_values by each List<int> rows, then this should in theory be working
            CalculatorBatchComputeScottPlot.Plot.YAxis.Label(string.Join(values: tick_values
                                                                            .First()
                                                                            .Select(x => setting_to_string[x.Key]),
                                                                        separator: ", "));

            CalculatorBatchComputeScottPlot.Plot.YTicks(positions: Enumerable.Range(0, batch_results.dimensions[^1]).Select(x => x + 0.5).ToArray(),
                                                        labels: tick_values
                                                                    .Select(x => string.Join(values: x.Values.Select(y => y.ToString()),
                                                                                                separator: ", ")).ToArray());

            CalculatorBatchComputeScottPlot.Plot.XAxis.Label("Encounter Damage");

            // Set the camera/axis limits
            CalculatorBatchComputeScottPlot.Plot.SetAxisLimits(xMin: 0, xMax: batch_results.max_width,
                                                               yMin: 0, yMax: batch_results.dimensions[^1]);
            CalculatorBatchComputeScottPlot.Plot.SetOuterViewLimits(xMin: 0, xMax: batch_results.max_width,
                                                                    yMin: 0, yMax: batch_results.dimensions[^1]);

            // Add & Configure the color bar
            var addedColorBar = CalculatorBatchComputeScottPlot.Plot.AddColorbar(addedHeatmap);
            addedColorBar.AutomaticTicks(formatter: ConvertDoubleToPercentageString);
        }
        private string ConvertDoubleToPercentageString(double value)
        {
            return Math.Round(value * 100 / damageStats_BATCH.highest_encounter_count, 2).ToString() + "%";
        }

        // SAFETY
        //
        private void CheckComputeSafety()
        {
            Exception ex = new();

            // Empty Data Exceptions
            // Attack Category
            if (CalculatorAttackBonusToHitTextBox.Text.Length == 0)
            {
                ex.Data.Add(201, "Missing Data Exception: Missing bonus to hit");
            }
            if (CalculatorAttackCriticalHitMinimumTextBox.Text.Length == 0)
            {
                ex.Data.Add(202, "Missing Data Exception: Missing crit threshhold");
            }
            if (CalculatorAttackACTextBox.Text.Length == 0)
            {
                ex.Data.Add(203, "Missing Data Exception: Missing AC");
            }
            if (CalculatorAttackMAPModifierTextBox.Text.Length == 0)
            {
                ex.Data.Add(204, "Missing Data Exception: Missing MAP modifier");
            }

            // Ammunition Category
            if (CalculatorAmmunitionReloadTextBox.Text.Length == 0)
            {
                ex.Data.Add(205, "Missing Data Exception: Missing reload");
            }
            if (CalculatorAmmunitionMagazineSizeCheckBox.Checked && CalculatorAmmunitionMagazineSizeTextBox.Text.Length == 0)
            {
                ex.Data.Add(206, "Missing Data Exception: Missing magazine size");
            }
            if (CalculatorAmmunitionMagazineSizeCheckBox.Checked && CalculatorAmmunitionDrawLengthTextBox.Text.Length == 0)
            {
                ex.Data.Add(207, "Missing Data Exception: Missing long reload");
            }
            if (CalculatorAmmunitionDrawLengthTextBox.Text.Length == 0)
            {
                ex.Data.Add(208, "Missing Data Exception: Missing draw length");
            }

            // Reach
            if (CalculatorReachRangeIncrementCheckBox.Checked && CalculatorReachRangeIncrementTextBox.Text.Length == 0)
            {
                ex.Data.Add(209, "Missing Data Exception: Missing range increment");
            }
            if (CalculatorReachVolleyIncrementCheckBox.Checked && CalculatorReachVolleyIncrementTextBox.Text.Length == 0)
            {
                ex.Data.Add(210, "Missing Data Exception: Missing volley increment");
            }

            // Encounter
            if (CalculatorEncounterNumberOfEncountersTextBox.Text.Length == 0)
            {
                ex.Data.Add(211, "Missing Data Exception: Missing number of encounters");
            }
            if (CalculatorEncounterRoundsPerEncounterTextBox.Text.Length == 0)
            {
                ex.Data.Add(212, "Missing Data Exception: Missing rounds per encounter");
            }
            if (CalculatorActionActionsPerRoundTextBox.Text.Length == 0)
            {
                ex.Data.Add(213, "Missing Data Exception: Missing actions per round");
            }

            // Distance
            if (CalculatorEncounterEngagementRangeCheckBox.Checked && CalculatorEncounterEngagementRangeTextBox.Text.Length == 0)
            {
                ex.Data.Add(214, "Missing Data Exception: Missing engagement range");
            }
            if (CalculatorReachMovementSpeedCheckBox.Checked && CalculatorReachMovementSpeedTextBox.Text.Length == 0)
            {
                ex.Data.Add(215, "Missing Data Exception: Missing movement speed");
            }

            // Action
            if (CalculatorActionExtraLimitedActionsDrawNumericUpDown.Text.Length == 0)
            {
                ex.Data.Add(216, "Missing Data Exception: Missing limited action draw");
            }
            if (CalculatorActionExtraLimitedActionsStrideNumericUpDown.Text.Length == 0)
            {
                ex.Data.Add(217, "Missing Data Exception: Missing limited action stride");
            }
            if (CalculatorActionExtraLimitedActionsReloadNumericUpDown.Text.Length == 0)
            {
                ex.Data.Add(218, "Missing Data Exception: Missing limited action reload");
            }
            if (CalculatorActionExtraLimitedActionsLongReloadNumericUpDown.Text.Length == 0)
            {
                ex.Data.Add(219, "Missing Data Exception: Missing limited action long reload");
            }
            if (CalculatorActionExtraLimitedActionsStrikeNumericUpDown.Text.Length == 0)
            {
                ex.Data.Add(220, "Missing Data Exception: Missing limited action strike");
            }


            // Bad Data Exceptions
            if (currEncounterSettings.number_of_encounters <= 0)
            { // Throw bad encounter data exception
                ex.Data.Add(101, "Bad Data Exception: Zero or negative encounter count");
            }
            if (currEncounterSettings.move_speed <= 0)
            { // Throw bad movement speed data exception
                ex.Data.Add(102, "Bad Data Exception: Zero or negative movement speed");
            }
            if (currEncounterSettings.rounds_per_encounter < 0)
            { // Throw bad rounds/encounter data exception
                ex.Data.Add(103, "Bad Data Exception: Negative rounds per encounter");
            }
            if (currEncounterSettings.actions_per_round.Total < 0)
            { // Throw bad actions/round data exception
                ex.Data.Add(104, "Bad Data Exception: Negative actions per round");
            }
            if (currEncounterSettings.range < 0)
            { // Throw bad range data exception
                ex.Data.Add(105, "Bad Data Exception: Negative range");
            }
            if (currEncounterSettings.engagement_range < 0)
            { // Throw bad encounter range data exception
                ex.Data.Add(106, "Bad Data Exception: Negative encounter range");
            }
            if (currEncounterSettings.damage_dice.Count <= 0 || currEncounterSettings.damage_dice_DOT.Count <= 0)
            { //  Throw no damage dice exception
                ex.Data.Add(51, "Damage Count Exception: No damage dice");
            }
            if (currEncounterSettings.damage_dice.Count != currEncounterSettings.damage_dice_DOT.Count)
            { // Throw imbalanced damage die exception
                ex.Data.Add(52, "Damage Count Exception: Imbalanced damage/edit_bleed dice");
            }

            // Throw exception on problems
            if (ex.Data.Count > 0)
                throw ex;
        }
        private void PushErrorMessages(Exception ex)
        {
            foreach (int errorCode in ex.Data.Keys)
            {
                switch (errorCode)
                {
                    case 41: // Push
                        PushError(CalculatorReachVolleyIncrementTextBox, "Volley longer Range will have unintended implications with movement!");
                        break;
                    case 51: // Push
                        PushError(CalculatorDamageListBox, "No damage dice detected!");
                        break;
                    case 52: // Fix don't push
                        // N/A
                        break;
                    case 101: // Push
                        PushError(CalculatorEncounterNumberOfEncountersTextBox, "Number of encounters must be greater than 0!");
                        break;
                    case 102: // Push
                        PushError(CalculatorReachMovementSpeedTextBox, "Movement speed must be greater than 0!");
                        break;
                    case 103: // Push
                        PushError(CalculatorEncounterRoundsPerEncounterTextBox, "Rounds per encounter must be greater than 0!");
                        break;
                    case 104: // Push
                        PushError(CalculatorActionActionsPerRoundTextBox, "Actions per round must be positive!");
                        break;
                    case 105: // Push
                        PushError(CalculatorReachRangeIncrementTextBox, "Range increment must be positive!");
                        break;
                    case 106: // Push
                        PushError(CalculatorEncounterEngagementRangeTextBox, "Encounter distance must be positive!");
                        break;
                    case 201: // Push
                        PushError(CalculatorAttackBonusToHitTextBox, "No bonus to-hit value provided!");
                        break;
                    case 202: // Push
                        PushError(CalculatorAttackCriticalHitMinimumTextBox, "No crit at/below value provided!");
                        break;
                    case 203: // Push
                        PushError(CalculatorAttackACTextBox, "No AC value provided!");
                        break;
                    case 204: // Push
                        PushError(CalculatorAttackMAPModifierTextBox, "No MAP value provided!");
                        break;
                    case 205: // Push
                        PushError(CalculatorAmmunitionReloadTextBox, "No reload value provided!");
                        break;
                    case 206: // Push
                        PushError(CalculatorAmmunitionMagazineSizeTextBox, "No magazine size value provided!");
                        break;
                    case 207: // Push
                        PushError(CalculatorAmmunitionLongReloadTextBox, "No long reload value provided!");
                        break;
                    case 208: // Push
                        PushError(CalculatorAmmunitionDrawLengthTextBox, "No draw length value provided!");
                        break;
                    case 209: // Push
                        PushError(CalculatorReachRangeIncrementTextBox, "No range increment value provided!");
                        break;
                    case 210: // Push
                        PushError(CalculatorReachVolleyIncrementTextBox, "No volley increment value provided!");
                        break;
                    case 211: // Push
                        PushError(CalculatorEncounterNumberOfEncountersTextBox, "Number of encounters value not provided!");
                        break;
                    case 212: // Push
                        PushError(CalculatorEncounterRoundsPerEncounterTextBox, "No rounds per encounter value provided!");
                        break;
                    case 213: // Push
                        PushError(CalculatorActionActionsPerRoundTextBox, "No actions per round value provided!");
                        break;
                    case 214: // Push
                        PushError(CalculatorEncounterEngagementRangeTextBox, "No engagement range value provided!");
                        break;
                    case 215: // Push
                        PushError(CalculatorReachMovementSpeedTextBox, "No movement speed value provided!");
                        break;
                    case 216: // Push
                        PushError(CalculatorReachMovementSpeedTextBox, "No bonus draw action value provided!");
                        break;
                    case 217: // Push
                        PushError(CalculatorActionExtraLimitedActionsStrideNumericUpDown, "No bonus stride action value provided!", true);
                        break;
                    case 218: // Push
                        PushError(CalculatorActionExtraLimitedActionsReloadNumericUpDown, "No bonus reload action value provided!", true);
                        break;
                    case 219: // Push
                        PushError(CalculatorActionExtraLimitedActionsLongReloadNumericUpDown, "No bonus long reload action value provided!", true);
                        break;
                    case 220: // Push
                        PushError(CalculatorActionExtraLimitedActionsStrikeNumericUpDown, "No bonus draw strike value provided!", true);
                        break;
                }
            }
        }
        private void PushError(Control control, string errorString, bool isNumericUpDown = false, bool replace = false)
        {
            CalculatorErrorProvider.SetError(control, replace ? errorString : (CalculatorErrorProvider.GetError(control) + errorString));
            CalculatorErrorProvider.SetIconPadding(control, isNumericUpDown ? -36 : -18);
        }
        private void ClearError(Control control)
        {
            CalculatorErrorProvider.SetError(control, String.Empty);
        }
        // CONTROL SAFETY CLEARING
        private void NumericUpDown_ClearError(object sender, EventArgs e)
        {
            ClearError(sender as Control);
        }

        // DATA
        //
        private async Task<DamageStats> ComputeAverageDamage_SINGLE(int number_of_encounters,
                                                        int rounds_per_encounter,
                                                        Actions actions_per_round,
                                                        int reload_size,
                                                        int reload,
                                                        int long_reload,
                                                        int draw,
                                                        List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> damage_dice,
                                                        int bonus_to_hit,
                                                        int AC,
                                                        int crit_threshhold,
                                                        int MAP_modifier,
                                                        int engagement_range,
                                                        int move_speed,
                                                        bool seek_favorable_range,
                                                        int range,
                                                        int volley,
                                                        List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> damage_dice_DOT)
        {
            // Track Progress Bar
            Progress<int> progress = new();
            progress.ProgressChanged += SetProgressBar;

            // Set the flag for actively computing damage
            computing_damage = true;

            // Compute the damage
            Task<DamageStats> computeDamage = Task.Run(() => CalculateAverageDamage(number_of_encounters: number_of_encounters,
                                                        rounds_per_encounter: rounds_per_encounter,
                                                        actions_per_round: actions_per_round,
                                                        reload_size: reload_size,
                                                        reload: reload,
                                                        long_reload: long_reload,
                                                        draw: draw,
                                                        damage_dice: damage_dice,
                                                        bonus_to_hit: bonus_to_hit,
                                                        AC: AC,
                                                        crit_threshhold: crit_threshhold,
                                                        MAP_modifier: MAP_modifier,
                                                        engagement_range: engagement_range,
                                                        move_speed: move_speed,
                                                        seek_favorable_range: seek_favorable_range,
                                                        range: range,
                                                        volley: volley,
                                                        damage_dice_DOT: damage_dice_DOT,
                                                        progress: progress));

            damageStats = await computeDamage;
            computing_damage = false;

            return damageStats;
        }
        private EncounterSettings GetDamageSimulationVariables(EncounterSettings oldSettings)
        {
            EncounterSettings newSettings = new()
            {
                number_of_encounters = GetSettingFromControl(CalculatorEncounterNumberOfEncountersTextBox),
                rounds_per_encounter = GetSettingFromControl(CalculatorEncounterRoundsPerEncounterTextBox),
                actions_per_round = new Actions(any: GetSettingFromControl(CalculatorActionActionsPerRoundTextBox),
                                                strike: GetSettingFromControl(CalculatorActionExtraLimitedActionsStrikeNumericUpDown),
                                                reload: GetSettingFromControl(CalculatorActionExtraLimitedActionsReloadNumericUpDown),
                                                long_reload: GetSettingFromControl(CalculatorActionExtraLimitedActionsLongReloadNumericUpDown),
                                                draw: GetSettingFromControl(CalculatorActionExtraLimitedActionsDrawNumericUpDown),
                                                stride: GetSettingFromControl(CalculatorActionExtraLimitedActionsStrideNumericUpDown)),
                bonus_to_hit = GetSettingFromControl(CalculatorAttackBonusToHitTextBox),
                AC = GetSettingFromControl(CalculatorAttackACTextBox),
                crit_threshhold = GetSettingFromControl(CalculatorAttackCriticalHitMinimumTextBox),
                MAP_modifier = GetSettingFromControl(CalculatorAttackMAPModifierTextBox),
                draw = GetSettingFromControl(CalculatorAmmunitionDrawLengthTextBox),
                reload = GetSettingFromControl(CalculatorAmmunitionReloadTextBox),
                // Magazine / Long Reload
                magazine_size = GetSettingFromControl(CalculatorAmmunitionMagazineSizeTextBox),
                // Magazine / Long Reload
                long_reload = GetSettingFromControl(CalculatorAmmunitionLongReloadTextBox),
                // Engagement Range
                engagement_range = GetSettingFromControl(CalculatorEncounterEngagementRangeTextBox),
                // Movement / Seek
                seek_favorable_range = GetSettingFromControl(CalculatorReachMovementSpeedCheckBox) == 1,
                move_speed = GetSettingFromControl(CalculatorReachMovementSpeedTextBox),
                // Range Increment
                range = GetSettingFromControl(CalculatorReachRangeIncrementTextBox),
                // Range Increment
                volley = GetSettingFromControl(CalculatorReachVolleyIncrementTextBox),
                damage_dice = oldSettings.damage_dice,
                damage_dice_DOT = oldSettings.damage_dice_DOT
            };

            return newSettings;
        }


        // FILTERING
        //

        // Filter normal digit entry out for TextBoxes.
        private void TextBox_KeyPressFilterToDigits(object sender, KeyPressEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            if (CheckControlBatched(control_hash_to_setting[textBox.GetHashCode()]))
                return;

            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                if (!e.KeyChar.Equals('+') && !e.KeyChar.Equals('-') || textBox?.Text.Count(x => (x.Equals('+') || x.Equals('-'))) >= 1)
                {
                    e.Handled = true;
                }
            }

            ClearError(textBox);
        }
        // Filter non-digits and non-sign characters out of pasted entries using regex.
        private void TextBox_TextChangedFilterToDigitsAndSign(object sender, EventArgs e)
        {

            TextBox textBox = sender as TextBox;

            if (CheckControlBatched(control_hash_to_setting[textBox.GetHashCode()]))
                return;

            int currSelectStart = textBox.SelectionStart;
            
            // Strip characters out of pasted text
            textBox.Text = DigitSignRegex().Replace(input: textBox.Text, "");
            for (int index = textBox.Text.Length - 1; index > 0; index--)
            {
                if (textBox.Text.Length < index)
                    break;
                if (textBox.Text[index].Equals('+') || textBox.Text[index].Equals('-'))
                {
                    textBox.Text = textBox.Text.Remove(startIndex: index, count: 1); // Remove any pluses/minuses after the first
                }
            }

            textBox.SelectionStart = Math.Clamp((textBox.Text.Length > 0) 
                                                    ? currSelectStart
                                                    : currSelectStart, 
                                                0, 
                                                (textBox.Text.Length > 0) 
                                                    ? textBox.Text.Length
                                                    : 0);
        }
        // Filter digits out of pasted entries using regex.
        private void TextBox_TextChangedFilterToDigits(object sender, EventArgs e)
        { // Filter only if the textbox isn't batched

            TextBox textBox = sender as TextBox;

            if (CheckControlBatched(control_hash_to_setting[textBox.GetHashCode()]))
                return;

            // Strip characters out of pasted text
            textBox.Text = DigitRegex().Replace(input: textBox.Text, replacement: "");

            ClearError(textBox);
        }
        private void TextBox_TextChangedFilterToDigitsAndCommaAndSign(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;

            int currSelectStart = textBox.SelectionStart;

            // Strip characters out of pasted text
            textBox.Text = DigitCommaSignRegex().Replace(input: textBox.Text, replacement: "");
            textBox.Text = SingleCommaRegex().Replace(input: textBox.Text, replacement: "");
            textBox.Text = SingleSignRegex().Replace(input: textBox.Text, replacement: "");
            textBox.Text = SignIntoCommaRegex().Replace(input: textBox.Text, replacement: "");
            Match matches = DigitSignDigitRegex().Match(input: textBox.Text);
            if (matches.Length > 0)
            {
                textBox.Text = DigitSignDigitRegex().Replace(input: textBox.Text, replacement: ",$0");
                currSelectStart++;
            }

            textBox.SelectionStart = Math.Clamp((textBox.Text.Length > 0)
                                                    ? currSelectStart
                                                    : currSelectStart,
                                                0,
                                                (textBox.Text.Length > 0)
                                                    ? textBox.Text.Length
                                                    : 0);

            ClearError(textBox);
        }
        // Filter out lone plus/minus
        private void TextBox_LeaveClearLoneSymbol(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text.Length == 1 && (textBox.Text[0].Equals('+') || textBox.Text[0].Equals('-')))
                textBox.Text = string.Empty;
        }
        private void TextBox_LeaveClearLoneOrTrailingComma(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            while (textBox.Text.Length > 0 && (textBox.Text[^1].Equals(',') 
                                                || textBox.Text[^1].Equals('-') 
                                                || textBox.Text[^1].Equals('+')))
                textBox.Text = textBox.Text[..(textBox.TextLength - 1)];
        }


        [GeneratedRegex(@"([^\d\,\+\-])")] // abc or = etc
        private static partial Regex DigitCommaSignRegex();
        [GeneratedRegex(@"[^\d-+]")] // abc or =, etc
        private static partial Regex DigitSignRegex();
        [GeneratedRegex(@"(?<=[\d])[\+\-](?=[\d])")] // 25-15 or 1+62
        private static partial Regex DigitSignDigitRegex();
        [GeneratedRegex(@"[^\d]")] // abc or =-, etc
        private static partial Regex DigitRegex();

        [GeneratedRegex(@"\,(?=\,)")] // ,, or ,,,, etc
        private static partial Regex SingleCommaRegex();
        [GeneratedRegex(@"[\+\-](?=[\+\-])")] // -- or ++ etc
        private static partial Regex SingleSignRegex();
        [GeneratedRegex(@"[\+\-](?=[\,])")] // -, or +,
        private static partial Regex SignIntoCommaRegex();

        // CARAT CONTROL
        [DllImport("user32.dll")] static extern bool HideCaret(IntPtr hWnd);
        void TextBox_GotFocusDisableCarat(object sender, EventArgs e)
        {
            HideCaret((sender as TextBox).Handle);
        }
        
        // CHECK BOXES
        private void CheckBox_CheckChangedToggleTextBoxes(object sender, EventArgs e)
        {
            SelectCheckBoxForToggle(sender as CheckBox);
        }
        private void SelectCheckBoxForToggle(CheckBox checkBox)
        {
            int hash = checkBox.GetHashCode();

            if (hash == CalculatorAmmunitionMagazineSizeCheckBox.GetHashCode())
            { // Ammunition
                CheckboxToggleTextbox(checkBox, new() { CalculatorAmmunitionMagazineSizeTextBox, CalculatorAmmunitionLongReloadTextBox });
            }
            // DAMAGE
            else if (hash == CalculatorDamageCriticalDieCheckBox.GetHashCode())
            { // Critical
                CheckboxToggleTextbox(checkBox, new() { CalculatorDamageCriticalDieCountTextBox,
                                                        CalculatorDamageCriticalDieSizeTextBox,
                                                        CalculatorDamageCriticalDieBonusTextBox});
            }
            else if (hash == CalculatorDamageCriticalDieCheckBox.GetHashCode())
            { // Bleed
                CheckboxToggleTextbox(checkBox, new() { CalculatorDamageBleedDieCountTextBox,
                                                        CalculatorDamageBleedDieSizeTextBox,
                                                        CalculatorDamageBleedDieBonusTextBox});
            }
            else if (hash == CalculatorDamageCriticalBleedDieCheckBox.GetHashCode())
            { // Critical Bleed
                CheckboxToggleTextbox(checkBox, new() { CalculatorDamageCriticalBleedDieCountTextBox,
                                                        CalculatorDamageCriticalBleedDieSizeTextBox,
                                                        CalculatorDamageCriticalBleedDieBonusTextBox});
            }
            // REACH
            else if (hash == CalculatorReachRangeIncrementCheckBox.GetHashCode())
            { // Range
                CheckboxToggleTextbox(checkBox, new() { CalculatorReachRangeIncrementTextBox });
            }
            else if (hash == CalculatorReachVolleyIncrementCheckBox.GetHashCode())
            { // Volley
                CheckboxToggleTextbox(checkBox, new() { CalculatorReachVolleyIncrementTextBox });
            }
            else if (hash == CalculatorReachMovementSpeedCheckBox.GetHashCode())
            { // Movement Speed
                CheckboxToggleTextbox(checkBox, new() { CalculatorReachMovementSpeedTextBox });
            }
            // ENCOUNTER
            else if (hash == CalculatorEncounterEngagementRangeCheckBox.GetHashCode())
            { // Encounter
                CheckboxToggleTextbox(checkBox, new() { CalculatorEncounterEngagementRangeTextBox });
            }
        }
        private void CheckboxToggleTextbox(CheckBox checkBox, List<System.Windows.Forms.TextBox> textBoxes)
        {
            foreach (TextBox textBox in textBoxes)
            {
                textBox.Text = string.Empty;
                if (checkBox.Checked)
                {
                    textBox.Enabled = true;
                    textBox.ReadOnly = false;
                }
                else
                {
                    textBox.Enabled = false;
                    textBox.ReadOnly = true;
                    ActiveControl = null;
                }
            }
        }

        // DAMAGE BUTTONS
        private void DamageEditButton_MouseClick(object sender, MouseEventArgs e)
        {
            int index = CalculatorDamageListBox.SelectedIndex;
            if (index != -1)
            {
                LoadDamageDice(index);
            }
        }
        private void DamageDeleteButton_MouseClick(object sender, MouseEventArgs e)
        {
            int index = CalculatorDamageListBox.SelectedIndex;
            if (index != -1)
            {
                // Clear Old Data
                DeleteDamageDice(index);
            }
        }
        private void DamageAddButton_MouseClick(object sender, MouseEventArgs e)
        {
            AddDamageDice();
        }
        private void DamageSaveButton_MouseClick(object sender, MouseEventArgs e)
        {
            int index = CalculatorDamageListBox.SelectedIndex;
            if (index != -1)
            {
                // Clear Old Data
                DeleteDamageDice(index);

                // Add New Data
                AddDamageDice(index);
            }
        }
        
        // Damage Dice Helper Methods
        private void LoadDamageDice(int index)
        {
            // Store references to each dice/bonus
            var currDamageDie = currEncounterSettings.damage_dice[index];
            var currBleedDie = currEncounterSettings.damage_dice_DOT[index];

            // Save each dice to the GUI
            // Regular Damage Die
            CalculatorDamageDieCountTextBox.Text = currDamageDie.Item1.Item1.ToString();
            CalculatorDamageDieSizeTextBox.Text = currDamageDie.Item1.Item2.ToString();
            CalculatorDamageDieBonusTextBox.Text = currDamageDie.Item1.Item3.ToString();

            // Critical Damage Die
            if (currDamageDie.Item2.Item1 != 0 && currDamageDie.Item2.Item1 != currDamageDie.Item1.Item1
                || currDamageDie.Item2.Item2 != 0 && currDamageDie.Item2.Item2 != currDamageDie.Item1.Item2
                || currDamageDie.Item2.Item3 != 0 && currDamageDie.Item2.Item3 != currDamageDie.Item1.Item3)
            {
                CalculatorDamageCriticalDieCheckBox.Checked = true;
                CalculatorDamageCriticalDieCountTextBox.Text = (currDamageDie.Item2.Item1 != 0) // X != 0
                                                        ? currDamageDie.Item2.Item1.ToString()
                                                        : currDamageDie.Item1.Item1.ToString();
                CalculatorDamageCriticalDieSizeTextBox.Text = (currDamageDie.Item2.Item2 != 0) // Y != 0
                                                        ? currDamageDie.Item2.Item2.ToString()
                                                        : currDamageDie.Item1.Item2.ToString();
                CalculatorDamageCriticalDieBonusTextBox.Text = (currDamageDie.Item2.Item3 != 0) // Z != 0
                                                        ? currDamageDie.Item2.Item3.ToString()
                                                        : currDamageDie.Item1.Item3.ToString();
            }
            else
            {
                CalculatorDamageCriticalDieCheckBox.Checked = false;
            }


            // Bleed Die
            if (currBleedDie.Item1.Item1 != 0
                || currBleedDie.Item1.Item2 != 0
                || currBleedDie.Item1.Item3 != 0)
            {
                CalculatorDamageBleedDieCheckBox.Checked = true;
                CalculatorDamageBleedDieCountTextBox.Text = (currBleedDie.Item1.Item1 != 0) // X != 0
                                                        ? currBleedDie.Item1.Item1.ToString()
                                                        : "0";
                CalculatorDamageBleedDieSizeTextBox.Text = (currBleedDie.Item1.Item2 != 0) // Y != 0
                                                        ? currBleedDie.Item1.Item2.ToString()
                                                        : "0";
                CalculatorDamageBleedDieBonusTextBox.Text = (currBleedDie.Item1.Item3 != 0) // Z != 0
                                                        ? currBleedDie.Item1.Item3.ToString()
                                                        : "0";
            }
            else
            {
                CalculatorDamageBleedDieCheckBox.Checked = false;
            }

            // Critical Damage Die
            if (currBleedDie.Item2.Item1 != 0 && currBleedDie.Item2.Item1 != currBleedDie.Item1.Item1
                || currBleedDie.Item2.Item2 != 0 && currBleedDie.Item2.Item2 != currBleedDie.Item1.Item2
                || currBleedDie.Item2.Item3 != 0 && currBleedDie.Item2.Item3 != currBleedDie.Item1.Item3)
            {
                CalculatorDamageCriticalBleedDieCheckBox.Checked = true;
                CalculatorDamageCriticalBleedDieCountTextBox.Text = (currBleedDie.Item2.Item1 != 0) // X != 0
                                                        ? currBleedDie.Item2.Item1.ToString()
                                                        : currBleedDie.Item1.Item1.ToString();
                CalculatorDamageCriticalBleedDieSizeTextBox.Text = (currBleedDie.Item2.Item2 != 0) // Y != 0
                                                        ? currBleedDie.Item2.Item2.ToString()
                                                        : currBleedDie.Item1.Item2.ToString();
                CalculatorDamageCriticalBleedDieBonusTextBox.Text = (currBleedDie.Item2.Item3 != 0) // Z != 0
                                                        ? currBleedDie.Item2.Item3.ToString()
                                                        : currBleedDie.Item1.Item3.ToString();
            }
            else
            {
                CalculatorDamageCriticalBleedDieCheckBox.Checked = false;
            }
        }
        private void DeleteDamageDice(int index)
        {
            currEncounterSettings.damage_dice.RemoveAt(index);
            currEncounterSettings.damage_dice_DOT.RemoveAt(index);
            CalculatorDamageListBox.Items.RemoveAt(index);
        }
        /// <summary>
        /// Reads damage dice text/check boxes and adds data accordingly to the back of the list, or at the given index.
        /// </summary>
        /// <param name="index"></param>
        private void AddDamageDice(int index = -1)
        {
            // Save each dice from the GUI
            var values = GetDamageControlValues();

            // Store references to each dice/bonus
            Tuple<Tuple<int, int, int>, Tuple<int, int, int>> currDamageDie = new(values.Item1, values.Item2);
            Tuple<Tuple<int, int, int>, Tuple<int, int, int>> currBleedDie = new(values.Item3, values.Item4);

            string entryString = CreateDamageListBoxString(currDamageDie, currBleedDie);

            // Add the new string to the list
            if (index == -1)
            {
                CalculatorDamageListBox.Items.Add(item: entryString); // Store the text entry
                currEncounterSettings.damage_dice.Add(item: currDamageDie); // Store the damage
                currEncounterSettings.damage_dice_DOT.Add(item: currBleedDie); // Store the edit_bleed damage
            }
            else
            {
                CalculatorDamageListBox.Items.Insert(index: index, item: entryString); // Store the text entry
                currEncounterSettings.damage_dice.Insert(index: index, item: currDamageDie); // Store the damage
                currEncounterSettings.damage_dice_DOT.Insert(index: index, item: currBleedDie); // Store the edit_bleed damage
            }
        }
        private static string CreateDamageListBoxString(Tuple<Tuple<int, int, int>, Tuple<int, int, int>> currDamageDie, 
                                                    Tuple<Tuple<int, int, int>, Tuple<int, int, int>> currBleedDie)
        {
            // Store references to each dice/bonus
            // Add a visual entry to the GUIc
            // Base Base Bools
            bool hasBaseBonus = currDamageDie.Item1.Item3 != 0;
            // Bleed Critical Bools
            bool hasCriticalCount = currDamageDie.Item2.Item1 != currDamageDie.Item1.Item1;
            bool hasCriticalSides = currDamageDie.Item2.Item2 != currDamageDie.Item1.Item2;
            bool hasCriticalBonus = currDamageDie.Item2.Item3 != currDamageDie.Item1.Item3;
            // Bleed Base Bools
            bool hasBaseBleedCount = currBleedDie.Item1.Item1 != 0;
            bool hasBaseBleedSides = currBleedDie.Item1.Item2 != 0;
            bool hasBaseBleedBonus = currBleedDie.Item1.Item3 != 0;
            // Bleed Critical Bools
            bool hasCriticalBleedCount = currBleedDie.Item2.Item1 != currBleedDie.Item1.Item1;
            bool hasCriticalBleedSides = currBleedDie.Item2.Item2 != currBleedDie.Item1.Item2;
            bool hasCriticalBleedBonus = currBleedDie.Item2.Item3 != currBleedDie.Item1.Item3;
            // Store signs of each bonus
            string[] signs = new string[4] { (currDamageDie.Item1.Item3 < 0) ? "" : "+",
                                        (currDamageDie.Item2.Item3 < 0) ? "" : "+",
                                        (currBleedDie.Item1.Item3 < 0) ? "" : "+",
                                        (currBleedDie.Item2.Item3 < 0) ? "" : "+" };

            // Base
            string entryString = currDamageDie.Item1.Item1 + "D" + currDamageDie.Item1.Item2;
            if (hasBaseBonus)
                entryString += signs[0] + currDamageDie.Item1.Item3;
            // Critical
            if (hasCriticalCount || hasCriticalSides || hasCriticalBonus)
                entryString += " ("
                                + (hasCriticalCount ? currDamageDie.Item2.Item1.ToString() : "")
                                + ((hasCriticalCount || hasCriticalSides) ? "D" : "")
                                + (hasCriticalSides ? currDamageDie.Item2.Item2.ToString() : "")
                                + (hasCriticalBonus ? signs[1] + currDamageDie.Item2.Item3 : "")
                                + ")";
            // Bleed
            if (hasBaseBleedCount || hasBaseBleedSides || hasBaseBleedBonus
                || hasCriticalBleedCount || hasCriticalBleedSides || hasCriticalBleedBonus)
                entryString += " 🗡 ";
            // Base
            if (hasBaseBleedCount || hasBaseBleedSides || hasBaseBleedBonus)
                entryString += (hasBaseBleedCount ? currBleedDie.Item1.Item1.ToString() : "")
                                + ((hasBaseBleedCount || hasBaseBleedSides) ? "D" : "")
                                + (hasBaseBleedSides ? currBleedDie.Item1.Item2.ToString() : "")
                                + (hasBaseBleedBonus ? signs[2] + currBleedDie.Item1.Item3 : "");
            // Critical
            if (hasCriticalBleedCount || hasCriticalBleedSides || hasCriticalBleedBonus)
                entryString += " ("
                                + (hasCriticalBleedCount ? currBleedDie.Item2.Item1.ToString() : "")
                                + ((hasCriticalBleedCount || hasCriticalBleedSides) ? "D" : "")
                                + (hasCriticalBleedSides ? currBleedDie.Item2.Item2.ToString() : "")
                                + (hasCriticalBleedBonus ? signs[3] + currBleedDie.Item2.Item3 : "")
                                + ")";

            return entryString;
        }
        private Tuple<Tuple<int, int, int>, Tuple<int, int, int>, 
                    Tuple<int, int, int>, Tuple<int, int, int>> GetDamageControlValues()
        {
            // COUNT
            int damageCount = GetSettingFromControl(CalculatorDamageDieCountTextBox);
            int damageCritCount = GetSettingFromControl(CalculatorDamageCriticalDieCountTextBox);
            int damageBleedCount = GetSettingFromControl(CalculatorDamageBleedDieCountTextBox);
            int damageCritBleedCount = GetSettingFromControl(CalculatorDamageCriticalBleedDieCountTextBox);

            // SIZE
            int damageSize = GetSettingFromControl(CalculatorDamageDieSizeTextBox);
            int damageCritSize = GetSettingFromControl(CalculatorDamageCriticalDieSizeTextBox);
            int damageBleedSize = GetSettingFromControl(CalculatorDamageBleedDieSizeTextBox);
            int damageCritBleedSize = GetSettingFromControl(CalculatorDamageCriticalBleedDieSizeTextBox);

            // BONUS
            int damageBonus = GetSettingFromControl(CalculatorDamageDieBonusTextBox);
            int damageCritBonus = GetSettingFromControl(CalculatorDamageCriticalDieBonusTextBox);
            int damageBleedBonus = GetSettingFromControl(CalculatorDamageBleedDieBonusTextBox);
            int damageCritBleedBonus = GetSettingFromControl(CalculatorDamageCriticalBleedDieBonusTextBox);

            return new(new(damageCount, damageSize, damageBonus),
                        new(damageCritCount, damageCritSize, damageCritBonus),
                        new(damageBleedCount, damageBleedSize, damageBleedBonus),
                        new(damageCritBleedCount, damageCritBleedSize, damageCritBleedBonus));
        }

        private void DamageListBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (CalculatorDamageListBox.SelectedIndex != -1)
                DamageEditButton_MouseClick(sender: sender, e: e);
        }

        private void DefaultSettingsButton_Click(object sender, EventArgs e)
        {
            currEncounterSettings.ResetSettings();
            ResetVisuals(currEncounterSettings);
        }

        // MOUSE
        //

        // HELP BUTTON
        private void HelpButton_MouseClick(object sender, MouseEventArgs e)
        {
            // Toggle Help Mode
            SetHelpMode(!help_mode_enabled);
            UpdateMouse();
        }
        private void SetHelpMode(bool mode)
        {
            help_mode_enabled = mode;
            CalculatorHelpModeButton.Text = mode ? "Disable Help Mode"
                                                 : "Enable Help Mode";
        }
        // MOUSE APPEARANCE
        private enum MouseAppearance
        {
            NormalHelpOff,
            NormalHelpOn,
            BatchHelpOff,
            BatchHelpOn,
            BatchClickOn,
        }
        private void UpdateMouse()
        {
            if (help_mode_enabled)
            {
                if (batch_mode_enabled)
                    if (hovering_entrybox)
                        SetMouse(MouseAppearance.BatchClickOn);
                    else
                        SetMouse(MouseAppearance.BatchHelpOn);
                else
                    SetMouse(MouseAppearance.NormalHelpOn);
            }
            else
            {
                if (batch_mode_enabled)
                // Disable Help Mode
                    SetMouse(MouseAppearance.BatchHelpOff);
                
                else
                // Disable Regular Help Mode
                    SetMouse(MouseAppearance.NormalHelpOff);
                
            }
        }
        private void SetMouse(MouseAppearance mode)
        {
            switch (mode)
            {
                case MouseAppearance.NormalHelpOff:
                    Cursor = Cursors.Default;
                    break;
                case MouseAppearance.NormalHelpOn:
                    Cursor = Cursors.Help;
                    break;
                case MouseAppearance.BatchHelpOff:
                    Cursor = Cursors.Cross;
                    break;
                case MouseAppearance.BatchHelpOn:
                    Cursor = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("Pickings_For_Kurtulmak.cross_question.cur"));
                    break;
                case MouseAppearance.BatchClickOn:
                    Cursor = new(Assembly.GetExecutingAssembly().GetManifestResourceStream("Pickings_For_Kurtulmak.cross_exclaim.cur"));
                    break;
            }
        }
        private void Control_MouseHoverShowTooltip(object sender, EventArgs e)
        {
            if (help_mode_enabled)
            {
                CalculatorHelpToolTip.SetToolTip(sender as Control, label_hashes_help[sender.GetHashCode()]);
            }
        }

        // PROGRESS BAR
        public void SetProgressBar(object sender, int progress)
        {
            if (batch_mode_enabled)
            {
                CalculatorMiscStatisticsCalculateStatsProgressBars.Value = Math.Clamp(
                    value: (int)(CalculatorMiscStatisticsCalculateStatsProgressBars.Maximum
                    * computationStepsCurrent / computationStepsTotal) + 1, 
                    min: 0, 
                    max: CalculatorMiscStatisticsCalculateStatsProgressBars.Maximum);
                CalculatorMiscStatisticsCalculateStatsProgressBars.Value -= 1;
            }
            else
            {
                CalculatorMiscStatisticsCalculateStatsProgressBars.Value = Math.Clamp(progress + 1, 0, CalculatorMiscStatisticsCalculateStatsProgressBars.Maximum);
                CalculatorMiscStatisticsCalculateStatsProgressBars.Value = progress;
            }
        }

        // BATCH COMPUTATION
        // Toggle Batch Computation Mode
        private void CalculatorToggleBatchComputeButton_MouseClick(object sender, MouseEventArgs e)
        {
            if (batch_mode_enabled)
            { // Disable batch mode
                CalculatorBatchComputeButton.Text = "Enable Batch Mode";
                SetBatchMode(false);
                SetBatchGraphVisibility(false);
            }
            else
            { // Enable batch mode
                // Fix the scaling on the button & set the first text to be right
                CalculatorBatchComputeButton.Text = "Disable Batch Mode";
                SetBatchMode(true);
                SetBatchGraphVisibility(true);
            }
            UpdateMouse();
        }
        private void SetBatchMode(bool mode)
        {
            if (mode)
            { // Enable Batch Mode
                batch_mode_enabled = true;

                // Enable Warning Text
                CalculatorWarningLabel.Visible = true;
                CalculatorBCLGLabel.Visible = true;
                CalculatorEMELabel.Visible = true;
            }
            else
            { // Disable Batch Mode
                batch_mode_enabled = false;

                // Enable Warning Text
                CalculatorWarningLabel.Visible = false;
                CalculatorBCLGLabel.Visible = false;
                CalculatorEMELabel.Visible = false;

                // Reset Visible Batch Control
                HideBatchPopup();
            }
        }
        private void SetBatchGraphVisibility(bool state)
        {
            if (state)
            { // Show the batch graph
                CalculatorMiscStatisticsGroupBox.Visible = false;
                CalculatorEncounterStatisticsGroupBox.Visible = false;
                CalculatorDamageDistributionScottPlot.Visible = false;
                CalculatorBatchComputeScottPlot.Visible = true;
            }
            else
            { // Hide the batch graph
                CalculatorMiscStatisticsGroupBox.Visible = true;
                CalculatorEncounterStatisticsGroupBox.Visible = true;
                CalculatorDamageDistributionScottPlot.Visible = true;
                CalculatorBatchComputeScottPlot.Visible = false;
            }
        }

        // Batch Menu Buttons
        private void CalculatorBatchComputePopupXButton_Click(object sender, EventArgs e)
        { // Close/Discard the current batch settings
            SetControlAsBatched(batch_mode_selected, false, default);

            HideBatchPopup();
        }
        private void CalculatorBatchComputePopupSaveButton_Click(object sender, EventArgs e)
        { // Save the current batch settings
            try
            {
                CheckBatchParseSafety();

                BatchModeSettings settings = ReadBatchSettings();

                // Check it's safe to compute
                CheckBatchLogicSafety(settings);

                // Process the step value after confirming it's safe
                settings.GetValueAtStep();

                // Update the control
                SetControlAsBatched(batch_mode_selected, true, settings);

                // Reset the window after saving
                HideBatchPopup();
            }
            catch (Exception ex)
            {
                PushBatchErrorMessages(ex);
            }
        }

        // Batch Interaction
        private void TextBox_MouseClickShowBatchComputation(object sender, EventArgs e)
        {
            if (batch_mode_enabled)
            {
                // Store the casted control to improve performance
                Control control = sender as Control;
                
                // Move the popup
                HideBatchPopup();
                ShowBatchPopup(control);

                // Get the setting reference
                EncounterSetting encounterSetting = control_hash_to_setting[control.GetHashCode()];
                
                if (batched_variables.TryGetValue(encounterSetting, out BatchModeSettings settings))
                { // Loaded the batch variable

                    // Load variables to the popup
                    LoadSettingsToBatchComputePopup(settings);

                    // Remove the old value from the list
                    batched_variables.Remove(encounterSetting);
                }
                else
                {
                    // Store the value to being batched
                    batched_variables_last_value.Add(encounterSetting, GetSettingFromControl(control));
                }
            }
        }

        // Batch Data
        private BatchModeSettings ReadBatchSettings()
        {
            bool isDamageDie = setting_to_info.TryGetValue(control_hash_to_setting[batch_mode_selected.GetHashCode()], out var value);
            BatchModeSettings return_setting = new(layer: (int)CalculatorBatchComputePopupLayerNumericUpDown.Value,
                                                    start: (int)CalculatorBatchComputePopupStartValueNumericUpDown.Value,
                                                    end: (int)CalculatorBatchComputePopupEndValueNumericUpDown.Value,
                                                    steps: GetIntListFromCommaString(CalculatorBatchComputePopupStepPatternTextBox.Text),
                                                    index: (int)control_hash_to_setting[batch_mode_selected.GetHashCode()] >= 21
                                                                ? CalculatorDamageListBox.SelectedIndex
                                                                : -1,
                                                    die_field: isDamageDie
                                                                ? value.Item1
                                                                : DieField.bonus,
                                                    critical: isDamageDie && value.Item3,
                                                    bleed: isDamageDie && value.Item2);
            
            return return_setting;
        }
        private bool CheckControlBatched(EncounterSetting encounterSetting)
        {
            return batched_variables.ContainsKey(encounterSetting);
        }
        private static List<int> GetIntListFromCommaString(string str)
        {
            return str.Split(',').Select(int.Parse).ToList();
        }

        // Show Batch Computation Menu
        private void LoadSettingsToBatchComputePopup(BatchModeSettings settings)
        { // Displays the given settings on the comptute popup
            CalculatorBatchComputePopupEndValueNumericUpDown.Value = settings.end;
            CalculatorBatchComputePopupStartValueNumericUpDown.Value = settings.start;
            CalculatorBatchComputePopupStepPatternTextBox.Text = string.Join(",", settings.steps.Select(x => x.ToString()));
            CalculatorBatchComputePopupLayerNumericUpDown.Value = settings.layer;
        }
        private void ShowBatchPopup(Control control)
        {
            // Update reference variable
            batch_mode_selected = control;

            // Position Panel next to the clicked TextBox
            CalculatorBatchComputePopupPanel.Location = CalculatorTabPage.PointToClient(control.PointToScreen(new Point(0, 0)));
            CalculatorBatchComputePopupPanel.Location = new Point(CalculatorBatchComputePopupPanel.Location.X - 2, CalculatorBatchComputePopupPanel.Location.Y - 2);

            // Show Panel Regardless of wheter it was loaded or not
            CalculatorBatchComputePopupPanel.Visible = true;
            CalculatorBatchComputePopupPanel.BringToFront();
        }
        private void HideBatchPopup()
        { // Hide and reset the batch popup
            batch_mode_selected = null;
            CalculatorBatchComputePopupPanel.Visible = false;
            CalculatorBatchComputePopupStartValueNumericUpDown.Value = 0;
            CalculatorBatchComputePopupEndValueNumericUpDown.Value = 0;
            CalculatorBatchComputePopupStepPatternTextBox.Text = string.Empty;
            CalculatorBatchComputePopupLayerNumericUpDown.Value = 0;
        }
        private void SetControlAsBatched(Control control, bool batched, BatchModeSettings settings)
        {
            EncounterSetting encounterSetting = control_hash_to_setting[batch_mode_selected.GetHashCode()];

            if (batched)
            {
                // Save the Settings
                batched_variables.Add(encounterSetting, settings);

                // Lock the Input on the Field
                if (control is TextBox textBox)
                {
                    textBox.ReadOnly = true;
                    textBox.Text = "BATCHED";
                }
                else if (control is NumericUpDown numericUpDown)
                {
                    numericUpDown.ReadOnly = true;
                    numericUpDown.Text = "BATCHED";
                }

                // Update Visuals of Batched Control
                batch_mode_selected.BackColor = Color.Lavender;
            }
            else
            { // Unbatch Variable
                batched_variables.Remove(encounterSetting);

                // Lock the Input on the Field
                if (control is TextBox textBox)
                {
                    textBox.ReadOnly = false;
                    textBox.Text = batched_variables_last_value[encounterSetting].ToString();
                }
                else if (control is NumericUpDown numericUpDown)
                {
                    numericUpDown.ReadOnly = false;
                    numericUpDown.Value = batched_variables_last_value[encounterSetting];
                }

                // Remove the old last-value.
                batched_variables_last_value.Remove(encounterSetting);
                
                // Update visuals on Unbatched Control
                batch_mode_selected.BackColor = SystemColors.Window;
            }
        }

        // BATCH COMPUTATION ERROR CHECKING
        private void CheckBatchParseSafety()
        { // Check and throw any errors found
            Exception ex = new();

            // Empty Data Exceptions
            // Attack Category
            if (CalculatorBatchComputePopupStepPatternTextBox.TextLength == 0)
            {
                ex.Data.Add(531, "Missing Data Exception: Missing step pattern");
            }

            // Throw exception on problems
            if (ex.Data.Count > 0)
                throw ex;
        }
        private static void CheckBatchLogicSafety(BatchModeSettings setting)
        { // Check and throw any errors found
            Exception ex = new();

            // Empty Data Exceptions
            // Attack Category
            if (setting.end < setting.start && setting.step_direction > 0)
            {
                ex.Data.Add(551, "Bad Data Combination Exception: End is smaller than Start with non-negative Step Size");
            }
            if (setting.end > setting.start && setting.step_direction < 0)
            {
                ex.Data.Add(552, "Bad Data Combination Exception: End is larger than Start with negative Step Size");
            }
            if (setting.end != setting.start && setting.step_direction == 0)
            {
                ex.Data.Add(541, "Bad Data Exception: Net step size is zero");
            }

            // Throw exception on problems
            if (ex.Data.Count > 0)
                throw ex;
        }
        private void PushBatchErrorMessages(Exception ex)
        { // Push Error Codes to their respective boxes
            ClearBatchErrorMessages();
            foreach (int errorCode in ex.Data.Keys)
            {
                switch (errorCode)
                {
                    case 531:
                        PushError(CalculatorBatchComputePopupStepPatternTextBox, "A step pattern is missing!");
                        break;
                    case 551: // Push
                        PushError(CalculatorBatchComputePopupStepPatternTextBox, "The End value is smaller than the Start value with non-negative Step Size (infinite steps)!");
                        break;
                    case 552: // Push
                        PushError(CalculatorBatchComputePopupStepPatternTextBox, "The End value is larger than the Start value with negative Step Size (infinite steps)!");
                        break;
                    case 541: // Push
                        PushError(CalculatorBatchComputePopupStepPatternTextBox, "The net step size is zero (infinite steps)!");
                        break;
                }
            }
        }
        private void ClearBatchErrorMessages()
        {
            PushError(CalculatorBatchComputePopupStepPatternTextBox, "", replace: true);
        }

        // Batch Computation
        public static async Task<BatchResults> ComputeAverageDamage_BATCH(EncounterSettings encounter_settings, 
                                                            SortedDictionary<int, List<Tuple<EncounterSetting, 
                                                            BatchModeSettings>>> binned_compute_layers,
                                                            int[] maxSteps,
                                                            Dictionary<EncounterSetting, BatchModeSettings> batched_variables,
                                                            IProgress<int> progress)
        {

            // Declare & Initialize the return value
            BatchResults batch_results = new();

            // To-Do: Implement proper progress tracking
            // To-Do: Fix batch compute not working with any layers. Crashes halfway in?
            // COMPUTATION FLAGS

            // Set the flag for actively computing damage
            computing_damage = true;

            // Store the dimensions of the array
            batch_results.dimensions = maxSteps.ToList();

            // Set number of dimensions
            int dimensions = binned_compute_layers.Count;

            // Create an array to hold all the damage stats, sized by the most number of steps in each batch layer
            Array totalDamageStats = Array.CreateInstance(typeof(DamageStats), maxSteps);
            batch_results.tick_values = Array.CreateInstance(typeof(Dictionary<EncounterSetting, int>), maxSteps);

            // Create an array to track current index such that
            // [X, Y, Z, ... END] represent the [LOWEST, SECOND LOWEST, THIRD LOWEST, ... HIGHEST] layers.
            int[] indices = new int[dimensions];

            // Calculate the total number of entries in the array
            int number_of_values = maxSteps.Aggregate((sum, next) => sum * next);
            computationStepsTotal = number_of_values;
            computationStepsCurrent = 1;

            // Use a dictionary to track the current value of each variable
            Dictionary<EncounterSetting, int> computeVariables = new()
            {
            // ENCOUNTER
                // Number of Encounters
                { EncounterSetting.number_of_encounters, encounter_settings.number_of_encounters },
                // Rounds Per Encounter
                { EncounterSetting.rounds_per_encounter, batched_variables.TryGetValue(EncounterSetting.rounds_per_encounter,
                                                                                        out BatchModeSettings roundsPerEncounterSetting)
                                                            ? roundsPerEncounterSetting.start
                                                            : encounter_settings.rounds_per_encounter },
                // Engagement Range
                { EncounterSetting.engagement_range, batched_variables.TryGetValue(EncounterSetting.engagement_range,
                                                                                        out BatchModeSettings engagementRangeSetting)
                                                            ? engagementRangeSetting.start
                                                            : encounter_settings.engagement_range },
            // ACTIONS
                    // Any
                { EncounterSetting.actions_per_round_any, batched_variables.TryGetValue(EncounterSetting.actions_per_round_any,
                                                                                        out BatchModeSettings actionsPerRoundAnySetting)
                                                            ? actionsPerRoundAnySetting.start
                                                            : encounter_settings.actions_per_round.any },
                    // Strike
                { EncounterSetting.actions_per_round_strike, batched_variables.TryGetValue(EncounterSetting.actions_per_round_strike,
                                                                                        out BatchModeSettings actionsPerRoundStrikeSetting)
                                                            ? actionsPerRoundStrikeSetting.start
                                                            : encounter_settings.actions_per_round.strike },
                    // Draw
                { EncounterSetting.actions_per_round_draw, batched_variables.TryGetValue(EncounterSetting.actions_per_round_draw,
                                                                                        out BatchModeSettings actionsPerRoundDrawSetting)
                                                            ? actionsPerRoundDrawSetting.start
                                                            : encounter_settings.actions_per_round.draw },
                    // Reload
                { EncounterSetting.actions_per_round_reload, batched_variables.TryGetValue(EncounterSetting.actions_per_round_reload,
                                                                                        out BatchModeSettings actionsPerRoundReloadSetting)
                                                            ? actionsPerRoundReloadSetting.start
                                                            : encounter_settings.actions_per_round.reload },
                    // Long Reload
                { EncounterSetting.actions_per_round_long_reload, batched_variables.TryGetValue(EncounterSetting.actions_per_round_long_reload,
                                                                                        out BatchModeSettings actionsPerRoundLongReloadSetting)
                                                            ? actionsPerRoundLongReloadSetting.start
                                                            : encounter_settings.actions_per_round.long_reload },
                    // Stride
                { EncounterSetting.actions_per_round_stride, batched_variables.TryGetValue(EncounterSetting.actions_per_round_stride,
                                                                                        out BatchModeSettings actionsPerRoundStrideSetting)
                                                            ? actionsPerRoundStrideSetting.start
                                                            : encounter_settings.actions_per_round.stride },
                    // Draw
                { EncounterSetting.draw, batched_variables.TryGetValue(EncounterSetting.draw,
                                                                                        out BatchModeSettings drawSetting)
                                                            ? drawSetting.start
                                                            : encounter_settings.draw },
            // RELOAD
                // Reload
                { EncounterSetting.reload, batched_variables.TryGetValue(EncounterSetting.reload,
                                                                                        out BatchModeSettings reloadSetting)
                                                            ? reloadSetting.start
                                                            : encounter_settings.reload },
                // Long Reload
                { EncounterSetting.long_reload, batched_variables.TryGetValue(EncounterSetting.long_reload,
                                                                                        out BatchModeSettings longReloadSetting)
                                                            ? longReloadSetting.start
                                                            : encounter_settings.long_reload },
                // Magazine Size
                { EncounterSetting.magazine_size, batched_variables.TryGetValue(EncounterSetting.magazine_size,
                                                                                        out BatchModeSettings magazineSizeSetting)
                                                            ? magazineSizeSetting.start
                                                            : encounter_settings.magazine_size },
            // ATTACK
                // Bonus To Hit
                { EncounterSetting.bonus_to_hit, batched_variables.TryGetValue(EncounterSetting.bonus_to_hit,
                                                                                        out BatchModeSettings bonusToHitSetting)
                                                            ? bonusToHitSetting.start
                                                            : encounter_settings.bonus_to_hit },
                // AC
                { EncounterSetting.AC, batched_variables.TryGetValue(EncounterSetting.AC,
                                                                                        out BatchModeSettings ACSetting)
                                                            ? ACSetting.start
                                                            : encounter_settings.AC },
                // Crit Threshhold
                { EncounterSetting.crit_threshhold, batched_variables.TryGetValue(EncounterSetting.crit_threshhold,
                                                                                        out BatchModeSettings critThreshholdSetting)
                                                            ? critThreshholdSetting.start
                                                            : encounter_settings.crit_threshhold },
                // MAP Modifier
                { EncounterSetting.MAP_modifier, batched_variables.TryGetValue(EncounterSetting.MAP_modifier,
                                                                                        out BatchModeSettings MAPModifierSetting)
                                                            ? MAPModifierSetting.start
                                                            : encounter_settings.MAP_modifier },
            // REACH
                // Range Increment
                { EncounterSetting.range, batched_variables.TryGetValue(EncounterSetting.range,
                                                                                        out BatchModeSettings rangeSetting)
                                                            ? rangeSetting.start
                                                            : encounter_settings.range },
                // Volley Increment
                { EncounterSetting.volley, batched_variables.TryGetValue(EncounterSetting.volley,
                                                                                        out BatchModeSettings volleySetting)
                                                            ? volleySetting.start
                                                            : encounter_settings.volley },
                // Move Speed
                { EncounterSetting.move_speed, batched_variables.TryGetValue(EncounterSetting.move_speed,
                                                                                        out BatchModeSettings moveSpeedSetting)
                                                            ? moveSpeedSetting.start
                                                            : encounter_settings.move_speed },
            };
            List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> computeVariablesDamageDice = new (encounter_settings.damage_dice);
            List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> computeVariablesDamageDiceDOT = new (encounter_settings.damage_dice_DOT);

            // BATCH COMPUTATION
            // Start a do-while loop to iterate through all elements of the array
            do
            {
                // Compute the Damage at the Current Iteration Step
                Task<DamageStats> computeDamage = Task.Run(() => CalculateAverageDamage(number_of_encounters: computeVariables[EncounterSetting.number_of_encounters],
                                                        rounds_per_encounter: computeVariables[EncounterSetting.rounds_per_encounter],
                                                        actions_per_round: new Actions(any: computeVariables[EncounterSetting.actions_per_round_any],
                                                                                strike: computeVariables[EncounterSetting.actions_per_round_strike],
                                                                                stride: computeVariables[EncounterSetting.actions_per_round_stride],
                                                                                draw: computeVariables[EncounterSetting.draw],
                                                                                reload: computeVariables[EncounterSetting.reload],
                                                                                long_reload: computeVariables[EncounterSetting.long_reload]),
                                                        reload_size: computeVariables[EncounterSetting.magazine_size],
                                                        reload: computeVariables[EncounterSetting.reload],
                                                        long_reload: computeVariables[EncounterSetting.long_reload],
                                                        draw: computeVariables[EncounterSetting.draw],
                                                        damage_dice: computeVariablesDamageDice, // Figure something out for this one
                                                        bonus_to_hit: computeVariables[EncounterSetting.bonus_to_hit],
                                                        AC: computeVariables[EncounterSetting.AC],
                                                        crit_threshhold: computeVariables[EncounterSetting.crit_threshhold],
                                                        MAP_modifier: computeVariables[EncounterSetting.MAP_modifier],
                                                        engagement_range: computeVariables[EncounterSetting.engagement_range],
                                                        move_speed: computeVariables[EncounterSetting.move_speed],
                                                        seek_favorable_range: computeVariables[EncounterSetting.move_speed] > 0,
                                                        range: computeVariables[EncounterSetting.range],
                                                        volley: computeVariables[EncounterSetting.volley],
                                                        damage_dice_DOT: computeVariablesDamageDiceDOT, // This one too
                                                        progress: progress)); // To-do: Add some batch computation detection to the progress code

                // How to access the current element of the array using the GetValue method
                DamageStats currDamage = await computeDamage;

                // Track the highest damage encounter thus far
                batch_results.max_width = currDamage.highest_encounter_damage > batch_results.max_width
                                            ? currDamage.highest_encounter_damage
                                            : batch_results.max_width;

                // How to set the current element of the array using the SetValue method
                totalDamageStats.SetValue(value: currDamage, indices: indices);

                // Iterate across each layer to check for/apply changes to the existing variables
                foreach (int layer_index in binned_compute_layers.Keys)
                {
                    // Iterate within each layer (through each "slice", representing a batched variable)
                    foreach (var slice in binned_compute_layers[layer_index])
                    {
                        // To-do: Store the array instead to a list of graph views
                        // To-do: Load current graph from the list
                        // To-do: Add button/dropdown to select which one
                        // To-do: Make 'gif' feature
                        /*
                        if (layer_index == binned_compute_layers.Keys.Max()
                            && (indices.Length == 1 // Pass through if there's only one layer
                                || indices.ToList().Take(indices.Length - 1).All(x => x == 0))) // Pass through only if we're on the first iteration layer
                        { // Only store the top layer iteration labels
                            // Catch an issue with the first value being skipped due to how index iteration is implemented
                            int tick_index = indices[^1] - 1;
                            batch_results.tick_values[tick_index].TryAdd(slice.Item1, computeVariables[slice.Item1]);
                        }*/
                        int storedValue;
                        if (slice.Item2.index != -1)
                        { // Store the current damageDie value
                            var readDamageList = slice.Item2.bleed
                                                ? computeVariablesDamageDiceDOT
                                                : computeVariablesDamageDice;
                            var readDamageTuple = slice.Item2.critical
                                                ? readDamageList[slice.Item2.index].Item2
                                                : readDamageList[slice.Item2.index].Item1;
                            storedValue = (slice.Item2.die_field) switch
                            {
                                DieField.count => readDamageTuple.Item1,
                                DieField.size => readDamageTuple.Item2,
                                DieField.bonus => readDamageTuple.Item3
                            };
                        }// To-do: Fix simultaneous stepping of damage dice variables
                        else
                        { // Store the non-list variable
                            storedValue = computeVariables[slice.Item1];
                        }

                        if (batch_results.tick_values.GetValue(indices: indices) is Dictionary<EncounterSetting, int> tick_dictionary)
                        {
                            tick_dictionary.TryAdd(slice.Item1, storedValue);
                        }
                        else
                        {
                            Dictionary<EncounterSetting, int> new_tick_dictionary = new()
                                {
                                    { slice.Item1, storedValue }
                                };
                            batch_results.tick_values.SetValue(indices: indices, value: new_tick_dictionary);
                        }
                    }
                }

                // Increment the rightmost dimension (the last index), which will be the root iterator (highest layer)
                indices[dimensions - 1]++;

                // Cascade the current indices
                for (int dimension_index = dimensions - 1; dimension_index > 0; dimension_index--)
                {
                    // If the current dimension is larger than its limit, iterate the next index down the line
                    if (indices[dimension_index] >= totalDamageStats.GetLength(dimension_index)) // Check if the current index reached the back of the respective dimension
                    { // End of current dimension reached
                      // Cascade-revert all indices to the right of the current index, then iterate the next dimension
                        for (int cascade_index = dimension_index; cascade_index < dimensions; cascade_index++)
                        {
                            indices[dimension_index] = 0;
                        }

                        // Iterate the next higher dimension if not at the frontmost index
                        if (dimension_index != 0)
                            indices[dimension_index - 1]++;
                    }
                }

                // Iterate across each layer to check for/apply changes to the existing variables
                foreach (int layer_index in binned_compute_layers.Keys)
                {
                    // Iterate within each layer (through each "slice", representing a batched variable)
                    foreach (var slice in binned_compute_layers[layer_index])
                    {
                        // To-do: Store the array instead to a list of graph views
                        // To-do: Load current graph from the list
                        // To-do: Add button/dropdown to select which one
                        // To-do: Make 'gif' feature
                        /*
                        if (layer_index == binned_compute_layers.Keys.Max()
                            && (indices.Length == 1 // Pass through if there's only one layer
                                || indices.ToList().Take(indices.Length - 1).All(x => x == 0))) // Pass through only if we're on the first iteration layer
                        { // Only store the top layer iteration labels
                            // Catch an issue with the first value being skipped due to how index iteration is implemented
                            int tick_index = indices[^1] - 1;
                            batch_results.tick_values[tick_index].TryAdd(slice.Item1, computeVariables[slice.Item1]);
                        }*/

                        // Run last-loop computation
                        if (indices[^1] == maxSteps[^1] - 1)
                        {
                            int storedValue;
                            if (slice.Item2.index != -1)
                            { // Store the current damageDie value
                                var readDamageList = slice.Item2.bleed
                                                    ? computeVariablesDamageDiceDOT
                                                    : computeVariablesDamageDice;
                                var readDamageTuple = slice.Item2.critical
                                                    ? readDamageList[slice.Item2.index].Item2
                                                    : readDamageList[slice.Item2.index].Item1;
                                storedValue = (slice.Item2.die_field) switch
                                {
                                    DieField.count => readDamageTuple.Item1,
                                    DieField.size => readDamageTuple.Item2,
                                    DieField.bonus => readDamageTuple.Item3
                                };
                            }
                            else
                            { // Store the non-list variable
                                storedValue = computeVariables[slice.Item1];
                            }

                            if (batch_results.tick_values.GetValue(indices: indices) is Dictionary<EncounterSetting, int> tick_dictionary)
                            {
                                tick_dictionary.TryAdd(slice.Item1, storedValue);
                            }
                            else
                            {
                                Dictionary<EncounterSetting, int> new_tick_dictionary = new()
                                {
                                    { slice.Item1, storedValue }
                                };
                                batch_results.tick_values.SetValue(indices: indices, value: new_tick_dictionary);
                            }
                        }

                        if (slice.Item2.index != -1)
                        { // Check if currently a damage die
                            List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> editedDamage = slice.Item2.bleed
                                                ? computeVariablesDamageDiceDOT
                                                : computeVariablesDamageDice;

                            EditDamageDie(damage_dice: editedDamage,
                                            setting: slice.Item2.die_field,
                                            value: slice.Item2.GetValueAtStep(indices[binned_compute_layers.Keys.ToList().IndexOf(layer_index)]),
                                            index: slice.Item2.index,
                                            edit_critical: slice.Item2.critical);
                        }
                        else
                        {
                            computeVariables[slice.Item1] = slice.Item2.GetValueAtStep(indices[binned_compute_layers.Keys.ToList().IndexOf(layer_index)]);
                        }
                    }
                }

                // Track progress thus far
                computationStepsCurrent++;
            } while (indices[0] < totalDamageStats.GetLength(0));

            // Reset the damage computation flag
            computing_damage = false;

            // Store the computed results
            batch_results.raw_data = totalDamageStats;

            return batch_results;
        }
        // Collects same-layered batch variables into a SortedDictionary
        public static SortedDictionary<int, List<Tuple<EncounterSetting, BatchModeSettings>>> ComputeBinnedLayersForBatch(Dictionary<EncounterSetting, BatchModeSettings> batched_vars)
        {
            // Collect all batched settings into layers
            SortedDictionary<int, List<Tuple<EncounterSetting, BatchModeSettings>>> binned_compute_layers = new();
            foreach (var batched_var in batched_vars)
            {
                if (binned_compute_layers.TryGetValue(batched_var.Value.layer, out var bin))
                { // Store the batched layer in the existing bin
                    bin.Add(new(batched_var.Key, batched_var.Value));
                }
                else
                { // Create a new bin for the given layer
                    binned_compute_layers.Add(batched_var.Value.layer, new() { new(batched_var.Key, batched_var.Value) });
                }
            }
            return binned_compute_layers;
        }
        // Computes the dimensions array, i.e. the number of iteration steps necessary for each layer within a set of batch layers.
        public static int[] ComputeMaxStepsArrayForBatch(SortedDictionary<int, List<Tuple<EncounterSetting, BatchModeSettings>>> binned_compute_layers)
        {
            // Get the size of each dimension
            return binned_compute_layers.Select(kvp => kvp.Value.Max(t => t.Item2.number_of_steps + 1))
                                        .ToArray();
        }
    }
}