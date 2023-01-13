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

        private void Form1_Load(object sender, EventArgs e)
        {
            // Default Settings
            ResetSettings();
            ResetVisuals();

            // Enable Carat Hiding
            EncounterStatisticsMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            EncounterStatisticsMedianTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            EncounterStatisticsUpperQuartileTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            EncounterStatisticsLowerQuartileBoxTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            MiscStatisticsRoundDamageMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            MiscStatisticsAttackDamageMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            MiscStatisticsAccuracyMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
        }

        private void ResetSettings()
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
            seek_favorable_range = false;
            range = 999;
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
            DistanceEngagementRangeTextBox.Text = engagement_range.ToString();
            DistanceMovementSpeedTextBox.Text = move_speed.ToString();
            DistanceMovementSpeedCheckBox.Checked = seek_favorable_range;
            ReachRangeIncrementTextBox.Text = range.ToString();
            ReachVolleyIncrementTextBox.Text = volley.ToString();
            ActiveControl = null;
        }

        private void CalculateDamageStatsButton_Click(object sender, EventArgs e)
        {
            // To-do: Add help question-marks
            // Update Data
            UpdateDamageSimulationVariables();

            // Do Safety Check
            if (!SafeToCompute())
                return;

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
            
            // Compute 25th, 50th, and 75th Percentiles
            Tuple<int, int, int> percentiles = HelperFunctions.ComputePercentiles(damageStats.damageBins);

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
        
        private bool SafeToCompute()
        {
            return (damage_dice.Count > 0 && damage_dice.Count == damage_dice_DOT.Count && number_of_encounters > 0);
        }
        private void UpdateDamageSimulationVariables()
        {
            number_of_encounters = int.Parse(EncounterNumberOfEncountersTextBox.Text);
            rounds_per_encounter = int.Parse(EncounterRoundsPerEncounterTextBox.Text);
            actions_per_round = int.Parse(EncounterActionsPerRoundTextBox.Text);
            magazine_size = int.Parse(AmmunitionMagazineSizeTextBox.Text);
            reload = int.Parse(AmmunitionReloadTextBox.Text);
            long_reload = int.Parse(AmmunitionLongReloadTextBox.Text);
            draw = int.Parse(AmmunitionDrawLengthTextBox.Text);
            bonus_to_hit = int.Parse(AttackBonusToHitTextBox.Text);
            AC = int.Parse(AttackACTextBox.Text);
            crit_threshhold = int.Parse(AttackCriticalHitMinimumTextBox.Text);
            MAP_modifier = int.Parse(AttackMAPModifierTextBox.Text);
            engagement_range = int.Parse(DistanceEngagementRangeTextBox.Text);
            move_speed = int.Parse(DistanceMovementSpeedTextBox.Text);
            seek_favorable_range = DistanceMovementSpeedCheckBox.Checked;
            range = int.Parse(ReachRangeIncrementTextBox.Text);
            volley = int.Parse(ReachVolleyIncrementTextBox.Text);
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
        private string CreateDamageListBoxString(Tuple<Tuple<int, int, int>, Tuple<int, int, int>> currDamageDie, 
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
    }
}