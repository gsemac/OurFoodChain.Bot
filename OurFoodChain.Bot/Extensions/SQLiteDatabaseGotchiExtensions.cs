using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Debug;
using OurFoodChain.Gotchis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Extensions {

    public static class SQLiteDatabaseGotchiExtensions {

        // Public members

        public static async Task AddGotchiAsync(this SQLiteDatabase database, ICreator user, ISpecies species) {

            // We need to generate a name for this Gotchi that doesn't already exist for this user.

            string name = GenerateGotchiName(user, species);

            while (!(await database.GetGotchiAsync(user.UserId, name) is null))
                name = GenerateGotchiName(user, species);

            // Add the Gotchi to the database.

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Gotchi(species_id, name, owner_id, fed_ts, born_ts, died_ts, evolved_ts) VALUES($species_id, $name, $owner_id, $fed_ts, $born_ts, $died_ts, $evolved_ts)")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);
                cmd.Parameters.AddWithValue("$owner_id", user.UserId);
                cmd.Parameters.AddWithValue("$name", name.ToLower());
                cmd.Parameters.AddWithValue("$fed_ts", ts - 60 * 60); // subtract an hour to keep it from eating immediately after creation
                cmd.Parameters.AddWithValue("$born_ts", ts);
                cmd.Parameters.AddWithValue("$died_ts", 0);
                cmd.Parameters.AddWithValue("$evolved_ts", ts);

                await Database.ExecuteNonQuery(cmd);

            }

            // Set this gotchi as the user's primary Gotchi.

            GotchiUserInfo user_data = await database.GetUserInfoAsync(user);

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT id FROM Gotchi WHERE owner_id = $owner_id AND born_ts = $born_ts")) {

                cmd.Parameters.AddWithValue("$owner_id", user.UserId);
                cmd.Parameters.AddWithValue("$born_ts", ts);

                user_data.PrimaryGotchiId = await database.GetScalarAsync<long>(cmd);

            }

            await database.UpdateUserInfoAsync(user_data);

        }

        public static async Task<Gotchi> GetGotchiAsync(this SQLiteDatabase database, ICreator creator) {

            GotchiUserInfo userData = await database.GetUserInfoAsync(creator);

            // Get this user's primary Gotchi.

            Gotchi gotchi = await database.GetGotchiAsync(userData.PrimaryGotchiId);

            // If this user's primary gotchi doesn't exist (either it was never set or no longer exists), pick a primary gotchi from their current gotchis.

            if (gotchi is null) {

                IEnumerable<Gotchi> gotchis = await database.GetGotchisAsync(userData.UserId);

                if (gotchis.Count() > 0) {

                    gotchi = gotchis.First();

                    userData.PrimaryGotchiId = gotchi.Id;

                    await database.UpdateUserInfoAsync(userData);

                }

            }

            return gotchi;

        }
        public static async Task<Gotchi> GetGotchiAsync(this SQLiteDatabase database, long gotchiId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gotchi WHERE id = $id;")) {

                cmd.Parameters.AddWithValue("$id", gotchiId);

                DataRow row = await database.GetRowAsync(cmd);

                return row is null ? null : CreateGotchFromDataRow(row);

            }

        }
        public static async Task<Gotchi> GetGotchiAsync(this SQLiteDatabase database, ulong? userId, string name) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gotchi WHERE owner_id = $owner_id AND name = $name;")) {

                cmd.Parameters.AddWithValue("$owner_id", userId);
                cmd.Parameters.AddWithValue("$name", name.ToLower());

                DataRow row = await database.GetRowAsync(cmd);

                return row is null ? null : CreateGotchFromDataRow(row);

            }

        }
        public static async Task<IEnumerable<Gotchi>> GetGotchisAsync(this SQLiteDatabase database) {

            List<Gotchi> gotchis = new List<Gotchi>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gotchi"))
                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    gotchis.Add(CreateGotchFromDataRow(row));

            return gotchis;

        }
        public static async Task<IEnumerable<Gotchi>> GetGotchisAsync(this SQLiteDatabase database, ulong userId) {

            List<Gotchi> gotchis = new List<Gotchi>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gotchi WHERE owner_id = $owner_id ORDER BY born_ts ASC;")) {

                cmd.Parameters.AddWithValue("$owner_id", userId);

                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    gotchis.Add(CreateGotchFromDataRow(row));

            }

            return gotchis;

        }

        public static async Task SetGotchiNameAsync(this SQLiteDatabase database, Gotchi gotchi, string name) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET name = $name WHERE owner_id = $owner_id AND id = $id")) {

                cmd.Parameters.AddWithValue("$name", name.ToLowerInvariant());
                cmd.Parameters.AddWithValue("$owner_id", gotchi.OwnerId);
                cmd.Parameters.AddWithValue("$id", gotchi.Id);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }

        public static async Task<int> FeedGotchisAsync(this SQLiteDatabase database, GotchiContext context, ulong userId) {

            // Although we only display the state of the primary Gotchi at the moment, update the feed time for all Gotchis owned by this user.
            // Only Gotchis that are still alive (i.e. have been fed recently enough) get their timestamp updated.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET fed_ts = $fed_ts WHERE owner_id = $owner_id AND fed_ts >= $min_ts")) {

                cmd.Parameters.AddWithValue("$fed_ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                cmd.Parameters.AddWithValue("$owner_id", userId);
                cmd.Parameters.AddWithValue("$min_ts", context.MinimumFedTimestamp());

                return await database.ExecuteNonQueryAsync(cmd);

            }

        }

        public static async Task<bool> EvolveAndUpdateGotchiAsync(this SQLiteDatabase database, Gotchi gotchi) {

            return await database.EvolveAndUpdateGotchiAsync(gotchi, string.Empty);

        }
        public static async Task<bool> EvolveAndUpdateGotchiAsync(this SQLiteDatabase database, Gotchi gotchi, string desiredEvo) {

            bool evolved = false;

            if (string.IsNullOrEmpty(desiredEvo)) {

                // Find all descendatants of this species.

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT species_id FROM Ancestors WHERE ancestor_id=$ancestor_id;")) {

                    List<long> descendant_ids = new List<long>();

                    cmd.Parameters.AddWithValue("$ancestor_id", gotchi.SpeciesId);

                    foreach (DataRow row in await database.GetRowsAsync(cmd))
                        descendant_ids.Add(row.Field<long>("species_id"));

                    // Pick an ID at random.

                    if (descendant_ids.Count > 0) {

                        gotchi.SpeciesId = descendant_ids[NumberUtilities.GetRandomInteger(descendant_ids.Count)];

                        evolved = true;

                    }

                }

            }
            else {

                // Get the desired evo.
                IEnumerable<ISpecies> sp = await database.GetSpeciesAsync(desiredEvo);

                if (sp is null || sp.Count() != 1)
                    return false;

                // Ensure that the species evolves into the desired evo.

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Ancestors WHERE ancestor_id = $ancestor_id AND species_id = $species_id")) {

                    cmd.Parameters.AddWithValue("$ancestor_id", gotchi.SpeciesId);
                    cmd.Parameters.AddWithValue("$species_id", sp.First().Id);

                    if (await Database.GetScalar<long>(cmd) <= 0)
                        return false;

                    gotchi.SpeciesId = (long)sp.First().Id;

                    evolved = true;

                }

            }

            // Update the gotchi in the database.

            if (evolved) {

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET species_id=$species_id, evolved_ts=$evolved_ts WHERE id=$id;")) {

                    cmd.Parameters.AddWithValue("$species_id", gotchi.SpeciesId);

                    // The "last evolved" timestamp is now only updated in the event the gotchi evolves (in order to make the "IsEvolved" check work correctly).
                    // Note that this means that the background service will attempt to evolve the gotchi at every iteration (unless it evolves by leveling).

                    cmd.Parameters.AddWithValue("$evolved_ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    cmd.Parameters.AddWithValue("$id", gotchi.Id);

                    await database.ExecuteNonQueryAsync(cmd);

                }

            }

            return evolved;

        }

        public static async Task<BattleGotchi> GenerateGotchiAsync(this SQLiteDatabase database, GotchiGenerationParameters parameters) {

            BattleGotchi result = new BattleGotchi();

            if (!(parameters.Base is null)) {

                // If a base gotchi was provided, copy over some of its characteristics.

                result.Gotchi.BornTimestamp = parameters.Base.BornTimestamp;
                result.Gotchi.DiedTimestamp = parameters.Base.DiedTimestamp;
                result.Gotchi.EvolvedTimestamp = parameters.Base.EvolvedTimestamp;
                result.Gotchi.FedTimestamp = parameters.Base.FedTimestamp;

            }

            // Select a random base species to start with.

            ISpecies species = parameters.Species;

            if (species is null) {

                IEnumerable<ISpecies> base_species_list = await database.GetBaseSpeciesAsync();

                if (base_species_list.Count() > 0)
                    species = base_species_list.ElementAt(NumberUtilities.GetRandomInteger(0, base_species_list.Count()));

            }

            if (species != null)
                result.Gotchi.SpeciesId = (long)species.Id;

            // Evolve it the given number of times.

            for (int i = 0; i < parameters.MaxEvolutions; ++i)
                if (!await database.EvolveAndUpdateGotchiAsync(result.Gotchi))
                    break;

            // Generate stats (if applicable).

            if (parameters.GenerateStats) {

                result.Gotchi.Experience = GotchiExperienceCalculator.ExperienceToLevel(result.Stats.ExperienceGroup, NumberUtilities.GetRandomInteger(parameters.MinLevel, parameters.MaxLevel + 1));

                result.Stats = await new GotchiStatsCalculator(Global.GotchiContext).GetStatsAsync(result.Gotchi);

            }

            // Generate moveset (if applicable).

            if (parameters.GenerateMoveset)
                result.Moves = await Global.GotchiContext.MoveRegistry.GetMoveSetAsync(result.Gotchi);

            // Generate types.
            result.Types = await Global.GotchiContext.TypeRegistry.GetTypesAsync(result.Gotchi);

            // Generate a name for the gotchi.

            result.Gotchi.Name = (species is null ? "Wild Gotchi" : species.GetShortName()) + string.Format(" (Lv. {0})", result.Stats is null ? 1 : result.Stats.Level);

            return result;

        }

        public static async Task DeleteGotchiAsync(this SQLiteDatabase database, long gotchiId) {

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Gotchi WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", gotchiId);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }

        public static async Task<GotchiUserInfo> GetUserInfoAsync(this SQLiteDatabase database, ICreator creator) {

            return await database.GetUserInfoAsync(creator.UserId);

        }
        public static async Task<GotchiUserInfo> GetUserInfoAsync(this SQLiteDatabase database, ulong? userId) {

            if (userId.HasValue) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM GotchiUser WHERE user_id = $user_id")) {

                    cmd.Parameters.AddWithValue("$user_id", userId);

                    DataRow row = await database.GetRowAsync(cmd);

                    if (row != null)
                        return CreateGotchiUserInfoFromDataRow(row);

                }

            }

            // If the user is not yet in the database, return a default user object.
            return new GotchiUserInfo(userId);

        }
        public static async Task<long> GetGotchiCountAsync(this SQLiteDatabase database, ICreator creator) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Gotchi WHERE owner_id = $user_id")) {

                cmd.Parameters.AddWithValue("$user_id", creator.UserId);

                long count = await database.GetScalarAsync<long>(cmd);

                return count;

            }

        }
        public static async Task UpdateUserInfoAsync(this SQLiteDatabase database, GotchiUserInfo userInfo) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO GotchiUser(user_id, g, gotchi_limit, primary_gotchi_id) VALUES ($user_id, $g, $gotchi_limit, $primary_gotchi_id)")) {

                cmd.Parameters.AddWithValue("$user_id", userInfo.UserId);
                cmd.Parameters.AddWithValue("$g", userInfo.G);
                cmd.Parameters.AddWithValue("$gotchi_limit", userInfo.GotchiLimit);
                cmd.Parameters.AddWithValue("$primary_gotchi_id", userInfo.PrimaryGotchiId);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }

        public static async Task<string> CreateGotchiGifAsync(this SQLiteDatabase database, Gotchi gotchi) {

            GotchiGifCreatorParams p = new GotchiGifCreatorParams {
                gotchi = gotchi,
                auto = true
            };

            return await CreateGotchiGifAsync(new GotchiGifCreatorParams[] { p },
                new GotchiGifCreatorExtraParams { backgroundFileName = await GetGotchiBackgroundFilenameAsync(database, gotchi) });

        }

        public static async Task SetViewedTimestampAsync(this SQLiteDatabase database, Gotchi gotchi, long viewedTimestamp) {

            await database.SetViewedTimestampAsync(gotchi.Id, viewedTimestamp);

        }
        public static async Task SetViewedTimestampAsync(this SQLiteDatabase database, long gotchiId, long viewedTimestamp) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET viewed_ts = $viewed_ts WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", gotchiId);
                cmd.Parameters.AddWithValue("$viewed_ts", viewedTimestamp);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }

        public static async Task<GotchiInventory> GetInventoryAsync(this SQLiteDatabase database, ulong userId) {

            List<GotchiInventoryItem> items = new List<GotchiInventoryItem>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM GotchiInventory WHERE user_id = $user_id")) {

                cmd.Parameters.AddWithValue("$user_id", userId);

                foreach (DataRow row in await database.GetRowsAsync(cmd)) {

                    items.Add(new GotchiInventoryItem {
                        Item = await GotchiUtils.GetGotchiItemAsync(row.Field<long>("item_id")),
                        Count = row is null ? 0 : row.Field<long>("count")
                    });

                }

            }

            return new GotchiInventory(items.OrderBy(x => x.Item.Id));

        }
        public static async Task<GotchiInventoryItem> AddItemToInventoryAsync(this SQLiteDatabase database, ulong userId, GotchiItem item, long count) {

            // We could just increment the "count" field, but the user is not guaranteed to already have the item.

            GotchiInventoryItem inventoryItem = await database.GetItemFromInventoryAsync(userId, item);

            inventoryItem.Count = Math.Max(0, inventoryItem.Count + count);

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO GotchiInventory(user_id, item_id, count) VALUES($user_id, $item_id, $count)")) {

                cmd.Parameters.AddWithValue("$user_id", userId);
                cmd.Parameters.AddWithValue("$item_id", item is null ? GotchiItem.NullId : item.Id);
                cmd.Parameters.AddWithValue("$count", inventoryItem.Count);

                await database.ExecuteNonQueryAsync(cmd);

            }

            return inventoryItem;

        }
        public static async Task<GotchiInventoryItem> GetItemFromInventoryAsync(this SQLiteDatabase database, ulong userId, GotchiItem item) {

            return await database.GetItemFromInventoryAsync(userId, (GotchiItemId)item.Id);

        }
        public static async Task<GotchiInventoryItem> GetItemFromInventoryAsync(this SQLiteDatabase database, ulong userId, GotchiItemId itemId) {

            return (await database.GetInventoryAsync(userId))
                .Where(i => i.Item.Id == (int)itemId)
                .FirstOrDefault() ?? new GotchiInventoryItem { Item = await GotchiUtils.GetGotchiItemAsync((long)itemId), Count = 0 };

        }

        // Private members

        private static GotchiUserInfo CreateGotchiUserInfoFromDataRow(DataRow row) {

            return new GotchiUserInfo((ulong)row.Field<long>("user_id")) {
                G = row.Field<long>("g"),
                GotchiLimit = row.Field<long>("gotchi_limit"),
                PrimaryGotchiId = row.Field<long>("primary_gotchi_id")
            };

        }
        private static Gotchi CreateGotchFromDataRow(DataRow row) {

            Gotchi result = new Gotchi() {

                Id = row.Field<long>("id"),
                SpeciesId = row.Field<long>("species_id"),
                Name = row.Field<string>("name"),
                OwnerId = (ulong)row.Field<long>("owner_id"),
                FedTimestamp = row.Field<long>("fed_ts"),
                BornTimestamp = row.Field<long>("born_ts"),
                DiedTimestamp = row.Field<long>("died_ts"),
                EvolvedTimestamp = row.Field<long>("evolved_ts")

            };

            if (!row.IsNull("exp"))
                result.Experience = (int)row.Field<double>("exp");

            if (!row.IsNull("viewed_ts"))
                result.ViewedTimestamp = row.Field<long>("viewed_ts");

            if (row.Table.Columns.Contains("level") && !row.IsNull("level")) {

                // Level is calculated based off of total EXP now, but if level data exists, use it.

                result.Experience += GotchiExperienceCalculator.ExperienceToLevel(ExperienceGroup.Default, (int)Math.Max(1, row.Field<long>("level")));

            }

            return result;

        }

        private static async Task<string> CreateGotchiGifAsync(GotchiGifCreatorParams[] gifParams, GotchiGifCreatorExtraParams extraParams) {

            // Create the temporary directory where the GIF will be saved.

            string tempDirectory = Constants.TempDirectory + "gotchi";

            if (!System.IO.Directory.Exists(tempDirectory))
                System.IO.Directory.CreateDirectory(tempDirectory);

            // Get the image for this gotchi.

            Dictionary<long, Bitmap> gotchiImages = new Dictionary<long, Bitmap>();
            List<string> gotchiImagePaths = new List<string>();

            foreach (GotchiGifCreatorParams p in gifParams) {

                string gotchiImagePath = await DownloadGotchiImageAsync(p.gotchi);

                gotchiImagePaths.Add(gotchiImagePath);
                gotchiImages[p.gotchi.Id] = GraphicsUtils.TryCreateBitmap(gotchiImagePath);

            }

            // Create the gotchi GIF.

            string outputPath = System.IO.Path.Combine(tempDirectory, string.Format("{0}.gif", StringUtilities.GetMD5(string.Join("", gotchiImagePaths))));

            using (GotchiGifCreator gif = new GotchiGifCreator()) {

                string backgroundFilename = System.IO.Path.Combine(Constants.GotchiImagesDirectory, extraParams.backgroundFileName);

                if (System.IO.File.Exists(backgroundFilename))
                    gif.SetBackground(backgroundFilename);

                foreach (GotchiGifCreatorParams p in gifParams) {

                    if (p.auto)
                        gif.AddGotchi(gotchiImages[p.gotchi.Id], p.gotchi.State);
                    else
                        gif.AddGotchi(p.x, p.y, gotchiImages[p.gotchi.Id], p.state);

                }

                gif.Save(outputPath, extraParams.overlay);

            }

            // Free all bitmaps.

            foreach (long key in gotchiImages.Keys)
                if (!(gotchiImages[key] is null))
                    gotchiImages[key].Dispose();

            return outputPath;

        }
        private static async Task<string> DownloadGotchiImageAsync(Gotchi gotchi) {

            // Get the species.

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.SpeciesId);

            if (sp is null)
                return string.Empty;

            // Download the gotchi image if possible.

            string gotchi_pic = Constants.GotchiImagesDirectory + "default.png";

            if (!string.IsNullOrEmpty(sp.Picture) && (!Global.GotchiContext.Config.ImageWhitelistEnabled || Regex.Match(sp.Picture, @"^https:\/\/.+?\.discordapp\.(?:com|net)\/.+?\.(?:jpg|png)(?:\?.+)?$", RegexOptions.IgnoreCase).Success)) {

                string downloads_dir = Constants.TempDirectory + "downloads";
                string ext = Regex.Match(sp.Picture, @"(\.(?:jpg|png))(?:\?.+)?$", RegexOptions.IgnoreCase).Groups[1].Value;
                string disk_fpath = System.IO.Path.Combine(downloads_dir, StringUtilities.GetMD5(sp.Picture) + ext);

                if (!System.IO.Directory.Exists(downloads_dir))
                    System.IO.Directory.CreateDirectory(downloads_dir);

                try {

                    if (!System.IO.File.Exists(disk_fpath))
                        using (System.Net.WebClient client = new System.Net.WebClient())
                            await client.DownloadFileTaskAsync(new Uri(sp.Picture), disk_fpath);

                    if (System.IO.File.Exists(disk_fpath))
                        gotchi_pic = disk_fpath;

                }
                catch (Exception ex) {

                    // We'll just keep using the default picture if this happens.

                    Console.WriteLine(new LogMessage(LogSeverity.Error, "gotchi", string.Format("Error occurred when loading gotchi image: {0}\n{1}", sp.Picture, ex.ToString())).ToString());

                }

            }

            return gotchi_pic;

        }
        private static async Task<string> GetGotchiBackgroundFilenameAsync(SQLiteDatabase database, Gotchi gotchi, string defaultFileName = "home_aquatic.png") {

            // Returns a background image based on the gotchi passed in (i.e., corresponding to the zone it resides in).

            if (!(gotchi is null) && gotchi.SpeciesId > 0) {

                IEnumerable<ISpeciesZoneInfo> zones = await database.GetZonesAsync(gotchi.SpeciesId);

                foreach (IZone zone in zones.Select(info => info.Zone)) {

                    string candidate_filename = string.Format("home_{0}.png", StringUtilities.ReplaceWhitespaceCharacters(zone.GetFullName().ToLower()));

                    if (System.IO.File.Exists(Constants.GotchiImagesDirectory + candidate_filename))
                        return candidate_filename;

                }

            }

            return defaultFileName;

        }

        public static string GenerateGotchiName(ICreator user, ISpecies species) {

            List<string> name_options = new List<string>();

            // Add the default name first.
            name_options.Add(string.Format("{0} Jr.", user.Name));

            // We'll generate some names using some portion of the species name.
            // For example, "gigas" might generate "gi-gi", "mr. giga", or "giga".

            string species_name = species.Name;
            MatchCollection vowel_matches = Regex.Matches(species_name, "[aeiou]");
            string[] roots = vowel_matches.Cast<Match>().Where(x => x.Index > 1).Select(x => species_name.Substring(0, x.Index + 1)).ToArray();

            for (int i = 0; i < 2; ++i) {

                string name = roots[BotUtils.RandomInteger(roots.Count())];

                if (BotUtils.RandomInteger(2) == 0)
                    name = name.Substring(0, name.Length - 1); // cut off the last vowel

                if (BotUtils.RandomInteger(2) == 0 && name.Length <= 5)
                    name += "-" + name;

                if (BotUtils.RandomInteger(2) == 0 && name.Length > 1 && (name.Last() == 'r' || name.Last() == 't'))
                    name += "y";

                if (BotUtils.RandomInteger(2) == 0)
                    name = (new string[] { "Mr.", "Sir" }).Random() + " " + name;

                name_options.Add(name);

            }

            return name_options.Select(x => StringUtilities.ToTitleCase(x)).ToArray()[BotUtils.RandomInteger(name_options.Count())];

        }

    }

}