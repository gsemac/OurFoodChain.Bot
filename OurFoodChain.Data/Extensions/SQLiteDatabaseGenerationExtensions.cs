﻿using OurFoodChain.Common.Generations;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Extensions {

    public static class SQLiteDatabaseGenerationExtensions {

        // Public members

        public static async Task<IEnumerable<IGeneration>> GetGenerationsAsync(this SQLiteDatabase database) {

            List<IGeneration> generations = new List<IGeneration>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Period WHERE name LIKE 'gen %'"))
                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    generations.Add(CreateGenerationFromDataRow(row));

            // Make sure that we always have at least one generation.

            if (generations.Count() <= 0)
                generations.Add(await database.GetCurrentGenerationAsync());

            return generations;

        }
        public static async Task<IGeneration> GetGenerationByDateAsync(this SQLiteDatabase database, DateTimeOffset date) {

            foreach (Generation generation in await database.GetGenerationsAsync())
                if (date >= generation.StartDate && date < generation.EndDate)
                    return generation;

            return null;

        }

        public static async Task<IGeneration> GetCurrentGenerationAsync(this SQLiteDatabase database) {

            // Returns the latest generation, which the current generation always will be.

            IGeneration generation = await database.GetCurrentGenerationOrNullAsync();

            if (generation is null)
                return await database.AdvanceGenerationAsync();

            return generation;

        }
        public static async Task<IGeneration> AdvanceGenerationAsync(this SQLiteDatabase database) {

            IGeneration generation = await database.GetCurrentGenerationOrNullAsync();

            if (generation != null) {

                // Update the end date of the current generation.

                generation.EndDate = DateUtilities.GetCurrentUtcDate();

                await database.UpdateGenerationAsync(generation);

            }

            // Create and add the next generation.

            generation = new Generation {
                Number = generation is null ? 1 : generation.Number + 1,
                StartDate = generation is null ? DateTimeOffset.MinValue : DateUtilities.GetCurrentUtcDate(),
                EndDate = DateTimeOffset.MaxValue
            };

            await database.AddGenerationAsync(generation);

            return generation;

        }

        // Private members

        private static IGeneration CreateGenerationFromDataRow(DataRow row) {

            if (row is null)
                return null;

            IGeneration generation = new Generation {
                Id = row.Field<long>("id"),
                Number = int.Parse(Regex.Match(row.Field<string>("name"), @"\d+$").Value),
                StartDate = DateUtilities.TimestampToDate(row.Field<string>("start_ts")),
                EndDate = DateUtilities.TimestampToDate(row.Field<string>("end_ts"))
            };

            return generation;

        }

        private static async Task<IGeneration> GetCurrentGenerationOrNullAsync(this SQLiteDatabase database) {

            // Note that this will always return a row even if no such row exists.
            // Because of that, we need to check if the "max" field is non-null.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT *, MAX(end_ts) AS max FROM Period WHERE name LIKE 'gen %'")) {

                DataRow row = await database.GetRowAsync(cmd);

                if (row is null || row.IsNull("max"))
                    return null;

                return CreateGenerationFromDataRow(row);

            }

        }
        private static async Task AddGenerationAsync(this SQLiteDatabase database, IGeneration generation) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Period(name, start_ts, end_ts) VALUES($name, $start_ts, $end_ts)")) {

                cmd.Parameters.AddWithValue("$name", generation.Name.ToLowerInvariant());
                cmd.Parameters.AddWithValue("$start_ts", DateUtilities.DateToTimestamp(generation.StartDate));
                cmd.Parameters.AddWithValue("$end_ts", DateUtilities.DateToTimestamp(generation.EndDate));

                await database.ExecuteNonQueryAsync(cmd);

            }

        }
        private static async Task UpdateGenerationAsync(this SQLiteDatabase database, IGeneration generation) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Period SET name = $name, start_ts = $start_ts, end_ts = $end_ts WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", generation.Id);
                cmd.Parameters.AddWithValue("$name", generation.Name.ToLowerInvariant());
                cmd.Parameters.AddWithValue("$start_ts", DateUtilities.DateToTimestamp(generation.StartDate));
                cmd.Parameters.AddWithValue("$end_ts", DateUtilities.DateToTimestamp(generation.EndDate));

                await database.ExecuteNonQueryAsync(cmd);

            }
        }

    }

}