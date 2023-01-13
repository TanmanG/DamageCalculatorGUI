using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static DamageCalculatorGUI.DamageCalculator;
using static System.Net.Mime.MediaTypeNames;

namespace DamageCalculatorGUI
{
    public partial class Form1 : Form
    {
        // Damage Stats Struct
        public DamageStats damageStats = new();

        // Damage Computation Variables
        static int number_of_encounters;
        static int rounds_per_encounter;
        static int actions_per_round;
        static int reloadSize;
        static int reload;
        static int longReload;
        static int draw;
        static List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>>? damageDice; // List of <X, Y. Z> <M, N, O> where XDY+Z, on crit MDN+O
        static int bonusToHit; // Bonus added to-hit.
        static int AC; // AC to test against. Tie attack roll hits.
        static int critThreshhold; // Raw attack roll at and above this critically strike.
        static int MAPmodifier; // Modifier applied to MAP. By default 0 and thus -5, -10.
        static int engagementRange; // Range to start the combat encounter at.
        static int moveSpeed; // Distance covered by Stride.
        static bool seekFavorableRange; // Whether to Stride to an optimal firing position before firing.
        static int range; // Range increment. Past this and for every increment thereafter applies stacking -2 penalty to-hit.
        static int volley; // Minimum range increment. Firing within this applies a -2 penalty to-hit.
        static List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>>? damageDiceDOT; // DOT damage to apply on a hit/crit.

        /// <summary>
        /// Reset saved settings.
        /// </summary>
        private static void DefaultDamageStats()
        {
            number_of_encounters = 10000;
            rounds_per_encounter = 6;
            actions_per_round = 3;
            reloadSize = 0;
            reload = 1;
            longReload = 0;
            draw = 1;
            damageDice = new() { new(new(1, 8, 0), new(1, 12, 0)) };
            bonusToHit = 100;
            AC = 21;
            critThreshhold = 20;
            MAPmodifier = 0;
            engagementRange = 30;
            moveSpeed = 25;
            seekFavorableRange = false;
            range = 1000;
            volley = 0;
            damageDiceDOT = null;
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DefaultDamageStats();

            // Enable Carat Hiding
            EncounterStatisticsMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            EncounterStatisticsMedianTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            EncounterStatisticsUpperQuartileTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            EncounterStatisticsLowerQuartileBoxTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            MiscStatisticsRoundDamageMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            MiscStatisticsAttackDamageMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
            MiscStatisticsAccuracyMeanTextBox.GotFocus += new EventHandler(TextBox_GotFocusDisableCarat);
        }

        private void CalculateDamageStatsButton_Click(object sender, EventArgs e)
        {
            damageStats = CalculateAverageDamage(number_of_encounters: number_of_encounters,
                                                        rounds_per_encounter: rounds_per_encounter,
                                                        actions_per_round: actions_per_round,
                                                        reloadSize: reloadSize,
                                                        reload: reload,
                                                        longReload: longReload,
                                                        draw: draw,
                                                        damageDice: damageDice,
                                                        bonusToHit: bonusToHit,
                                                        AC: AC,
                                                        critThreshhold: critThreshhold,
                                                        MAPmodifier: MAPmodifier,
                                                        engagementRange: engagementRange,
                                                        moveSpeed: moveSpeed,
                                                        seekFavorableRange: seekFavorableRange,
                                                        range: range,
                                                        volley: volley,
                                                        damageDiceDOT: damageDiceDOT);

            Tuple<int, int, int> percentiles = HelperFunctions.ComputePercentiles(damageStats.damageBins);

            // Update Encounter Statistics
            EncounterStatisticsMeanTextBox.Text = Math.Round(damageStats.averageEncounterDamage, 2).ToString();
            EncounterStatisticsMedianTextBox.Text = percentiles.Item2.ToString();
            EncounterStatisticsUpperQuartileTextBox.Text = percentiles.Item3.ToString();
            EncounterStatisticsLowerQuartileBoxTextBox.Text = percentiles.Item1.ToString();


            // Update Misc Statistics
            MiscStatisticsRoundDamageMeanTextBox.Text = Math.Round(damageStats.averageRoundDamage, 2).ToString();
            MiscStatisticsAttackDamageMeanTextBox.Text = Math.Round(damageStats.averageHitDamage, 2).ToString();
            MiscStatisticsAccuracyMeanTextBox.Text = (Math.Round(damageStats.averageAccuracy, 4) * 100).ToString() + "%";

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
                if (textBox.Text.Length == 0)
                    break;
                // To-do: fix pasting this: +6bb26+-3hyh2-g=
                // Breaks the progam when pasted into CriticaL Min
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

            int currSelectStart = textBox.SelectionStart;

            // Strip characters out of pasted text
            textBox.Text = DigitsRegex().Replace(input: textBox.Text, replacement: "");
        }


        [System.Text.RegularExpressions.GeneratedRegex("[^0-9-+]")]
        private static partial System.Text.RegularExpressions.Regex DigitSignRegex();
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
            TextBox textBox = ReachRangeIncrementTextBox;

            if ((sender as CheckBox).Checked)
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
        private void ReachVolleyIncrementCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            TextBox textBox = ReachVolleyIncrementTextBox;

            if ((sender as CheckBox).Checked)
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
        private void DistanceEngagementRangeCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            TextBox textBox = DistanceEngagementRangeTextBox;

            if ((sender as CheckBox).Checked)
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
        private void DistanceMovementSpeedCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            TextBox textBox = DistanceMovementSpeedTextBox;

            if ((sender as CheckBox).Checked)
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
        private void DamageCriticalDieCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { DamageCriticalDieCountTextBox,
                                                DamageCriticalDieSizeTextBox,
                                                DamageCriticalDieBonusTextBox};

            foreach (TextBox textBox in textBoxes)
            {
                if ((sender as CheckBox).Checked)
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
        private void DamageBleedDieCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { DamageBleedDieCountTextBox, 
                                                DamageBleedDieSizeTextBox,
                                                DamageBleedDieBonusTextBox};

            foreach (TextBox textBox in textBoxes)
            {
                if ((sender as CheckBox).Checked)
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
        private void DamageCriticalBleedDieCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { DamageCriticalBleedDieCountTextBox,
                                                DamageCriticalBleedDieSizeTextBox,
                                                DamageCriticalBleedDieBonusTextBox};

            foreach (TextBox textBox in textBoxes)
            {
                if ((sender as CheckBox).Checked)
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
        private void AmmunitionMagazineSizeCheckBox_CheckedChanged(object sender, EventArgs e)
        {// Unfortunate semi-generic implementation of TextBox toggling CheckBox
            List<TextBox> textBoxes = new() { AmmunitionMagazineSizeTextBox,
                                                AmmunitionLongReloadTextBox };

            foreach (TextBox textBox in textBoxes)
            {
                if ((sender as CheckBox).Checked)
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
            // Store references to each dice/bonus
            var currDamageDie = damageDice[DamageListBox.SelectedIndex];
            var currBleedDie = damageDiceDOT[DamageListBox.SelectedIndex];

            // Save each dice to the GUI
            // Regular Damage Die
            DamageDieCountTextBox.Text = currDamageDie.Item1.Item1.ToString();
            DamageDieSizeTextBox.Text = currDamageDie.Item1.Item2.ToString();
            DamageDieBonusTextBox.Text = currDamageDie.Item1.Item3.ToString();

            // Critical Damage Die
            if (currDamageDie.Item2.Item1 == 0 && currDamageDie.Item2.Item2 == 0 && currDamageDie.Item2.Item3 == 0)
            { // Check if no crit shift
                DamageCriticalDieCheckBox.Checked = false;
                DamageCriticalDieCountTextBox.Text = string.Empty;
                DamageCriticalDieSizeTextBox.Text = string.Empty;
                DamageCriticalDieBonusTextBox.Text = string.Empty;
            }
            else
            {
                DamageCriticalDieCheckBox.Checked = true;
                DamageCriticalDieCountTextBox.Text = currDamageDie.Item2.Item1.ToString();
                DamageCriticalDieSizeTextBox.Text = currDamageDie.Item2.Item2.ToString();
                DamageCriticalDieBonusTextBox.Text = currDamageDie.Item2.Item3.ToString();
            }

            // Bleed Die
            if (currBleedDie.Item1.Item1 == 0 && currBleedDie.Item1.Item2 == 0 && currBleedDie.Item1.Item3 == 0)
            { // Check if no crit shift
                DamageCriticalBleedDieCheckBox.Checked = false;
                DamageCriticalBleedDieCountTextBox.Text = string.Empty;
                DamageCriticalBleedDieSizeTextBox.Text = string.Empty;
                DamageCriticalBleedDieBonusTextBox.Text = string.Empty;
            }
            else
            {
                DamageCriticalBleedDieCheckBox.Checked = true;
                DamageCriticalBleedDieCountTextBox.Text = currBleedDie.Item1.Item1.ToString();
                DamageCriticalBleedDieSizeTextBox.Text = currBleedDie.Item1.Item2.ToString();
                DamageCriticalBleedDieBonusTextBox.Text = currBleedDie.Item1.Item3.ToString();
            }

            // Critical Bleed Die
            if (currBleedDie.Item1.Item2 == 0 && currBleedDie.Item2.Item2 == 0 && currDamageDie.Item1.Item3 == 0)
            { // Check if no crit shift
                DamageCriticalBleedDieCheckBox.Checked = false;
                DamageCriticalBleedDieCountTextBox.Text = string.Empty;
                DamageCriticalBleedDieSizeTextBox.Text = string.Empty;
                DamageCriticalBleedDieBonusTextBox.Text = string.Empty;
            }
            else
            {
                DamageCriticalBleedDieCheckBox.Checked = true;
                DamageCriticalBleedDieCountTextBox.Text = currBleedDie.Item1.Item1.ToString();
                DamageCriticalBleedDieSizeTextBox.Text = currBleedDie.Item1.Item2.ToString();
                DamageCriticalBleedDieBonusTextBox.Text = currBleedDie.Item1.Item3.ToString();
            }
        }
        private void DamageDeleteButton_MouseClick(object sender, MouseEventArgs e)
        {
            DamageListBox.Items.RemoveAt(DamageListBox.SelectedIndex);
            damageDice.RemoveAt(DamageListBox.SelectedIndex);
            damageDiceDOT.RemoveAt(DamageListBox.SelectedIndex);
        }
        private void DamageAddButton_MouseClick(object sender, MouseEventArgs e)
        {
            // Save each dice from the GUI
            var values = ReadDamageDieValues();

            // Store references to each dice/bonus
            Tuple<Tuple<int, int, int>, Tuple<int, int, int>> currDamageDie = new(values.Item1, values.Item2);
            Tuple<Tuple<int, int, int>, Tuple<int, int, int>> currBleedDie = new(values.Item3, values.Item4);


            // To-do: Fix this to account for XDY+Z
            // Add a visual entry to the GUIc
            // Base Base Bools
            bool hasBase_baseBonus = currDamageDie.Item1.Item3 == 0;
            // Bleed Critical Bools
            bool critical_affects_baseSides = currDamageDie.Item2.Item1 != currDamageDie.Item1.Item1;
            bool critical_affects_baseSize = currDamageDie.Item2.Item2 != currDamageDie.Item1.Item2;
            bool critical_affects_baseBonus = currDamageDie.Item2.Item3 != currDamageDie.Item1.Item3;
            // Bleed Base Bools
            bool hasBase_bleedSides = currBleedDie.Item1.Item1 != 0;
            bool hasBase_bleedSize = currBleedDie.Item1.Item2 != 0;
            bool hasBase_bleedBonus = currBleedDie.Item1.Item3 != 0;
            // Bleed Critical Bools
            bool critical_affects_bleedSides = currBleedDie.Item2.Item1 != currBleedDie.Item1.Item1;
            bool critical_affects_bleedSize = currBleedDie.Item2.Item2 != currBleedDie.Item1.Item2;
            bool critical_affects_bleedBonus = currBleedDie.Item2.Item3 != currBleedDie.Item1.Item3;
            // Store signs of each bonus
            char[] signs = new char[4] { (currDamageDie.Item1.Item3 < 0) ? '-' : '+',
                                        (currDamageDie.Item2.Item3 < 0) ? '-' : '+',
                                        (currBleedDie.Item1.Item3 < 0) ? '-' : '+',
                                        (currBleedDie.Item2.Item3 < 0) ? '-' : '+' };

            string entryString = currDamageDie.Item1.Item1 + "D" + currDamageDie.Item1.Item2;

        }
        private void DamageSaveButton_MouseClick(object sender, MouseEventArgs e)
        {

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

    }
}