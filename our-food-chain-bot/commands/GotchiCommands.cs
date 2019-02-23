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
            // If the user is currently in battle, show their battle stats instead.

            GotchiStats stats;

            GotchiBattleState battle_state = GotchiBattleState.GetBattleStateByUser(Context.User.Id);

            if (!(battle_state is null))
                stats = battle_state.GetStats(gotchi);
            else
                stats = await GotchiStats.CalculateStats(gotchi);

            // Create the embed.

            EmbedBuilder stats_page = new EmbedBuilder();

            stats_page.WithTitle(string.Format("{0}'s {2}, **Level {1}** (Age {3})", Context.User.Username, gotchi.level, sp.GetShortName(), gotchi.Age()));
            stats_page.WithThumbnailUrl(sp.pics);
            stats_page.WithFooter(string.Format("{0} experience points until next level", gotchi.exp));

            stats_page.AddField("❤ Hit points", (int)stats.hp, inline: true);
            stats_page.AddField("💥 Attack", (int)stats.atk, inline: true);
            stats_page.AddField("🛡 Defense", (int)stats.def, inline: true);
            stats_page.AddField("💨 Speed", (int)stats.spd, inline: true);

            await ReplyAsync("", false, stats_page.Build());



        }

        [Command("moves"), Alias("moveset")]
        public async Task Moves() {

            // Get this user's gotchi.

            Gotchi gotchi = await GotchiUtils.GetGotchiAsync(Context.User);

            if (!await GotchiUtils.Reply_ValidateGotchiAsync(Context, gotchi))
                return;

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (!await BotUtils.ReplyAsync_ValidateSpecies(Context, sp))
                return;

            // Get moveset for this gotchi.

            GotchiMoveset set = await GotchiMoveset.GetMovesetAsync(gotchi);

            // Create the embed.

            EmbedBuilder set_page = new EmbedBuilder();

            set_page.WithTitle(string.Format("{0}'s {2}, **Level {1}** (Age {3})", Context.User.Username, gotchi.level, sp.GetShortName(), gotchi.Age()));
            set_page.WithThumbnailUrl(sp.pics);
            set_page.WithFooter(string.Format("{0} experience points until next level", gotchi.exp));

            int move_index = 1;

            foreach (GotchiMove move in set.moves)
                set_page.AddField(string.Format("Move {0}: **{1}**", move_index++, StringUtils.ToTitleCase(move.name)), move.description);

            await ReplyAsync("", false, set_page.Build());

        }

        [Command("battle"), Alias("challenge")]
        public async Task Battle(IUser user) {

            // Get this user's gotchi.

            Gotchi gotchi = await GotchiUtils.GetGotchiAsync(Context.User);

            if (!await GotchiUtils.Reply_ValidateGotchiAsync(Context, gotchi))
                return;

            if (gotchi.IsDead()) {

                await BotUtils.ReplyAsync_Info(Context, "Your gotchi has died, and is unable to battle.");

                return;

            }

            // Get the opponent's gotchi.

            Gotchi opposing_gotchi = await GotchiUtils.GetGotchiAsync(user);

            if (!GotchiUtils.ValidateGotchi(opposing_gotchi)) {

                await BotUtils.ReplyAsync_Info(Context, "Your opponent doesn't have a gotchi yet.");

                return;

            }

            if (opposing_gotchi.IsDead()) {

                await BotUtils.ReplyAsync_Info(Context, "Your opponent's has died, and is unable to battle.");

                return;

            }

            // If the user is involved in an existing battle (in progress), do not permit them to start another.

            GotchiBattleState state = GotchiBattleState.GetBattleStateByUser(Context.User.Id);

            if (!(state is null) && state.accepted) {

                ulong other_user_id = state.gotchi1.owner_id == Context.User.Id ? state.gotchi2.owner_id : state.gotchi1.owner_id;
                IUser other_user = await Context.Guild.GetUserAsync(other_user_id);

                // We won't lock them into the battle if the other user has left the server.

                if (!(other_user is null)) {

                    await BotUtils.ReplyAsync_Info(Context, string.Format("You are already battling **{0}**. You must finish the battle (or forfeit) before beginning a new one.", other_user.Mention));

                    return;

                }

            }

            // If the other user is involved in a battle, do not permit them to start another.

            state = GotchiBattleState.GetBattleStateByUser(user.Id);

            if (!(state is null) && state.accepted) {

                ulong other_user_id = state.gotchi1.owner_id == Context.User.Id ? state.gotchi2.owner_id : state.gotchi1.owner_id;
                IUser other_user = await Context.Guild.GetUserAsync(other_user_id);

                // We won't lock them into the battle if the other user has left the server.

                if (!(other_user is null)) {

                    await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** is currently battling someone else. Challenge them again later when they have finished.", other_user.Mention));

                    return;

                }

            }

            // Challenge the user to a battle.

            await GotchiBattleState.RegisterBattleAsync(gotchi, opposing_gotchi);

            await ReplyAsync(string.Format("{0}, **{1}** is challenging you to a battle! Use `{2}gotchi accept` or `{2}gotchi deny` to respond to their challenge.",
                user.Mention,
                Context.User.Username,
                OurFoodChainBot.GetInstance().GetConfig().prefix));

        }

        [Command("accept")]
        public async Task Accept() {

            // Get the battle state that the user is involved with.

            GotchiBattleState state = GotchiBattleState.GetBattleStateByUser(Context.User.Id);

            // If the state is null, the user has not been challenged to a battle.

            if (state is null || (await state.GetOtherUserAsync(Context, Context.User.Id)) is null) {

                await BotUtils.ReplyAsync_Info(Context, "You have not been challenged to a battle.");

                return;

            }

            // Check if the battle was already accepted.

            if (state.accepted) {

                await BotUtils.ReplyAsync_Info(Context, "Your battle is already in progress!");

                return;

            }

            // Accept the battle.

            state.accepted = true;

            StringBuilder message_builder = new StringBuilder();

            if (state.stats1.spd != state.stats2.spd) {

                message_builder.AppendLine(string.Format("The battle has begun! **{0}** is faster, so {1} goes first.",
                StringUtils.ToTitleCase(state.stats1.spd > state.stats2.spd ? state.gotchi1.name : state.gotchi2.name),
                state.stats1.spd > state.stats2.spd ? (await state.GetUser1Async(Context)).Mention : (await state.GetUser2Async(Context)).Mention));

            }
            else {

                message_builder.AppendLine(string.Format("The battle has begun! {0} has been randomly selected to go first.",
                state.IsTurn(Context.User.Id) ? Context.User.Mention : (await state.GetOtherUserAsync(Context, Context.User.Id)).Mention));

            }

            message_builder.AppendLine();
            message_builder.AppendLine(string.Format("Pick a move with `{0}gotchi move`.\nSee your gotchi's moveset with `{0}gotchi moveset`.",
                OurFoodChainBot.GetInstance().GetConfig().prefix));

            await ReplyAsync(string.Format("{0}, **{1}** has accepted your challenge!",
               (await state.GetOtherUserAsync(Context, Context.User.Id)).Mention,
               Context.User.Username));

            await GotchiBattleState.ShowBattleStateAsync(Context, state);

            await BotUtils.ReplyAsync_Info(Context, state.message);

        }

        [Command("deny")]
        public async Task Deny() {

            // Get the battle state that the user is involved with.

            GotchiBattleState state = GotchiBattleState.GetBattleStateByUser(Context.User.Id);

            // If the state is null, the user has not been challenged to a battle.

            if (state is null || (await state.GetOtherUserAsync(Context, Context.User.Id)) is null) {

                await BotUtils.ReplyAsync_Info(Context, "You have not been challenged to a battle.");

                return;

            }

            // Check if the battle was already accepted.

            if (state.accepted) {

                await BotUtils.ReplyAsync_Info(Context, "Your battle is already in progress!");

                return;

            }

            // Deny the battle.

            GotchiBattleState.DeregisterBattle(Context.User.Id);

            await ReplyAsync(string.Format("{0}, **{1}** has denied your challenge.",
               (await state.GetOtherUserAsync(Context, Context.User.Id)).Mention,
               Context.User.Username));

        }

        [Command("move")]
        public async Task Move(string moveIdentifier) {

            // Get the battle state that the user is involved with.

            GotchiBattleState state = GotchiBattleState.GetBattleStateByUser(Context.User.Id);

            // If the state is null, the user has not been challenged to a battle.

            if (state is null || (await state.GetOtherUserAsync(Context, Context.User.Id)) is null) {

                await BotUtils.ReplyAsync_Error(Context, "You have not been challenged to a battle.");

                return;

            }

            // Make sure that it is this user's turn.

            if (!state.IsTurn(Context.User.Id)) {

                await BotUtils.ReplyAsync_Error(Context, string.Format("It is currently {0}'s turn.", (await state.GetOtherUserAsync(Context, Context.User.Id)).Mention));

                return;

            }

            // Get the move that was used.

            GotchiMoveset moves = await GotchiMoveset.GetMovesetAsync(state.GetGotchi(Context.User.Id));
            GotchiMove move = moves.GetMove(moveIdentifier);

            if (move is null) {

                await BotUtils.ReplyAsync_Error(Context, "The move you have selected is invalid. Please select a valid move.");

                return;

            }

            // Use the move/update the battle state.

            await state.UseMoveAsync(Context, move);

        }

    }

}