using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Extensions {

    public static class SQLiteDatabaseExtensions {

        // Public members

        public static async Task<long> GetUserVersionAsync(this ISQLiteDatabase database) {

            using (SQLiteCommand cmd = new SQLiteCommand("PRAGMA user_version"))
                return await database.GetScalarAsync<long>(cmd);

        }
        public static async Task SetUserVersionAsync(this ISQLiteDatabase database, long value) {

#pragma warning disable CA2100 

            using (SQLiteCommand cmd = new SQLiteCommand(string.Format("PRAGMA user_version = {0}", value)))
                await database.ExecuteNonQueryAsync(cmd);

#pragma warning restore CA2100

        }

        public static async Task<SQLiteConnection> GetConnectionAsync(this ISQLiteDatabase database) {

            SQLiteConnection conn = await Task.FromResult(new SQLiteConnection(database.ConnectionString));

            await conn.OpenAsync();

            return conn;

        }
        public static async Task<DataRow> GetRowAsync(this ISQLiteDatabase database, SQLiteCommand command) {

            using (SQLiteConnection conn = await database.GetConnectionAsync()) {

                command.Connection = conn;

                return await database.GetRowAsync(conn, command);

            }

        }
        public static async Task<IEnumerable<DataRow>> GetRowsAsync(this ISQLiteDatabase database, SQLiteCommand command) {

            using (SQLiteConnection conn = await database.GetConnectionAsync()) {

                command.Connection = conn;

                return await GetRowsAsync(conn, command);

            }

        }
        public static async Task<int> ExecuteNonQueryAsync(this ISQLiteDatabase database, SQLiteCommand command) {

            using (SQLiteConnection conn = await database.GetConnectionAsync()) {

                return await ExecuteNonQueryAsync(conn, command);

            }

        }
        public static async Task<T> GetScalarAsync<T>(this ISQLiteDatabase database, SQLiteCommand command) {

            using (SQLiteConnection conn = await database.GetConnectionAsync())
                return await database.GetScalarAsync<T>(conn, command);

        }

        public static async Task<DataRow> GetRowAsync(this ISQLiteDatabase database, SQLiteConnection conn, SQLiteCommand command) {

            IEnumerable<DataRow> rows = await GetRowsAsync(conn, command);

            return rows.FirstOrDefault();

        }
        public static async Task<T> GetScalarAsync<T>(this ISQLiteDatabase database, SQLiteConnection conn, SQLiteCommand command) {

            DataRow row = await database.GetRowAsync(conn, command);

            try {

                return row is null ? default(T) : (T)row[0];

            }
            catch (InvalidCastException) {

                return default(T);

            }

        }

        // Private members

        public static async Task<IEnumerable<DataRow>> GetRowsAsync(SQLiteConnection conn, SQLiteCommand command) {

            command.Connection = conn;

            using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
            using (DataTable dt = new DataTable()) {

                await Task.Run(() => adapter.Fill(dt));

                return dt.Rows.Cast<DataRow>();

            }

        }
        public static async Task<int> ExecuteNonQueryAsync(SQLiteConnection conn, SQLiteCommand command) {

            command.Connection = conn;

            return await command.ExecuteNonQueryAsync();

        }

    }

}