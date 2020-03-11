using OurFoodChain.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Extensions {

    public static class SQLiteDatabasePeriodExtensions {

        // Public members

        public static async Task<Period> GetPeriodAsync(this SQLiteDatabase database, string name) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Period WHERE name = $name")) {

                cmd.Parameters.AddWithValue("$name", name.ToLower());

                DataRow row = await database.GetRowAsync(cmd);

                if (row != null)
                    return CreatePeriodFromDataRow(row);

            }

            return null;

        }
        public static async Task<IEnumerable<Period>> GetPeriodsAsync(this SQLiteDatabase database) {

            List<Period> results = new List<Period>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Period"))
                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    results.Add(CreatePeriodFromDataRow(row));

            // Have more recent periods listed first.
            results.Sort((lhs, rhs) => rhs.GetStartTimestamp().CompareTo(lhs.GetStartTimestamp()));

            return results;

        }

        // Private members

        private static Period CreatePeriodFromDataRow(DataRow row) {

            Period result = new Period {
                id = row.Field<long>("id"),
                name = row.Field<string>("name"),
                start_ts = row.Field<string>("start_ts"),
                end_ts = row.Field<string>("end_ts"),
                description = row.Field<string>("description")
            };

            return result;

        }

    }

}