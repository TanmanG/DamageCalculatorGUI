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
        public static readonly FastRandom random = new();

        public struct Actions
        {
            public int any = 3;
            public int strike = 0;
            public int reload = 0;
            public int long_reload = 0;
            public int draw = 0;
            public int stride = 0;

            public Actions()
            {
                any = 3;
                strike = 0;
                reload = 0;
                long_reload = 0;
                draw = 0;
                stride = 0;
            }
            public Actions(int any = 0, int strike = 0, int reload = 0, int long_reload = 0, int draw = 0, int stride = 0)
            {
                this.any = any;
                this.strike = strike;
                this.reload = reload;
                this.long_reload = long_reload;
                this.draw = draw;
                this.stride = stride;
            }

            public int Total
            {
                get { return any + strike + reload + long_reload + draw + stride; }
                set { }
            }
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
        enum SuccessDegree
        {
            CriticalFail,
            Fail,
            Success,
            CriticalSuccess
        };

        /// <summary>
        /// Computes and returns the stats of the simulation.
        /// </summary>
        /// <param name="number_of_encounters">Number of encounters to simulate. Higher equates to more accurate results.</param>
        /// <param name="rounds_per_encounter">Number of rounds in an encounter (typically 6-7).</param>
        /// <param name="actions_per_round">Number of actions in round (typically 3).</param>
        /// <param name="reload_size">Number of rounds in a magazine.</param>
        /// <param name="reload">Number of actions required to rechamber the weapon.</param>
        /// <param name="long_reload">Number of actions required to reload a magazine.</param>
        /// <param name="draw">Number of actions required to unholster the weapon at the start of an encounter.</param>
        /// <param name="damage_dice">XDY damage, NDM on crit damage.</param>
        /// <param name="bonusDamage">(Optional) Bonus damage on hit for each damage dice. Default 0s.</param>
        /// <param name="bonus_to_hit">(Optional) Bonus to-hit. Default 0.</param>
        /// <param name="AC">(Optional) AC of the target being tested against.</param>
        /// <param name="crit_threshhold">(Optional) Which natrual rolls will critically strike. Default 20.</param>
        /// <param name="MAP_modifier">(Optional) Modifier to the MAP penalty (i.e. -1 equates to MAP of -4, -8). Default 0 (MAP 5, 10).</param>
        /// <param name="engagement_range">(Optional) Starting range for the engagement. Default 30.</param>
        /// <param name="move_speed">(Optional) Movement speed per Stride action. Default 25.</param>
        /// <param name="range">(Optional) Range Increment of the weapon. Default 1000.</param>
        /// <param name="seek_favorable_range">(Optional) Whether to try to Stride into range/out of volley.</param>
        /// <param name="volley">(Optional) Volley increment of the weapon. Default 0.</param>
        /// <param name="damage_dice_DOT">(Optional) Damage dice for any DOT effects. Default 0s.</param>
        /// <returns>Struct containing all the data from this computation.</returns>
        public static DamageStats CalculateAverageDamage(int number_of_encounters, int rounds_per_encounter, // Round parameters
                                    List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> damage_dice, // List of <X, Y, Z> <M, N, O> where XDY+Z, on crit MDN+O
                                    int reload, int long_reload = 0, int reload_size = 0, int draw = 1, // Reload parameters
                                    int bonus_to_hit = 0, // Bonus to damage per damage dice
                                    int AC = 21, int crit_threshhold = 20, int MAP_modifier = 0,
                                    int engagement_range = 30, int move_speed = 25, bool seek_favorable_range = false,
                                    int range = 1000, int volley = 0, // Range stats of the weapon
                                    List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>>? damage_dice_DOT = null,// Same as damage, but for DOT effects)
                                    Actions actions_per_round = default)
        {
            if (damage_dice_DOT is null)
            {
                damage_dice_DOT = new();
                for (int i = 0; i < damage_dice.Count; i++)
                {
                    damage_dice_DOT.Add(new(new(0, 0, 0), new(0, 0, 0))); // Zero out the DOT list
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
                loadedAmmo = reload_size; // Reload the magazine
                loaded = true; // Reload the magazine

                chambered = true; // Rechamber the weapon
                chamberProgress = 0; // Rechamber the weapon

                currentDistance = engagement_range; // Reset engagement distance
                rangeTooClose = engagement_range < volley; // Re-check player close-ness
                rangeTooFar = engagement_range > range; // Re-check player far-ness
                rangePenalty = (currentDistance > range) ? currentDistance / range * 2 : 0; // Calculate range increment penalty
                rangePenalty += (currentDistance < volley) ? 2 : 0; // Calculate volley penalty

                dotEffects.Clear();

                for (int currentRound = 0; currentRound < rounds_per_encounter; currentRound++)
                { // Iterate each round
                    attacksThisRound = 0; // Reset MAP
                    int damageThisRound = 0; // Damage dealt this turn

                    bool canTakeAction = true;
                    int actionsRemainingAny = actions_per_round.any;
                    int actionsRemainingStrike = actions_per_round.strike;
                    int actionsRemainingReload = actions_per_round.reload;
                    int actionsRemainingLongReload = actions_per_round.long_reload;
                    int actionsRemainingDraw = actions_per_round.draw;
                    int actionsRemainingStride = actions_per_round.stride;

                    while (canTakeAction)
                    {
                        if (seek_favorable_range && rangeTooClose && (actionsRemainingAny > 0 || actionsRemainingStride > 0))
                        { // Try to Stride farther
                            // Compute how many stride actions are necessary to get out of volley
                            int actionsRequired = (volley - currentDistance) / move_speed;

                            if (actionsRemainingAny + actionsRemainingStride < actionsRequired)
                            { // Cannot fully walk out of volley with both stride and walk actions
                                currentDistance += (actionsRemainingAny + actionsRemainingStride) * move_speed;
                                actionsRemainingStride = 0;
                                actionsRemainingAny = 0;
                            }
                            else
                            { // Can fully walk out of volley
                                currentDistance = volley;
                                
                                // Store how many actions can be pulled from extra actions
                                int extraActionsSpent = Math.Clamp(actionsRequired, 0, actionsRemainingStride);
                                // Pull as many extra actions out as possible
                                actionsRemainingStride -= extraActionsSpent;
                                actionsRemainingAny -= actionsRequired - extraActionsSpent;

                                rangeTooClose = false;
                            }

                            // Adjust Range Penalty
                            rangePenalty = (currentDistance > range) ? currentDistance / range * 2 : 0; // Calculate range increment penalty
                            rangePenalty += (currentDistance < volley) ? 2 : 0; // Calculate volley penalty
                        }
                        else if (seek_favorable_range && rangeTooFar && (actionsRemainingAny > 0 || actionsRemainingStride > 0))
                        { // Try to Stride closer
                            // Compute how many stride actions are necessary to get into range
                            int actionsRequired = (currentDistance - range) / move_speed + 1;

                            if (actionsRemainingAny < actionsRequired)
                            { // Cannot fully walk into range
                                currentDistance -= (actionsRemainingAny + actionsRemainingStride) * move_speed;
                                actionsRemainingStride = 0;
                                actionsRemainingAny = 0;
                            }
                            else
                            { // Can fully walk into range
                                currentDistance = range;

                                // Store how many actions can be pulled from extra actions
                                int extraActionsSpent = Math.Clamp(actionsRequired, 0, actionsRemainingStride);
                                // Pull as many extra actions out as possible
                                actionsRemainingStride -= extraActionsSpent;
                                actionsRemainingAny -= actionsRequired - extraActionsSpent;

                                rangeTooFar = false;
                            }

                            // Adjust Range Penalty
                            rangePenalty = (currentDistance > range) ? currentDistance / range * 2 : 0; // Calculate range increment penalty
                            rangePenalty += (currentDistance < volley) ? 2 : 0; // Calculate volley penalty
                        }
                        else if (!drawn && (actionsRemainingAny > 0 || actionsRemainingDraw > 0))
                        { // Try to draw
                            int actionsRequired = draw - drawnProgress;

                            if (actionsRemainingAny + actionsRemainingDraw < actionsRequired)
                            { // Cannot fully rechamber this turn
                                drawnProgress += actionsRemainingAny + actionsRemainingDraw;

                                actionsRemainingDraw = 0;
                                actionsRemainingAny = 0;
                            }
                            else
                            { // Can fully rechamber this turn
                                drawnProgress = 0;

                                // Store how many actions can be pulled from extra actions
                                int extraActionsSpent = Math.Clamp(actionsRequired, 0, actionsRemainingDraw);
                                // Pull as many extra actions out as possible
                                actionsRemainingDraw -= extraActionsSpent;
                                actionsRemainingAny -= actionsRequired - extraActionsSpent;

                                drawn = true;
                            }
                        }
                        else if (!loaded && (actionsRemainingAny > 0 || actionsRemainingLongReload > 0))
                        { // Try to reload
                            // Store how many actions are required to reload
                            int actionsRequired = long_reload - reloadProgress;

                            if (actionsRemainingAny + actionsRemainingLongReload < actionsRequired)
                            { // Cannot fully reload this turn
                                reloadProgress += actionsRemainingAny + actionsRemainingLongReload;
                                actionsRemainingLongReload = 0;
                                actionsRemainingAny = 0;
                            }
                            else
                            { // Can fully reload this turn
                                reloadProgress = 0;

                                // Store how many actions can be pulled from extra actions
                                int extraActionsSpent = Math.Clamp(actionsRequired, 0, actionsRemainingLongReload);
                                // Pull as many extra actions out as possible
                                actionsRemainingLongReload -= extraActionsSpent;
                                actionsRemainingAny -= actionsRequired - extraActionsSpent;

                                loaded = true;
                                loadedAmmo = reload_size;
                            }
                        }
                        else if (!chambered && (actionsRemainingAny > 0 || actionsRemainingReload > 0))
                        { // Try to chamber
                            // Store how many actions are required to chamber
                            int actionsRequired = reload - chamberProgress;

                            if (actionsRemainingAny + actionsRemainingReload < actionsRequired)
                            { // Cannot fully rechamber this turn
                                chamberProgress += actionsRemainingAny + actionsRemainingReload;
                                actionsRemainingReload = 0;
                                actionsRemainingAny = 0;
                            }
                            else
                            { // Can fully rechamber this turn
                                chamberProgress = 0;

                                // Store how many actions can be pulled from extra actions
                                int extraActionsSpent = Math.Clamp(actionsRequired, 0, actionsRemainingReload);
                                // Pull as many extra actions out as possible
                                actionsRemainingReload -= extraActionsSpent;
                                actionsRemainingAny -= actionsRequired - extraActionsSpent;

                                chambered = true;
                            }
                        }
                        else if (actionsRemainingAny > 0 || actionsRemainingStrike > 0)
                        { // Shoot
                            int attackRoll = RollD(20);
                            int attackDamage = 0;
                            int MAPpenalty = 0;

                            if (attacksThisRound > 0)
                            {
                                MAPpenalty = 5 + MAP_modifier;
                            }
                            if (attacksThisRound > 1)
                            {
                                MAPpenalty *= 2;
                            }

                            int attackRollPostMod = attackRoll + bonus_to_hit - MAPpenalty - rangePenalty;
                            SuccessDegree successDegree = (attackRollPostMod >= AC)
                                                            ? SuccessDegree.Success
                                                            : SuccessDegree.Fail;
                            if (attackRoll == 1)
                                successDegree = (SuccessDegree)Math.Clamp(((int)successDegree) - 1, 1, 4);
                            else if (attackRoll >= crit_threshhold || attackRoll - 10 >= AC)
                                successDegree = (SuccessDegree) Math.Clamp(((int)successDegree) + 1, 1, 4);

                            if (successDegree == SuccessDegree.Success || successDegree == SuccessDegree.CriticalSuccess)
                            { // Attack hit
                                hits++;
                                if (successDegree == SuccessDegree.CriticalSuccess)
                                { // Attack was a crit
                                    crits++;
                                    // Base Damage
                                    for (int damageDiceIndex = 0; damageDiceIndex < damage_dice.Count; damageDiceIndex++)
                                    { // Roll damage for each damage dice block
                                        Tuple<int, int, int> currDamageDie = damage_dice[damageDiceIndex].Item2;
                                        attackDamage += RollD(currDamageDie.Item1, currDamageDie.Item2) + currDamageDie.Item3;
                                    }

                                    // DOT Damage
                                    for (int damageDiceDOTIndex = 0; damageDiceDOTIndex < damage_dice_DOT.Count; damageDiceDOTIndex++)
                                    {
                                        Tuple<int, int, int> currDamageDieDOT = damage_dice_DOT[damageDiceDOTIndex].Item2;
                                        
                                        dotEffects.Add(RollD(currDamageDieDOT.Item1, currDamageDieDOT.Item2) + currDamageDieDOT.Item3);
                                    }

                                    // Double damage for the crit
                                    attackDamage *= 2;
                                    dotEffects[^1] *= 2;
                                }
                                else
                                { // Attack wasn't a crit
                                    // Base Damage
                                    for (int damageDiceIndex = 0; damageDiceIndex < damage_dice.Count; damageDiceIndex++)
                                    { // Roll damage for each damage dice type
                                        Tuple<int, int, int> currDamageDie = damage_dice[damageDiceIndex].Item1;
                                        attackDamage += RollD(currDamageDie.Item1, currDamageDie.Item2) + currDamageDie.Item3;
                                    }

                                    // Apply DOT Damage
                                    for (int damageDiceDOTIndex = 0; damageDiceDOTIndex < damage_dice_DOT.Count; damageDiceDOTIndex++)
                                    {
                                        Tuple<int, int, int> currDamageDieDOT = damage_dice_DOT[damageDiceDOTIndex].Item1;
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
                            if (actionsRemainingStrike > 0)
                                actionsRemainingStrike--;
                            else
                                actionsRemainingAny--;
                            attacksThisRound++;
                        }
                        else
                        { // Cannot do any actions
                            canTakeAction = false;
                        }
                        // End of action
                    }
                    // End of round

                    // Flat DC 15 save against any active DOT effects
                    for (int i = dotEffects.Count - 1; i >= 0; i--)
                    { // Iterate through DOT effects and apply poison for each
                        // Apply poison
                        damageThisRound += dotEffects[i];
                        if (RollD(20) >= 15)
                        { // Remove the current DOT effect
                            dotEffects.RemoveAt(i);
                        }
                    }

                    damageThisEncounter += damageThisRound;
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
