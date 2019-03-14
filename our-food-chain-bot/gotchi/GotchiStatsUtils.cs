using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public class GotchiStatsUtils {

        public static double ExperienceRequired(LuaGotchiStats stats) {

            return ExperienceToNextLevel(stats) - stats.exp;

        }
        public static double ExperienceToNextLevel(LuaGotchiStats stats) {

            // level * 10 * 1.5 EXP required per level
            // This means, to get to Level 10, a minimum of 15 battles are required.

            return (stats.level * 10 * 1.5);

        }
        public static long LeveUp(LuaGotchiStats stats, double experience) {

            stats.exp += experience;

            long levels = 0;

            while (stats.exp >= ExperienceToNextLevel(stats)) {

                stats.exp -= ExperienceToNextLevel(stats);

                ++stats.level;
                ++levels;

            }

            return levels;

        }

        public static async Task<LuaGotchiStats> CalculateStats(Gotchi gotchi) {

            LuaGotchiStats stats = new LuaGotchiStats();

            // Get level and EXP from the database.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT level, exp FROM Gotchi WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$id", gotchi.id);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null)) {

                    stats.exp = row.IsNull("exp") ? 0.0 : row.Field<double>("exp");
                    stats.level = row.IsNull("level") ? 1 : Math.Max(1, row.Field<long>("level"));

                }

            }

            return await CalculateStats(gotchi, stats);

        }
        public static async Task<LuaGotchiStats> CalculateStats(Gotchi gotchi, LuaGotchiStats stats) {

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (sp is null)
                return stats;

            // Calculate base stat multipliers, which depend on the species' role(s).

            Role[] roles = await BotUtils.GetRolesFromDbBySpecies(sp);

            if (roles.Count() > 0) {

                switch (roles[0].name.ToLower()) {

                    // Balanced, but does not excel anywhere.
                    case "base-consumer":
                        stats.hp *= 1.1;
                        stats.atk *= 1.1;
                        stats.def *= 1.1;
                        stats.spd *= 1.1;
                        break;

                    // decent HP, subpar attack and speed.
                    case "decomposer":
                    case "scavenger":
                    case "detritvore":
                        stats.hp *= 1.1;
                        stats.atk *= 0.8;
                        stats.spd *= 0.5;
                        break;

                    // Good attack and defense, but subpar speed and HP.
                    case "parasite":
                        stats.hp *= 0.8;
                        stats.atk *= 1.5;
                        stats.def *= 1.5;
                        stats.spd *= 0.8;
                        break;

                    // Fast attacker, but poor defender.
                    case "predator":
                        stats.atk *= 1.5;
                        stats.spd *= 1.5;
                        stats.def *= 0.2;
                        break;

                    // Good health and recovery, but slow.
                    case "producer":
                        stats.hp *= 2.0;
                        stats.spd *= 0.1;
                        stats.atk *= 0.3;
                        break;

                    // Fast, but not defensive.
                    case "pollinator":
                        stats.spd *= 1.5;
                        stats.def *= 0.5;
                        break;

                }

            }

            // More evolved species will have better base stats.

            Species[] ancestors = await BotUtils.GetAncestorsFromDb(sp.id);

            stats.hp += ancestors.Count() * 0.1;
            stats.atk += ancestors.Count() * 0.1;
            stats.def += ancestors.Count() * 0.1;
            stats.spd += ancestors.Count() * 0.1;

            // Add bonus multipliers depending on characteristics mentioned in the species' description.

            if (Regex.IsMatch(sp.description, "photosynthesi(s|izes)", RegexOptions.IgnoreCase))
                stats.hp += 0.2;

            if (Regex.IsMatch(sp.description, "spikes?|claws?|teeth|jaws|fangs", RegexOptions.IgnoreCase))
                stats.atk += 0.2;

            if (Regex.IsMatch(sp.description, "shell|carapace|exoskeleton", RegexOptions.IgnoreCase))
                stats.def += 0.2;

            foreach (Match m in Regex.Matches(sp.description, "flies|fly|quick|fast|agile|nimble", RegexOptions.IgnoreCase))
                stats.spd += 0.2;

            foreach (Match m in Regex.Matches(sp.description, "slow|heavy", RegexOptions.IgnoreCase))
                stats.spd = Math.Max(0.1, stats.spd - 0.2);

            // For additional variation, assign bonus multipliers randomly according to the species name.

            Random random = new Random(StringUtils.SumStringChars(sp.name + gotchi.born_ts.ToString()));

            stats.hp += random.Next(0, 5) / 10.0;
            stats.atk += random.Next(0, 5) / 10.0;
            stats.def += random.Next(0, 5) / 10.0;
            stats.spd += random.Next(0, 5) / 10.0;

            // The gotchi's final stats depend on its level/age.

            double age = gotchi.Age();
            double multiplier = stats.level + (age / 10.0);

            stats.hp *= multiplier;
            stats.atk *= multiplier;
            stats.def *= multiplier;
            stats.spd *= multiplier;

            // Make sure required stats are >= 1.

            stats.hp = Math.Max(1.0, stats.hp);
            stats.atk = Math.Max(1.0, stats.atk);

            stats.max_hp = stats.hp;

            // Copy stats over to base stats, so base stats can be referenced later even if they are changed during battle.
            stats.base_hp = stats.hp;
            stats.base_max_hp = stats.max_hp;
            stats.base_atk = stats.atk;
            stats.base_def = stats.def;
            stats.base_spd = stats.spd;

            return stats;

        }

    }

}