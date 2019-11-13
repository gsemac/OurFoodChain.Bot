using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public class GotchiStatsCalculator {

        // Public members

        public GotchiContext Context { get; private set; }

        public GotchiStatsCalculator(GotchiContext gotchiContext) {

            Context = gotchiContext;

        }

        public async Task<GotchiStats> GetBaseStatsAsync(Gotchi gotchi) {

            GotchiStats result = new GotchiStats();

            int denominator = 0;

            GotchiType[] gotchiTypes = await Context.TypeRegistry.GetTypesAsync(gotchi);

            if (gotchiTypes.Count() > 0) {

                // Include the average of all types of this species.

                result.MaxHp = gotchiTypes.Sum(x => x.BaseHp);
                result.Atk = gotchiTypes.Sum(x => x.BaseAtk);
                result.Def = gotchiTypes.Sum(x => x.BaseDef);
                result.Spd = gotchiTypes.Sum(x => x.BaseSpd);

                denominator += gotchiTypes.Count();

            }

            long[] ancestor_ids = await SpeciesUtils.GetAncestorIdsAsync(gotchi.SpeciesId);

            // Factor in the base stats of this species' ancestor (which will, in turn, factor in all other ancestors).

            if (ancestor_ids.Count() > 0) {

                GotchiStats ancestor_stats = await GetBaseStatsAsync(new Gotchi { SpeciesId = ancestor_ids.Last() });

                result.MaxHp += ancestor_stats.MaxHp;
                result.Atk += ancestor_stats.Atk;
                result.Def += ancestor_stats.Def;
                result.Spd += ancestor_stats.Spd;

                denominator += 1;

            }

            // Add 20 points if this species has an ancestor (this effect will be compounded by the previous loop).

            if (ancestor_ids.Count() > 0) {

                result.MaxHp += 20;
                result.Atk += 20;
                result.Def += 20;
                result.Spd += 20;

            }

            // Get the average of each base stat.

            denominator = Math.Max(denominator, 1);

            result.MaxHp /= denominator;
            result.Atk /= denominator;
            result.Def /= denominator;
            result.Spd /= denominator;

            // Add 0.5 points for every week the gotchi has been alive.

            int age_bonus = (int)(0.5 * (gotchi.Age / 7));

            result.MaxHp += age_bonus;
            result.Atk += age_bonus;
            result.Def += age_bonus;
            result.Spd += age_bonus;

            // Add or remove stats based on the species' description.
            await _calculateDescriptionBasedBaseStats(gotchi, result);

            return result;

        }
        public async Task<GotchiStats> GetStatsAsync(Gotchi gotchi) {

            GotchiStats result = await GetBaseStatsAsync(gotchi);

            result.Experience = gotchi.Experience;

            // Calculate final stats based on level and base stats.
            // #todo Implement IVs/EVs

            int level = result.Level;

            result.MaxHp = _calculateHp(result.MaxHp, 0, 0, level);
            result.Atk = _calculateStat(result.Atk, 0, 0, level);
            result.Def = _calculateStat(result.Def, 0, 0, level);
            result.Spd = _calculateStat(result.Spd, 0, 0, level);

            result.Hp = result.MaxHp;

            return await Task.FromResult(result);

        }

        private static int _calculateHp(int baseStat, int iv, int ev, int level) {

            return (int)((Math.Floor(((2.0 * baseStat) + iv + (ev / 4.0)) * level) / 100.0) + level + 10.0);

        }
        private static int _calculateStat(int baseStat, int iv, int ev, int level) {

            return (int)(Math.Floor((((2.0 * baseStat) + iv + (ev / 4.0)) * level) / 100.0) + 5.0);

        }

        private static async Task _calculateDescriptionBasedBaseStats(Gotchi gotchi, GotchiStats stats) {

            Species species = await SpeciesUtils.GetSpeciesAsync(gotchi.SpeciesId);

            int weight = 20;

            if (Regex.IsMatch(species.Description, "photosynthesi(s|izes)", RegexOptions.IgnoreCase))
                stats.MaxHp += weight;

            if (Regex.IsMatch(species.Description, "spikes?|claws?|teeth|jaws|fangs", RegexOptions.IgnoreCase))
                stats.Atk += weight;

            if (Regex.IsMatch(species.Description, "shell|carapace|exoskeleton", RegexOptions.IgnoreCase))
                stats.Def += weight;

            foreach (Match m in Regex.Matches(species.Description, "flies|fly|quick|fast|agile|nimble", RegexOptions.IgnoreCase))
                stats.Spd += weight;

            foreach (Match m in Regex.Matches(species.Description, "slow|heavy", RegexOptions.IgnoreCase))
                stats.Spd -= weight;

        }

    }

}