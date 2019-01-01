using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    class GotchiUtils {

        /// <summary>
        /// Creates necessarily tables for supporting gotchi-related commands.
        /// </summary>
        /// <returns></returns>
        static public async Task InitializeDatabaseAsync() {

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Gotchi(id INTEGER PRIMARY KEY AUTOINCREMENT, species_id INTEGER, name TEXT, owner_id INTEGER, fed_ts INTEGER, born_ts INTEGER, died_ts INTEGER, evolved_ts INTEGER, FOREIGN KEY(species_id) REFERENCES Species(id));"))
                await Database.ExecuteNonQuery(cmd);

        }

        static public async Task CreateGotchiAsync(IUser user, Species species) {

            await InitializeDatabaseAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Gotchi(species_id, name, owner_id, fed_ts, born_ts, died_ts, evolved_ts) VALUES($species_id, $name, $owner_id, $fed_ts, $born_ts, $died_ts, $evolved_ts);")) {

                long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                cmd.Parameters.AddWithValue("$species_id", species.id);
                cmd.Parameters.AddWithValue("$owner_id", user.Id);
                cmd.Parameters.AddWithValue("$name", string.Format("{0} Jr.", user.Username).ToLower());
                cmd.Parameters.AddWithValue("$fed_ts", ts - 60 * 60); // subtract an hour to keep it from eating immediately after creation
                cmd.Parameters.AddWithValue("$born_ts", ts);
                cmd.Parameters.AddWithValue("$died_ts", 0);
                cmd.Parameters.AddWithValue("$evolved_ts", ts);

                await Database.ExecuteNonQuery(cmd);

            }

        }
        static public async Task<Gotchi> GetGotchiAsync(IUser user) {

            await InitializeDatabaseAsync();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gotchi WHERE owner_id=$owner_id;")) {

                cmd.Parameters.AddWithValue("$owner_id", user.Id);

                DataRow row = await Database.GetRowAsync(cmd);

                return row is null ? null : Gotchi.FromDataRow(row);

            }

        }

        static public async Task<bool> Reply_ValidateGotchiAsync(ICommandContext context, Gotchi gotchi) {

            if (gotchi is null) {

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

            if (!string.IsNullOrEmpty(sp.pics) && Regex.Match(sp.pics, @"^https:\/\/cdn\.discordapp\.com\/.+?\.(?:jpg|png)$", RegexOptions.IgnoreCase).Success) {

                string downloads_dir = "res/gotchi/downloads";
                string disk_fpath = System.IO.Path.Combine("res/gotchi/downloads", System.IO.Path.GetFileName(sp.pics));

                if (!System.IO.Directory.Exists(downloads_dir))
                    System.IO.Directory.CreateDirectory(downloads_dir);

                try {

                    if (!System.IO.File.Exists(disk_fpath))
                        using (System.Net.WebClient client = new System.Net.WebClient())
                            await client.DownloadFileTaskAsync(new Uri(sp.pics), disk_fpath);

                    if (System.IO.File.Exists(disk_fpath))
                        gotchi_pic = disk_fpath;

                }
                catch (Exception) {
                    // We'll just keep using the default picture if this happens.
                }

            }

            return gotchi_pic;

        }
        static public async Task<string> GenerateGotchiGif(Gotchi gotchi) {

            // Create the temporary directory where the GIF will be saved.

            string temp_dir = "res/gotchi/temp";

            if (!System.IO.Directory.Exists(temp_dir))
                System.IO.Directory.CreateDirectory(temp_dir);

            // Get the image for this gotchi.

            string gotchi_image = await DownloadGotchiImage(gotchi);

            // Create the gotchi GIF.

            string file_path = System.IO.Path.Combine(temp_dir, string.Format("{0}.gif", gotchi.owner_id));

            using (GotchiGifCreator gif = new GotchiGifCreator()) {

                string background_fpath = "res/gotchi/home_aquatic.png";

                if (System.IO.File.Exists(background_fpath))
                    gif.SetBackgroundImage(background_fpath);

                if (System.IO.File.Exists(gotchi_image))
                    gif.SetGotchiImage(gotchi_image);

                if (gotchi.IsDead())
                    gif.SetGotchiGifType(GotchiGifType.Dead);
                else if (gotchi.IsSleeping())
                    gif.SetGotchiGifType(GotchiGifType.Sleeping);
                else if (gotchi.IsHungry())
                    gif.SetGotchiGifType(GotchiGifType.Hungry);
                else if (gotchi.IsEating())
                    gif.SetGotchiGifType(GotchiGifType.Eating);
                else
                    gif.SetGotchiGifType(GotchiGifType.Happy);

                gif.Save(file_path);

            }

            return file_path;

        }
        static public async Task<string> Reply_GenerateAndUploadGotchiGifAsync(ICommandContext context, Gotchi gotchi) {

            string file_path = await GenerateGotchiGif(gotchi);

            if (!string.IsNullOrEmpty(file_path))
                return await BotUtils.Reply_UploadFileToScratchServerAsync(context, file_path);

            await BotUtils.ReplyAsync_Error(context, "Failed to generate gotchi image.");

            return string.Empty;

        }

    }

}