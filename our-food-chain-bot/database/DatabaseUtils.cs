using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public static class DatabaseUtils {

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
        public static async Task<DataTable> GetRowsAsync(SQLiteConnection conn, SQLiteCommand command) {

            command.Connection = conn;

            using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
            using (DataTable dt = new DataTable()) {

                await Task.Run(() => adapter.Fill(dt));

                return dt;

            }

        }

        public static async Task<T> GetScalarAsync<T>(SQLiteConnection conn, SQLiteCommand command) {

            DataRow row = await GetRowAsync(conn, command);

            return (T)row[0];

        }
        public static async Task<T> GetScalarOrDefaultAsync<T>(SQLiteConnection conn, SQLiteCommand command, T defaultValue) {

            DataRow row = await GetRowAsync(conn, command);

            return row is null ? defaultValue : (T)row[0];

        }

        public static async Task ExecuteNonQuery(SQLiteConnection conn, string query) {

            using (SQLiteCommand cmd = new SQLiteCommand(query))
                await ExecuteNonQuery(conn, cmd);

        }
        public static async Task ExecuteNonQuery(SQLiteConnection conn, SQLiteCommand command) {

            command.Connection = conn;

            await command.ExecuteNonQueryAsync();

        }

        public static async Task ForEachRowAsync(SQLiteConnection conn, SQLiteCommand command, Func<DataRow, Task> callback) {

            using (DataTable table = await GetRowsAsync(conn, command))
                foreach (DataRow row in table.Rows)
                    await callback(row);

        }

    }

}
