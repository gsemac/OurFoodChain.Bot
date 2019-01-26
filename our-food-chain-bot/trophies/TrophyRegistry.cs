using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.trophies {

    public class TrophyRegistry {

        public static async Task InitializeAsync() {

            await _registerAllAsync();

        }
        public static async Task<long> GetTimesUnlocked(Trophy trophy) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Trophies WHERE trophy_name=$trophy_name;")) {

                cmd.Parameters.AddWithValue("$trophy_name", trophy.GetIdentifier());

                return await Database.GetScalar<long>(cmd);

            }

        }
        public static async Task<UnlockedTrophyInfo[]> GetUnlockedTrophiesAsync(ulong userId) {

            List<UnlockedTrophyInfo> unlocked = new List<UnlockedTrophyInfo>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Trophies WHERE user_id=$user_id;")) {

                cmd.Parameters.AddWithValue("$user_id", userId);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    foreach (DataRow row in rows.Rows) {

                        string trophy_name = row.Field<string>("trophy_name");
                        long times_unlocked = 0;

                        using (SQLiteCommand cmd2 = new SQLiteCommand("SELECT COUNT(*) FROM Trophies WHERE trophy_name=$trophy_name;")) {

                            cmd2.Parameters.AddWithValue("$trophy_name", trophy_name);

                            times_unlocked = await Database.GetScalar<long>(cmd2);

                        }

                        UnlockedTrophyInfo info = new UnlockedTrophyInfo {
                            identifier = trophy_name,
                            timesUnlocked = times_unlocked,
                            timestamp = row.Field<long>("timestamp")
                        };

                        unlocked.Add(info);

                    }

                }

            }

            return unlocked.ToArray();

        }
        public static async Task<Trophy> GetTrophyByIdentifierAsync(string identifier) {

            foreach (Trophy trophy in await GetTrophiesAsync())
                if (trophy.GetIdentifier() == identifier)
                    return trophy;

            return null;

        }
        public static async Task<Trophy> GetTrophyByNameAsync(string name) {

            foreach (Trophy trophy in await GetTrophiesAsync())
                if (trophy.name.ToLower() == name.ToLower())
                    return trophy;

            return null;

        }
        public static async Task SetUnlocked(ulong userId, Trophy trophy) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Trophies(user_id, trophy_name, timestamp) VALUES($user_id, $trophy_name, $timestamp);")) {

                cmd.Parameters.AddWithValue("$user_id", userId);
                cmd.Parameters.AddWithValue("$trophy_name", trophy.GetIdentifier());
                cmd.Parameters.AddWithValue("$timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task<IReadOnlyCollection<Trophy>> GetTrophiesAsync() {

            await InitializeAsync();

            return _registry.AsReadOnly();

        }


        private static List<Trophy> _registry = new List<Trophy>();

        private static async Task _registerAllAsync() {

            // Don't bother if we've already registered the trophies.
            if (_registry.Count > 0)
                return;

            await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Info, "Trophies", "Registering trophies");

            // Creation achievements

            _registry.Add(new Trophy("Polar Power", "Create a species that lives within a zone with a cold climate.", _checkTrophy_polarPower));
            _registry.Add(new Trophy("Heating Up", "Create a species that lives within a zone with a warm climate.", _checkTrophy_heatingUp));
            _registry.Add(new Trophy("Atlantean", "Create a species that lives in water.", _checkTrophy_atlantean));
            _registry.Add(new Trophy("Kiss The Ground", "Create a species that lives on land.", _checkTrophy_kissTheGround));
            _registry.Add(new Trophy("Best of Both Worlds", "Create an amphibious species.", _checkTrophy_bestOfBothWorlds));
            _registry.Add(new Trophy("Hunter", "Create a carnivorous species.", _checkTrophy_hunter));
            _registry.Add(new Trophy("Pacifist", "Create a herbivorous species.", _checkTrophy_pacifist));
            _registry.Add(new Trophy("Basics", "Create a producer species.", _checkTrophy_basics));
            _registry.Add(new Trophy("Death Brings Life", "Create a species that thrives off dead organisms.", _checkTrophy_deathBringsLife));
            _registry.Add(new Trophy("Communism", "Create a species that is eusocial.", TrophyFlags.Hidden, _checkTrophy_Placeholder));
            _registry.Add(new Trophy("All Mine", "Create a species that parasitic.", _checkTrophy_allMine));
            _registry.Add(new Trophy("Together", "Create a species that benefits from mutualism or is eusocial.", TrophyFlags.Hidden, _checkTrophy_Placeholder));
            _registry.Add(new Trophy("Scrap That", "Create an evolution to your own species.", _checkTrophy_scrapThat));
            _registry.Add(new Trophy("Lift Off", "Create a species that can fly.", TrophyFlags.Hidden, _checkTrophy_liftOff));
            _registry.Add(new Trophy("Trademarked", "Create a new genus.", _checkTrophy_trademarked));
            _registry.Add(new Trophy("Mad Scientist", "Create a species that uses chemical defense.", _checkTrophy_Placeholder));
            _registry.Add(new Trophy("Beneath You", "Create a species that burrows or tunnels.", _checkTrophy_beneathYou));
            _registry.Add(new Trophy("Outcast", "Create a species that has a change drastic enough to make it barely stay within its predecessors genus.", _checkTrophy_Placeholder));
            _registry.Add(new Trophy("Centi", "Create a species that has more than 10 legs.", _checkTrophy_Placeholder));

            // Natural event achievements

            _registry.Add(new Trophy("Superior Survivor", "Have a species you own survive an extinction event.", _checkTrophy_Placeholder));
            _registry.Add(new Trophy("Natural Selection", "Have a species you own go extinct.", _checkTrophy_Placeholder));
            _registry.Add(new Trophy("To Infinity And Beyond", "Own a species that spreads to another zone.", _checkTrophy_Placeholder));
            _registry.Add(new Trophy("A New World", "Create a species that spreads across an ocean body.", _checkTrophy_Placeholder));
            _registry.Add(new Trophy("One To Rule Them All", "Create a species that turns into an apex predator.", _checkTrophy_Placeholder));
            _registry.Add(new Trophy("I Am Selection", "Create a species that is the direct cause of another species extinction.", _checkTrophy_Placeholder));

            // One-time achievements

            _registry.Add(new Trophy("Colonization", "Be the first to create a eusocial species.", TrophyFlags.Hidden | TrophyFlags.OneTime, _checkTrophy_Placeholder));
            _registry.Add(new Trophy("Let There Be Light", "Be the first to create a species that makes light.", TrophyFlags.Hidden | TrophyFlags.OneTime, _checkTrophy_Placeholder));
            _registry.Add(new Trophy("Master Of The Skies", "Be the first to create a species capable of flight.", TrophyFlags.Hidden | TrophyFlags.OneTime, _checkTrophy_Placeholder));
            _registry.Add(new Trophy("Did You Hear That?", "Be the first to make a species that makes noise.", TrophyFlags.Hidden | TrophyFlags.OneTime, _checkTrophy_Placeholder));
            _registry.Add(new Trophy("Double Trouble", "Be the first to make a species with two legs.", TrophyFlags.Hidden | TrophyFlags.OneTime, _checkTrophy_Placeholder));
            _registry.Add(new Trophy("Can We Keep It?", "Be the first to create a species with fur.", TrophyFlags.Hidden | TrophyFlags.OneTime, _checkTrophy_Placeholder));
            _registry.Add(new Trophy("Turn On The AC!", "Be the first to create a warm-blooded species.", TrophyFlags.Hidden | TrophyFlags.OneTime, _checkTrophy_Placeholder));
            _registry.Add(new Trophy("Do You See What I See?", "Be the first to create a species with developed eyes.", TrophyFlags.Hidden | TrophyFlags.OneTime, _checkTrophy_Placeholder));

        }

        private static async Task<bool> _checkTrophy_Placeholder(TrophyScanner.ScannerQueueItem item) { return await Task.FromResult(false); }
        private static async Task<bool> _checkTrophy_polarPower(TrophyScanner.ScannerQueueItem item) {
            return await _checkTrophy_helper_hasSpeciesWithZoneDescriptionMatch(item, "frigid|arctic|cold");
        }
        private static async Task<bool> _checkTrophy_heatingUp(TrophyScanner.ScannerQueueItem item) {
            return await _checkTrophy_helper_hasSpeciesWithZoneDescriptionMatch(item, "warm|hot|desert|tropical");
        }
        private static async Task<bool> _checkTrophy_atlantean(TrophyScanner.ScannerQueueItem item) {
            return await _checkTrophy_helper_hasSpeciesWithZoneTypeMatch(item, ZoneType.Aquatic);
        }
        private static async Task<bool> _checkTrophy_kissTheGround(TrophyScanner.ScannerQueueItem item) {
            return await _checkTrophy_helper_hasSpeciesWithZoneTypeMatch(item, ZoneType.Terrestrial);
        }
        private static async Task<bool> _checkTrophy_bestOfBothWorlds(TrophyScanner.ScannerQueueItem item) {

            return await _checkTrophy_helper_hasSpeciesMatchingSQLiteCountQuery(item, @"SELECT COUNT(*) FROM Species WHERE owner=$owner 
                AND id IN(SELECT species_id FROM SpeciesZones WHERE zone_id IN(SELECT id FROM Zones WHERE type =""aquatic""))
                AND id IN(SELECT species_id FROM SpeciesZones WHERE zone_id IN(SELECT id FROM Zones WHERE type =""terrestrial""))");

        }
        private static async Task<bool> _checkTrophy_hunter(TrophyScanner.ScannerQueueItem item) {

            return await _checkTrophy_helper_hasSpeciesMatchingSQLiteCountQuery(item, @"SELECT COUNT(*) FROM Species WHERE owner=$owner
                AND id IN(SELECT species_id FROM SpeciesRoles WHERE role_id IN(SELECT id FROM Roles WHERE name = ""predator"" OR name = ""carnivore""))");

        }
        private static async Task<bool> _checkTrophy_pacifist(TrophyScanner.ScannerQueueItem item) {

            return await _checkTrophy_helper_hasSpeciesMatchingSQLiteCountQuery(item, @"SELECT COUNT(*) FROM Species WHERE owner=$owner
                AND id IN(SELECT species_id FROM SpeciesRoles WHERE role_id IN(SELECT id FROM Roles WHERE name = ""base-consumer"" OR name = ""herbivore""))");

        }
        private static async Task<bool> _checkTrophy_basics(TrophyScanner.ScannerQueueItem item) {

            return await _checkTrophy_helper_hasSpeciesMatchingSQLiteCountQuery(item, @"SELECT COUNT(*) FROM Species WHERE owner=$owner
                AND id IN(SELECT species_id FROM SpeciesRoles WHERE role_id IN(SELECT id FROM Roles WHERE name = ""producer""))");

        }
        private static async Task<bool> _checkTrophy_deathBringsLife(TrophyScanner.ScannerQueueItem item) {

            return await _checkTrophy_helper_hasSpeciesMatchingSQLiteCountQuery(item, @"SELECT COUNT(*) FROM Species WHERE owner=$owner
                AND id IN(SELECT species_id FROM SpeciesRoles WHERE role_id IN(SELECT id FROM Roles WHERE name = ""scavenger"" OR name = ""decomposer"" OR name = ""detritivore""))");

        }
        private static async Task<bool> _checkTrophy_allMine(TrophyScanner.ScannerQueueItem item) {

            return await _checkTrophy_helper_hasSpeciesMatchingSQLiteCountQuery(item, @"SELECT COUNT(*) FROM Species WHERE owner=$owner
                AND id IN(SELECT species_id FROM SpeciesRoles WHERE role_id IN(SELECT id FROM Roles WHERE name = ""parasite""))");

        }
        private static async Task<bool> _checkTrophy_scrapThat(TrophyScanner.ScannerQueueItem item) {

            return await _checkTrophy_helper_hasSpeciesMatchingSQLiteCountQuery(item, @"SELECT COUNT(*) FROM Species WHERE owner=$owner
	            AND id IN (SELECT ancestor_id FROM Ancestors WHERE species_id IN (SELECT id FROM Species WHERE owner=$owner))");

        }
        private static async Task<bool> _checkTrophy_liftOff(TrophyScanner.ScannerQueueItem item) {

            return await _checkTrophy_helper_hasSpeciesMatchingSQLiteCountQuery(item,
                @"SELECT COUNT(*) FROM Species WHERE owner=$owner AND description LIKE ""%can fly%"" OR description LIKE ""%flies%""");

        }
        private static async Task<bool> _checkTrophy_trademarked(TrophyScanner.ScannerQueueItem item) {

            return await _checkTrophy_helper_hasSpeciesMatchingSQLiteCountQuery(item,
                @"SELECT COUNT(*) FROM (SELECT owner, genus_id, MIN(timestamp) FROM Species GROUP BY genus_id) WHERE owner = $owner");

        }
        private static async Task<bool> _checkTrophy_beneathYou(TrophyScanner.ScannerQueueItem item) {

            return await _checkTrophy_helper_hasSpeciesMatchingSQLiteCountQuery(item,
                @"SELECT COUNT(*) FROM Species WHERE owner=$owner AND description LIKE ""%burrow%"";");

        }

        private static async Task<bool> _checkTrophy_helper_hasSpeciesMatchingSQLiteCountQuery(TrophyScanner.ScannerQueueItem item, string query) {

            using (SQLiteCommand cmd = new SQLiteCommand(query)) {

                cmd.Parameters.AddWithValue("$owner", (await item.context.Guild.GetUserAsync(item.userId)).Username);

                if (await Database.GetScalar<long>(cmd) > 0)
                    return true;

            }

            return false;

        }
        private static async Task<bool> _checkTrophy_helper_hasSpeciesWithZoneDescriptionMatch(TrophyScanner.ScannerQueueItem item, string regexPattern) {

            // Get all zones.
            List<Zone> zones = new List<Zone>(await BotUtils.GetZonesFromDb());

            // Filter list so we only have zones with cold climates.
            zones.RemoveAll(zone => !Regex.IsMatch(zone.description, regexPattern));

            // Check if the user has any species in these zones.

            string username = (await item.context.Guild.GetUserAsync(item.userId)).Username;
            bool unlocked = false;

            foreach (Zone zone in zones) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM SpeciesZones WHERE zone_id=$zone_id AND species_id IN (SELECT id FROM Species WHERE owner=$owner);")) {

                    cmd.Parameters.AddWithValue("$zone_id", zone.id);
                    cmd.Parameters.AddWithValue("$owner", username);

                    if (await Database.GetScalar<long>(cmd) > 0) {

                        unlocked = true;
                        break;

                    }

                }

            }

            return unlocked;

        }
        private static async Task<bool> _checkTrophy_helper_hasSpeciesWithZoneTypeMatch(TrophyScanner.ScannerQueueItem item, ZoneType type) {

            string type_string = "";

            switch (type) {
                case ZoneType.Aquatic:
                    type_string = "aquatic";
                    break;
                case ZoneType.Terrestrial:
                    type_string = "terrestrial";
                    break;
                default:
                    return false;
            }

            string username = (await item.context.Guild.GetUserAsync(item.userId)).Username;
            bool unlocked = false;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Species WHERE owner=$owner AND id IN (SELECT species_id FROM SpeciesZones WHERE zone_id IN (SELECT id FROM Zones WHERE type=$type))")) {

                cmd.Parameters.AddWithValue("$owner", username);
                cmd.Parameters.AddWithValue("$type", type_string);

                if (await Database.GetScalar<long>(cmd) > 0)
                    return true;

            }

            return unlocked;

        }

    }

}