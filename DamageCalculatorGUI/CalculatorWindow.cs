using ScottPlot.Drawing.Colormaps;
using ScottPlot;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static DamageCalculatorGUI.DamageCalculator;
using static System.Net.Mime.MediaTypeNames;

namespace DamageCalculatorGUI
{
    public partial class CalculatorWindow : Form
    {
        // Damage Stats Struct
        public DamageStats damageStats = new();

        // Help Variable
        static bool help_mode_enabled;
        Dictionary<int, string> label_hashes = new();

        // Damage Computation Variables
        static int number_of_encounters = 1;
        static int rounds_per_encounter = 0;
        static int actions_per_round = 0;
        static int magazine_size = 0;
        static int reload = 0;
        static int long_reload = 0;
        static int draw = 0;
        static List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> damage_dice = new (); // List of <X, Y. Z> <M, N, O> where XDY+Z, on crit MDN+O
        static int bonus_to_hit = 0; // Bonus added to-hit.
        static int AC = 0; // AC to test against. Tie attack roll hits.
        static int crit_threshhold = 0; // Raw attack roll at and above this critically strike.
        static int MAP_modifier = 0; // Modifier applied to MAP. By default 0 and thus -5, -10.
        static int engagement_range = 0; // Range to start the combat encounter at.
        static int move_speed = 0; // Distance covered by Stride.
        static bool seek_favorable_range = false; // Whether to Stride to an optimal firing position before firing.
        static int range = 0; // Range increment. Past this and for every increment thereafter applies stacking -2 penalty to-hit.
        static int volley = 0; // Minimum range increment. Firing within this applies a -2 penalty to-hit.
        static List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> damage_dice_DOT = new(); // DOT damage to apply on a hit/crit.

        public CalculatorWindow()
        {
            InitializeComponent();
        }
        private void CalculatorWindowLoad(object sender, EventArgs e)
        {
            // Default Settings
            ResetSettings();
            ResetVisuals();

            // Lock Window Dimensions
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // Set Version
            VersionLabel.Text = "PFK V" + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;

            // Enable Carat Hiding
            AddCaretHidingEvents();

            // Generate and store label hashes
            StoreLabelHashes();

            // Add a timer to the graph to render it regularly

        }

        private void UpdateGraph()
        {
            // To-do: Add buttons for adding additional plots to the graph. Maybe interactivity to view each weapon?
            // Clear the Plot
            DamageDistributionScottPlot.Plot.Clear();

            // Get the maximum value in the list
            int maxKey = damageStats.damageBins.Keys.Max();
            // Convert the damage bins into an array
            double[] graphBins = Enumerable.Range(0, maxKey + 1)
                .Select(i => damageStats.damageBins.ContainsKey(i) ? damageStats.damageBins[i] : 0)
                .Select(i => (double)i)
                .ToArray();
            // Generate and store the edges of each bin
            double[] binEdges = Enumerable.Range(0, graphBins.Length).Select(x => (double)x).ToArray();

            // Render the Plot
            ScottPlot.Plottable.BarPlot plot = DamageDistributionScottPlot.Plot.AddBar(values: graphBins, positions: binEdges);
            // Configure the Plot
            plot.BarWidth = 1;
            DamageDistributionScottPlot.Plot.YAxis.Label("Occurances");
            DamageDistributionScottPlot.Plot.XAxis.Label("Damage");
            DamageDistributionScottPlot.Plot.SetAxisLimits(yMin: 0, xMin: 0, xMax: maxKey, yMax: damageStats.damageBins.Values.Max() * 1.2);
            DamageDistributionScottPlot.Plot.SetOuterViewLimits(yMin: 0, xMin: 0, xMax: maxKey, yMax: damageStats.damageBins.Values.Max() * 1.2);
            DamageDistributionScottPlot.Plot.Legend(location: ScottPlot.Alignment.UpperLeft);
        }
        private void AddCaretHidingEvents()
        {
            EncounterStatisticsMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            EncounterStatisticsMedianTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            EncounterStatisticsUpperQuartileTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            EncounterStatisticsLowerQuartileBoxTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            MiscStatisticsRoundDamageMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            MiscStatisticsAttackDamageMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            MiscStatisticsAccuracyMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
        }
        private void StoreLabelHashes()
        {
            // Attack
            label_hashes.Add(AttackBonusToHitLabel.GetHashCode(), "The total bonus to-hit. Around ~12 for a 4th level Gunslinger.");
            label_hashes.Add(AttackCriticalHitMinimumLabel.GetHashCode(), "Minimum die roll to-hit in order to critically strike. Only way to crit other than getting 10 over the AC.");
            label_hashes.Add(AttackACLabel.GetHashCode(), "Armor Class of the target to be tested against. Typically ~21 for a 4th level enemy.");
            label_hashes.Add(AttackMAPModifierLabel.GetHashCode(), "Modifier to the Multiple-Attack-Penalty. Typically 0 unless you are using an Agile weapon or have Agile Grace, in which this is equal to -1 and -2 respectively.");
            // Ammunition
            label_hashes.Add(AmmunitionReloadLabel.GetHashCode(), "The number of Interact actions required to re-chamber a weapon after firing. I.e. a 3 action round would compose of Strike, Reload 1, Strike or Strike, Reload 2.");
            label_hashes.Add(AmmunitionMagazineSizeLabel.GetHashCode(), "The number of Strike actions that can be done before requiring a Long Reload.");
            label_hashes.Add(AmmunitionLongReloadLabel.GetHashCode(), "The number of Interact actions required to replenish the weapon's Magazine Size to max. Includes one complementary Reload.");
            label_hashes.Add(AmmunitionDrawLengthLabel.GetHashCode(), "The number of Interact actions required to draw the weapon. I.e. a 3 action round would compose of Draw, Strike, Reload 1.");
            // Damage
            label_hashes.Add(DamageDieSizeLabel.GetHashCode(), "The base damage of this weapon on hit. Represented by [X]d[Y]+[Z], such that a Y sided die will be rolled X times with a flat Z added (or subtracted if negative).");
            label_hashes.Add(DamageCriticalDieLabel.GetHashCode(), "The upgraded die quantity, size, and/or flat bonus from Critical Strikes. Represented in vanilla Pathfinder 2E as Brutal Critical.");
            label_hashes.Add(DamageBleedDieLabel.GetHashCode(), "The amount of Persistent Damage dealt on-hit by this weapon. Each applied instance of this is checked against a flat DC15 save every round to simulate a save.");
            label_hashes.Add(DamageCriticalBleedDieLabel.GetHashCode(), "The upgraded die quantity, size, and/or flat bonus to Persistent damage when Critically Striking.");
            // Reach
            label_hashes.Add(ReachRangeIncrementLabel.GetHashCode(), "The Range Increment of the weapon. Attacks attacks against targets beyond the range increment take a stacking -2 penalty to-hit for each increment away. I.e. Range Increment of 30ft will take a -4 at 70ft.");
            label_hashes.Add(ReachVolleyIncrementLabel.GetHashCode(), "The Volley Increment of the weapon. Attacks made within this distance suffer a flat -2 to-hit.");
            // Encounter
            label_hashes.Add(EncounterNumberOfEncountersLabel.GetHashCode(), "How many combat encounters to simulate. Default is around 100,000.");
            label_hashes.Add(EncounterRoundsPerEncounterLabel.GetHashCode(), "How many rounds to simulate per encounter. Higher simulates more drawn out encounters, shorter simulates more brief encounters.");
            label_hashes.Add(EncounterActionsPerRoundLabel.GetHashCode(), "How many actions granted each round. Typically 3 unless affected by an effect such as Haste.");
            // Distance
            label_hashes.Add(DistanceEngagementRangeLabel.GetHashCode(), "Distance to begin the encounter at. Defaults to 30 if unchecked.");
            label_hashes.Add(DistanceMovementSpeedLabel.GetHashCode(), "Amount of distance covered with one Stride action. The simulated player will Stride into the ideal (within range and out of volley) firing position before firing.");
            // Encounter Damage Statistics
            label_hashes.Add(EncounterStatisticsMeanLabel.GetHashCode(), "The 'true average' damage across all encounters. I.e. The sum of all encounters divded by the number of encounters.");
            label_hashes.Add(EncounterStatisticsUpperQuartileLabel.GetHashCode(), "The 75th percentile damage across all encounters. I.e. The average high-end/lucky damage performance of the weapon.");
            label_hashes.Add(EncounterStatisticsMedianLabel.GetHashCode(), "The 50th percentile damage across all encounters. I.e. The average center-most value and generally typical performance of the weapon.");
            label_hashes.Add(EncounterStatisticsLowerQuartileLabel.GetHashCode(), "The 25th percentile damage across all encounters. I.e. The aveerage lower-end/unlucky damage performance of the weapon.");
            // Misc Statistics
            label_hashes.Add(MiscStatisticsRoundDamageMeanLabel.GetHashCode(), "The 'true average' round damage for each round of combat. Best captures rapid-fire weapons.");
            label_hashes.Add(MiscStatisticsAttackDamageMeanLabel.GetHashCode(), "The 'true average' attack damage for each attack landed. Best captures the typical per-hit performance of weapon.");
            label_hashes.Add(MiscStatisticsAccuracyMeanLabel.GetHashCode(), "The average accuracy of the weapon. I.e. What percentage of attacks hit.");
        }
        private static void ResetSettings()
        {
            // Reset Settings
            number_of_encounters = 10000;
            rounds_per_encounter = 6;
            actions_per_round = 3;
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
        private void ResetVisuals()
        {
            // Reset Visuals
            EncounterNumberOfEncountersTextBox.Text = number_of_encounters.ToString();
            EncounterRoundsPerEncounterTextBox.Text = rounds_per_encounter.ToString();
            EncounterActionsPerRoundTextBox.Text = actions_per_round.ToString();
            AmmunitionReloadTextBox.Text = reload.ToString();
            AmmunitionLongReloadTextBox.Text = long_reload.ToString();
            AmmunitionMagazineSizeTextBox.Text = magazine_size.ToString();
            AmmunitionDrawLengthTextBox.Text = draw.ToString();
            DamageListBox.Items.Clear();
            for (int dieIndex = 0; dieIndex < damage_dice.Count; dieIndex++)
                DamageListBox.Items.Add(CreateDamageListBoxString(damage_dice[dieIndex], damage_dice_DOT[dieIndex]));
            AttackBonusToHitTextBox.Text = bonus_to_hit.ToString();
            AttackACTextBox.Text = AC.ToString();
            AttackCriticalHitMinimumTextBox.Text = crit_threshhold.ToString();
            AttackMAPModifierTextBox.Text = MAP_modifier.ToString();
            DistanceEngagementRangeCheckBox.Checked = true;
            DistanceEngagementRangeTextBox.Text = engagement_range.ToString();
            DistanceMovementSpeedCheckBox.Checked = seek_favorable_range;
            DistanceMovementSpeedTextBox.Text = move_speed.ToString();
            ReachRangeIncrementCheckBox.Checked = true;
            ReachRangeIncrementTextBox.Text = range.ToString();
            ReachVolleyIncrementCheckBox.Checked = true;
            ReachVolleyIncrementTextBox.Text = volley.ToString();
            ActiveControl = null;
        }

        private void CalculateDamageStatsButton_Click(object sender, EventArgs e)
        {
            // Update Data
            try
            { // Check for exception
                CheckComputeSafety();
                
                // Safe!
            } 
            catch (Exception ex)
            { // Exception caught!
                PushErrorMessages(ex);
                return;
            }

            UpdateDamageSimulationVariables();

            // Compute Damage Stats
            damageStats = CalculateAverageDamage(number_of_encounters: number_of_encounters,
                                                        rounds_per_encounter: rounds_per_encounter,
                                                        actions_per_round: actions_per_round,
                                                        reloadSize: magazine_size,
                                                        reload: reload,
                                                        longReload: long_reload,
                                                        draw: draw,
                                                        damageDice: damage_dice,
                                                        bonusToHit: bonus_to_hit,
                                                        AC: AC,
                                                        critThreshhold: crit_threshhold,
                                                        MAPmodifier: MAP_modifier,
                                                        engagementRange: engagement_range,
                                                        moveSpeed: move_speed,
                                                        seekFavorableRange: seek_favorable_range,
                                                        range: range,
                                                        volley: volley,
                                                        damageDiceDOT: damage_dice_DOT);

            // Update Graph
            UpdateGraph();

            // Compute 25th, 50th, and 75th Percentiles then update the GUI with them
            UpdateStatisticsGUI(HelperFunctions.ComputePercentiles(damageStats.damageBins));

            // Visually Update Graph
            DamageDistributionScottPlot.Render();
        }
        
        private void UpdateStatisticsGUI(Tuple<int, int, int> percentiles)
        {
            // Update Encounter Statistics GUI
            EncounterStatisticsMeanTextBox.Text = Math.Round(damageStats.average_encounter_damage, 2).ToString();
            EncounterStatisticsMedianTextBox.Text = percentiles.Item2.ToString();
            EncounterStatisticsUpperQuartileTextBox.Text = percentiles.Item3.ToString();
            EncounterStatisticsLowerQuartileBoxTextBox.Text = percentiles.Item1.ToString();


            // Update Misc Statistics GUI
            MiscStatisticsRoundDamageMeanTextBox.Text = Math.Round(damageStats.average_round_damage, 2).ToString();
            MiscStatisticsAttackDamageMeanTextBox.Text = Math.Round(damageStats.average_hit_damage, 2).ToString();
            MiscStatisticsAccuracyMeanTextBox.Text = (Math.Round(damageStats.average_accuracy * 100, 2)).ToString() + "%";
        }
        private void PushErrorMessages(Exception ex)
        {
            foreach (int errorCode in ex.Data.Keys)
            {
                switch (errorCode)
                {
                    case 41: // Push
                        AttackErrorProvider.SetError(ReachVolleyIncrementTextBox, "Volley longer Range will have unintended implications with movement!");
                        AttackErrorProvider.SetIconPadding(ReachVolleyIncrementTextBox, -18);
                        break;
                    case 51: // Push
                        AttackErrorProvider.SetError(DamageListBox, "No damage dice detected!");
                        AttackErrorProvider.SetIconPadding(DamageListBox, -18);
                        break;
                    case 52: // Fix don't push
                        // N/A
                        break;
                    case 101: // Push
                        AttackErrorProvider.SetError(EncounterNumberOfEncountersTextBox, "Number of encounters must be greater than 0!");
                        AttackErrorProvider.SetIconPadding(EncounterNumberOfEncountersTextBox, -18);
                        break;
                    case 102: // Push
                        AttackErrorProvider.SetError(DistanceMovementSpeedTextBox, "Movement speed must be greater than 0!");
                        AttackErrorProvider.SetIconPadding(DistanceMovementSpeedTextBox, -18);
                        break;
                    case 103: // Push
                        AttackErrorProvider.SetError(EncounterRoundsPerEncounterTextBox, "Rounds per encounter must be greater than 0!");
                        AttackErrorProvider.SetIconPadding(EncounterRoundsPerEncounterTextBox, -18);
                        break;
                    case 104: // Push
                        AttackErrorProvider.SetError(EncounterActionsPerRoundTextBox, "Actions per round must be positive!");
                        AttackErrorProvider.SetIconPadding(EncounterActionsPerRoundTextBox, -18);
                        break;
                    case 105: // Push
                        AttackErrorProvider.SetError(ReachRangeIncrementTextBox, "Range increment must be positive!");
                        AttackErrorProvider.SetIconPadding(ReachRangeIncrementTextBox, -18);
                        break;
                    case 106: // Push
                        AttackErrorProvider.SetError(DistanceEngagementRangeTextBox, "Encounter distance must be positive!");
                        AttackErrorProvider.SetIconPadding(EncounterActionsPerRoundTextBox, -18);
                        break;
                    case 201: // Push
                        AttackErrorProvider.SetError(AttackBonusToHitTextBox, "No bonus to-hit provided!");
                        AttackErrorProvider.SetIconPadding(AttackBonusToHitTextBox, -18);
                        break;
                    case 202: // Push
                        AttackErrorProvider.SetError(AttackCriticalHitMinimumTextBox, "No crit at/below provided!");
                        AttackErrorProvider.SetIconPadding(AttackCriticalHitMinimumTextBox, -18);
                        break;
                    case 203: // Push
                        AttackErrorProvider.SetError(AttackACTextBox, "No AC provided!");
                        AttackErrorProvider.SetIconPadding(AttackACTextBox, -18);
                        break;
                    case 204: // Push
                        AttackErrorProvider.SetError(AttackMAPModifierTextBox, "No MAP provided!");
                        AttackErrorProvider.SetIconPadding(AttackMAPModifierTextBox, -18);
                        break;
                    case 205: // Push
                        AttackErrorProvider.SetError(AmmunitionReloadTextBox, "No reload provided!");
                        AttackErrorProvider.SetIconPadding(AmmunitionReloadTextBox, -18);
                        break;
                    case 206: // Push
                        AttackErrorProvider.SetError(AmmunitionMagazineSizeTextBox, "No magazine size provided!");
                        AttackErrorProvider.SetIconPadding(AmmunitionMagazineSizeTextBox, -18);
                        break;
                    case 207: // Push
                        AttackErrorProvider.SetError(AmmunitionLongReloadTextBox, "No long reload provided!");
                        AttackErrorProvider.SetIconPadding(AmmunitionLongReloadTextBox, -18);
                        break;
                    case 208: // Push
                        AttackErrorProvider.SetError(AmmunitionDrawLengthTextBox, "No draw length provided!");
                        AttackErrorProvider.SetIconPadding(AmmunitionDrawLengthTextBox, -18);
                        break;
                    case 209: // Push
                        AttackErrorProvider.SetError(ReachRangeIncrementTextBox, "No range increment provided!");
                        AttackErrorProvider.SetIconPadding(ReachRangeIncrementTextBox, -18);
                        break;
                    case 210: // Push
                        AttackErrorProvider.SetError(ReachVolleyIncrementTextBox, "No volley increment provided!");
                        AttackErrorProvider.SetIconPadding(ReachVolleyIncrementTextBox, -18);
                        break;
                    case 211: // Push
                        AttackErrorProvider.SetError(EncounterNumberOfEncountersTextBox, "Number of encounters not provided!");
                        AttackErrorProvider.SetIconPadding(EncounterNumberOfEncountersTextBox, -18);
                        break;
                    case 212: // Push
                        AttackErrorProvider.SetError(EncounterRoundsPerEncounterTextBox, "No rounds per encounter provided!");
                        AttackErrorProvider.SetIconPadding(EncounterRoundsPerEncounterTextBox, -18);
                        break;
                    case 213: // Push
                        AttackErrorProvider.SetError(EncounterActionsPerRoundTextBox, "No actions per round provided!");
                        AttackErrorProvider.SetIconPadding(EncounterActionsPerRoundTextBox, -18);
                        break;
                    case 214: // Push
                        AttackErrorProvider.SetError(DistanceEngagementRangeTextBox, "No engagement range provided!");
                        AttackErrorProvider.SetIconPadding(DistanceEngagementRangeTextBox, -18);
                        break;
                    case 215: // Push
                        AttackErrorProvider.SetError(DistanceMovementSpeedTextBox, "No movement speed provided!");
                        AttackErrorProvider.SetIconPadding(DistanceMovementSpeedTextBox, -18);
                        break;
                }
            }
        }
        private void CheckComputeSafety()
        {
            Exception ex = new();

            // Empty Data Exceptions
            // Attack Category
            if (AttackBonusToHitTextBox.Text.Length == 0)
            {
                ex.Data.Add(201, "Missing Data Exception: Missing bonus to hit");
            }
            if (AttackCriticalHitMinimumTextBox.Text.Length == 0)
            {
                ex.Data.Add(202, "Missing Data Exception: Missing crit threshhold");
            }
            if (AttackACTextBox.Text.Length == 0)
            {
                ex.Data.Add(203, "Missing Data Exception: Missing AC");
            }
            if (AttackMAPModifierTextBox.Text.Length == 0)
            {
                ex.Data.Add(204, "Missing Data Exception: Missing MAP modifier");
            }

            // Ammunition Category
            if (AmmunitionReloadTextBox.Text.Length == 0)
            {
                ex.Data.Add(205, "Missing Data Exception: Missing reload");
            }
            if (AmmunitionMagazineSizeCheckBox.Checked && AmmunitionMagazineSizeTextBox.Text.Length == 0)
            {
                ex.Data.Add(206, "Missing Data Exception: Missing magazine size");
            }
            if (AmmunitionMagazineSizeCheckBox.Checked && AmmunitionDrawLengthTextBox.Text.Length == 0)
            {
                ex.Data.Add(207, "Missing Data Exception: Missing long reload");
            }
            if (AmmunitionDrawLengthTextBox.Text.Length == 0)
            {
                ex.Data.Add(208, "Missing Data Exception: Missing draw length");
            }

            // Reach
            if (ReachRangeIncrementCheckBox.Checked && ReachRangeIncrementTextBox.Text.Length == 0)
            {
                ex.Data.Add(209, "Missing Data Exception: Missing range increment");
            }
            if (ReachVolleyIncrementCheckBox.Checked && ReachVolleyIncrementTextBox.Text.Length == 0)
            {
                ex.Data.Add(210, "Missing Data Exception: Missing volley increment");
            }

            // Encounter
            if (EncounterNumberOfEncountersTextBox.Text.Length == 0)
            {
                ex.Data.Add(211, "Missing Data Exception: Missing number of encounters");
            }
            if (EncounterRoundsPerEncounterTextBox.Text.Length == 0)
            {
                ex.Data.Add(212, "Missing Data Exception: Missing rounds per encounter");
            }
            if (EncounterActionsPerRoundTextBox.Text.Length == 0)
            {
                ex.Data.Add(213, "Missing Data Exception: Missing actions per round");
            }

            // Distance
            if (DistanceEngagementRangeCheckBox.Checked && DistanceEngagementRangeTextBox.Text.Length == 0)
            {
                ex.Data.Add(214, "Missing Data Exception: Missing engagement range");
            }
            if (DistanceMovementSpeedCheckBox.Checked && DistanceMovementSpeedTextBox.Text.Length == 0)
            {
                ex.Data.Add(215, "Missing Data Exception: Missing movement speed");
            }

            // Bad Data Exceptions
            if (number_of_encounters <= 0)
            { // Throw bad encounter data exception
                ex.Data.Add(101, "Bad Data Exception: Zero or negative encounter count");
            }
            if (move_speed <= 0)
            { // Throw bad movement speed data exception
                ex.Data.Add(102, "Bad Data Exception: Zero or negative movement speed");
            }
            if (rounds_per_encounter < 0)
            { // Throw bad rounds/encounter data exception
                ex.Data.Add(103, "Bad Data Exception: Negative rounds per encounter");
            }
            if (actions_per_round < 0)
            { // Throw bad actions/round data exception
                ex.Data.Add(104, "Bad Data Exception: Negative actions per round");
            }
            if (range < 0)
            { // Throw bad range data exception
                ex.Data.Add(105, "Bad Data Exception: Negative range");
            }
            if (engagement_range < 0)
            { // Throw bad encounter range data exception
                ex.Data.Add(106, "Bad Data Exception: Negative encounter range");
            }
            if (volley > range && seek_favorable_range)
            { // Throw bad data combination exception
                ex.Data.Add(41, "Bad Data Combination Exception: Dangerous combination");
            }
            if (damage_dice.Count <= 0 || damage_dice_DOT.Count <= 0)
            { //  Throw no damage dice exception
                ex.Data.Add(51, "Damage Count Exception: No damage dice");
            }
            if (damage_dice.Count != damage_dice_DOT.Count)
            { // Throw imbalanced damage die exception
                ex.Data.Add(52, "Damage Count Exception: Imbalanced damage/bleed dice");
            }

            // Throw exception on problems
            if (ex.Data.Count > 0)
                throw ex;
        }
        private void UpdateDamageSimulationVariables()
        {
            number_of_encounters = int.Parse(EncounterNumberOfEncountersTextBox.Text);
            rounds_per_encounter = int.Parse(EncounterRoundsPerEncounterTextBox.Text);
            actions_per_round = int.Parse(EncounterActionsPerRoundTextBox.Text);
            reload = int.Parse(AmmunitionReloadTextBox.Text);

            // Magazine / Long Reload
            if (AmmunitionMagazineSizeCheckBox.Checked)
            {
                magazine_size = int.Parse(AmmunitionMagazineSizeTextBox.Text);
                long_reload = int.Parse(AmmunitionLongReloadTextBox.Text);
            }
            else
            {
                magazine_size = 0;
                long_reload = 0;
            }

            draw = int.Parse(AmmunitionDrawLengthTextBox.Text);
            bonus_to_hit = int.Parse(AttackBonusToHitTextBox.Text);
            AC = int.Parse(AttackACTextBox.Text);
            crit_threshhold = int.Parse(AttackCriticalHitMinimumTextBox.Text);
            MAP_modifier = int.Parse(AttackMAPModifierTextBox.Text);
            
            // Engagement Range
            if (DistanceEngagementRangeCheckBox.Checked)
                engagement_range = int.Parse(DistanceEngagementRangeTextBox.Text);
            else
                engagement_range = 30;

            // Movement / Seek
            seek_favorable_range = DistanceMovementSpeedCheckBox.Checked;
            if (DistanceMovementSpeedCheckBox.Checked)
            {
                move_speed = int.Parse(DistanceMovementSpeedTextBox.Text);
            }
            else
            {
                move_speed = 25;
            }

            // Range Increment
            if (ReachRangeIncrementCheckBox.Checked)
                range = int.Parse(ReachRangeIncrementTextBox.Text);
            else
                range = 100;

            // Range Increment
            if (ReachVolleyIncrementCheckBox.Checked)
                volley = int.Parse(ReachVolleyIncrementTextBox.Text);
            else
                volley = 0;

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
            if (!AttackErrorProvider.GetError(obj).Equals(string.Empty))
                AttackErrorProvider.SetError(obj, string.Empty);
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
            List<TextBox> textBoxes = new() { ReachRangeIncrementTextBox };
            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void ReachVolleyIncrementCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { ReachVolleyIncrementTextBox };
            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void DistanceEngagementRangeCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { DistanceEngagementRangeTextBox };
            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void DistanceMovementSpeedCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { DistanceMovementSpeedTextBox };
            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void DamageCriticalDieCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { DamageCriticalDieCountTextBox,
                                                DamageCriticalDieSizeTextBox,
                                                DamageCriticalDieBonusTextBox};
            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void DamageBleedDieCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { DamageBleedDieCountTextBox, 
                                                DamageBleedDieSizeTextBox,
                                                DamageBleedDieBonusTextBox};
            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void DamageCriticalBleedDieCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { DamageCriticalBleedDieCountTextBox,
                                                DamageCriticalBleedDieSizeTextBox,
                                                DamageCriticalBleedDieBonusTextBox};

            CheckboxToggleTextbox(sender as CheckBox, textBoxes);
        }
        private void AmmunitionMagazineSizeCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { AmmunitionMagazineSizeTextBox,
                                                AmmunitionLongReloadTextBox };

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
            int index = DamageListBox.SelectedIndex;
            if (index != -1)
            {
                LoadDamageDice(index);
            }
        }
        private void DamageDeleteButton_MouseClick(object sender, MouseEventArgs e)
        {
            int index = DamageListBox.SelectedIndex;
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
            int index = DamageListBox.SelectedIndex;
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
            var currDamageDie = damage_dice[index];
            var currBleedDie = damage_dice_DOT[index];

            // Save each dice to the GUI
            // Regular Damage Die
            DamageDieCountTextBox.Text = currDamageDie.Item1.Item1.ToString();
            DamageDieSizeTextBox.Text = currDamageDie.Item1.Item2.ToString();
            DamageDieBonusTextBox.Text = currDamageDie.Item1.Item3.ToString();

            // Critical Damage Die
            if (currDamageDie.Item2.Item1 != 0 || currDamageDie.Item2.Item2 != 0 || currDamageDie.Item2.Item3 != 0)
            {
                DamageCriticalDieCheckBox.Checked = true;
                DamageCriticalDieCountTextBox.Text = (currDamageDie.Item2.Item1 != 0) // X != 0
                                                        ? currDamageDie.Item2.Item1.ToString()
                                                        : currDamageDie.Item1.Item1.ToString();
                DamageCriticalDieSizeTextBox.Text = (currDamageDie.Item2.Item2 != 0) // Y != 0
                                                        ? currDamageDie.Item2.Item2.ToString()
                                                        : currDamageDie.Item1.Item2.ToString();
                DamageCriticalDieBonusTextBox.Text = (currDamageDie.Item2.Item3 != 0) // Z != 0
                                                        ? currDamageDie.Item2.Item3.ToString()
                                                        : currDamageDie.Item1.Item3.ToString();
            }
            else
            {
                DamageCriticalDieCheckBox.Checked = false;
            }


            // Bleed Die
            if (currBleedDie.Item1.Item1 != 0 || currBleedDie.Item1.Item2 != 0 || currBleedDie.Item1.Item3 != 0)
            {
                DamageBleedDieCheckBox.Checked = true;
                DamageBleedDieCountTextBox.Text = (currBleedDie.Item1.Item1 != 0) // X != 0
                                                        ? currBleedDie.Item1.Item1.ToString()
                                                        : "0";
                DamageBleedDieSizeTextBox.Text = (currBleedDie.Item1.Item2 != 0) // Y != 0
                                                        ? currBleedDie.Item1.Item2.ToString()
                                                        : "0";
                DamageBleedDieBonusTextBox.Text = (currBleedDie.Item1.Item3 != 0) // Z != 0
                                                        ? currBleedDie.Item1.Item3.ToString()
                                                        : "0";
            }
            else
            {
                DamageBleedDieCheckBox.Checked = false;
            }

            // Critical Damage Die
            if (currBleedDie.Item2.Item1 != 0 || currBleedDie.Item2.Item2 != 0 || currBleedDie.Item2.Item3 != 0)
            {
                DamageCriticalBleedDieCheckBox.Checked = true;
                DamageCriticalBleedDieCountTextBox.Text = (currBleedDie.Item2.Item1 != 0) // X != 0
                                                        ? currBleedDie.Item2.Item1.ToString()
                                                        : currBleedDie.Item1.Item1.ToString();
                DamageCriticalBleedDieSizeTextBox.Text = (currBleedDie.Item2.Item2 != 0) // Y != 0
                                                        ? currBleedDie.Item2.Item2.ToString()
                                                        : currBleedDie.Item1.Item2.ToString();
                DamageCriticalBleedDieBonusTextBox.Text = (currBleedDie.Item2.Item3 != 0) // Z != 0
                                                        ? currBleedDie.Item2.Item3.ToString()
                                                        : currBleedDie.Item1.Item3.ToString();
            }
            else
            {
                DamageCriticalBleedDieCheckBox.Checked = false;
            }
        }
        private void DeleteDamageDice(int index)
        {
            damage_dice.RemoveAt(index);
            damage_dice_DOT.RemoveAt(index);
            DamageListBox.Items.RemoveAt(index);
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
                DamageListBox.Items.Add(item: entryString); // Store the text entry
                damage_dice.Add(item: currDamageDie); // Store the damage
                damage_dice_DOT.Add(item: currBleedDie); // Store the bleed damage
            }
            else
            {
                DamageListBox.Items.Insert(index: index, item: entryString); // Store the text entry
                damage_dice.Insert(index: index, item: currDamageDie); // Store the damage
                damage_dice_DOT.Insert(index: index, item: currBleedDie); // Store the bleed damage
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
            bool hasCriticalCount = currDamageDie.Item2.Item1 != 0;
            bool hasCriticalSides = currDamageDie.Item2.Item2 != 0;
            bool hasCriticalBonus = currDamageDie.Item2.Item3 != 0;
            // Bleed Base Bools
            bool hasBaseBleedCount = currBleedDie.Item1.Item1 != 0;
            bool hasBaseBleedSides = currBleedDie.Item1.Item2 != 0;
            bool hasBaseBleedBonus = currBleedDie.Item1.Item3 != 0;
            // Bleed Critical Bools
            bool hasCriticalBleedCount = currBleedDie.Item2.Item1 != 0;
            bool hasCriticalBleedSides = currBleedDie.Item2.Item2 != 0;
            bool hasCriticalBleedBonus = currBleedDie.Item2.Item3 != 0;
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
            int damageCount = (DamageDieCountTextBox.Text.Length > 0)
                                        ? int.Parse(DamageDieCountTextBox.Text)
                                        : 0;
            // Count of the critical damage die
            int damageCritCount = (DamageCriticalDieCheckBox.Checked
                                    && DamageCriticalDieCountTextBox.Text.Length > 0)
                                        ? int.Parse(DamageCriticalDieCountTextBox.Text)
                                        : 0;

            // Count of the bleed die
            int bleedCount = (DamageBleedDieCheckBox.Checked
                                && DamageBleedDieCountTextBox.Text.Length > 0)
                                    ? int.Parse(DamageBleedDieCountTextBox.Text)
                                    : 0;
            // Count of the critical bleed die
            int bleedCritCount = (DamageCriticalBleedDieCheckBox.Checked
                                    && DamageCriticalBleedDieCountTextBox.Text.Length > 0)
                                        ? int.Parse(DamageCriticalBleedDieCountTextBox.Text)
                                        : 0;

            // DIE SIZE
            // Size of the damage die
            int damageSides = (DamageDieSizeTextBox.Text.Length > 0)
                                        ? int.Parse(DamageDieSizeTextBox.Text)
                                        : 0;
            // Size of the critical damage die
            int damageCritSides = (DamageCriticalDieCheckBox.Checked
                                    && DamageCriticalDieSizeTextBox.Text.Length > 0)
                                        ? int.Parse(DamageCriticalDieSizeTextBox.Text)
                                        : 0;
            // Size of the bleed die
            int bleedSides = (DamageBleedDieCheckBox.Checked
                                && DamageBleedDieSizeTextBox.Text.Length > 0)
                                    ? int.Parse(DamageBleedDieSizeTextBox.Text)
                                    : 0;
            // Size of the critical bleed die
            int bleedCritSides = (DamageCriticalBleedDieCheckBox.Checked
                                    && DamageCriticalBleedDieSizeTextBox.Text.Length > 0)
                                        ? int.Parse(DamageCriticalBleedDieSizeTextBox.Text)
                                        : 0;

            // DIE BONUS
            // Bonus to the damage die
            int damageBonus = (DamageDieBonusTextBox.Text.Length > 0)
                                        ? int.Parse(DamageDieBonusTextBox.Text)
                                        : 0;
            // Bonus to the critical damage die
            int damageCritBonus = (DamageCriticalDieCheckBox.Checked
                                    && DamageCriticalDieBonusTextBox.Text.Length > 0)
                                        ? int.Parse(DamageCriticalDieBonusTextBox.Text)
                                        : 0;
            // Bonus to the bleed die
            int bleedBonus = (DamageBleedDieCheckBox.Checked
                                && DamageBleedDieBonusTextBox.Text.Length > 0)
                                    ? int.Parse(DamageBleedDieBonusTextBox.Text)
                                    : 0;
            // Bonus to the critical bleed die
            int bleedCritBonus = (DamageCriticalBleedDieCheckBox.Checked
                                    && DamageCriticalBleedDieBonusTextBox.Text.Length > 0)
                                        ? int.Parse(DamageCriticalBleedDieBonusTextBox.Text)
                                        : 0;

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
            if (DamageListBox.SelectedIndex != -1)
                DamageEditButton_MouseClick(sender: sender, e: e);
        }

        private void DefaultSettingsButton_Click(object sender, EventArgs e)
        {
            ResetSettings();
            ResetVisuals();
        }

        private void HelpButton_MouseClick(object sender, MouseEventArgs e)
        {
            if (help_mode_enabled)
            { // Disable help mode
                help_mode_enabled = false;
                HelpModeButton.Text = "Enable Help Mode";
                Cursor = Cursors.Default;
            }
            else
            { // Enable help mode
                help_mode_enabled = true;
                HelpModeButton.Text = "Disable Help Mode";
                Cursor = Cursors.Help;
            }
        }

        private void AttackBonusToHitLabel_MouseHover(object sender, EventArgs e)
        {
            if (help_mode_enabled)
            {
                HelpToolTip.SetToolTip(sender as Control, label_hashes[sender.GetHashCode()]);
            }
        }

    }
}