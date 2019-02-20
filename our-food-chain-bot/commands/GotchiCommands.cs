using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    [Group("gotchi")]
    public class Commands :
     ModuleBase {

        static Random _rng = new Random();

        [Command]
        public async Task Gotchi() {

            // Get this user's gotchi.

            Gotchi gotchi = await GotchiUtils.GetGotchiAsync(Context.User);

            if (!await GotchiUtils.Reply_ValidateGotchiAsync(Context, gotchi))
                return;

            // Check if the gotchi is able to evolve. If so, evolve it and update the species ID.

            bool evolved = false;

            if (!gotchi.IsDead() && gotchi.IsReadyToEvolve()) {

                // Find all descendatants of this species.

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT species_id FROM Ancestors WHERE ancestor_id=$ancestor_id;")) {

                    List<long> descendant_ids = new List<long>();

                    cmd.Parameters.AddWithValue("$ancestor_id", gotchi.species_id);

                    using (DataTable rows = await Database.GetRowsAsync(cmd))
                        foreach (DataRow row in rows.Rows)
                            descendant_ids.Add(row.Field<long>("species_id"));

                    // Pick an ID at random.

                    if (descendant_ids.Count > 0) {

                        gotchi.species_id = descendant_ids[_rng.Next(descendant_ids.Count)];

                        evolved = true;

                    }

                }

                // Update the gotchi.
                // Update the evolution timestamp, even if it didn't evolve (in case it has an evolution available next week).

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET species_id=$species_id, evolved_ts=$evolved_ts WHERE id=$id;")) {

                    cmd.Parameters.AddWithValue("$species_id", gotchi.species_id);
                    cmd.Parameters.AddWithValue("$evolved_ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    cmd.Parameters.AddWithValue("$id", gotchi.id);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

            // If the gotchi tried to evolve but failed, update its evolution timestamp so that we get a valid state (i.e., not "ready to evolve").
            // (Note that it will have already been updated in the database by this point.)
            if (gotchi.IsReadyToEvolve() && !evolved)
                gotchi.evolved_ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Create the gotchi GIF.

            string gif_url = await GotchiUtils.Reply_GenerateAndUploadGotchiGifAsync(Context, gotchi);

            if (string.IsNullOrEmpty(gif_url))
                return;

            // Get the gotchi's species.

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            // Pick status text.

            string status = "{0} is feeling happy!";

            switch (gotchi.State()) {

                case GotchiState.Dead:
                    status = "Oh no... {0} has died...";
                    break;

                case GotchiState.ReadyToEvolve:
                    status = "Congratulations, {0} " + string.Format("evolved into {0}!", sp.GetShortName());
                    break;

                case GotchiState.Sleeping:
                    long hours_left = gotchi.HoursOfSleepLeft();
                    status = "{0} is taking a nap. " + string.Format("Check back in {0} hour{1}.", hours_left, hours_left > 1 ? "s" : string.Empty);
                    break;

                case GotchiState.Hungry:
                    status = "{0} is feeling hungry!";
                    break;

                case GotchiState.Eating:
                    status = "{0} is enjoying some delicious Suka-Flakes™!";
                    break;

                case GotchiState.Energetic:
                    status = "{0} is feeling rowdy!";
                    break;

                case GotchiState.Tired:
                    status = "{0} is getting a bit sleepy...";
                    break;

            }

            // Send the message.

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithTitle(string.Format("{0}'s \"{1}\"", Context.User.Username, StringUtils.ToTitleCase(gotchi.name)));
            embed.WithDescription(string.Format("{0}, age {1}", sp.GetShortName(), gotchi.Age()));
            embed.WithImageUrl(gif_url);
            embed.WithFooter(string.Format(status, StringUtils.ToTitleCase(gotchi.name)));

            await ReplyAsync("", false, embed.Build());

        }

        [Command("get")]
        public async Task Get(string species) {
            await Get("", species);
        }
        [Command("get")]
        public async Task Get(string genus, string species) {

            // If the user already has a gotchi (and it's still alive!), don't let them make a new one.

            Gotchi gotchi = await GotchiUtils.GetGotchiAsync(Context.User);

            if (!(gotchi is null) && !gotchi.IsDead()) {

                await BotUtils.ReplyAsync_Error(Context, "You already have a gotchi!");

                return;

            }

            // Delete the user's old gotchi if already existed.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Gotchi WHERE owner_id=$owner_id;")) {

                cmd.Parameters.AddWithValue("$owner_id", Context.User.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            // Get the species that the user specified.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // The species must be a base species (e.g., doesn't evolve from anything).

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT count(*) FROM Ancestors WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.id);

                long count = await Database.GetScalar<long>(cmd);

                if (count > 0) {

                    await BotUtils.ReplyAsync_Error(Context, "You must start with a base species (i.e., a species that doesn't evolve from anything).");

                    return;

                }

            }

            // Create a gotchi for this user.

            await GotchiUtils.CreateGotchiAsync(Context.User, sp);

            await BotUtils.ReplyAsync_Success(Context, string.Format("All right **{0}**, take care of your new **{1}**!", Context.User.Username, sp.GetShortName()));

        }

        [Command("name")]
        public async Task Name(string name) {

            Gotchi gotchi = await GotchiUtils.GetGotchiAsync(Context.User);

            if (!await GotchiUtils.Reply_ValidateGotchiAsync(Context, gotchi))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET name=$name WHERE owner_id=$owner_id;")) {

                cmd.Parameters.AddWithValue("$name", name.ToLower());
                cmd.Parameters.AddWithValue("$owner_id", Context.User.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Sucessfully set {0}'s name to **{1}**.", StringUtils.ToTitleCase(gotchi.name), StringUtils.ToTitleCase(name)));

        }

        [Command("feed")]
        public async Task Feed() {

            Gotchi gotchi = await GotchiUtils.GetGotchiAsync(Context.User);

            if (!await GotchiUtils.Reply_ValidateGotchiAsync(Context, gotchi))
                return;

            if (gotchi.IsDead()) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("You went to feed **{0}**, but it looks like it's too late...", StringUtils.ToTitleCase(gotchi.name)));

                return;

            }
            else if (gotchi.IsSleeping()) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("Shhh, do not disturb! **{0}** is currently asleep. Try feeding them again later.", StringUtils.ToTitleCase(gotchi.name)));

                return;

            }

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET fed_ts=$fed_ts WHERE owner_id=$owner_id;")) {

                cmd.Parameters.AddWithValue("$fed_ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                cmd.Parameters.AddWithValue("$owner_id", Context.User.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Fed **{0}** some delicious Suka-Flakes™!", StringUtils.ToTitleCase(gotchi.name)));

        }

        [Command("stats")]
        public async Task Stats() {

            // Get this user's gotchi.

            Gotchi gotchi = await GotchiUtils.GetGotchiAsync(Context.User);

            if (!await GotchiUtils.Reply_ValidateGotchiAsync(Context, gotchi))
                return;

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (!await BotUtils.ReplyAsync_ValidateSpecies(Context, sp))
                return;

            // Calculate stats for this gotchi.

            GotchiStats stats = await GotchiStats.CalculateStats(gotchi);

            // Get moveset for this gotchi.

            GotchiMoveset set = await GotchiMoveset.GetMoveset(gotchi);

            // Create the embed.

            StringBuilder description_builder = new StringBuilder();
            description_builder.AppendLine(string.Format("{0}'s {2}, **Level {1}** (Age {3})", Context.User.Username, gotchi.level, sp.GetShortName(), gotchi.Age()));
            description_builder.AppendLine(string.Format("{0} experience points until next level", gotchi.exp));

            EmbedBuilder stats_page = new EmbedBuilder();
            stats_page.WithTitle(string.Format("{0}'s stats", gotchi.name));
            stats_page.AddField("❤ Hit points", (int)stats.hp, inline: true);
            stats_page.AddField("💥 Attack", (int)stats.atk, inline: true);
            stats_page.AddField("🛡 Defense", (int)stats.def, inline: true);
            stats_page.AddField("💨 Speed", (int)stats.spd, inline: true);

            EmbedBuilder set_page = new EmbedBuilder();

            set_page.WithTitle(string.Format("{0}'s moveset", gotchi.name));

            int move_index = 1;

            foreach (GotchiMove move in set.moves)
                set_page.AddField(string.Format("{0}. {1}", move_index++, StringUtils.ToTitleCase(move.name)), move.description);

            PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder(new List<EmbedBuilder>(new EmbedBuilder []{ stats_page, set_page }));

            embed.SetThumbnailUrl(sp.pics);
            embed.SetDescription(description_builder.ToString());

            await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, embed.Build());

        }

    }

}