using static DamageCalculatorGUI.DamageCalculator;

namespace DamageCalculatorGUI
{
    public partial class Form1 : Form
    {
        // Damage Stats Struct
        public DamageStats damageStats = new();

        // Damage Computation Variables
        int number_of_encounters;
        int rounds_per_encounter;
        int actions_per_round;
        int reloadSize;
        int reload;
        int longReload;
        int draw;
        List<Tuple<Tuple<int, int>, Tuple<int, int>>>? damageDice; // List of <X, Y> <M, N> where XDY, on crit MDN
        List<Tuple<int, int>>? bonusDamage; // Tuple List of normal and crit strike bonus damage
        int bonusToHit; // Bonus added to-hit.
        int AC; // AC to test against. Tie attack roll hits.
        int critThreshhold; // Raw attack roll at and above this critically strike.
        int MAPmodifier; // Modifier applied to MAP. By default 0 and thus -5, -10.
        int engagementRange; // Range to start the combat encounter at.
        int moveSpeed; // Distance covered by Stride.
        bool seekFavorableRange; // Whether to Stride to an optimal firing position before firing.
        int range; // Range increment. Past this and for every increment thereafter applies stacking -2 penalty to-hit.
        int volley; // Minimum range increment. Firing within this applies a -2 penalty to-hit.
        List<Tuple<Tuple<int, int>, Tuple<int, int>>>? damageDiceDOT; // DOT damage to apply on a hit/crit.
        List<Tuple<int, int>>? bonusDamageDOT;

        public Form1()
        {
            InitializeComponent();
        }

        private void CalculateDamageStatsButton_Click(object sender, EventArgs e)
        {
            damageStats = DamageCalculator.CalculateAverageDamage(number_of_encounters: 100,
                                                        rounds_per_encounter: 6,
                                                        actions_per_round: 3,
                                                        reload: 1,
                                                        draw: 1,
                                                        damageDice: new()
                                                        { // List of damage dice
                                                            new( // Damage Dice Pair
                                                                new(1, 8), // Normal Damage
                                                                new(1, 12)  // Crit Damage
                                                                ),
                                                        },
                                                        bonusToHit: 12);

            AverageDamageEncounterTextbox.Text = Math.Round(damageStats.averageEncounterDamage, 2).ToString();
            AverageDamageRoundTextbox.Text = Math.Round(damageStats.averageRoundDamage, 2).ToString();
            AverageDamageAttackTextbox.Text = Math.Round(damageStats.averageHitDamage, 2).ToString();
            AverageAccuracyTextbox.Text = (Math.Round(damageStats.averageAccuracy, 4) * 100).ToString() + "%";
        }

        // Damage Dice Text Box
        private void DamageDiceTextBox_Enter(object sender, EventArgs e)
        {
            // Check if box is unmodified
            if (DamageDiceTextBox.Text.Equals("Ex. 2D4+1"))
            {
                DamageDiceTextBox.Text = string.Empty;
            }
        }

        private void DamageDiceListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Check if auto-populating is safe
            if(DamageDiceTextBox.Text.Equals("Ex. 2D4+2") 
                || DamageDiceTextBox.Text.Equals(string.Empty))
            {
                DamageDiceTextBox.Text = DamageDiceListBox.SelectedItem?.ToString();
            }
        }

        private void DamageDiceAddButton_Click(object sender, EventArgs e)
        {

        }

        private void DamageDiceListBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DamageDiceTextBox.Text = DamageDiceListBox.SelectedItem?.ToString();
        }

        private void DamageDiceGroupBox_Leave(object sender, EventArgs e)
        {
            
        }
    }
}