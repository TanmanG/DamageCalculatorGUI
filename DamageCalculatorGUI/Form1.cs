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
        static List<Tuple<Tuple<int, int>, Tuple<int, int>>>? damageDice; // List of <X, Y> <M, N> where XDY, on crit MDN
        static List<Tuple<int, int>>? bonusDamage; // Tuple List of normal and crit strike bonus damage
        static int bonusToHit; // Bonus added to-hit.
        static int AC; // AC to test against. Tie attack roll hits.
        static int critThreshhold; // Raw attack roll at and above this critically strike.
        static int MAPmodifier; // Modifier applied to MAP. By default 0 and thus -5, -10.
        static int engagementRange; // Range to start the combat encounter at.
        static int moveSpeed; // Distance covered by Stride.
        static bool seekFavorableRange; // Whether to Stride to an optimal firing position before firing.
        static int range; // Range increment. Past this and for every increment thereafter applies stacking -2 penalty to-hit.
        static int volley; // Minimum range increment. Firing within this applies a -2 penalty to-hit.
        static List<Tuple<Tuple<int, int>, Tuple<int, int>>>? damageDiceDOT; // DOT damage to apply on a hit/crit.
        static List<Tuple<int, int>>? bonusDamageDOT = new();

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
            damageDice = new() { new(new(1, 8), new(1, 12)) };
            bonusDamage = null;
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
            bonusDamageDOT = null;
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
                                                        bonusDamage: bonusDamage,
                                                        bonusToHit: bonusToHit,
                                                        AC: AC,
                                                        critThreshhold: critThreshhold,
                                                        MAPmodifier: MAPmodifier,
                                                        engagementRange: engagementRange,
                                                        moveSpeed: moveSpeed,
                                                        seekFavorableRange: seekFavorableRange,
                                                        range: range,
                                                        volley: volley,
                                                        damageDiceDOT: damageDiceDOT,
                                                        bonusDamageDOT: bonusDamageDOT);

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
            textBox.Text = Regex.Replace(input: textBox.Text, pattern: "[^0-9]", replacement: "");
        }


        [System.Text.RegularExpressions.GeneratedRegex("[^0-9-+]")]
        private static partial System.Text.RegularExpressions.Regex DigitSignRegex();
        
        [DllImport("user32.dll")] static extern bool HideCaret(IntPtr hWnd);
        void TextBox_GotFocusDisableCarat(object sender, EventArgs e)
        {
            HideCaret((sender as TextBox).Handle);
        }
    }
}