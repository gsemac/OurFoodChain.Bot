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

        public const int DATABASE_VERSION = 16;

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

                int version = await _getDatabaseVersion(conn);

                // Update the database to the latest version.

                for (int i = ++version; i <= DATABASE_VERSION; ++i)
                    await _applyDatabaseUpdate(conn, i);

                conn.Close();

            }

        }
        private static async Task<int> _getDatabaseVersion(SQLiteConnection conn) {

            // Create metadata table if it doesn't already exist.
            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Meta (version INTEGER);", conn))
                await cmd.ExecuteNonQueryAsync();

            // Get the current table version from the database. If the query was successful, return the result.
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT version FROM Meta;", conn))
            using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                    return reader.GetInt32(0);

            // If no version information exists, insert it.
            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Meta VALUES (0);", conn))
                await cmd.ExecuteNonQueryAsync();

            // Return 0 to indicate the database has not yet been initialized.
            return 0;

        }
        private static async Task _updateDatabaseVersion(SQLiteConnection conn, int version) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Meta SET version = $version WHERE version < $version;", conn)) {

                cmd.Parameters.AddWithValue("$version", version);

                await cmd.ExecuteNonQueryAsync();

            }

        }
        private static async Task _applyDatabaseUpdate(SQLiteConnection conn, int updateNumber) {

            if (updateNumber <= 0)
                return;

            await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Info, "Database", string.Format("Updating database to version {0}", updateNumber));

            switch (updateNumber) {

                case 1:
                    await _update001(conn);
                    break;

                case 2:
                    await _update002(conn);
                    break;

                case 3:
                    await _update003(conn);
                    break;

                case 4:
                    await _update004(conn);
                    break;

                case 5:
                    await _update005(conn);
                    break;

                case 6:
                    await _update006(conn);
                    break;

                case 7:
                    await _update007(conn);
                    break;

                case 8:
                    await _update008(conn);
                    break;

                case 9:
                    await _update009(conn);
                    break;

                case 10:
                    await _update010(conn);
                    break;

                case 11:
                    await _update011(conn);
                    break;

                case 12:
                    await _update012(conn);
                    break;

                case 13:
                    await _update013(conn);
                    break;

                case 14:
                    await _update014(conn);
                    break;

                case 15:
                    await _update015(conn);
                    break;

                case 16:
                    await _update016(conn);
                    break;

            }

            await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Info, "Database", string.Format("Updated database to version {0}", updateNumber));

        }

        private static async Task _update001(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Zones(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, type TEXT, description TEXT, UNIQUE(name, type));", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Phylum(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE, description TEXT);", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Genus(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE, description TEXT);", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Species(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, description TEXT, genus_id INTEGER, timestamp NUMERIC, UNIQUE(name, genus_id), FOREIGN KEY(genus_id) REFERENCES Genus(id));", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS SpeciesZones(species_id INTEGER, zone_id INTEGER, notes TEXT, FOREIGN KEY(species_id) REFERENCES Species(id), FOREIGN KEY(zone_id) REFERENCES Zones(id), PRIMARY KEY(species_id, zone_id));", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Extinctions(species_id INTEGER PRIMARY KEY, reason TEXT, timestamp NUMERIC, FOREIGN KEY(species_id) REFERENCES Species(id));", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Ancestors(species_id INTEGER PRIMARY KEY, ancestor_id INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id), FOREIGN KEY(ancestor_id) REFERENCES Species(id));", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 1);

        }
        private static async Task _update002(SQLiteConnection conn) {

            // Add the "pics" field to the "Species" table.
            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Species ADD COLUMN pics TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 2);

        }
        private static async Task _update003(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Species ADD COLUMN owner TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 3);

        }
        private static async Task _update004(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Species ADD COLUMN common_name TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 4);

        }
        private static async Task _update005(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Predates(species_id INTEGER, eats_id INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id), FOREIGN KEY(eats_id) REFERENCES Species(id), PRIMARY KEY(species_id, eats_id));", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 5);

        }
        private static async Task _update006(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Predates ADD COLUMN notes TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 6);

        }
        private static async Task _update007(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Roles(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE, description TEXT);", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS SpeciesRoles(species_id INTEGER, role_id INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id), FOREIGN KEY(role_id) REFERENCES Roles(id), PRIMARY KEY(species_id, role_id));", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 7);

        }
        private static async Task _update008(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE SpeciesRoles ADD COLUMN notes TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 8);

        }
        private static async Task _update009(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Domain(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE, description TEXT);", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Kingdom(id INTEGER PRIMARY KEY AUTOINCREMENT, domain_id INTEGER, name TEXT UNIQUE, description TEXT, FOREIGN KEY(domain_id) REFERENCES Domain(id));", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Phylum ADD COLUMN kingdom_id INTEGER REFERENCES Kingdom(id);", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Class(id INTEGER PRIMARY KEY AUTOINCREMENT, phylum_id INTEGER, name TEXT UNIQUE, description TEXT, FOREIGN KEY(phylum_id) REFERENCES Phylum(id));", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Ord(id INTEGER PRIMARY KEY AUTOINCREMENT, class_id INTEGER, name TEXT UNIQUE, description TEXT, FOREIGN KEY(class_id) REFERENCES Class(id));", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Family(id INTEGER PRIMARY KEY AUTOINCREMENT, order_id INTEGER, name TEXT UNIQUE, description TEXT, FOREIGN KEY(order_id) REFERENCES Ord(id));", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Genus ADD COLUMN family_id INTEGER REFERENCES Family(id);", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 9);

        }
        private static async Task _update010(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Domain ADD COLUMN pics TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Kingdom ADD COLUMN pics TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Phylum ADD COLUMN pics TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Class ADD COLUMN pics TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Ord ADD COLUMN pics TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Family ADD COLUMN pics TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Genus ADD COLUMN pics TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 10);

        }
        private static async Task _update011(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Gallery(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, UNIQUE(name));", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Picture(id INTEGER PRIMARY KEY AUTOINCREMENT, url TEXT, gallery_id INTEGER, name TEXT, description TEXT, artist TEXT, FOREIGN KEY(gallery_id) REFERENCES Gallery(id), UNIQUE(gallery_id, url));", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 11);

        }
        private static async Task _update012(SQLiteConnection conn) {

            // Initializes the Trophies table for storing records of earned trophies.

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Trophies(user_id INTEGER, trophy_name TEXT, timestamp INTEGER, UNIQUE(user_id, trophy_name));", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 12);

        }
        private static async Task _update013(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Genus ADD COLUMN common_name TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Family ADD COLUMN common_name TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Ord ADD COLUMN common_name TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Class ADD COLUMN common_name TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Phylum ADD COLUMN common_name TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Kingdom ADD COLUMN common_name TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Domain ADD COLUMN common_name TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 13);

        }
        private static async Task _update014(SQLiteConnection conn) {

            // Adds support for zone pictures.

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Zones ADD COLUMN pics TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 14);

        }
        private static async Task _update015(SQLiteConnection conn) {

            // Adds support for periods.

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Period(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, start_ts TEXT, end_ts TEXT, description TEXT, UNIQUE(name));", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 15);

        }
        private static async Task _update016(SQLiteConnection conn) {

            // User ID is now stored alongside usernames.
            // Usernames are saved so users can come and go from the server and their name won't be lost. However, user IDs are still required for the "ownedby" command to work after username changes.

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Species ADD COLUMN user_id INTEGER;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _updateDatabaseVersion(conn, 16);

        }

    }

}