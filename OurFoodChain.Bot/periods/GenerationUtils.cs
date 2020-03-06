using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class GenerationUtils {

        public static async Task<Generation[]> GetGenerationsAsync() {

            List<Generation> generations = new List<Generation>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Period WHERE name LIKE 'gen %'"))
                foreach (DataRow row in (await Database.GetRowsAsync(cmd)).Rows) {

                    generations.Add(GenerationFromDataRow(row));

                }

            // Make sure that we always have at least one generation.

            if (generations.Count() <= 0)
                generations.Add(await GetCurrentGenerationAsync());

            return generations.ToArray();

        }
        public static async Task<Generation> GetGenerationAsync(int number) {

            foreach (Generation generation in await GetGenerationsAsync())
                if (generation.Number == number)
                    return generation;

            return null;

        }
        public static async Task<Generation> GetGenerationByTimestampAsync(long timestamp) {

            foreach (Generation generation in await GetGenerationsAsync())
                if (timestamp >= generation.StartTimestamp && timestamp < generation.EndTimestamp)
                    return generation;

            return null;

        }

        public static async Task<Generation> GetCurrentGenerationAsync() {

            // Returns the latest generation, which the current generation always will be.

            Generation generation = await _getCurrentGenerationAsync();

            if (generation is null)
                return await AdvanceGenerationAsync();

            return generation;

        }
        public static async Task<Generation> AdvanceGenerationAsync() {

            Generation generation = await _getCurrentGenerationAsync();

            if (generation != null) {

                // Update the end date of the current generation.

                generation.EndTimestamp = DateUtilities.GetCurrentTimestampUtc();

                await _updateGenerationAsync(generation);

            }

            // Create and add the next generation.

            generation = new Generation {
                Number = generation is null ? 1 : generation.Number + 1,
                StartTimestamp = generation is null ? 0 : DateUtilities.GetCurrentTimestampUtc(),
                EndTimestamp = DateUtilities.GetMaxTimestamp()
            };

            await _addGenerationAsync(generation);

            return generation;

        }
        public static async Task<bool> RevertGenerationAsync() {

            Generation generation = await GetCurrentGenerationAsync();

            if (generation.Number <= 1)
                return false;

            // Delete the current generation.

            await _deleteGenerationAsync(generation);

            generation = await GetCurrentGenerationAsync();

            // Update the end timestamp of the previous generation so that it is now the current generation.

            generation.EndTimestamp = DateUtilities.GetMaxTimestamp();

            await _updateGenerationAsync(generation);

            return true;

        }

        public static Generation GenerationFromDataRow(DataRow row) {

            if (row is null)
                return null;

            Generation generation = new Generation {
                Id = row.Field<long>("id"),
                Number = int.Parse(Regex.Match(row.Field<string>("name"), @"\d+$").Value),
                StartTimestamp = DateUtilities.ParseTimestamp(row.Field<string>("start_ts")),
                EndTimestamp = DateUtilities.ParseTimestamp(row.Field<string>("end_ts"))
            };

            return generation;

        }

        public static bool GenerationIsValid(Generation generation) {

            if (generation is null || generation.Id < 0)
                return false;

            return true;

        }

        private static async Task<Generation> _getCurrentGenerationAsync() {

            // Note that this will always return a row even if no such row exists.
            // Because of that, we need to check if the "max" field is non-null.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT *, MAX(end_ts) AS max FROM Period WHERE name LIKE 'gen %'")) {

                DataRow row = await Database.GetRowAsync(cmd);

                if (row is null || row.IsNull("max"))
                    return null;

                return GenerationFromDataRow(row);

            }

        }
        private static async Task _addGenerationAsync(Generation generation) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Period(name, start_ts, end_ts) VALUES($name, $start_ts, $end_ts)")) {

                cmd.Parameters.AddWithValue("$name", generation.Name.ToLower());
                cmd.Parameters.AddWithValue("$start_ts", generation.StartTimestamp);
                cmd.Parameters.AddWithValue("$end_ts", generation.EndTimestamp);

                await Database.ExecuteNonQuery(cmd);

            }

        }
        private static async Task _updateGenerationAsync(Generation generation) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Period SET name = $name, start_ts = $start_ts, end_ts = $end_ts WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", generation.Id);
                cmd.Parameters.AddWithValue("$name", generation.Name.ToLower());
                cmd.Parameters.AddWithValue("$start_ts", generation.StartTimestamp);
                cmd.Parameters.AddWithValue("$end_ts", generation.EndTimestamp);

                await Database.ExecuteNonQuery(cmd);

            }
        }
        private static async Task _deleteGenerationAsync(Generation generation) {

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Period WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", generation.Id);

                await Database.ExecuteNonQuery(cmd);

            }

        }

    }

}