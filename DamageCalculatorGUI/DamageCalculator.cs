using SharpNeatLib.Maths;

namespace DamageCalculatorGUI
{
    public class DamageCalculator
    {
        public static readonly FastRandom random = new();

        public struct EncounterSettings
        {
            // Damage Computation Variables
            public int number_of_encounters = 10000; // Total rounds to simulate
            public int rounds_per_encounter = 6; // Rounds per simulated encounter
            public RoundActions actions_per_round = default; // Actions per round
            public int magazine_size = 0; // Number of shots per Long Reload
            public int reload = 0; // Actions to reload each shot
            public int long_reload = 0; // Actions to reload the magazine.
            public int draw = 0; // Actions to draw the weapon.
            public List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> damage_dice = new(); // List of <X, Y. Z> <M, N, O> where XDY+Z, on crit MDN+O
            public int bonus_to_hit = 0; // Bonus added to-hit.
            public int AC = 0; // AC to test against. Tie attack roll hits.
            public int crit_threshhold = 0; // Raw attack roll at and above this critically strike.
            public int MAP_modifier = 0; // Modifier applied to MAP. By default 0 and thus -5+(0), -10+(0) per strike.
            public int engagement_range = 0; // Range to start the combat encounter at.
            public int move_speed = 0; // Distance covered by Stride.
            public bool seek_favorable_range = false; // Whether to Stride to an optimal firing position before firing.
            public int range = 0; // Range increment. Past this and for every increment thereafter applies stacking -2 penalty to-hit.
            public int volley = 0; // Minimum range increment. Firing within this applies a -2 penalty to-hit.
            public List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> damage_dice_DOT = new(); // DOT damage to apply on a hit/crit.

            public EncounterSettings() { }
            public void ResetSettings()
            {
                // Reset Settings
                number_of_encounters = 10000;
                rounds_per_encounter = 6;
                actions_per_round = new();
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
        }
        public struct RoundActions
        {
            public int any = 3;
            public int strike = 0;
            public int reload = 0;
            public int long_reload = 0;
            public int draw = 0;
            public int stride = 0;

            public RoundActions()
            {
                any = 3;
                strike = 0;
                reload = 0;
                long_reload = 0;
                draw = 0;
                stride = 0;
            }
            public RoundActions(int any = 0, int strike = 0, int reload = 0, int long_reload = 0, int draw = 0, int stride = 0)
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
            public Dictionary<int, int> damage_bins = new();
            // Total number of hits through the simulation.
            public int hits = 0;
            // Total number of misses through the simulation.
            public int misses = 0;
            // Total number of crits through the simulation.
            public int crits = 0;
            // Total damage through the encounter.
            public int damage_total = 0;
            // Highest damage encounter
            public int highest_encounter_damage = 0;
            // Total number of encounters ran for the given collection
            public int encounters_simulated = 0;
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
                damage_bins = new();
                hits = 0;
                misses = 0;
                crits = 0;
                damage_total = 0;
                encounters_simulated = 0;
                average_round_damage = 0;
                average_encounter_damage = 0;
                average_round_damage = 0;
                average_hit_damage = 0;
                average_accuracy = 0;
            }
            public DamageStats(Dictionary<int, int> damageBins, int hits, int misses, int crits, int damageTotal, int encountersSimulated, int highestEncounterDamage, double averageEncounterDamage, double averageRoundDamage, double averageHitDamage, double averageAccuracy)
            {
                damage_bins = damageBins;
                this.hits = hits;
                this.misses = misses;
                this.crits = crits;
                damage_total = damageTotal;
                encounters_simulated = encountersSimulated;
                highest_encounter_damage = highestEncounterDamage;
                average_encounter_damage = averageEncounterDamage;
                average_round_damage = averageRoundDamage;
                average_hit_damage = averageHitDamage;
                average_accuracy = averageAccuracy;
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
        public static DamageStats CalculateAverageDamage(IProgress<int> progress,
                                    int number_of_encounters, int rounds_per_encounter, // Round parameters
                                    List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>> damage_dice, // List of <X, Y, Z> <M, N, O> where XDY+Z, on crit MDN+O
                                    int reload, int long_reload = 0, int reload_size = 0, int draw = 1, // Reload parameters
                                    int bonus_to_hit = 0, // Bonus to damage per damage dice
                                    int AC = 21, int crit_threshhold = 20, int MAP_modifier = 0,
                                    int engagement_range = 30, int move_speed = 25, bool seek_favorable_range = false,
                                    int range = 1000, int volley = 0, // Range stats of the weapon
                                    List<Tuple<Tuple<int, int, int>, Tuple<int, int, int>>>? damage_dice_DOT = null,// Same as damage, but for DOT effects)
                                    RoundActions actions_per_round = default)
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
            int highestDamage = 0;
            int encountersSimulated = 0;
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
            Dictionary<int, int> dotEffects = new(); // Dictionary of damage over time effects and their origin index
            

            for (int currentEncounter = 0; currentEncounter < number_of_encounters; currentEncounter++)
            { // Iterate each encounter
                // Track how many encounters were simulated.
                encountersSimulated++;
                // Set Progress on Bar
                if (progress != null && HelperFunctions.Mod(a: currentEncounter + 1, b: number_of_encounters / 250) == 0)
                    // Update progress
                    progress.Report((currentEncounter + 1) * 250 / number_of_encounters);
                

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
                                        attackDamage += HelperFunctions.ClampAboveZero(RollD(currDamageDie.Item1, currDamageDie.Item2) + currDamageDie.Item3);
                                    }

                                    // DOT Damage
                                    for (int damageDiceDOTIndex = 0; damageDiceDOTIndex < damage_dice_DOT.Count; damageDiceDOTIndex++)
                                    {
                                        Tuple<int, int, int> currDamageDieDOT = damage_dice_DOT[damageDiceDOTIndex].Item2;

                                        int dotDamageRollResult = HelperFunctions.ClampAboveZero(RollD(currDamageDieDOT.Item1, currDamageDieDOT.Item2) + currDamageDieDOT.Item3) * 2;
                                        if (dotEffects.TryGetValue(damageDiceDOTIndex, out int activeDOTsize))
                                        {
                                            if (activeDOTsize > dotDamageRollResult)
                                                dotEffects[damageDiceDOTIndex] = activeDOTsize;
                                        }
                                        else
                                            dotEffects.Add(damageDiceDOTIndex, dotDamageRollResult);
                                    }

                                    // Double damage for the crit
                                    attackDamage *= 2;
                                }
                                else
                                { // Attack wasn't a crit
                                    // Base Damage
                                    for (int damageDiceIndex = 0; damageDiceIndex < damage_dice.Count; damageDiceIndex++)
                                    { // Roll damage for each damage dice type
                                        Tuple<int, int, int> currDamageDie = damage_dice[damageDiceIndex].Item1;
                                        attackDamage += HelperFunctions.ClampAboveZero(RollD(currDamageDie.Item1, currDamageDie.Item2) + currDamageDie.Item3);
                                    }

                                    // Apply DOT Damage
                                    for (int damageDiceDOTIndex = 0; damageDiceDOTIndex < damage_dice_DOT.Count; damageDiceDOTIndex++)
                                    {
                                        Tuple<int, int, int> currDamageDieDOT = damage_dice_DOT[damageDiceDOTIndex].Item1;
                                        
                                        int dotDamageRollResult = HelperFunctions.ClampAboveZero(RollD(currDamageDieDOT.Item1, currDamageDieDOT.Item2) + currDamageDieDOT.Item3);
                                        if (dotEffects.TryGetValue(damageDiceDOTIndex, out int activeDOTsize))
                                        {
                                            if (activeDOTsize > dotDamageRollResult)
                                                dotEffects[damageDiceDOTIndex] = activeDOTsize;
                                        }
                                        else
                                            dotEffects.Add(damageDiceDOTIndex, dotDamageRollResult);
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
                    List<int> dotEffectsSaved = new();
                    foreach (KeyValuePair<int, int> dotEffect in dotEffects)
                    { // Iterate through DOT effects, dealing damage and saving
                        // Apply poison
                        damageThisRound += dotEffect.Value;
                        if (RollD(20) >= 15)
                        { // Remove the current DOT effect
                            dotEffectsSaved.Add(dotEffect.Key);
                        }
                    }
                    if (dotEffectsSaved.Count > 0)
                        dotEffectsSaved.ForEach(x => dotEffects.Remove(x));

                    damageThisEncounter += damageThisRound;
                }
                // End of encounter
                // Store damage in the respective bin
                if (damageBins.ContainsKey(damageThisEncounter))
                    damageBins[damageThisEncounter]++;
                else
                    damageBins.Add(damageThisEncounter, 1);
                damageTotal += damageThisEncounter;
                
                // Track the highest damage dealt
                highestDamage = damageThisEncounter > highestDamage ? damageThisEncounter : highestDamage;
            }

            // Fill in holes in damage_bins
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
                        encountersSimulated: encountersSimulated,
                        highestEncounterDamage: highestDamage,
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
