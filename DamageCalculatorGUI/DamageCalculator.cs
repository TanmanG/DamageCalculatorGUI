using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeatLib.Maths;

namespace DamageCalculatorGUI
{
    public class DamageCalculator
    {
        static readonly FastRandom random = new();

        public static void TestRun()
        {
            DamageStats stats = CalculateAverageDamage(number_of_encounters: 1000000,
                                                        rounds_per_encounter: 1,
                                                        actions_per_round: 3,
                                                        reloadSize: 0,
                                                        reload: 1,
                                                        longReload: 0,
                                                        draw: 1,
                                                        damageDice: new()
                                                        { // List of damage dice
                                                            new( // Damage Dice Pair
                                                                new(1, 20, 0), // Normal Damage
                                                                new(1, 40, 0)  // Crit Damage
                                                                ),
                                                        });

            // Get the largest value
            int largest = 0;
            foreach (KeyValuePair<int, int> pair in stats.damageBins)
                if (pair.Key > largest)
                    largest = pair.Key;
            // Then use the largest value to zero-out gaps in the list
            for (int i = 0; i < largest; i++)
                if (!stats.damageBins.ContainsKey(i))
                    stats.damageBins.Add(i, 0);

            // Print Stats
            Console.WriteLine("Encounter: " + stats.average_encounter_damage);
            Console.WriteLine("Round: " + stats.average_round_damage);
            Console.WriteLine("Hit: " + stats.average_hit_damage);
            stats.damageBins.OrderBy(x => x.Key).ToList().ForEach(x => Console.Write(x.Value + ","));
        }
        public struct DamageStats
        {
            public Dictionary<int, int> damageBins = new();
            // Total number of hits through the simulation.
            public int hits = 0;
            // Total number of misses through the simulation.
            public int misses = 0;
            // Total number of crits through the simulation.
            public int crits = 0;
            // Total damage through the encounter.
            public int damage_total = 0;
            // Average damage per encounter.
            public double average_encounter_damage = 0;
            // Average damage per round.
            public double average_round_damage = 0;
            // Average damage per hit.
            public double average_hit_damage = 0;
            // Average accuracy.
            public double average_accuracy = 0;

            public DamageStats()
            {
                damageBins = new();
                hits = 0;
                misses = 0;
                crits = 0;
                damage_total = 0;
                average_encounter_damage = 0;
                average_round_damage = 0;
                average_hit_damage = 0;
                average_accuracy = 0;
            }
            public DamageStats(Dictionary<int, int> damageBins, int hits, int misses, int crits, int damageTotal, double averageEncounterDamage, double averageRoundDamage, double averageHitDamage, double averageAccuracy)
            {
                this.damageBins = damageBins;
                this.hits = hits;
                this.misses = misses;
                this.crits = crits;
                this.damage_total = damageTotal;
                this.average_encounter_damage = averageEncounterDamage;
                this.average_round_damage = averageRoundDamage;
                this.average_hit_damage = averageHitDamage;
                this.average_accuracy = averageAccuracy;
            }
        }

        /// <summary>
        /// Computes and returns the stats of the simulation.
        /// </summary>
        /// <param name="number_of_encounters">Number of encounters to simulate. Higher equates to more accurate results.</param>
        /// <param name="rounds_per_encounter">Number of rounds in an encounter (typically 6-7).</param>
        /// <param name="actions_per_round">Number of actions in round (typically 3).</param>
        /// <param name="reloadSize">Number of rounds in a magazine.</param>
        /// <param name="reload">Number of actions required to rechamber the weapon.</param>
        /// <param name="longReload">Number of actions required to reload a magazine.</param>
        /// <param name="draw">Number of actions required to unholster the weapon at the start of an encounter.</param>
        /// <param name="damageDice">XDY damage, NDM on crit damage.</param>
        /// <param name="bonusDamage">(Optional) Bonus damage on hit for each damage dice. Default 0s.</param>
        /// <param name="bonusToHit">(Optional) Bonus to-hit. Default 0.</param>
        /// <param name="AC">(Optional) AC of the target being tested against.</param>
        /// <param name="critThreshhold">(Optional) Which natrual rolls will critically strike. Default 20.</param>
        /// <param name="MAPmodifier">(Optional) Modifier to the MAP penalty (i.e. -1 equates to MAP of -4, -8). Default 0 (MAP 5, 10).</param>
        /// <param name="engagementRange">(Optional) Starting range for the engagement. Default 30.</param>
        /// <param name="moveSpeed">(Optional) Movement speed per Stride action. Default 25.</param>
        /// <param name="range">(Optional) Range Increment of the weapon. Default 1000.</param>
        /// <param name="seekFavorableRange">(Optional) Whether to try to Stride into range/out of volley.</param>
        /// <param name="volley">(Optional) Volley increment of the weapon. Default 0.</param>
        /// <param name="damageDiceDOT">(Optional) Damage dice for any DOT effects. Default 0s.</param>
        /// <returns>Struct containing all the data from this computation.</returns>
        public static DamageStats CalculateAverageDamage(int number_of_encounters, int rounds_per_encounter, int actions_per_round, // Round parameters
                                    List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> damageDice, // List of <X, Y, Z> <M, N, O> where XDY+Z, on crit MDN+O
                                    int reload, int longReload = 0, int reloadSize = 0, int draw = 1, // Reload parameters
                                    int bonusToHit = 0, // Bonus to damage per damage dice
                                    int AC = 21, int critThreshhold = 20, int MAPmodifier = 0,
                                    int engagementRange = 30, int moveSpeed = 25, bool seekFavorableRange = false,
                                    int range = 1000, int volley = 0, // Range stats of the weapon
                                    List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>>? damageDiceDOT = null) // Same as damage, but for DOT effects)
                                    
        {
            if (damageDiceDOT is null)
            {
                damageDiceDOT = new();
                for (int i = 0; i < damageDice.Count; i++)
                {
                    damageDiceDOT.Add(new(new(0, 0, 0), new(0, 0, 0))); // Zero out the DOT list
                }
            }

            // Initialize Tracking Variables
            int hits = 0;
            int misses = 0;
            int crits = 0;
            int damageTotal = 0;
            Dictionary<int, int> damageBins = new();

            int damageThisEncounter; // Damage dealt on the current encounter iteration.

            // Initialize Status Variables
            bool drawn; // Whether the gun is drawn
            int drawnProgress = 0; // Progress (in actions) of how many actions have been used on drawing (Draw)
            bool chambered; // Whether a bullet is ready to fire in chamber
            int chamberProgress; // Progress (in actions) of how many actions have been used on re-chambering (Reload)
            bool loaded; // Whether a magazine is loaded and ready
            int reloadProgress; // Progress (in actions) of how many actions have been used on re-loading (Long Reload)
            int loadedAmmo; // Number of bullets left in the magazine
            int actionsRemaining; // Actions left in this round
            int attacksThisRound; // Attacks that have been done this round
            bool rangeTooClose; // Whether the player is too close and needs to back up
            bool rangeTooFar; // Whether the player is too far and needs to get closer
            int currentDistance; // Distance away from the target
            int rangePenalty; // Current penalty from distance
            List<int> dotEffects = new(); // List of damage over time effects

            for (int currentEncounter = 0; currentEncounter < number_of_encounters; currentEncounter++)
            { // Iterate each encounter
                // Reset Encounter-based Variables
                drawn = false; // Re-holster the weapon
                damageThisEncounter = 0;

                reloadProgress = 0; // Reload the magazine
                loadedAmmo = reloadSize; // Reload the magazine
                loaded = true; // Reload the magazine

                chambered = true; // Rechamber the weapon
                chamberProgress = 0; // Rechamber the weapon

                currentDistance = engagementRange; // Reset engagement distance
                rangeTooClose = engagementRange < volley; // Re-check player close-ness
                rangeTooFar = engagementRange > range; // Re-check player far-ness
                rangePenalty = (currentDistance > range) ? currentDistance / range * 2 : 0; // Calculate range increment penalty
                rangePenalty += (currentDistance < volley) ? 2 : 0; // Calculate volley penalty

                dotEffects.Clear();

                for (int currentRound = 0; currentRound < rounds_per_encounter; currentRound++)
                { // Iterate each round
                    attacksThisRound = 0; // Reset MAP
                    int damageThisRound = 0; // Damage dealt this turn
                    for (actionsRemaining = actions_per_round; actionsRemaining > 0;)
                    {
                        if (seekFavorableRange && rangeTooClose)
                        {
                            // Compute how many stride actions are necessary to get out of volley
                            int actionsRequired = (volley - currentDistance) / moveSpeed;

                            if (actionsRemaining < actionsRequired)
                            { // Cannot fully walk out of volley
                                currentDistance += actionsRemaining * moveSpeed;
                                actionsRemaining = 0;
                            }
                            else
                            { // Can fully walk out of volley
                                currentDistance = volley;
                                actionsRemaining -= actionsRequired;

                                rangeTooClose = false;
                            }

                            // Adjust Range Penalty
                            rangePenalty = (currentDistance > range) ? currentDistance / range * 2 : 0; // Calculate range increment penalty
                            rangePenalty += (currentDistance < volley) ? 2 : 0; // Calculate volley penalty
                        }
                        else if (seekFavorableRange && rangeTooFar)
                        {
                            // Compute how many stride actions are necessary to get into range
                            int actionsRequired = (currentDistance - range) / moveSpeed + 1;

                            if (actionsRemaining < actionsRequired)
                            { // Cannot fully walk into range
                                currentDistance -= actionsRemaining * moveSpeed;
                                actionsRemaining = 0;
                            }
                            else
                            { // Can fully walk into range
                                currentDistance = range;
                                actionsRemaining -= actionsRequired;
                                rangeTooFar = false;
                            }

                            // Adjust Range Penalty
                            rangePenalty = (currentDistance > range) ? currentDistance / range * 2 : 0; // Calculate range increment penalty
                            rangePenalty += (currentDistance < volley) ? 2 : 0; // Calculate volley penalty
                        }
                        else if (!drawn)
                        { // Try to draw
                            int actionsRequired = draw - drawnProgress;

                            if (actionsRemaining < actionsRequired)
                            { // Cannot fully rechamber this turn
                                drawnProgress += actionsRemaining;
                                actionsRemaining = 0;
                            }
                            else
                            { // Can fully rechamber this turn
                                drawnProgress = 0;
                                actionsRemaining -= actionsRequired;

                                drawn = true;
                            }
                        }
                        else if (!loaded)
                        { // Try to reload
                            // Store how many actions are required to reload
                            int actionsRequired = longReload - reloadProgress;

                            if (actionsRemaining < actionsRequired)
                            { // Cannot fully rechamber this turn
                                reloadProgress += actionsRemaining;
                                actionsRemaining = 0;
                            }
                            else
                            { // Can fully rechamber this turn
                                reloadProgress = 0;
                                actionsRemaining -= actionsRequired;

                                loaded = true;
                                loadedAmmo = reloadSize;
                            }
                        }
                        else if (!chambered)
                        { // Try to chamber
                            // Store how many actions are required to chamber
                            int actionsRequired = reload - chamberProgress;

                            if (actionsRemaining < actionsRequired)
                            { // Cannot fully rechamber this turn
                                chamberProgress += actionsRemaining;
                                actionsRemaining = 0;
                            }
                            else
                            { // Can fully rechamber this turn
                                chamberProgress = 0;
                                actionsRemaining -= actionsRequired;

                                chambered = true;
                            }
                        }
                        else
                        { // Shoot
                            int attackRoll = RollD(20);
                            int attackDamage = 0;
                            int MAPpenalty = 0;

                            if (attacksThisRound > 0)
                            {
                                MAPpenalty = 5 + MAPmodifier;
                            }
                            if (attacksThisRound > 1)
                            {
                                MAPpenalty *= 2;
                            }

                            if (attackRoll + bonusToHit - MAPpenalty - rangePenalty >= AC)
                            { // Attack hit
                                hits++;
                                if (attackRoll >= critThreshhold)
                                { // Attack was a crit
                                    crits++;
                                    // Base Damage
                                    for (int damageDiceIndex = 0; damageDiceIndex < damageDice.Count; damageDiceIndex++)
                                    { // Roll damage for each damage dice block
                                        Tuple<int, int, int> currDamageDie = damageDice[damageDiceIndex].Item2;
                                        attackDamage += RollD(currDamageDie.Item1, currDamageDie.Item2) + currDamageDie.Item3;
                                    }

                                    // DOT Damage
                                    for (int damageDiceDOTIndex = 0; damageDiceDOTIndex < damageDiceDOT.Count; damageDiceDOTIndex++)
                                    {
                                        Tuple<int, int, int> currDamageDieDOT = damageDiceDOT[damageDiceDOTIndex].Item2;
                                        dotEffects.Add(RollD(currDamageDieDOT.Item1, currDamageDieDOT.Item2) + currDamageDieDOT.Item3);
                                    }

                                    // Double damage for the crit
                                    attackDamage *= 2;
                                    dotEffects[^1] *= 2;
                                }
                                else
                                { // Attack wasn't a crit
                                    // Base Damage
                                    for (int damageDiceIndex = 0; damageDiceIndex < damageDice.Count; damageDiceIndex++)
                                    { // Roll damage for each damage dice type
                                        Tuple<int, int, int> currDamageDie = damageDice[damageDiceIndex].Item1;
                                        attackDamage += RollD(currDamageDie.Item1, currDamageDie.Item2) + currDamageDie.Item3;
                                    }

                                    // Apply DOT Damage
                                    for (int damageDiceDOTIndex = 0; damageDiceDOTIndex < damageDiceDOT.Count; damageDiceDOTIndex++)
                                    {
                                        Tuple<int, int, int> currDamageDieDOT = damageDiceDOT[damageDiceDOTIndex].Item1;
                                        dotEffects.Add(RollD(currDamageDieDOT.Item1, currDamageDieDOT.Item2) + currDamageDieDOT.Item3);
                                    }
                                }
                                damageThisRound += attackDamage;
                            }
                            else
                            { // Attack missed
                                misses++;
                            }

                            // Update Magazine & Chamber
                            loadedAmmo--;
                            if (loadedAmmo == 0)
                                loaded = false;
                            chambered = false;

                            // Update Actions & MAP
                            actionsRemaining--;
                            attacksThisRound++;
                        }
                        // End of action
                    }
                    // End of round
                    damageThisEncounter += damageThisRound;

                    // Flat DC 15 save against any active DOT effects
                    for (int i = 0; i < dotEffects.Count; i++)
                    { // Iterate through DOT effects and apply poison for each
                        if (RollD(20) >= 15)
                        { // Nullify the current DOT effect
                            dotEffects[i] = 0;
                        }
                        else
                        { // Apply poison
                            damageThisRound += dotEffects[i];
                        }
                    }
                }
                // End of encounter
                // Store damage in the respective bin
                if (damageBins.ContainsKey(damageThisEncounter))
                    damageBins[damageThisEncounter]++;
                else
                    damageBins.Add(damageThisEncounter, 1);
                damageTotal += damageThisEncounter;
            }

            // Fill in holes in damageBins
            if (damageBins.Count > 0)
            for (int damageIndex = 0; damageIndex < damageBins.OrderByDescending(kvp => kvp.Key).First().Key; damageIndex++)
            {
                if (!damageBins.ContainsKey(damageIndex))
                {
                    damageBins.Add(damageIndex, 0);
                }
            }

            return new(damageBins: damageBins,
                        hits: hits,
                        misses: misses,
                        crits: crits,
                        damageTotal: damageTotal,
                        averageEncounterDamage: Math.Round((double)damageTotal / number_of_encounters, 2),
                        averageRoundDamage: Math.Round((double)damageTotal / number_of_encounters / rounds_per_encounter, 2),
                        averageHitDamage: Math.Round((double)damageTotal / hits, 2),
                        averageAccuracy: (double)hits / (hits + misses));
        }


        static int RollD(int sides)
        {
            return random.Next(lowerBound: 1, upperBound: sides + 1);
        }
        static int RollD(int count, int sides)
        {
            int sum = 0;
            for (int i = 0; i < count; i++)
            {
                sum += RollD(sides);
            }
            return sum;
        }
    }

}
