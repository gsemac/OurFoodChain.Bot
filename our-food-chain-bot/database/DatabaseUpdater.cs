using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class DatabaseUpdater {

        public const int LATEST_VERSION = 23;

        public DatabaseUpdater(SQLiteConnection conn) {
            Connection = conn;
        }

        public SQLiteConnection Connection { get; set; } = null;

        public async Task<int> GetDatabaseVersionAsync() {

            Debug.Assert(Connection != null);

            // Create metadata table if it doesn't already exist.
            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Meta (version INTEGER);", Connection))
                await cmd.ExecuteNonQueryAsync();

            // Get the current table version from the database. If the query was successful, return the result.
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT version FROM Meta;", Connection))
            using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                    return reader.GetInt32(0);

            // If no version information exists, insert it.
            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Meta VALUES (0);", Connection))
                await cmd.ExecuteNonQueryAsync();

            // Return 0 to indicate the database has not yet been initialized.
            return 0;

        }
        public async Task<int> UpdateToLatestVersionAsync() {

            Debug.Assert(Connection != null);

            int version = await GetDatabaseVersionAsync();

            // Update the database to the latest version.

            for (int i = ++version; i <= LATEST_VERSION; ++i)
                await _applyDatabaseUpdateAsync(Connection, i);

            return LATEST_VERSION;

        }

        private static async Task _setDatabaseVersionAsync(SQLiteConnection conn, int versionNumber) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Meta SET version = $version WHERE version < $version;", conn)) {

                cmd.Parameters.AddWithValue("$version", versionNumber);

                await cmd.ExecuteNonQueryAsync();

            }

        }
        private static async Task _applyDatabaseUpdateAsync(SQLiteConnection conn, int versionNumber) {

            if (versionNumber <= 0)
                return;

            await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Info, "Database", string.Format("Updating database to version {0}", versionNumber));

            switch (versionNumber) {

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

                case 17:
                    await _update017(conn);
                    break;

                case 18:
                    await _update018(conn);
                    break;

                case 19:
                    await _update019(conn);
                    break;

                case 20:
                    await _update020(conn);
                    break;

                case 21:
                    await _update021(conn);
                    break;

                case 22:
                    await _update022(conn);
                    break;

                case 23:
                    await _update023(conn);
                    break;

            }

            await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Info, "Database", string.Format("Updated database to version {0}", versionNumber));

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

            await _setDatabaseVersionAsync(conn, 1);

        }
        private static async Task _update002(SQLiteConnection conn) {

            // Add the "pics" field to the "Species" table.
            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Species ADD COLUMN pics TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 2);

        }
        private static async Task _update003(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Species ADD COLUMN owner TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 3);

        }
        private static async Task _update004(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Species ADD COLUMN common_name TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 4);

        }
        private static async Task _update005(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Predates(species_id INTEGER, eats_id INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id), FOREIGN KEY(eats_id) REFERENCES Species(id), PRIMARY KEY(species_id, eats_id));", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 5);

        }
        private static async Task _update006(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Predates ADD COLUMN notes TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 6);

        }
        private static async Task _update007(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Roles(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE, description TEXT);", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS SpeciesRoles(species_id INTEGER, role_id INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id), FOREIGN KEY(role_id) REFERENCES Roles(id), PRIMARY KEY(species_id, role_id));", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 7);

        }
        private static async Task _update008(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE SpeciesRoles ADD COLUMN notes TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 8);

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

            await _setDatabaseVersionAsync(conn, 9);

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

            await _setDatabaseVersionAsync(conn, 10);

        }
        private static async Task _update011(SQLiteConnection conn) {

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Gallery(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, UNIQUE(name));", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Picture(id INTEGER PRIMARY KEY AUTOINCREMENT, url TEXT, gallery_id INTEGER, name TEXT, description TEXT, artist TEXT, FOREIGN KEY(gallery_id) REFERENCES Gallery(id), UNIQUE(gallery_id, url));", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 11);

        }
        private static async Task _update012(SQLiteConnection conn) {

            // Initializes the Trophies table for storing records of earned trophies.

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Trophies(user_id INTEGER, trophy_name TEXT, timestamp INTEGER, UNIQUE(user_id, trophy_name));", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 12);

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

            await _setDatabaseVersionAsync(conn, 13);

        }
        private static async Task _update014(SQLiteConnection conn) {

            // Adds support for zone pictures.

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Zones ADD COLUMN pics TEXT;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 14);

        }
        private static async Task _update015(SQLiteConnection conn) {

            // Adds support for periods.

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Period(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, start_ts TEXT, end_ts TEXT, description TEXT, UNIQUE(name));", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 15);

        }
        private static async Task _update016(SQLiteConnection conn) {

            // User ID is now stored alongside usernames.
            // Usernames are saved so users can come and go from the server and their name won't be lost. However, user IDs are still required for the "ownedby" command to work after username changes.

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Species ADD COLUMN user_id INTEGER;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 16);

        }
        private static async Task _update017(SQLiteConnection conn) {

            // Adds support for Relationships.

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Relationships(id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, description TEXT, UNIQUE(name));", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS SpeciesRelationships(species1_id INTEGER, species2_id INTEGER, relationship_id INTEGER, FOREIGN KEY(species1_id) REFERENCES Species(id), FOREIGN KEY(species2_id) REFERENCES Species(id), FOREIGN KEY(relationship_id) REFERENCES Relationships(id), UNIQUE(species1_id, species2_id));", conn))
                await cmd.ExecuteNonQueryAsync();

            // Insert default relationships.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Relationships(name, description) VALUES(\"parasitism\", \"A relationship where one organism lives in or on another, receiving benefits at the expense of the host.\")", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Relationships(name, description) VALUES(\"mutualism\", \"A relationship where both organisms benefit from interacting with the other.\")", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Relationships(name, description) VALUES(\"commensalism\", \"A relationship where one organism benefits from interacting with another, while the other organism is unaffected.\")", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 17);

        }
        private static async Task _update018(SQLiteConnection conn) {

            // The following used to occur when a gotchi was created or accessed, so the table didn't exist if no users used the gotchi features.
            // Since gotchi features have been extended since this was added (battles, trades, etc.), it's safer to ensure that the table always exists.
            // This is especially important, since some updates (such as #18 and #19) assume that the table already exists.

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Gotchi(id INTEGER PRIMARY KEY AUTOINCREMENT, species_id INTEGER, name TEXT, owner_id INTEGER, fed_ts INTEGER, born_ts INTEGER, died_ts INTEGER, evolved_ts INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id));", conn))
                await cmd.ExecuteNonQueryAsync();

            // Adds fields required for gotchi battles.

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Gotchi ADD COLUMN level INTEGER;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Gotchi ADD COLUMN exp REAL;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 18);

        }
        private static async Task _update019(SQLiteConnection conn) {

            // Adds fields related to gotchi training.

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Gotchi ADD COLUMN training_ts INTEGER;", conn))
                await cmd.ExecuteNonQueryAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE Gotchi ADD COLUMN training_left INTEGER;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 19);

        }
        private static async Task _update020(SQLiteConnection conn) {

            // Add support for Favorites.

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Favorites(user_id INTEGER, species_id INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id), UNIQUE(user_id, species_id));", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 20);

        }
        private static async Task _update021(SQLiteConnection conn) {

            // Adds support for storing Gotchi user information (currency, gotchi limit, etc.).

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS GotchiUser(user_id INTEGER PRIMARY KEY, g INTEGER, gotchi_limit INTEGER, primary_gotchi_id INTEGER)", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 21);

        }
        private static async Task _update022(SQLiteConnection conn) {

            // Adds support for multiple common names for species.

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS SpeciesCommonNames(species_id INTEGER, name TEXT, FOREIGN KEY(species_id) REFERENCES Species(id), UNIQUE(species_id, name))", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 22);

        }
        private static async Task _update023(SQLiteConnection conn) {

            // Adds timestamps for species zone additions.

            using (SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE SpeciesZones ADD COLUMN timestamp INTEGER;", conn))
                await cmd.ExecuteNonQueryAsync();

            await _setDatabaseVersionAsync(conn, 23);

        }

    }

}