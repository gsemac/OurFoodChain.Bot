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

namespace OurFoodChain.gotchi {

    public class GotchiTradeRequest {

        public Gotchi requesterGotchi;
        public Gotchi partnerGotchi;
        public long timestamp = 0;

        public enum GotchiTradeRequestResult {
            Success,
            Invalid,
            RequestPending
        }

        public bool IsExpired() {

            long minutes_until_expired = 5;

            return ((DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp) / 60) > minutes_until_expired;

        }
        public async Task<bool> IsValid(ICommandContext context) {

            // The request is invalid if:
            // - Either user involved in the trade has gotten a new gotchi since the trade was initiated
            // - Either gotchi has died since the trade was initiated
            // - The request has expired

            if (IsExpired() || requesterGotchi is null || partnerGotchi is null)
                return false;

            IUser user1 = await context.Guild.GetUserAsync(requesterGotchi.owner_id);
            Gotchi gotchi1 = user1 is null ? null : await GotchiUtils.GetPrimaryGotchiByUserAsync(user1);

            if (gotchi1 is null || gotchi1.IsDead() || gotchi1.id != requesterGotchi.id)
                return false;

            IUser user2 = await context.Guild.GetUserAsync(partnerGotchi.owner_id);
            Gotchi gotchi2 = user2 is null ? null : await GotchiUtils.GetPrimaryGotchiByUserAsync(user2);

            if (gotchi2 is null || gotchi2.IsDead() || gotchi2.id != partnerGotchi.id)
                return false;

            return true;

        }
        public async Task ExecuteRequest(ICommandContext context) {

            // Get both users and their gotchis.

            IUser user1 = await context.Guild.GetUserAsync(requesterGotchi.owner_id);
            Gotchi gotchi1 = await GotchiUtils.GetPrimaryGotchiByUserAsync(user1);

            IUser user2 = await context.Guild.GetUserAsync(partnerGotchi.owner_id);
            Gotchi gotchi2 = await GotchiUtils.GetPrimaryGotchiByUserAsync(user2);

            // Swap the owners of the gotchis.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET owner_id = $owner_id WHERE id = $id;")) {

                cmd.Parameters.AddWithValue("$owner_id", user1.Id);
                cmd.Parameters.AddWithValue("$id", gotchi2.id);

                await Database.ExecuteNonQuery(cmd);

            }

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET owner_id = $owner_id WHERE id = $id;")) {

                cmd.Parameters.AddWithValue("$owner_id", user2.Id);
                cmd.Parameters.AddWithValue("$id", gotchi1.id);

                await Database.ExecuteNonQuery(cmd);

            }

            // Remove all existing trade requests involving either user.
            TRADE_REQUESTS.RemoveAll(x => x.requesterGotchi.owner_id == user1.Id || x.partnerGotchi.owner_id == user2.Id);

        }

        public static async Task<GotchiTradeRequestResult> MakeTradeRequest(ICommandContext context, Gotchi requesterGotchi, Gotchi partnerGotchi) {

            // If either gotchi passed in is null, the request is invalid.

            if (requesterGotchi is null || partnerGotchi is null)
                return GotchiTradeRequestResult.Invalid;

            // If the user has made previous trade requests, remove them.
            TRADE_REQUESTS.RemoveAll(x => x.requesterGotchi.owner_id == requesterGotchi.owner_id);

            // If their partner already has an open trade request that hasn't been accepted, don't allow a new trade request to be made.
            // This is so users cannot make a new trade request right before one is accepted and snipe the trade.

            GotchiTradeRequest request = GetTradeRequest(partnerGotchi);

            if (!(request is null)) {

                if (request.IsExpired())
                    TRADE_REQUESTS.RemoveAll(x => x.partnerGotchi.owner_id == partnerGotchi.owner_id);
                else
                    return GotchiTradeRequestResult.RequestPending;

            }

            request = new GotchiTradeRequest {
                requesterGotchi = requesterGotchi,
                partnerGotchi = partnerGotchi,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            if (!await request.IsValid(context))
                return GotchiTradeRequestResult.Invalid;

            TRADE_REQUESTS.Add(request);

            return GotchiTradeRequestResult.Success;

        }
        public static GotchiTradeRequest GetTradeRequest(Gotchi partnerGotchi) {

            // Returns the trade request that this user is a partner in.
            // If the partner has changed gotchis since the request was initiated, the request is invalid and thus not returned.

            foreach (GotchiTradeRequest req in TRADE_REQUESTS)
                if (req.partnerGotchi.owner_id == partnerGotchi.owner_id && req.partnerGotchi.id == partnerGotchi.id)
                    return req;

            return null;

        }

        private static List<GotchiTradeRequest> TRADE_REQUESTS = new List<GotchiTradeRequest>();

    }

    public class GotchiUtils {

        public class GotchiGifCreatorParams {

            public Gotchi gotchi = null;
            public int x = 0;
            public int y = 0;
            public GotchiState state = GotchiState.Happy;
            public bool auto = true;

        }
        public class GotchiGifCreatorExtraParams {

            public string backgroundFileName = "home_aquatic.png";
            public Action<Graphics> overlay = null;

        }

        static public async Task AddGotchiAsync(IUser user, Species species) {

            // We need to generate a name for this Gotchi that doesn't already exist for this user.

            string name = GenerateGotchiName(user, species);

            while (!(await GetGotchiByNameAsync(user.Id, name) is null))
                name = GenerateGotchiName(user, species);

            // Add the Gotchi to the database.

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Gotchi(species_id, name, owner_id, fed_ts, born_ts, died_ts, evolved_ts) VALUES($species_id, $name, $owner_id, $fed_ts, $born_ts, $died_ts, $evolved_ts);")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);
                cmd.Parameters.AddWithValue("$owner_id", user.Id);
                cmd.Parameters.AddWithValue("$name", name.ToLower());
                cmd.Parameters.AddWithValue("$fed_ts", ts - 60 * 60); // subtract an hour to keep it from eating immediately after creation
                cmd.Parameters.AddWithValue("$born_ts", ts);
                cmd.Parameters.AddWithValue("$died_ts", 0);
                cmd.Parameters.AddWithValue("$evolved_ts", ts);

                await Database.ExecuteNonQuery(cmd);

            }

            // Set this gotchi as the user's primary Gotchi.

            GotchiUser user_data = await GetGotchiUserAsync(user);

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT id FROM Gotchi WHERE owner_id = $owner_id AND born_ts = $born_ts")) {

                cmd.Parameters.AddWithValue("$owner_id", user.Id);
                cmd.Parameters.AddWithValue("$born_ts", ts);

                user_data.PrimaryGotchiId = await Database.GetScalar<long>(cmd);

            }

            await UpdateGotchiUserAsync(user_data);

        }
        static public async Task<Gotchi> GetGotchiByIdAsync(long gotchiId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gotchi WHERE id = $id;")) {

                cmd.Parameters.AddWithValue("$id", gotchiId);

                DataRow row = await Database.GetRowAsync(cmd);

                return row is null ? null : Gotchi.FromDataRow(row);

            }

        }
        static public async Task<Gotchi> GetGotchiByNameAsync(ulong userId, string name) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gotchi WHERE owner_id = $owner_id AND name = $name;")) {

                cmd.Parameters.AddWithValue("$owner_id", userId);
                cmd.Parameters.AddWithValue("$name", name.ToLower());

                DataRow row = await Database.GetRowAsync(cmd);

                return row is null ? null : Gotchi.FromDataRow(row);

            }

        }
        static public async Task<Gotchi[]> GetGotchisByUserIdAsync(ulong userId) {

            List<Gotchi> gotchis = new List<Gotchi>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gotchi WHERE owner_id = $owner_id ORDER BY born_ts ASC;")) {

                cmd.Parameters.AddWithValue("$owner_id", userId);

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows)
                        gotchis.Add(Gotchi.FromDataRow(row));

            }

            return gotchis.ToArray();

        }
        static public async Task<Gotchi> GetPrimaryGotchiByUserAsync(IUser user) {

            // Get this user's Gotchi user data.
            GotchiUser user_data = await GetGotchiUserAsync(user);

            // Get this user's primary Gotchi.
            Gotchi gotchi = await GetGotchiByIdAsync(user_data.PrimaryGotchiId);

            // If this user's primary gotchi doesn't exist (either it was never set or no longer exists), pick a primary gotchi from their current gotchis.

            if (gotchi is null) {

                Gotchi[] gotchis = await GetGotchisByUserIdAsync(user_data.UserId);

                if (gotchis.Count() > 0) {

                    gotchi = gotchis[0];

                    user_data.PrimaryGotchiId = gotchi.id;

                    await UpdateGotchiUserAsync(user_data);

                }

            }

            return gotchi;

        }
        static public async Task DeleteGotchiByIdAsync(long gotchiId) {

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Gotchi WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", gotchiId);

                await Database.ExecuteNonQuery(cmd);

            }

        }

        static public async Task<GotchiUser> GetGotchiUserByUserIdAsync(ulong userId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM GotchiUser WHERE user_id = $user_id;")) {

                cmd.Parameters.AddWithValue("$user_id", userId);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return GotchiUser.FromDataRow(row);

            }

            // If the user is not yet in the database, return a default user object.
            return new GotchiUser(userId);

        }
        static public async Task<GotchiUser> GetGotchiUserAsync(IUser user) {
            return await GetGotchiUserByUserIdAsync(user.Id);
        }
        static public async Task UpdateGotchiUserAsync(GotchiUser userData) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO GotchiUser(user_id, g, gotchi_limit, primary_gotchi_id) VALUES ($user_id, $g, $gotchi_limit, $primary_gotchi_id);")) {

                cmd.Parameters.AddWithValue("$user_id", userData.UserId);
                cmd.Parameters.AddWithValue("$g", userData.G);
                cmd.Parameters.AddWithValue("$gotchi_limit", userData.GotchiLimit);
                cmd.Parameters.AddWithValue("$primary_gotchi_id", userData.PrimaryGotchiId);

                await Database.ExecuteNonQuery(cmd);

            }

        }
        static public async Task<ulong> GetGotchiCountAsync(IUser user) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Gotchi WHERE owner_id = $user_id;")) {

                cmd.Parameters.AddWithValue("$user_id", user.Id);

                long count = await Database.GetScalar<long>(cmd);

                Debug.Assert(count >= 0);

                return (ulong)count;

            }

        }
        static public async Task<bool> EvolveAndUpdateGotchiAsync(Gotchi gotchi) {

            bool evolved = false;

            // Find all descendatants of this species.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT species_id FROM Ancestors WHERE ancestor_id=$ancestor_id;")) {

                List<long> descendant_ids = new List<long>();

                cmd.Parameters.AddWithValue("$ancestor_id", gotchi.species_id);

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        descendant_ids.Add(row.Field<long>("species_id"));

                // Pick an ID at random.

                if (descendant_ids.Count > 0) {

                    gotchi.species_id = descendant_ids[BotUtils.RandomInteger(descendant_ids.Count)];

                    evolved = true;

                }

            }

            // Update the gotchi in the database.
            // Update the evolution timestamp, even if it didn't evolve (in case it has an evolution available next week).

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET species_id=$species_id, evolved_ts=$evolved_ts WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$species_id", gotchi.species_id);
                cmd.Parameters.AddWithValue("$evolved_ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                cmd.Parameters.AddWithValue("$id", gotchi.id);

                await Database.ExecuteNonQuery(cmd);

            }

            return evolved;

        }

        static public GotchiItem[] GetAllGotchiItems() {

            List<GotchiItem> items = new List<GotchiItem>();
            string[] files = System.IO.Directory.GetFiles("res/gotchi/items", "*.json");

            foreach (string file in files) {

                GotchiItem item = JsonConvert.DeserializeObject<GotchiItem>(System.IO.File.ReadAllText(file));

                items.Add(item);

            }

            return items.ToArray();

        }

        static public bool ValidateGotchi(Gotchi gotchi) {

            if (gotchi is null)
                return false;

            return true;

        }
        static public async Task<bool> Reply_ValidateGotchiAsync(ICommandContext context, Gotchi gotchi) {

            if (!ValidateGotchi(gotchi)) {

                await BotUtils.ReplyAsync_Info(context, "You don't have a gotchi yet! Get one with `gotchi get <species>`.");

                return false;

            }

            return true;

        }

        static public async Task<string> DownloadGotchiImage(Gotchi gotchi) {

            // Get the species.

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (sp is null)
                return string.Empty;

            // Download the gotchi image if possible.

            string gotchi_pic = "res/gotchi/default.png";

            if (!string.IsNullOrEmpty(sp.pics) && Regex.Match(sp.pics, @"^https:\/\/.+?\.discordapp\.(?:com|net)\/.+?\.(?:jpg|png)(?:\?.+)?$", RegexOptions.IgnoreCase).Success) {

                string downloads_dir = "res/gotchi/downloads";
                string ext = Regex.Match(sp.pics, @"(\.(?:jpg|png))(?:\?.+)?$", RegexOptions.IgnoreCase).Groups[1].Value;
                string disk_fpath = System.IO.Path.Combine("res/gotchi/downloads", StringUtils.CreateMD5(sp.pics) + ext);

                if (!System.IO.Directory.Exists(downloads_dir))
                    System.IO.Directory.CreateDirectory(downloads_dir);

                try {

                    if (!System.IO.File.Exists(disk_fpath))
                        using (System.Net.WebClient client = new System.Net.WebClient())
                            await client.DownloadFileTaskAsync(new Uri(sp.pics), disk_fpath);

                    if (System.IO.File.Exists(disk_fpath))
                        gotchi_pic = disk_fpath;

                }
                catch (Exception ex) {

                    // We'll just keep using the default picture if this happens.
                    await OurFoodChainBot.GetInstance().Log(LogSeverity.Error, "gotchi", string.Format("Error occurred when loading gotchi image: {0}\n{1}", sp.pics, ex.ToString()));

                }

            }

            return gotchi_pic;

        }
        static public async Task<string> GenerateGotchiGif(GotchiGifCreatorParams[] gifParams, GotchiGifCreatorExtraParams extraParams) {

            // Create the temporary directory where the GIF will be saved.

            string temp_dir = "res/gotchi/temp";

            if (!System.IO.Directory.Exists(temp_dir))
                System.IO.Directory.CreateDirectory(temp_dir);

            // Get the image for this gotchi.

            Dictionary<long, Bitmap> gotchi_images = new Dictionary<long, Bitmap>();
            List<string> gotchi_image_paths = new List<string>();

            foreach (GotchiGifCreatorParams p in gifParams) {

                string gotchi_image_path = await DownloadGotchiImage(p.gotchi);

                gotchi_image_paths.Add(gotchi_image_path);
                gotchi_images[p.gotchi.id] = System.IO.File.Exists(gotchi_image_path) ? new Bitmap(gotchi_image_path) : null;

            }

            // Create the gotchi GIF.

            string output_path = System.IO.Path.Combine(temp_dir, string.Format("{0}.gif", StringUtils.CreateMD5(string.Join("", gotchi_image_paths))));

            using (GotchiGifCreator gif = new GotchiGifCreator()) {

                string background_fpath = System.IO.Path.Combine("res/gotchi", extraParams.backgroundFileName);

                if (System.IO.File.Exists(background_fpath))
                    gif.SetBackground(background_fpath);

                foreach (GotchiGifCreatorParams p in gifParams) {

                    if (p.auto)
                        gif.AddGotchi(gotchi_images[p.gotchi.id], p.gotchi.State());
                    else
                        gif.AddGotchi(p.x, p.y, gotchi_images[p.gotchi.id], p.state);

                }

                gif.Save(output_path, extraParams.overlay);

            }

            // Free all bitmaps.

            foreach (long key in gotchi_images.Keys)
                if (!(gotchi_images[key] is null))
                    gotchi_images[key].Dispose();

            return output_path;

        }
        static public async Task<string> Reply_GenerateAndUploadGotchiGifAsync(ICommandContext context, Gotchi gotchi) {

            GotchiGifCreatorParams p = new GotchiGifCreatorParams {
                gotchi = gotchi,
                auto = true
            };

            return await Reply_GenerateAndUploadGotchiGifAsync(context, new GotchiGifCreatorParams[] { p }, new GotchiGifCreatorExtraParams { backgroundFileName = await GetGotchiBackgroundFileNameAsync(gotchi) });

        }
        static public async Task<string> Reply_GenerateAndUploadGotchiGifAsync(ICommandContext context, GotchiGifCreatorParams[] gifParams, GotchiGifCreatorExtraParams extraParams) {

            string file_path = await GenerateGotchiGif(gifParams, extraParams);

            if (!string.IsNullOrEmpty(file_path))
                return await BotUtils.Reply_UploadFileToScratchServerAsync(context, file_path);

            await BotUtils.ReplyAsync_Error(context, "Failed to generate gotchi image.");

            return string.Empty;

        }

        public static async Task<string> GetGotchiBackgroundFileNameAsync(Gotchi gotchi, string defaultFileName = "home_aquatic.png") {

            // Returns a background image based on the gotchi passed in (i.e., corresponding to the zone it resides in).

            if (!(gotchi is null) && gotchi.species_id > 0) {

                Zone[] zones = await BotUtils.GetZonesFromDb(gotchi.species_id);

                foreach (Zone zone in zones) {

                    string candidate_filename = string.Format("home_{0}.png", StringUtils.ReplaceWhitespaceCharacters(zone.GetFullName().ToLower()));

                    if (System.IO.File.Exists("res/gotchi/" + candidate_filename))
                        return candidate_filename;

                }

            }

            return defaultFileName;

        }

        /// <summary>
        /// Returns the minimum timestamp that the Gotchi should have been fed at to avoid starving to death.
        /// </summary>
        /// <returns>The minimum timestamp that the Gotchi should have been fed at to avoid starving to death.</returns>
        public static long MinimumFedTimestamp() {

            long current_ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            current_ts -= Gotchi.MAXIMUM_STARVATION_DAYS * Gotchi.HOURS_PER_DAY * 60 * 60;

            return current_ts;

        }
        public static string GenerateGotchiName(IUser user, Species species) {

            List<string> name_options = new List<string>();

            // Add the default name first.
            name_options.Add(string.Format("{0} Jr.", user.Username));

            // We'll generate some names using some portion of the species name.
            // For example, "gigas" might generate "gi-gi", "mr. giga", or "giga".

            string species_name = species.name;
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

    }

}