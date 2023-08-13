using DamageCalculatorGUI;
using SharpNeatLib.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickings_For_Kurtulmak
{
    internal class WeaponGenerator
    {
        public struct WeaponTraitData
        {
            public int dieCount;
            public int dieSize;
            public int dieBonus;
            public int level;
        }
        public enum WeaponTrait
        {
            Agile,
            Brutal
        }
        public enum WeaponType
        {

        }
        public DamageCalculator.EncounterSettings GenerateRandomWeaponStats(List<WeaponType> types = default)
        {
            return (types.Equals(default)) ? GenerateRandomWeaponStats_RANDOM()
                                            : GenerateRandomWeaponStats_ARCHETYPAL(types[DamageCalculator.random.Next(types.Count)]);
        }
        private DamageCalculator.EncounterSettings GenerateRandomWeaponStats_ARCHETYPAL(WeaponType type)
        {
            int level = 1;

            int bonus_to_hit_class = 9;
            int enemy_ac_at_level = 17;

            // Trait X pairs
            Dictionary<WeaponTrait, WeaponTraitData> weapon_traits = new();

            DamageCalculator.EncounterSettings generatedSettings = new()
            {
                number_of_encounters = 500,
                rounds_per_encounter = 6,
                actions_per_round = new(),
                magazine_size = 0,
                reload = 0,
                long_reload = 0,
                draw = 0,
                damage_dice = new(),
                damage_dice_DOT = new(),
                bonus_to_hit = 0,
                AC = enemy_ac_at_level,
                crit_threshhold = 0,
                MAP_modifier = weapon_traits.TryGetValue(WeaponTrait.Agile, out WeaponTraitData traitData)
                                    ? -1 * traitData.level
                                    : 0,
                // To-do: Generator
                // To-do: Finish rest of things here
                // Add: BaseGame Weapon Traits (Agile, Brutal XDY, etc)
                // To-do: Serialization (saving & export, prolly to local txt then later CSV)
                // To-do: Comparison (Compare two exported files to eachother, or loaded stats against other)
            };
            return generatedSettings;
        }
        private DamageCalculator.EncounterSettings GenerateRandomWeaponStats_RANDOM()
        {
            DamageCalculator.EncounterSettings generatedSettings;

            return generatedSettings = new();
        }
    }
}
