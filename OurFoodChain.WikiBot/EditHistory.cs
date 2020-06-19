using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Wiki {

    public class UploadRecord {
        public long Timestamp { get; set; }
        public string FilePath { get; set; }
        public string UploadFileName { get; set; }
    }

    public class EditRecord {
        public long Id { get; set; }
        public long Timestamp { get; set; }
        public string Title { get; set; }
        public string ContentHash { get; set; }
    }

    public class RedirectRecord {
        public long Timestamp { get; set; }
        public string Title { get; set; }
        public string Target { get; set; }
    }

    public class EditHistory {

        // Public members

        public string DatabaseFilePath { get; set; } = "edit_history.db";

        public EditHistory() {

            database = SQLiteDatabase.FromFile(DatabaseFilePath);

        }

        public async Task AddUploadRecordAsync(string filePath, string fileName) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO UploadHistory(timestamp, file_path, upload_file_name) VALUES($timestamp, $file_path, $upload_file_name)")) {

                cmd.Parameters.AddWithValue("$timestamp", _getTimestamp());
                cmd.Parameters.AddWithValue("$file_path", filePath);
                cmd.Parameters.AddWithValue("$upload_file_name", fileName);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }
        public async Task<UploadRecord> GetUploadRecordAsync(string filePath) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM UploadHistory WHERE file_path = $file_path")) {

                cmd.Parameters.AddWithValue("$file_path", filePath);

                IEnumerable<DataRow> rows = await database.GetRowsAsync(cmd);

                if (rows.Count() > 0) {

                    return new UploadRecord {
                        Timestamp = rows.First().Field<long>("timestamp"),
                        FilePath = rows.First().Field<string>("file_path"),
                        UploadFileName = rows.First().Field<string>("upload_file_name")
                    };

                }

            }

            return null;

        }

        public async Task AddEditRecordAsync(string title, string content) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO PageEditHistory(timestamp, title, content_hash) VALUES($timestamp, $title, $content_hash)")) {

                cmd.Parameters.AddWithValue("$timestamp", _getTimestamp());
                cmd.Parameters.AddWithValue("$title", title);
                cmd.Parameters.AddWithValue("$content_hash", StringUtilities.GetMD5(content).ToLower());

                await database.ExecuteNonQueryAsync(cmd);

            }

        }
        public async Task AddEditRecordAsync(long speciesId, EditRecord record) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO SpeciesPageEditHistory(timestamp, edit_id, species_id) VALUES($timestamp, $edit_id, $species_id)")) {

                cmd.Parameters.AddWithValue("$timestamp", record.Timestamp);
                cmd.Parameters.AddWithValue("$edit_id", record.Id);
                cmd.Parameters.AddWithValue("$species_id", speciesId);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }
        public async Task<EditRecord> GetEditRecordAsync(string title, string content) {

            // Returns the latest upload record for a page, but only if the content hash matches.

            string content_hash = StringUtilities.GetMD5(content).ToLower();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM PageEditHistory WHERE title = $title ORDER BY timestamp DESC LIMIT 1")) {

                cmd.Parameters.AddWithValue("$title", title);

                IEnumerable<DataRow> rows = await database.GetRowsAsync(cmd);

                if (rows.Count() > 0) {

                    EditRecord record = new EditRecord {
                        Id = rows.First().Field<long>("id"),
                        Timestamp = rows.First().Field<long>("timestamp"),
                        Title = rows.First().Field<string>("title"),
                        ContentHash = rows.First().Field<string>("content_hash")
                    };

                    if (record.ContentHash == content_hash)
                        return record;

                }

            }

            return null;

        }
        public async Task<EditRecord[]> GetEditRecordsAsync(long speciesId) {

            List<EditRecord> records = new List<EditRecord>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM SpeciesPageEditHistory INNER JOIN PageEditHistory ON PageEditHistory.id = SpeciesPageEditHistory.edit_id WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", speciesId);

                foreach (DataRow row in await database.GetRowsAsync(cmd)) {

                    records.Add(new EditRecord {
                        Id = row.Field<long>("id"),
                        Timestamp = row.Field<long>("timestamp"),
                        Title = row.Field<string>("title"),
                        ContentHash = row.Field<string>("content_hash")
                    });

                }

            }

            return records.ToArray();

        }

        public async Task AddRedirectRecordAsync(string title, string target) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO RedirectHistory(timestamp, title, target) VALUES($timestamp, $title, $target)")) {

                cmd.Parameters.AddWithValue("$timestamp", _getTimestamp());
                cmd.Parameters.AddWithValue("$title", title);
                cmd.Parameters.AddWithValue("$target", target);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }
        public async Task<RedirectRecord[]> GetRedirectRecordsAsync() {

            List<RedirectRecord> records = new List<RedirectRecord>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM RedirectHistory;"))
                foreach (DataRow row in await database.GetRowsAsync(cmd)) {

                    records.Add(new RedirectRecord {
                        Timestamp = row.Field<long>("timestamp"),
                        Title = row.Field<string>("title"),
                        Target = row.Field<string>("target")
                    });

                }

            return records.ToArray();

        }
        public async Task<RedirectRecord[]> GetRedirectRecordsAsync(string redirectsToTitle) {

            List<RedirectRecord> records = new List<RedirectRecord>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM RedirectHistory WHERE target = $target")) {

                cmd.Parameters.AddWithValue("$target", redirectsToTitle);

                foreach (DataRow row in await database.GetRowsAsync(cmd)) {

                    records.Add(new RedirectRecord {
                        Timestamp = row.Field<long>("timestamp"),
                        Title = row.Field<string>("title"),
                        Target = row.Field<string>("target")
                    });

                }

            }

            return records.ToArray();

        }

        // Private members

        private bool initialized = false;
        private readonly SQLiteDatabase database;

        private async Task _initializeDatabaseAsync() {

            initialized = true;

            await _executeNonQueryAsync("CREATE TABLE IF NOT EXISTS UploadHistory(timestamp INTEGER, file_path TEXT, upload_file_name TEXT)");
            await _executeNonQueryAsync("CREATE TABLE IF NOT EXISTS PageEditHistory(id INTEGER PRIMARY KEY AUTOINCREMENT, timestamp INTEGER, title TEXT, content_hash TEXT)");
            await _executeNonQueryAsync("CREATE TABLE IF NOT EXISTS RedirectHistory(timestamp INTEGER, title TEXT, target TEXT)");
            await _executeNonQueryAsync("CREATE TABLE IF NOT EXISTS SpeciesPageEditHistory(timestamp INTEGER, edit_id INTEGER, species_id INTEGER, FOREIGN KEY(edit_id) REFERENCES PageEditHistory(id))");

            await _executeNonQueryAsync("CREATE TABLE IF NOT EXISTS Meta(version INTEGER)");

            if (await _getDatabaseVersionAsync() <= 0)
                await _executeNonQueryAsync("INSERT INTO Meta(version) VALUES(1)");

        }
        private async Task _executeNonQueryAsync(string command) {

            using (SQLiteCommand cmd = new SQLiteCommand(command))
                await database.ExecuteNonQueryAsync(cmd);

        }
        private async Task<bool> _executeExistenceQueryAsync(SQLiteCommand command) {

            using (SQLiteConnection conn = await database.GetConnectionAsync()) {

                command.Connection = conn;

                object result = command.ExecuteScalar();

                if (result is null)
                    return false;

                return (long)result > 0;

            }

        }
        private async Task<long> _getDatabaseVersionAsync() {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT version FROM Meta"))
                return await database.GetScalarAsync<long>(cmd);

        }

        private long _getTimestamp() {
            return DateTimeOffset.Now.ToUniversalTime().ToUnixTimeSeconds();
        }

    }

}