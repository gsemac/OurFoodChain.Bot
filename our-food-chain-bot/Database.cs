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

                if (version <= 0) {

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

                if (version < 2)
                    await _update002(conn);
                if (version < 3)
                    await _update003(conn);
                if (version < 4)
                    await _update004(conn);
                if (version < 5)
                    await _update005(conn);
                if (version < 6)
                    await _update006(conn);
                if (version < 7)
                    await _update007(conn);
                if (version < 8)
                    await _update008(conn);
                if (version < 9)
                    await _update009(conn);
                if (version < 10)
                    await _update010(conn);

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

        private static async Task _update002(SQLiteConnection conn) {

            await _updateDatabaseVersion(conn, 2);

            // Add the "pics" field to the "Species" table.
            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Species ADD COLUMN pics TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

        }
        private static async Task _update003(SQLiteConnection conn) {

            await _updateDatabaseVersion(conn, 3);

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Species ADD COLUMN owner TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

        }
        private static async Task _update004(SQLiteConnection conn) {

            await _updateDatabaseVersion(conn, 4);

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Species ADD COLUMN common_name TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

        }
        private static async Task _update005(SQLiteConnection conn) {

            await _updateDatabaseVersion(conn, 5);

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Predates(species_id INTEGER, eats_id INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id), FOREIGN KEY(eats_id) REFERENCES Species(id), PRIMARY KEY(species_id, eats_id));", conn))
                await cmd.ExecuteNonQueryAsync();

        }
        private static async Task _update006(SQLiteConnection conn) {

            await _updateDatabaseVersion(conn, 6);

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Predates ADD COLUMN notes TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

        }
        private static async Task _update007(SQLiteConnection conn) {

            await _updateDatabaseVersion(conn, 7);

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Roles(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE, description TEXT);", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS SpeciesRoles(species_id INTEGER, role_id INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id), FOREIGN KEY(role_id) REFERENCES Roles(id), PRIMARY KEY(species_id, role_id));", conn))
                await cmd.ExecuteNonQueryAsync();

        }
        private static async Task _update008(SQLiteConnection conn) {

            await _updateDatabaseVersion(conn, 8);

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE SpeciesRoles ADD COLUMN notes TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

        }
        private static async Task _update009(SQLiteConnection conn) {

            await _updateDatabaseVersion(conn, 9);

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

        }
        private static async Task _update010(SQLiteConnection conn) {

            await _updateDatabaseVersion(conn, 10);

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

        }

    }

}