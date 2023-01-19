using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace DamageCalculatorGUI
{
    internal class HelperFunctions
    {
        public static float Mod(float a, float b)
        {
            return a - b * MathF.Floor(a / b);
        }
        public static T GetRandomEnum<T>() where T : Enum
        {
            return (T)(object)DamageCalculator.random.Next(upperBound: Enum.GetNames(typeof(T)).Length);
        }
        public static bool IsControlDown()
        {
            return (Control.ModifierKeys & Keys.Control) == Keys.Control;
        }
        public static Tuple<Tuple<int, int, int>, Tuple<int, int, int>> ParseDiceValues(string str)
        {
            string count_d_sides_regex = @"((?<!\()\d+[d]\d+)";
            string plus_damage_regex = @"((?<=\+)\d+(?!\)))";
            string count_d_sides_crit_regex = @"((?<=\()\d+[d]\d+)";
            string plus_damage_crit_regex = @"((?<=\+)\d+(?=\)))";

            RegexOptions options = RegexOptions.IgnoreCase;

            // Run regex for xDy on xDy+z (mDn+o)
            string xDy = Regex.Match(input: str, pattern: count_d_sides_regex, options: options).Value;

            // Run regex for z on xDy+z (mDn+o)
            string xDyPlus = Regex.Match(input: str, pattern: plus_damage_regex, options: options).Value;
            if (xDyPlus.Length == 0) // Catch and prevent failed regex parsing problems
                xDyPlus = "0";

            // Run regex for mDn on xDy+z (mDn+o)
            string xDyCrit = Regex.Match(input: str, pattern: count_d_sides_crit_regex, options: options).Value;
            if (xDyCrit.Length == 0) // Catch and prevent failed regex parsing problems
                xDyCrit = "0d0";

            // Run regex for o on xDy+z (mDn+o)
            string xDyCritPlus = Regex.Match(input: str, pattern: plus_damage_crit_regex, options: options).Value;
            if (xDyCritPlus.Length == 0) // Catch and prevent failed regex parsing problems
                xDyCritPlus = "0";

            Tuple<int, int, int> damage = new(Int32.Parse(xDy[..(str.IndexOf('d'))]), // Get count
                                        Int32.Parse(xDy[(str.IndexOf('d') + 1)..]),
                                        Int32.Parse(xDyPlus)); // Get faces
            Tuple<int, int, int> damageCrit = new(Int32.Parse(xDyCrit[..(str.IndexOf('d'))]), // Get count
                                        Int32.Parse(xDyCrit[(str.IndexOf('d') + 1)..]),
                                        Int32.Parse(xDyCritPlus)); // Get faces

            return new(damage, damageCrit);
        }

        public static Tuple<int, int, int> ComputePercentiles(Dictionary<int, int> damageBins)
        {
            // Declare & Initialize Base Percentiles
            int halfPercentileFinal = -1; // (Median)
            int quarterPercentileFinal = -1;
            int threeQuarterPercentileFinal = -1;

            // Compute the total for finding the target number weight
            int sum = 0;
            for (int index = 0; index < damageBins.Count; index++)
            { // Iterate through the dictionary, storing the weighted sum
                sum += damageBins[index]; // Damage val * number of damage values
            }

            // Compute Required Weights for each percentile bracket
            float quarterPercentileWeight = sum / 4;
            float halfPercentileWeight = sum / 2;
            float threeQuarterPercentileWeight = sum * 3 / 4;

            // Compute the percentiles
            for (int binIndex = 0; binIndex < damageBins.Count; binIndex++)
            {
                // 25th Percentile
                quarterPercentileWeight -= damageBins[binIndex]; // Reduce weight until...
                if (quarterPercentileWeight <= 0 && quarterPercentileFinal == -1) // At or below 0, then...
                    quarterPercentileFinal = binIndex; // Store the index of when we've hit the quarter percentile.

                // 50th Percentile
                halfPercentileWeight -= damageBins[binIndex];
                if (halfPercentileWeight <= 0 && halfPercentileFinal == -1)
                    halfPercentileFinal = binIndex;

                // 75th Percentile
                threeQuarterPercentileWeight -= damageBins[binIndex];
                if (threeQuarterPercentileWeight <= 0 && threeQuarterPercentileFinal == -1)
                    threeQuarterPercentileFinal = binIndex;
            }

            return new(quarterPercentileFinal, halfPercentileFinal, threeQuarterPercentileFinal);
        }
    }
}
