
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static DamageCalculatorGUI.DamageCalculator;
using System.Reflection;
using Microsoft.VisualBasic.Devices;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using SharpNeatLib.Maths;
using ScottPlot;

namespace DamageCalculatorGUI
{
    public partial class CalculatorWindow : Form
    {
        // Damage Stats Struct
        public DamageStats damageStats = new();

        // Help Variable
        static bool help_mode_enabled;
        Dictionary<int, string> label_hashes = new();
        struct EncounterSettings
        {
            // Damage Computation Variables
            public int number_of_encounters = 1;
            public int rounds_per_encounter = 0;
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

        public CalculatorWindow()
        {
            InitializeComponent();
        }
        private void CalculatorWindowLoad(object sender, EventArgs e)
        {
            // Default Settings
            currEncounterSettings.ResetSettings();
            ResetVisuals(currEncounterSettings);

            // Lock Window Dimensions
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // Enable Carat Hiding
            AddCaretHidingEvents();

            // Generate and store label hashes
            StoreLabelHashes();

            // Add a timer to the graph to render it regularly

        }

        private void UpdateGraph()
        {
            // Clear the Plot
            CalculatorDamageDistributionScottPlot.Plot.Clear();

            // Get the maximum value in the list
            int maxKey = damageStats.damageBins.Keys.Max();
            // Convert the damage bins into an array
            double[] graphBins = Enumerable.Range(0, maxKey + 1)
                .Select(i => damageStats.damageBins.TryGetValue(i, out int value) ? value : 0)
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
            CalculatorDamageDistributionScottPlot.Plot.SetAxisLimits(yMin: 0, xMin: 0, xMax: maxKey, yMax: damageStats.damageBins.Values.Max() * 1.2);
            CalculatorDamageDistributionScottPlot.Plot.SetOuterViewLimits(yMin: 0, xMin: 0, xMax: maxKey, yMax: damageStats.damageBins.Values.Max() * 1.2);
            CalculatorDamageDistributionScottPlot.Plot.Legend(location: ScottPlot.Alignment.UpperLeft);
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
        private void StoreLabelHashes()
        {
            // Attack
            label_hashes.Add(CalculatorAttackBonusToHitLabel.GetHashCode(), "The total bonus to-hit. Around ~12 for a 4th level Gunslinger.");
            label_hashes.Add(CalculatorAttackCriticalHitMinimumLabel.GetHashCode(), "Minimum die roll to-hit in order to critically strike. Only way to crit other than getting 10 over the AC.");
            label_hashes.Add(CalculatorAttackACLabel.GetHashCode(), "Armor Class of the target to be tested against. Typically ~21 for a 4th level enemy.");
            label_hashes.Add(CalculatorAttackMAPModifierLabel.GetHashCode(), "Modifier to the Multiple-Attack-Penalty. Typically 0 unless you are using an Agile weapon or have Agile Grace, in which this is equal to -1 and -2 respectively.");
            // Ammunition
            label_hashes.Add(CalculatorAmmunitionReloadLabel.GetHashCode(), "The number of Interact actions required to re-chamber a weapon after firing. I.e. a 3 action round would compose of Strike, Reload 1, Strike or Strike, Reload 2.");
            label_hashes.Add(CalculatorAmmunitionMagazineSizeLabel.GetHashCode(), "The number of Strike actions that can be done before requiring a Long Reload.");
            label_hashes.Add(CalculatorAmmunitionLongReloadLabel.GetHashCode(), "The number of Interact actions required to replenish the weapon's Magazine Size to max. Includes one complementary Reload. I.e. two 3 action rounds would compose of Strike, Reload 1, Strike then Long Reload 2, Strike");
            label_hashes.Add(CalculatorAmmunitionDrawLengthLabel.GetHashCode(), "The number of Interact actions required to draw the weapon. I.e. a 3 action round would compose of Draw 1, Strike, Reload 1.");
            // Damage
            label_hashes.Add(CalculatorDamageDieSizeLabel.GetHashCode(), "The base damage of this weapon on hit. Represented by [X]d[Y]+[Z], such that a Y sided die will be rolled X times with a flat Z added (or subtracted if negative).");
            label_hashes.Add(CalculatorDamageCriticalDieLabel.GetHashCode(), "The upgraded die quantity, size, and/or flat bonus from Critical Strikes. Represented in vanilla Pathfinder 2E as Brutal Critical.");
            label_hashes.Add(CalculatorDamageBleedDieLabel.GetHashCode(), "The amount of Persistent Damage dealt on-hit by this weapon. Each applied instance of this is checked against a flat DC15 save every round to simulate a save.");
            label_hashes.Add(CalculatorDamageCriticalBleedDieLabel.GetHashCode(), "The upgraded die quantity, size, and/or flat bonus to Persistent damage when Critically Striking.");
            // Reach
            label_hashes.Add(CalculatorReachRangeIncrementLabel.GetHashCode(), "The Range Increment of the weapon. Attacks attacks against targets beyond the range increment take a stacking -2 penalty to-hit for each increment away. I.e. Range Increment of 30ft will take a -4 at 70ft.");
            label_hashes.Add(CalculatorReachVolleyIncrementLabel.GetHashCode(), "The Volley Increment of the weapon. Attacks made within this distance suffer a flat -2 to-hit.");
            // Encounter
            label_hashes.Add(CalculatorEncounterNumberOfEncountersLabel.GetHashCode(), "How many combat encounters to simulate. Default is around 100,000.");
            label_hashes.Add(CalculatorEncounterRoundsPerEncounterLabel.GetHashCode(), "How many rounds to simulate per encounter. Higher simulates more drawn out encounters, shorter simulates more brief encounters.");
            // Actions
            label_hashes.Add(CalculatorActionActionsPerRoundLabel.GetHashCode(), "How many actions granted each round. Typically 3 unless affected by an effect such as Haste.");
            label_hashes.Add(CalculatorActionExtraLimitedActionsLabel.GetHashCode(), "These actions can only be used for the specified action and are granted each round.");
            label_hashes.Add(CalculatorActionExtraLimitedActionsDrawLabel.GetHashCode(), "How many Draw-only actions to be granted each round.");
            label_hashes.Add(CalculatorActionExtraLimitedActionsReloadLabel.GetHashCode(), "How many Reload-only actions to be granted each round.");
            label_hashes.Add(CalculatorActionExtraLimitedActionsStrideLabel.GetHashCode(), "How many Stride-only actions to be granted each round.");
            label_hashes.Add(CalculatorActionExtraLimitedActionsStrikeLabel.GetHashCode(), "How many Strike-only actions to be granted each round.");
            label_hashes.Add(CalculatorActionExtraLimitedActionsLongReloadLabel.GetHashCode(), "How many Long Reload-only actions to be granted each round.");
            // Distance
            label_hashes.Add(CalculatorDistanceEngagementRangeLabel.GetHashCode(), "Distance to begin the encounter at. Defaults to 30 if unchecked.");
            label_hashes.Add(CalculatorReachMovementSpeedLabel.GetHashCode(), "Amount of distance covered with one Stride action. The simulated player will Stride into the ideal (within range and out of volley) firing position before firing.");
            // Encounter Damage Statistics
            label_hashes.Add(CalculatorEncounterStatisticsMeanLabel.GetHashCode(), "The 'true average' damage across all encounters. I.e. The sum of all encounters divded by the number of encounters.");
            label_hashes.Add(CalculatorEncounterStatisticsUpperQuartileLabel.GetHashCode(), "The 75th percentile damage across all encounters. I.e. The average high-end/lucky damage performance of the weapon.");
            label_hashes.Add(CalculatorEncounterStatisticsMedianLabel.GetHashCode(), "The 50th percentile damage across all encounters. I.e. The average center-most value and generally typical performance of the weapon.");
            label_hashes.Add(CalculatorEncounterStatisticsLowerQuartileLabel.GetHashCode(), "The 25th percentile damage across all encounters. I.e. The aveerage lower-end/unlucky damage performance of the weapon.");
            // Misc Statistics
            label_hashes.Add(CalculatorMiscStatisticsRoundDamageMeanLabel.GetHashCode(), "The 'true average' round damage for each round of combat. Best captures rapid-fire weapons.");
            label_hashes.Add(CalculatorMiscStatisticsAttackDamageMeanLabel.GetHashCode(), "The 'true average' attack damage for each attack landed. Best captures the typical per-hit performance of weapon.");
            label_hashes.Add(CalculatorMiscStatisticsAccuracyMeanLabel.GetHashCode(), "The average accuracy of the weapon. I.e. What percentage of attacks hit.");
            // Buttons
            label_hashes.Add(CalculatorCalculateDamageStatsButton.GetHashCode(), "CTRL + Left-Click to copy a comma separated list of the data below.");
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

            CalculatorDistanceEngagementRangeCheckBox.Checked = true;
            CalculatorDistanceEngagementRangeTextBox.Text = encounterSettings.engagement_range.ToString();
            CalculatorReachMovementSpeedCheckBoCalculatorx.Checked = encounterSettings.seek_favorable_range;
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
        private void PushErrorMessages(Exception ex)
        {
            foreach (int errorCode in ex.Data.Keys)
            {
                switch (errorCode)
                {
                    case 41: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorReachVolleyIncrementTextBox, "Volley longer Range will have unintended implications with movement!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorReachVolleyIncrementTextBox, -18);
                        break;
                    case 51: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorDamageListBox, "No damage dice detected!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorDamageListBox, -18);
                        break;
                    case 52: // Fix don't push
                        // N/A
                        break;
                    case 101: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorEncounterNumberOfEncountersTextBox, "Number of encounters must be greater than 0!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorEncounterNumberOfEncountersTextBox, -18);
                        break;
                    case 102: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorReachMovementSpeedTextBox, "Movement speed must be greater than 0!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorReachMovementSpeedTextBox, -18);
                        break;
                    case 103: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorEncounterRoundsPerEncounterTextBox, "Rounds per encounter must be greater than 0!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorEncounterRoundsPerEncounterTextBox, -18);
                        break;
                    case 104: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorActionActionsPerRoundTextBox, "Actions per round must be positive!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorActionActionsPerRoundTextBox, -18);
                        break;
                    case 105: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorReachRangeIncrementTextBox, "Range increment must be positive!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorReachRangeIncrementTextBox, -18);
                        break;
                    case 106: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorDistanceEngagementRangeTextBox, "Encounter distance must be positive!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorActionActionsPerRoundTextBox, -18);
                        break;
                    case 201: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorAttackBonusToHitTextBox, "No bonus to-hit value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorAttackBonusToHitTextBox, -18);
                        break;
                    case 202: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorAttackCriticalHitMinimumTextBox, "No crit at/below value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorAttackCriticalHitMinimumTextBox, -18);
                        break;
                    case 203: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorAttackACTextBox, "No AC value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorAttackACTextBox, -18);
                        break;
                    case 204: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorAttackMAPModifierTextBox, "No MAP value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorAttackMAPModifierTextBox, -18);
                        break;
                    case 205: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorAmmunitionReloadTextBox, "No reload value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorAmmunitionReloadTextBox, -18);
                        break;
                    case 206: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorAmmunitionMagazineSizeTextBox, "No magazine size value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorAmmunitionMagazineSizeTextBox, -18);
                        break;
                    case 207: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorAmmunitionLongReloadTextBox, "No long reload value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorAmmunitionLongReloadTextBox, -18);
                        break;
                    case 208: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorAmmunitionDrawLengthTextBox, "No draw length value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorAmmunitionDrawLengthTextBox, -18);
                        break;
                    case 209: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorReachRangeIncrementTextBox, "No range increment value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorReachRangeIncrementTextBox, -18);
                        break;
                    case 210: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorReachVolleyIncrementTextBox, "No volley increment value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorReachVolleyIncrementTextBox, -18);
                        break;
                    case 211: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorEncounterNumberOfEncountersTextBox, "Number of encounters value not provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorEncounterNumberOfEncountersTextBox, -18);
                        break;
                    case 212: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorEncounterRoundsPerEncounterTextBox, "No rounds per encounter value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorEncounterRoundsPerEncounterTextBox, -18);
                        break;
                    case 213: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorActionActionsPerRoundTextBox, "No actions per round value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorActionActionsPerRoundTextBox, -18);
                        break;
                    case 214: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorDistanceEngagementRangeTextBox, "No engagement range value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorDistanceEngagementRangeTextBox, -18);
                        break;
                    case 215: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorReachMovementSpeedTextBox, "No movement speed value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorReachMovementSpeedTextBox, -18);
                        break;
                    case 216: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorActionExtraLimitedActionsDrawNumericUpDown, "No bonus draw action value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorActionExtraLimitedActionsDrawNumericUpDown, -36);
                        break;
                    case 217: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorActionExtraLimitedActionsStrideNumericUpDown, "No bonus stride action value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorActionExtraLimitedActionsStrideNumericUpDown, -36);
                        break;
                    case 218: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorActionExtraLimitedActionsReloadNumericUpDown, "No bonus reload action value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorActionExtraLimitedActionsReloadNumericUpDown, -36);
                        break;
                    case 219: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorActionExtraLimitedActionsLongReloadNumericUpDown, "No bonus long reload action value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorActionExtraLimitedActionsLongReloadNumericUpDown, -36);
                        break;
                    case 220: // Push
                        CalculatorAttackErrorProvider.SetError(CalculatorActionExtraLimitedActionsStrikeNumericUpDown, "No bonus draw strike value provided!");
                        CalculatorAttackErrorProvider.SetIconPadding(CalculatorActionExtraLimitedActionsStrikeNumericUpDown, -36);
                        break;
                }
            }
        }
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
            if (CalculatorDistanceEngagementRangeCheckBox.Checked && CalculatorDistanceEngagementRangeTextBox.Text.Length == 0)
            {
                ex.Data.Add(214, "Missing Data Exception: Missing engagement range");
            }
            if (CalculatorReachMovementSpeedCheckBoCalculatorx.Checked && CalculatorReachMovementSpeedTextBox.Text.Length == 0)
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
            if (currEncounterSettings.volley > currEncounterSettings.range && currEncounterSettings.seek_favorable_range)
            { // Throw bad data combination exception
                ex.Data.Add(41, "Bad Data Combination Exception: Dangerous combination");
            }
            if (currEncounterSettings.damage_dice.Count <= 0 || currEncounterSettings.damage_dice_DOT.Count <= 0)
            { //  Throw no damage dice exception
                ex.Data.Add(51, "Damage Count Exception: No damage dice");
            }
            if (currEncounterSettings.damage_dice.Count != currEncounterSettings.damage_dice_DOT.Count)
            { // Throw imbalanced damage die exception
                ex.Data.Add(52, "Damage Count Exception: Imbalanced damage/bleed dice");
            }

            // Throw exception on problems
            if (ex.Data.Count > 0)
                throw ex;
        }
        private EncounterSettings UpdateDamageSimulationVariables(EncounterSettings oldSettings)
        {
            EncounterSettings newSettings = new()
            {
                number_of_encounters = int.Parse(CalculatorEncounterNumberOfEncountersTextBox.Text),
                rounds_per_encounter = int.Parse(CalculatorEncounterRoundsPerEncounterTextBox.Text),
                actions_per_round = new Actions(any: int.Parse(CalculatorActionActionsPerRoundTextBox.Text),
                                                strike: (int)CalculatorActionExtraLimitedActionsStrikeNumericUpDown.Value,
                                                reload: (int)CalculatorActionExtraLimitedActionsReloadNumericUpDown.Value,
                                                long_reload: (int)CalculatorActionExtraLimitedActionsLongReloadNumericUpDown.Value,
                                                draw: (int)CalculatorActionExtraLimitedActionsDrawNumericUpDown.Value,
                                                stride: (int)CalculatorActionExtraLimitedActionsStrideNumericUpDown.Value),
                bonus_to_hit = int.Parse(CalculatorAttackBonusToHitTextBox.Text),
                AC = int.Parse(CalculatorAttackACTextBox.Text),
                crit_threshhold = int.Parse(CalculatorAttackCriticalHitMinimumTextBox.Text),
                MAP_modifier = int.Parse(CalculatorAttackMAPModifierTextBox.Text),
                draw = int.Parse(CalculatorAmmunitionDrawLengthTextBox.Text),
                reload = int.Parse(CalculatorAmmunitionReloadTextBox.Text),
                // Magazine / Long Reload
                magazine_size = CalculatorAmmunitionMagazineSizeCheckBox.Checked
                                                ? int.Parse(CalculatorAmmunitionMagazineSizeTextBox.Text)
                                                : 0,
                // Magazine / Long Reload
                long_reload = CalculatorAmmunitionMagazineSizeCheckBox.Checked
                                                ? int.Parse(CalculatorAmmunitionLongReloadTextBox.Text)
                                                : 0,
                // Engagement Range
                engagement_range = CalculatorDistanceEngagementRangeCheckBox.Checked
                                                    ? int.Parse(CalculatorDistanceEngagementRangeTextBox.Text)
                                                    : 30,
                // Movement / Seek
                seek_favorable_range = CalculatorReachMovementSpeedCheckBoCalculatorx.Checked,
                move_speed = CalculatorReachMovementSpeedCheckBoCalculatorx.Checked
                                            ? int.Parse(CalculatorReachMovementSpeedTextBox.Text)
                                            : 25,
                // Range Increment
                range = CalculatorReachRangeIncrementCheckBox.Checked
                                        ? int.Parse(CalculatorReachRangeIncrementTextBox.Text)
                                        : 100,
                // Range Increment
                volley = (CalculatorReachVolleyIncrementCheckBox.Checked)
                                        ? int.Parse(CalculatorReachVolleyIncrementTextBox.Text)
                                        : 0,
                damage_dice = oldSettings.damage_dice,
                damage_dice_DOT = oldSettings.damage_dice_DOT
            };

            return newSettings;
        }

        private void DamageListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Filter normal digit entry out for TextBoxes.
        /// </summary>
        private void TextBox_KeyPressFilterToDigits(object sender, KeyPressEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                if (!e.KeyChar.Equals('+') && !e.KeyChar.Equals('-') || textBox?.Text.Count(x => (x.Equals('+') || x.Equals('-'))) >= 1)
                {
                    e.Handled = true;
                }
            }

            ClearErrorOnObject(textBox);
        }

        /// <summary>
        /// Filter non-digits and non-sign characters out of pasted entries using regex.
        /// </summary>
        private void TextBox_TextChangedFilterToDigitsAndSign(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;

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

        /// <summary>
        /// Filter digits out of pasted entries using regex.
        /// </summary>
        private void TextBox_TextChangedFilterToDigits(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // Strip characters out of pasted text
            textBox.Text = DigitsRegex().Replace(input: textBox.Text, replacement: "");

            ClearErrorOnObject(sender as Control);
        }

        private void ClearErrorOnObject(Control obj)
        {
            if (!CalculatorAttackErrorProvider.GetError(obj).Equals(string.Empty))
                CalculatorAttackErrorProvider.SetError(obj, string.Empty);
        }

        [GeneratedRegex("[^0-9-+]")]
        private static partial Regex DigitSignRegex();
        [GeneratedRegex("[^0-9]")]
        private static partial Regex DigitsRegex();

        // Windows API
        [DllImport("user32.dll")] static extern bool HideCaret(IntPtr hWnd);
        void TextBox_GotFocusDisableCarat(object sender, EventArgs e)
        {
            HideCaret((sender as TextBox).Handle);
        }
        
        // Check Boxes
        private void ReachRangeIncrementCheckBox_CheckedChanged(object sender, EventArgs e)
        { // Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { CalculatorReachRangeIncrementTextBox };
            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void ReachVolleyIncrementCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { CalculatorReachVolleyIncrementTextBox };
            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void DistanceEngagementRangeCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { CalculatorDistanceEngagementRangeTextBox };
            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void DistanceMovementSpeedCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { CalculatorReachMovementSpeedTextBox };
            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void DamageCriticalDieCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { CalculatorDamageCriticalDieCountTextBox,
                                                CalculatorDamageCriticalDieSizeTextBox,
                                                CalculatorDamageCriticalDieBonusTextBox};
            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void DamageBleedDieCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { CalculatorDamageBleedDieCountTextBox, 
                                                CalculatorDamageBleedDieSizeTextBox,
                                                CalculatorDamageBleedDieBonusTextBox};
            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void DamageCriticalBleedDieCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { CalculatorDamageCriticalBleedDieCountTextBox,
                                                CalculatorDamageCriticalBleedDieSizeTextBox,
                                                CalculatorDamageCriticalBleedDieBonusTextBox};

            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void AmmunitionMagazineSizeCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { CalculatorAmmunitionMagazineSizeTextBox,
                                                CalculatorAmmunitionLongReloadTextBox };

            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void CheckboxToggleTextbox(CheckBox checkBox, List<TextBox> textBoxes)
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

        // Damage Buttons
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
            if (currDamageDie.Item2.Item1 != 0 || currDamageDie.Item2.Item2 != 0 || currDamageDie.Item2.Item3 != 0)
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
            if (currBleedDie.Item1.Item1 != 0 || currBleedDie.Item1.Item2 != 0 || currBleedDie.Item1.Item3 != 0)
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
            if (currBleedDie.Item2.Item1 != 0 || currBleedDie.Item2.Item2 != 0 || currBleedDie.Item2.Item3 != 0)
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
            var values = ReadDamageDieValues();

            // Store references to each dice/bonus
            Tuple<Tuple<int, int, int>, Tuple<int, int, int>> currDamageDie = new(values.Item1, values.Item2);
            Tuple<Tuple<int, int, int>, Tuple<int, int, int>> currBleedDie = new(values.Item3, values.Item4);

            string entryString = CreateDamageListBoxString(currDamageDie, currBleedDie);

            // Add the new string to the list
            if (index == -1)
            {
                CalculatorDamageListBox.Items.Add(item: entryString); // Store the text entry
                currEncounterSettings.damage_dice.Add(item: currDamageDie); // Store the damage
                currEncounterSettings.damage_dice_DOT.Add(item: currBleedDie); // Store the bleed damage
            }
            else
            {
                CalculatorDamageListBox.Items.Insert(index: index, item: entryString); // Store the text entry
                currEncounterSettings.damage_dice.Insert(index: index, item: currDamageDie); // Store the damage
                currEncounterSettings.damage_dice_DOT.Insert(index: index, item: currBleedDie); // Store the bleed damage
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
                    Tuple<int, int, int>, Tuple<int, int, int>> ReadDamageDieValues()
        {
            // DIE COUNT
            // Count of the damage die
            int damageCount = (CalculatorDamageDieCountTextBox.Text.Length > 0)
                                        ? int.Parse(CalculatorDamageDieCountTextBox.Text)
                                        : 0;
            // Count of the critical damage die
            int damageCritCount = (CalculatorDamageCriticalDieCheckBox.Checked
                                    && CalculatorDamageCriticalDieCountTextBox.Text.Length > 0)
                                        ? int.Parse(CalculatorDamageCriticalDieCountTextBox.Text)
                                        : damageCount;

            // Count of the bleed die
            int bleedCount = (CalculatorDamageBleedDieCheckBox.Checked
                                && CalculatorDamageBleedDieCountTextBox.Text.Length > 0)
                                    ? int.Parse(CalculatorDamageBleedDieCountTextBox.Text)
                                    : 0;
            // Count of the critical bleed die
            int bleedCritCount = (CalculatorDamageCriticalBleedDieCheckBox.Checked
                                    && CalculatorDamageCriticalBleedDieCountTextBox.Text.Length > 0)
                                        ? int.Parse(CalculatorDamageCriticalBleedDieCountTextBox.Text)
                                        : bleedCount;

            // DIE SIZE
            // Size of the damage die
            int damageSides = (CalculatorDamageDieSizeTextBox.Text.Length > 0)
                                        ? int.Parse(CalculatorDamageDieSizeTextBox.Text)
                                        : 0;
            // Size of the critical damage die
            int damageCritSides = (CalculatorDamageCriticalDieCheckBox.Checked
                                    && CalculatorDamageCriticalDieSizeTextBox.Text.Length > 0)
                                        ? int.Parse(CalculatorDamageCriticalDieSizeTextBox.Text)
                                        : damageSides;
            // Size of the bleed die
            int bleedSides = (CalculatorDamageBleedDieCheckBox.Checked
                                && CalculatorDamageBleedDieSizeTextBox.Text.Length > 0)
                                    ? int.Parse(CalculatorDamageBleedDieSizeTextBox.Text)
                                    : 0;
            // Size of the critical bleed die
            int bleedCritSides = (CalculatorDamageCriticalBleedDieCheckBox.Checked
                                    && CalculatorDamageCriticalBleedDieSizeTextBox.Text.Length > 0)
                                        ? int.Parse(CalculatorDamageCriticalBleedDieSizeTextBox.Text)
                                        : bleedSides;

            // DIE BONUS
            // Bonus to the damage die
            int damageBonus = (CalculatorDamageDieBonusTextBox.Text.Length > 0)
                                        ? int.Parse(CalculatorDamageDieBonusTextBox.Text)
                                        : 0;
            // Bonus to the critical damage die
            int damageCritBonus = (CalculatorDamageCriticalDieCheckBox.Checked
                                    && CalculatorDamageCriticalDieBonusTextBox.Text.Length > 0)
                                        ? int.Parse(CalculatorDamageCriticalDieBonusTextBox.Text)
                                        : damageBonus;
            // Bonus to the bleed die
            int bleedBonus = (CalculatorDamageBleedDieCheckBox.Checked
                                && CalculatorDamageBleedDieBonusTextBox.Text.Length > 0)
                                    ? int.Parse(CalculatorDamageBleedDieBonusTextBox.Text)
                                    : 0;
            // Bonus to the critical bleed die
            int bleedCritBonus = (CalculatorDamageCriticalBleedDieCheckBox.Checked
                                    && CalculatorDamageCriticalBleedDieBonusTextBox.Text.Length > 0)
                                        ? int.Parse(CalculatorDamageCriticalBleedDieBonusTextBox.Text)
                                        : bleedBonus;

            return new(new(damageCount, damageSides, damageBonus), new(damageCritCount, damageCritSides, damageCritBonus), // Normal damage
                        new(bleedCount, bleedSides, bleedBonus), new(bleedCritCount, bleedCritSides, bleedCritBonus)); // Bleed damage
        }


        private void TextBox_LeaveClearLoneSymbol(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text.Length == 1 && (textBox.Text[0].Equals('+') || textBox.Text[0].Equals('-')))
                textBox.Text = string.Empty;
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

        private void HelpButton_MouseClick(object sender, MouseEventArgs e)
        {
            if (help_mode_enabled)
            { // Disable help mode
                help_mode_enabled = false;
                CalculatorHelpModeButton.Text = "Enable Help Mode";
                Cursor = Cursors.Default;
            }
            else
            { // Enable help mode
                help_mode_enabled = true;
                CalculatorHelpModeButton.Text = "Disable Help Mode";
                Cursor = Cursors.Help;
            }
        }

        private void Control_MouseHoverShowTooltip(object sender, EventArgs e)
        {
            if (help_mode_enabled)
            {
                CalculatorHelpToolTip.SetToolTip(sender as Control, label_hashes[sender.GetHashCode()]);
            }
        }

        
        public void SetProgressBar(Object sender, int progress)
        {
            CalculatorMiscStatisticsCalculateStatsProgressBars.Value = Math.Clamp(progress + 1, 0, CalculatorMiscStatisticsCalculateStatsProgressBars.Maximum);
            CalculatorMiscStatisticsCalculateStatsProgressBars.Value = progress;
        }
        private async void CalculateDamageStatsButton_MouseClick(object sender, MouseEventArgs e)
        {
            if (!HelperFunctions.IsControlDown())
            {
                // Update Data
                try
                { // Check for exception
                    CheckComputeSafety();

                    // Safe!
                    currEncounterSettings = UpdateDamageSimulationVariables(currEncounterSettings);

                    // Compute Damage Stats
                    Progress<int> progress = new();
                    progress.ProgressChanged += SetProgressBar;
                    Task<DamageStats> computeDamage = Task.Run(() => CalculateAverageDamage(number_of_encounters: currEncounterSettings.number_of_encounters,
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
                                                                damage_dice_DOT: currEncounterSettings.damage_dice_DOT,
                                                                progress: progress));
                    damageStats = await computeDamage;

                    // Update Graph
                    UpdateGraph();

                    // Compute 25th, 50th, and 75th Percentiles then update the GUI with them
                    UpdateStatisticsGUI(HelperFunctions.ComputePercentiles(damageStats.damageBins));

                    // Visually Update Graph
                    CalculatorDamageDistributionScottPlot.Render();
                }
                catch (Exception ex)
                { // Exception caught!
                    PushErrorMessages(ex);
                    return;
                }
            }
            else
            {
                if (damageStats.damageBins != null && damageStats.damageBins.Count > 0)
                    // Store damage bins to the clipboard
                    Clipboard.SetText(string.Join(",", damageStats.damageBins.OrderBy(x => x.Key).Select(x => x.Value)));
            }
        }

        enum Archetype
        {
            Rifle,
            Pistol,
            Shotgun,
            BattleRifle,
            HandCannon,
            Artillery
        }
        private void GenerateRandomWeaponStats(bool archetypal)
        {
            if (archetypal)
            {
                Archetype type = HelperFunctions.GetRandomEnum<Archetype>();
                
                //DataGen

                switch (type)
                {
                    case Archetype.Rifle:

                        break;
                }
            }
        }
    }
}