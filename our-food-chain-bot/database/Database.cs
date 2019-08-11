using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    class Database {



        public static async Task<SQLiteConnection> GetConnectionAsync() {

            if (!_initialized)
                await _initializeAsync();

            return new SQLiteConnection(DATABASE_CONNECTION_STRING);

        }
        public static async Task<DataRow> GetRowAsync(SQLiteCommand command) {

            using (SQLiteConnection conn = await GetConnectionAsync()) {

                await conn.OpenAsync();

                command.Connection = conn;

                return await GetRowAsync(conn, command);

            }

        }
        public static async Task<DataRow> GetRowAsync(SQLiteConnection conn, SQLiteCommand command) {

            using (DataTable dt = await GetRowsAsync(conn, command))
                if (dt.Rows.Count > 0)
                    return dt.Rows[0];

            return null;

        }
        public static async Task<DataRow> GetRowAsync(SQLiteConnection conn, string query) {

            using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                return await GetRowAsync(conn, cmd);

        }
        public static async Task<DataTable> GetRowsAsync(SQLiteCommand command) {

            using (SQLiteConnection conn = await GetConnectionAsync()) {

                command.Connection = conn;

                return await GetRowsAsync(conn, command);

            }

        }
        public static async Task<DataTable> GetRowsAsync(SQLiteConnection conn, SQLiteCommand command) {

            command.Connection = conn;

            using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
            using (DataTable dt = new DataTable()) {

                await Task.Run(() => adapter.Fill(dt));

                return dt;

            }

        }
        public static async Task ExecuteNonQuery(string query) {

            using (SQLiteCommand cmd = new SQLiteCommand(query))
                await ExecuteNonQuery(cmd);

        }
        public static async Task ExecuteNonQuery(SQLiteCommand command) {

            using (SQLiteConnection conn = await GetConnectionAsync()) {

                await conn.OpenAsync();

                await ExecuteNonQuery(conn, command);

            }

        }
        public static async Task ExecuteNonQuery(SQLiteConnection conn, SQLiteCommand command) {

            command.Connection = conn;

            await command.ExecuteNonQueryAsync();

        }
        public static async Task<T> GetScalar<T>(SQLiteCommand command) {

            using (SQLiteConnection conn = await GetConnectionAsync()) {

                await conn.OpenAsync();

                DataRow row = await GetRowAsync(conn, command);

                return (T)row[0];

            }

        }

        private static readonly string DATABASE_FILE_NAME = "data.db";
        private static readonly string DATABASE_CONNECTION_STRING = string.Format("Data Source={0}", DATABASE_FILE_NAME);
        private static bool _initialized = false;

        public static string GetFilePath() {
            return DATABASE_FILE_NAME;
        }

        private static void _backupDatabase() {

            if (System.IO.File.Exists(DATABASE_FILE_NAME)) {

                System.IO.Directory.CreateDirectory("backups");

                System.IO.File.Copy(DATABASE_FILE_NAME, System.IO.Path.Combine("backups", string.Format("{0}-{1}", DateTimeOffset.Now.ToUnixTimeSeconds(), DATABASE_FILE_NAME)));

            }

        }
        private static async Task _initializeAsync() {

            if (_initialized)
                return;

            _initialized = true;

            // Backup the database before performing any operations on it.
            _backupDatabase();

            using (SQLiteConnection conn = await GetConnectionAsync()) {

                await conn.OpenAsync();

                await new DatabaseUpdater(conn).UpdateToLatestVersionAsync();

                conn.Close();

            }

        }

    }

}