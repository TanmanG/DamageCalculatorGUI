using System.Text.RegularExpressions;

namespace DamageCalculatorGUI
{
    internal class HelperFunctions
    {
        public static Tuple<Tuple<int, int>, Tuple<int, int>, Tuple<int, int>> ParseDiceValues(string str)
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

            Tuple<int, int> damage = new(Int32.Parse(xDy[..(str.IndexOf('d'))]), // Get count
                                        Int32.Parse(xDy[(str.IndexOf('d') + 1)..])); // Get faces
            Tuple<int, int> damageCrit = new(Int32.Parse(xDyCrit[..(str.IndexOf('d'))]), // Get count
                                        Int32.Parse(xDyCrit[(str.IndexOf('d') + 1)..])); // Get faces
            Tuple<int, int> damagePluses = new(Int32.Parse(xDyPlus), Int32.Parse(xDyCritPlus));

            return new(damage, damageCrit, damagePluses);
        }

        public static Tuple<int, int, int> ComputePercentiles(Dictionary<int, int> damageBins)
        {
            // Declare & Initialize Base Percentiles
            int halfPercentileFinal = default; // (Median)
            int quarterPercentileFinal = default;
            int threeQuarterPercentileFinal = default;

            // Compute the total for finding the target number weight
            int sum = 0;
            for (int index = 0; index < damageBins.OrderByDescending(kvp => kvp.Key).First().Key; index++)
            { // Iterate through the dictionary, storing the weighted sum
                sum += index * damageBins[index]; // Damage val * number of damage values
            }

            // Compute Required Weights for each percentile bracket
            float halfPercentileWeight = sum * 0.5f;
            float quarterPercentileWeight = sum * 0.25f;
            float threeQuarterPercentileWeight = 3 * quarterPercentileWeight;

            // Compute the percentiles
            for (int damageIndex = 0; damageIndex < damageBins.Count; damageIndex++)
            {
                quarterPercentileWeight -= damageBins[damageIndex] * damageIndex;
                if (quarterPercentileWeight <= 0 && quarterPercentileFinal == default)
                    quarterPercentileFinal = damageIndex;

                halfPercentileWeight -= damageBins[damageIndex] * damageIndex;
                if (halfPercentileWeight <= 0 && halfPercentileFinal == default)
                    halfPercentileFinal = damageIndex;

                threeQuarterPercentileWeight -= damageBins[damageIndex] * damageIndex;
                if (threeQuarterPercentileWeight <= 0 && threeQuarterPercentileFinal == default)
                    threeQuarterPercentileFinal = damageIndex;
            }

            return new(quarterPercentileFinal, halfPercentileFinal, threeQuarterPercentileFinal);
        }
    }
}
