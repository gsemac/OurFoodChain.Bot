using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class GotchiStatCalculator {

        // Public members

        public static async Task<GotchiStats> GetStatsAsync(Gotchi gotchi, GotchiType[] types) {

            GotchiStats result = new GotchiStats();

            // Get level and EXP from the database.
            // #todo Don't access the database directly.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT level, exp FROM Gotchi WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", gotchi.id);

                DataRow row = await Database.GetRowAsync(cmd);

                if (row != null) {

                    result.Experience = (int)(row.IsNull("exp") ? 0.0 : row.Field<double>("exp"));
                    result.Level = (int)(row.IsNull("level") ? 1 : Math.Max(1, row.Field<long>("level")));

                }

            }
            
            // Set beginning stats to the average of all base stats from all types applicable to this gotchi.

            if (types.Count() > 0) {

                result.MaxHp = types.Sum(x => x.BaseHp) / types.Count();
                result.Atk = types.Sum(x => x.BaseAtk) / types.Count();
                result.Def = types.Sum(x => x.BaseDef) / types.Count();
                result.Spd = types.Sum(x => x.BaseSpd) / types.Count();

            }

            // #todo Implement IVs/EVs

            result.MaxHp = _calculateHp(result.MaxHp, 0, 0, result.Level);
            result.Atk = _calculateStat(result.Atk, 0, 0, result.Level);
            result.Def = _calculateStat(result.Def, 0, 0, result.Level);
            result.Spd = _calculateStat(result.Spd, 0, 0, result.Level);

            result.Hp = result.MaxHp;

            return await Task.FromResult(result);

        }

        private static int _calculateHp(int baseStat, int iv, int ev, int level) {

            return (int)((Math.Floor(((2.0 * baseStat) + iv + (ev / 4.0)) * level) / 100.0) + level + 10.0);

        }
        private static int _calculateStat(int baseStat, int iv, int ev, int level) {

            return (int)(Math.Floor((((2.0 * baseStat) + iv + (ev / 4.0)) * level) / 100.0) + 5.0);

        }

    }

}