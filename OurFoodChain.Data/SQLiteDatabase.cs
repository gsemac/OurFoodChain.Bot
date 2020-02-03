using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace OurFoodChain.Data {

    public class SQLiteDatabase {

        // Public members

        public string ConnectionString { get; }

        public SQLiteDatabase(string connectionString) {

            this.ConnectionString = connectionString;

        }

        public async Task<long> GetUserVersionAsync() {

            using (SQLiteCommand cmd = new SQLiteCommand("PRAGMA user_version"))
                return await GetScalarAsync<long>(cmd);

        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "<Pending>")]
        public async Task SetUserVersionAsync(long value) {

            using (SQLiteCommand cmd = new SQLiteCommand(string.Format("PRAGMA user_version = {0}", value)))
                await ExecuteNonQueryAsync(cmd);

        }

        public async Task<SQLiteConnection> GetConnectionAsync() {

            SQLiteConnection conn = await Task.FromResult(new SQLiteConnection(ConnectionString));

            await conn.OpenAsync();

            return conn;

        }
        public async Task<DataRow> GetRowAsync(SQLiteCommand command) {

            using (SQLiteConnection conn = await GetConnectionAsync()) {

                command.Connection = conn;

                return await GetRowAsync(conn, command);

            }

        }
        public async Task<IEnumerable<DataRow>> GetRowsAsync(SQLiteCommand command) {

            using (SQLiteConnection conn = await GetConnectionAsync()) {

                command.Connection = conn;

                return await GetRowsAsync(conn, command);

            }

        }
        public async Task<int> ExecuteNonQueryAsync(SQLiteCommand command) {

            using (SQLiteConnection conn = await GetConnectionAsync()) {

                return await ExecuteNonQueryAsync(conn, command);

            }

        }
        public async Task<T> GetScalarAsync<T>(SQLiteCommand command) {

            using (SQLiteConnection conn = await GetConnectionAsync())
                return await GetScalarAsync<T>(conn, command);

        }

        public static async Task<DataRow> GetRowAsync(SQLiteConnection conn, SQLiteCommand command) {

            IEnumerable<DataRow> rows = await GetRowsAsync(conn, command);

            if (rows.Count() > 0)
                return rows.First();

            return null;

        }
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
        public static async Task<T> GetScalarAsync<T>(SQLiteConnection conn, SQLiteCommand command) {

            DataRow row = await GetRowAsync(conn, command);

            try {

                return row is null ? default(T) : (T)row[0];

            }
            catch (InvalidCastException) {

                return default(T);

            }

        }

        public static SQLiteDatabase FromFile(string filePath) {

            return new SQLiteDatabase(string.Format("Data Source={0}", filePath));

        }

    }

}