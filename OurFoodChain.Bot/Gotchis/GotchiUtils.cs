using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public class GotchiUtils {

        public static async Task CreateGotchiAsync(IUser user, Species species) {

            // We need to generate a name for this Gotchi that doesn't already exist for this user.

            string name = GenerateGotchiName(user, species);

            while (!(await GetGotchiAsync(user.Id, name) is null))
                name = GenerateGotchiName(user, species);

            // Add the Gotchi to the database.

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Gotchi(species_id, name, owner_id, fed_ts, born_ts, died_ts, evolved_ts) VALUES($species_id, $name, $owner_id, $fed_ts, $born_ts, $died_ts, $evolved_ts);")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);
                cmd.Parameters.AddWithValue("$owner_id", user.Id);
                cmd.Parameters.AddWithValue("$name", name.ToLower());
                cmd.Parameters.AddWithValue("$fed_ts", ts - 60 * 60); // subtract an hour to keep it from eating immediately after creation
                cmd.Parameters.AddWithValue("$born_ts", ts);
                cmd.Parameters.AddWithValue("$died_ts", 0);
                cmd.Parameters.AddWithValue("$evolved_ts", ts);

                await Database.ExecuteNonQuery(cmd);

            }

            // Set this gotchi as the user's primary Gotchi.

            GotchiUserInfo user_data = await GetUserInfoAsync(user);

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT id FROM Gotchi WHERE owner_id = $owner_id AND born_ts = $born_ts")) {

                cmd.Parameters.AddWithValue("$owner_id", user.Id);
                cmd.Parameters.AddWithValue("$born_ts", ts);

                user_data.PrimaryGotchiId = await Database.GetScalar<long>(cmd);

            }

            await UpdateUserInfoAsync(user_data);

        }
        public static async Task<Gotchi> GetGotchiAsync(long gotchiId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gotchi WHERE id = $id;")) {

                cmd.Parameters.AddWithValue("$id", gotchiId);

                DataRow row = await Database.GetRowAsync(cmd);

                return row is null ? null : GotchiFromDataRow(row);

            }

        }
        public static async Task<Gotchi> GetGotchiAsync(ulong userId, string name) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gotchi WHERE owner_id = $owner_id AND name = $name;")) {

                cmd.Parameters.AddWithValue("$owner_id", userId);
                cmd.Parameters.AddWithValue("$name", name.ToLower());

                DataRow row = await Database.GetRowAsync(cmd);

                return row is null ? null : GotchiFromDataRow(row);

            }

        }
        public static async Task<Gotchi[]> GetUserGotchisAsync(ulong userId) {

            List<Gotchi> gotchis = new List<Gotchi>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gotchi WHERE owner_id = $owner_id ORDER BY born_ts ASC;")) {

                cmd.Parameters.AddWithValue("$owner_id", userId);

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows)
                        gotchis.Add(GotchiFromDataRow(row));

            }

            return gotchis.ToArray();

        }
        public static async Task<Gotchi> GetUserGotchiAsync(IUser user) {

            // Get this user's Gotchi user data.
            GotchiUserInfo user_data = await GetUserInfoAsync(user);

            // Get this user's primary Gotchi.
            Gotchi gotchi = await GetGotchiAsync(user_data.PrimaryGotchiId);

            // If this user's primary gotchi doesn't exist (either it was never set or no longer exists), pick a primary gotchi from their current gotchis.

            if (gotchi is null) {

                Gotchi[] gotchis = await GetUserGotchisAsync(user_data.UserId);

                if (gotchis.Count() > 0) {

                    gotchi = gotchis[0];

                    user_data.PrimaryGotchiId = gotchi.Id;

                    await UpdateUserInfoAsync(user_data);

                }

            }

            return gotchi;

        }
        public static async Task<Gotchi[]> GetGotchisAsync() {

            List<Gotchi> gotchis = new List<Gotchi>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gotchi"))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows)
                    gotchis.Add(GotchiFromDataRow(row));

            return gotchis.ToArray();

        }
        public static async Task DeleteGotchiAsync(long gotchiId) {

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Gotchi WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", gotchiId);

                await Database.ExecuteNonQuery(cmd);

            }

        }

        public static async Task<GotchiUserInfo> GetUserInfoAsync(ulong userId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM GotchiUser WHERE user_id = $user_id;")) {

                cmd.Parameters.AddWithValue("$user_id", userId);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return GotchiUserInfo.FromDataRow(row);

            }

            // If the user is not yet in the database, return a default user object.
            return new GotchiUserInfo(userId);

        }
        public static async Task<GotchiUserInfo> GetUserInfoAsync(IUser user) {
            return await GetUserInfoAsync(user.Id);
        }
        public static async Task<long> GetUserGotchiCountAsync(IUser user) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Gotchi WHERE owner_id = $user_id;")) {

                cmd.Parameters.AddWithValue("$user_id", user.Id);

                long count = await Database.GetScalar<long>(cmd);

                Debug.Assert(count >= 0);

                return count;

            }

        }
        public static async Task UpdateUserInfoAsync(GotchiUserInfo userInfo) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO GotchiUser(user_id, g, gotchi_limit, primary_gotchi_id) VALUES ($user_id, $g, $gotchi_limit, $primary_gotchi_id);")) {

                cmd.Parameters.AddWithValue("$user_id", userInfo.UserId);
                cmd.Parameters.AddWithValue("$g", userInfo.G);
                cmd.Parameters.AddWithValue("$gotchi_limit", userInfo.GotchiLimit);
                cmd.Parameters.AddWithValue("$primary_gotchi_id", userInfo.PrimaryGotchiId);

                await Database.ExecuteNonQuery(cmd);

            }

        }

        public static async Task<bool> EvolveAndUpdateGotchiAsync(Gotchi gotchi) {
            return await EvolveAndUpdateGotchiAsync(gotchi, string.Empty);
        }
        public static async Task<bool> EvolveAndUpdateGotchiAsync(Gotchi gotchi, string desiredEvo) {

            bool evolved = false;

            if (string.IsNullOrEmpty(desiredEvo)) {

                // Find all descendatants of this species.

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT species_id FROM Ancestors WHERE ancestor_id=$ancestor_id;")) {

                    List<long> descendant_ids = new List<long>();

                    cmd.Parameters.AddWithValue("$ancestor_id", gotchi.SpeciesId);

                    using (DataTable rows = await Database.GetRowsAsync(cmd))
                        foreach (DataRow row in rows.Rows)
                            descendant_ids.Add(row.Field<long>("species_id"));

                    // Pick an ID at random.

                    if (descendant_ids.Count > 0) {

                        gotchi.SpeciesId = descendant_ids[BotUtils.RandomInteger(descendant_ids.Count)];

                        evolved = true;

                    }

                }

            }
            else {

                // Get the desired evo.
                Species[] sp = await BotUtils.GetSpeciesFromDb("", desiredEvo);

                if (sp is null || sp.Length != 1)
                    return false;

                // Ensure that the species evolves into the desired evo.

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Ancestors WHERE ancestor_id = $ancestor_id AND species_id = $species_id")) {

                    cmd.Parameters.AddWithValue("$ancestor_id", gotchi.SpeciesId);
                    cmd.Parameters.AddWithValue("$species_id", sp[0].Id);

                    if (await Database.GetScalar<long>(cmd) <= 0)
                        return false;

                    gotchi.SpeciesId = sp[0].Id;

                    evolved = true;

                }

            }

            // Update the gotchi in the database.
            // Update the evolution timestamp, even if it didn't evolve (in case it has an evolution available next week).

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET species_id=$species_id, evolved_ts=$evolved_ts WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$species_id", gotchi.SpeciesId);
                cmd.Parameters.AddWithValue("$evolved_ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                cmd.Parameters.AddWithValue("$id", gotchi.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            return evolved;

        }

        public static async Task<int> FeedGotchisAsync(ulong userId) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET fed_ts = $fed_ts WHERE owner_id = $owner_id AND fed_ts >= $min_ts")) {

                cmd.Parameters.AddWithValue("$fed_ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                cmd.Parameters.AddWithValue("$owner_id", userId);
                cmd.Parameters.AddWithValue("$min_ts", MinimumFedTimestamp());

                return await Database.ExecuteNonQuery(cmd);

            }

        }

        public static async Task<BattleGotchi> GenerateGotchiAsync(GotchiGenerationParameters parameters) {

            BattleGotchi result = new BattleGotchi();

            if (!(parameters.Base is null)) {

                // If a base gotchi was provided, copy over some of its characteristics.

                result.Gotchi.BornTimestamp = parameters.Base.BornTimestamp;
                result.Gotchi.DiedTimestamp = parameters.Base.DiedTimestamp;
                result.Gotchi.EvolvedTimestamp = parameters.Base.EvolvedTimestamp;
                result.Gotchi.FedTimestamp = parameters.Base.FedTimestamp;

            }

            // Select a random base species to start with.

            Species species = parameters.Species;

            if (species is null) {

                Species[] base_species_list = await SpeciesUtils.GetBaseSpeciesAsync();


                if (base_species_list.Count() > 0)
                    species = base_species_list.ElementAt(BotUtils.RandomInteger(0, base_species_list.Count()));

            }

            if (!(species is null))
                result.Gotchi.SpeciesId = species.Id;

            // Evolve it the given number of times.

            for (int i = 0; i < parameters.MaxEvolutions; ++i)
                if (!await EvolveAndUpdateGotchiAsync(result.Gotchi))
                    break;

            // Generate stats (if applicable).

            if (parameters.GenerateStats) {

                result.Gotchi.Experience = GotchiExperienceCalculator.ExperienceToLevel(result.Stats.ExperienceGroup, BotUtils.RandomInteger(parameters.MinLevel, parameters.MaxLevel + 1));

                result.Stats = await new GotchiStatsCalculator(Global.GotchiContext).GetStatsAsync(result.Gotchi);

            }

            // Generate moveset (if applicable).

            if (parameters.GenerateMoveset)
                result.Moves = await Global.GotchiContext.MoveRegistry.GetMoveSetAsync(result.Gotchi);

            // Generate types.
            result.Types = await Global.GotchiContext.TypeRegistry.GetTypesAsync(result.Gotchi);

            // Generate a name for the gotchi.

            result.Gotchi.Name = (species is null ? "Wild Gotchi" : species.ShortName) + string.Format(" (Lv. {0})", result.Stats is null ? 1 : result.Stats.Level);

            return result;

        }

        public static async Task<GotchiItem[]> GetGotchiItemsAsync() {

            List<GotchiItem> items = new List<GotchiItem>();
            string[] files = System.IO.Directory.GetFiles(Global.GotchiItemsDirectory, "*.json");

            foreach (string file in files) {

                GotchiItem item = JsonConvert.DeserializeObject<GotchiItem>(System.IO.File.ReadAllText(file));

                items.Add(item);

            }

            return await Task.FromResult(items.ToArray());

        }
        public static async Task<GotchiItem> GetGotchiItemAsync(long identifier) {
            return await GetGotchiItemAsync(identifier.ToString());
        }
        public static async Task<GotchiItem> GetGotchiItemAsync(string identifier) {

            GotchiItem[] items = await GetGotchiItemsAsync();
            long id = -1;

            if (StringUtils.IsNumeric(identifier))
                id = long.Parse(identifier);

            foreach (GotchiItem item in items)
                if (item.Name.ToLower() == identifier.ToLower() || (id != GotchiItem.NullId && item.Id == id))
                    return item;

            return null;

        }

        public static async Task<GotchiInventory> GetInventoryAsync(ulong userId) {

            List<GotchiInventoryItem> items = new List<GotchiInventoryItem>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM GotchiInventory WHERE user_id = $user_id")) {

                cmd.Parameters.AddWithValue("$user_id", userId);

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows) {

                        items.Add(new GotchiInventoryItem {
                            Item = await GetGotchiItemAsync(row.Field<long>("item_id")),
                            Count = row is null ? 0 : row.Field<long>("count")
                        });

                    }

            }

            return new GotchiInventory(items.OrderBy(x => x.Item.Id));

        }
        public static async Task<GotchiInventoryItem> AddItemToInventoryAsync(ulong userId, GotchiItem item, long count) {

            // We could just increment the "count" field, but the user is not guaranteed to already have the item.

            GotchiInventoryItem inventoryItem = await GetItemFromInventoryAsync(userId, item);

            inventoryItem.Count = Math.Max(0, inventoryItem.Count + count);

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO GotchiInventory(user_id, item_id, count) VALUES($user_id, $item_id, $count)")) {

                cmd.Parameters.AddWithValue("$user_id", userId);
                cmd.Parameters.AddWithValue("$item_id", item is null ? GotchiItem.NullId : item.Id);
                cmd.Parameters.AddWithValue("$count", inventoryItem.Count);

                await Database.ExecuteNonQuery(cmd);

            }

            return inventoryItem;

        }
        public static async Task<GotchiInventoryItem> GetItemFromInventoryAsync(ulong userId, GotchiItem item) {

            return await GetItemFromInventoryAsync(userId, (GotchiItemId)item.Id);

        }
        public static async Task<GotchiInventoryItem> GetItemFromInventoryAsync(ulong userId, GotchiItemId itemId) {

            return (await GetInventoryAsync(userId))
                .Where(i => i.Item.Id == (int)itemId)
                .FirstOrDefault() ?? new GotchiInventoryItem { Item = await GetGotchiItemAsync((long)itemId), Count = 0 };

        }

        public static bool ValidateGotchi(Gotchi gotchi) {

            if (gotchi is null)
                return false;

            return true;

        }
        public static async Task<bool> ValidateUserGotchiAndReplyAsync(ICommandContext context, Gotchi gotchi) {

            if (!ValidateGotchi(gotchi)) {

                await BotUtils.ReplyAsync_Info(context, "You don't have a gotchi yet! Get one with `gotchi get <species>`.");

                return false;

            }

            return true;

        }

        public static async Task<string> DownloadGotchiImageAsync(Gotchi gotchi) {

            // Get the species.

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.SpeciesId);

            if (sp is null)
                return string.Empty;

            // Download the gotchi image if possible.

            string gotchi_pic = Global.GotchiImagesDirectory + "default.png";

            if (!string.IsNullOrEmpty(sp.Picture) && (!Global.GotchiContext.Config.ImageWhitelistEnabled || Regex.Match(sp.Picture, @"^https:\/\/.+?\.discordapp\.(?:com|net)\/.+?\.(?:jpg|png)(?:\?.+)?$", RegexOptions.IgnoreCase).Success)) {

                string downloads_dir = Global.TempDirectory + "downloads";
                string ext = Regex.Match(sp.Picture, @"(\.(?:jpg|png))(?:\?.+)?$", RegexOptions.IgnoreCase).Groups[1].Value;
                string disk_fpath = System.IO.Path.Combine(downloads_dir, StringUtils.CreateMD5(sp.Picture) + ext);

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
                    await Bot.OurFoodChainBot.Instance.LogAsync(Discord.LogSeverity.Error, "gotchi", string.Format("Error occurred when loading gotchi image: {0}\n{1}", sp.Picture, ex.ToString()));

                }

            }

            return gotchi_pic;

        }
        public static async Task<string> GenerateGotchiGifAsync(GotchiGifCreatorParams[] gifParams, GotchiGifCreatorExtraParams extraParams) {

            // Create the temporary directory where the GIF will be saved.

            string tempDirectory = Global.TempDirectory + "gotchi";

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

            string outputPath = System.IO.Path.Combine(tempDirectory, string.Format("{0}.gif", StringUtils.CreateMD5(string.Join("", gotchiImagePaths))));

            using (GotchiGifCreator gif = new GotchiGifCreator()) {

                string backgroundFilename = System.IO.Path.Combine(Global.GotchiImagesDirectory, extraParams.backgroundFileName);

                if (System.IO.File.Exists(backgroundFilename))
                    gif.SetBackground(backgroundFilename);

                foreach (GotchiGifCreatorParams p in gifParams) {

                    if (p.auto)
                        gif.AddGotchi(gotchiImages[p.gotchi.Id], p.gotchi.State());
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
        public static async Task<string> GenerateAndUploadGotchiGifAndReplyAsync(ICommandContext context, Gotchi gotchi) {

            GotchiGifCreatorParams p = new GotchiGifCreatorParams {
                gotchi = gotchi,
                auto = true
            };

            return await GenerateAndUploadGotchiGifAndReplyAsync(context, new GotchiGifCreatorParams[] { p }, new GotchiGifCreatorExtraParams { backgroundFileName = await GetGotchiBackgroundFileNameAsync(gotchi) });

        }
        public static async Task<string> GenerateAndUploadGotchiGifAndReplyAsync(ICommandContext context, GotchiGifCreatorParams[] gifParams, GotchiGifCreatorExtraParams extraParams) {

            string file_path = await GenerateGotchiGifAsync(gifParams, extraParams);

            if (!string.IsNullOrEmpty(file_path))
                return await BotUtils.Reply_UploadFileToScratchServerAsync(context, file_path, deleteAfterUpload: true);

            await BotUtils.ReplyAsync_Error(context, "Failed to generate gotchi image.");

            return string.Empty;

        }

        public static async Task<string> GetGotchiBackgroundFileNameAsync(Gotchi gotchi, string defaultFileName = "home_aquatic.png") {

            // Returns a background image based on the gotchi passed in (i.e., corresponding to the zone it resides in).

            if (!(gotchi is null) && gotchi.SpeciesId > 0) {

                Zone[] zones = await BotUtils.GetZonesFromDb(gotchi.SpeciesId);

                foreach (Zone zone in zones) {

                    string candidate_filename = string.Format("home_{0}.png", StringUtils.ReplaceWhitespaceCharacters(zone.GetFullName().ToLower()));

                    if (System.IO.File.Exists(Global.GotchiImagesDirectory + candidate_filename))
                        return candidate_filename;

                }

            }

            return defaultFileName;

        }

        public static async Task<bool> ValidateTradeRequestAsync(ICommandContext context, GotchiTradeRequest tradeRequest) {

            // The request is invalid if:
            // - Either user involved in the trade has gotten a new gotchi since the trade was initiated
            // - Either gotchi has died since the trade was initiated
            // - The request has expired

            if (tradeRequest.IsExpired || tradeRequest.OfferedGotchi is null || tradeRequest.ReceivedGotchi is null)
                return false;

            IUser user1 = await context.Guild.GetUserAsync(tradeRequest.OfferedGotchi.OwnerId);
            Gotchi gotchi1 = user1 is null ? null : await GetUserGotchiAsync(user1);

            if (gotchi1 is null || gotchi1.IsDead() || gotchi1.Id != tradeRequest.OfferedGotchi.Id)
                return false;

            IUser user2 = await context.Guild.GetUserAsync(tradeRequest.ReceivedGotchi.OwnerId);
            Gotchi gotchi2 = user2 is null ? null : await GetUserGotchiAsync(user2);

            if (gotchi2 is null || gotchi2.IsDead() || gotchi2.Id != tradeRequest.ReceivedGotchi.Id)
                return false;

            return true;

        }
        public static async Task ExecuteTradeRequestAsync(ICommandContext context, GotchiTradeRequest tradeRequest) {

            // Get both users and their gotchis.

            IUser user1 = await context.Guild.GetUserAsync(tradeRequest.OfferedGotchi.OwnerId);
            Gotchi gotchi1 = await GetUserGotchiAsync(user1);
            GotchiUserInfo userInfo1 = await GetUserInfoAsync(user1);

            IUser user2 = await context.Guild.GetUserAsync(tradeRequest.ReceivedGotchi.OwnerId);
            Gotchi gotchi2 = await GetUserGotchiAsync(user2);
            GotchiUserInfo userInfo2 = await GetUserInfoAsync(user2);

            // Swap the owners of the gotchis.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET owner_id = $owner_id WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$owner_id", user1.Id);
                cmd.Parameters.AddWithValue("$id", gotchi2.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            userInfo1.PrimaryGotchiId = gotchi2.Id;

            await UpdateUserInfoAsync(userInfo1);

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET owner_id = $owner_id WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$owner_id", user2.Id);
                cmd.Parameters.AddWithValue("$id", gotchi1.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            userInfo2.PrimaryGotchiId = gotchi1.Id;

            await UpdateUserInfoAsync(userInfo2);

            // Remove all existing trade requests involving either user.
            _trade_requests.RemoveAll(x => x.OfferedGotchi.OwnerId == user1.Id || x.ReceivedGotchi.OwnerId == user2.Id);

        }
        public static async Task<GotchiTradeRequestResult> MakeTradeRequestAsync(ICommandContext context, Gotchi offeredGotchi, Gotchi recievedGotchi) {

            // If either gotchi passed in is null, the request is invalid.

            if (offeredGotchi is null || recievedGotchi is null)
                return GotchiTradeRequestResult.Invalid;

            // If the user has made previous trade requests, remove them.
            _trade_requests.RemoveAll(x => x.OfferedGotchi.OwnerId == offeredGotchi.OwnerId);

            // If their partner already has an open trade request that hasn't been accepted, don't allow a new trade request to be made.
            // This is so users cannot make a new trade request right before one is accepted and snipe the trade.

            GotchiTradeRequest request = GetTradeRequest(recievedGotchi);

            if (!(request is null)) {

                if (request.IsExpired)
                    _trade_requests.RemoveAll(x => x.ReceivedGotchi.OwnerId == recievedGotchi.OwnerId);
                else
                    return GotchiTradeRequestResult.RequestPending;

            }

            request = new GotchiTradeRequest {
                OfferedGotchi = offeredGotchi,
                ReceivedGotchi = recievedGotchi
            };

            if (!await ValidateTradeRequestAsync(context, request))
                return GotchiTradeRequestResult.Invalid;

            _trade_requests.Add(request);

            return GotchiTradeRequestResult.Success;

        }
        public static GotchiTradeRequest GetTradeRequest(Gotchi recievedGotchi) {

            // Returns the trade request that this user is a partner in.
            // If the partner has changed gotchis since the request was initiated, the request is invalid and thus not returned.

            foreach (GotchiTradeRequest request in _trade_requests)
                if (request.ReceivedGotchi.OwnerId == recievedGotchi.OwnerId && request.ReceivedGotchi.Id == recievedGotchi.Id)
                    return request;

            return null;

        }

        /// <summary>
        /// Returns the minimum timestamp that the Gotchi should have been fed at to avoid starving to death.
        /// </summary>
        /// <returns>The minimum timestamp that the Gotchi should have been fed at to avoid starving to death.</returns>
        public static long MinimumFedTimestamp() {

            long current_ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            current_ts -= Global.GotchiContext.Config.MaxMissedFeedings * Gotchi.HoursPerDay * 60 * 60;

            return current_ts;

        }
        public static string GenerateGotchiName(IUser user, Species species) {

            List<string> name_options = new List<string>();

            // Add the default name first.
            name_options.Add(string.Format("{0} Jr.", user.Username));

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
                    name = "Mr. " + name;

                name_options.Add(name);

            }

            return name_options.Select(x => StringUtils.ToTitleCase(x)).ToArray()[BotUtils.RandomInteger(name_options.Count())];

        }

        public static Gotchi GotchiFromDataRow(DataRow row) {

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

            result.Experience = (int)(row.IsNull("exp") ? 0.0 : row.Field<double>("exp"));

            if (row.Table.Columns.Contains("level") && !row.IsNull("level")) {

                // Level is calculated based off of total EXP now, but if level data exists, use it.

                result.Experience += GotchiExperienceCalculator.ExperienceToLevel(ExperienceGroup.Default, (int)Math.Max(1, row.Field<long>("level")));

            }

            return result;

        }

        private static List<GotchiTradeRequest> _trade_requests = new List<GotchiTradeRequest>();

    }

}