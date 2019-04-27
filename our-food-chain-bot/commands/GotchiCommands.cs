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

        [Command]
        public async Task Gotchi() {

            // Get this user's primary Gotchi.
            Gotchi gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);

            // Get all Gotchis owned by this user so we can update them if necessary (e.g. evolve them if they're ready).
            Gotchi[] gotchis = await GotchiUtils.GetGotchisByUserIdAsync(Context.User.Id);

            if (!await GotchiUtils.Reply_ValidateGotchiAsync(Context, gotchi))
                return;

            // For all Gotchis owned by the user, evolve them if they're ready to evolve.

            foreach (Gotchi i in gotchis) {

                bool evolved = false;

                if (gotchi.IsReadyToEvolve())
                    evolved = await GotchiUtils.EvolveAndUpdateGotchiAsync(gotchi);

                // If the gotchi tried to evolve but failed, update its evolution timestamp so that we get a valid state (i.e., not "ready to evolve").
                // (Note that it will have already been updated in the database by this point.)
                if (gotchi.IsReadyToEvolve() && !evolved)
                    gotchi.evolved_ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            }

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

            // Get the species that the user specified.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // The species must be a base species (e.g., doesn't evolve from anything).

            if (!await BotUtils.IsBaseSpeciesAsync(sp)) {

                await BotUtils.ReplyAsync_Error(Context, "You must start with a base species (i.e., a species that doesn't evolve from anything).");

                return;

            }

            // If the user has already reached their gotchi limit, don't allow them to get any more.

            GotchiUser user_data = await GotchiUtils.GetGotchiUserAsync(Context.User);
            ulong gotchi_count = await GotchiUtils.GetGotchiCountAsync(Context.User);

            if (gotchi_count >= user_data.GotchiLimit) {

                // If the user's primary Gotchi is dead, remove it to be replaced. Otherwise, the new Gotchi cannot be added.

                Gotchi primary_gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);

                if (primary_gotchi.IsDead())
                    await GotchiUtils.DeleteGotchiByIdAsync(primary_gotchi.id);

                else {

                    await BotUtils.ReplyAsync_Error(Context, "You don't have room for any more Gotchis!\n\nYou will need to release one of your Gotchis, or expand your Gotchi tank. See the `gotchi release` and `gotchi shop` commands for more details.");

                    return;

                }

            }

            // Create a gotchi for this user.
            await GotchiUtils.AddGotchiAsync(Context.User, sp);

            await BotUtils.ReplyAsync_Success(Context, string.Format("All right **{0}**, take care of your new **{1}**!", Context.User.Username, sp.GetShortName()));

        }

        [Command("release")]
        public async Task Release(string name) {

            // Find the Gotchi with the given name.

            Gotchi gotchi = await GotchiUtils.GetGotchiByNameAsync(Context.User.Id, name);

            if (gotchi is null) {

                await BotUtils.ReplyAsync_Error(Context, string.Format("No Gotchi with the name \"{0}\" exists.", name));

                return;

            }

            // Delete the Gotchi from the database.
            await GotchiUtils.DeleteGotchiByIdAsync(gotchi.id);

            if (gotchi.IsDead())
                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}**'s corpse was successfully flushed. Rest in peace, **{0}**!", StringUtils.ToTitleCase(gotchi.name)));
            else
                await BotUtils.ReplyAsync_Success(Context, string.Format("Gotchi **{0}** was successfully released. Take care, **{0}**!", StringUtils.ToTitleCase(gotchi.name)));

        }

        [Command("name")]
        public async Task Name(string name) {

            Gotchi gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);

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

            Gotchi gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);

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

            // Although we only display the state of the primary Gotchi at the moment, update the feed time for all Gotchis owned by this user.
            // Only Gotchis that are still alive (i.e. have been fed recently enough) get their timestamp updated.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET fed_ts = $fed_ts WHERE owner_id = $owner_id AND fed_ts >= $min_ts;")) {

                cmd.Parameters.AddWithValue("$fed_ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                cmd.Parameters.AddWithValue("$owner_id", Context.User.Id);
                cmd.Parameters.AddWithValue("$min_ts", GotchiUtils.MinimumFedTimestamp());

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Fed **{0}** some delicious Suka-Flakes™!", StringUtils.ToTitleCase(gotchi.name)));

        }

        [Command("stats")]
        public async Task Stats() {

            // Get this user's gotchi.

            Gotchi gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);

            if (!await GotchiUtils.Reply_ValidateGotchiAsync(Context, gotchi))
                return;

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (!await BotUtils.ReplyAsync_ValidateSpecies(Context, sp))
                return;

            // Calculate stats for this gotchi.
            // If the user is currently in battle, show their battle stats instead.

            LuaGotchiStats stats;

            GotchiBattleState battle_state = GotchiBattleState.GetBattleStateByUserId(Context.User.Id);

            if (!(battle_state is null))
                stats = battle_state.GetGotchiStats(gotchi);
            else
                stats = await GotchiStatsUtils.CalculateStats(gotchi);

            // Create the embed.

            EmbedBuilder stats_page = new EmbedBuilder();

            stats_page.WithTitle(string.Format("{0}'s {2}, **Level {1}** (Age {3})", Context.User.Username, stats.level, sp.GetShortName(), gotchi.Age()));
            stats_page.WithThumbnailUrl(sp.pics);
            stats_page.WithFooter(string.Format("{0} experience points until next level", GotchiStatsUtils.ExperienceRequired(stats)));

            stats_page.AddField("❤ Hit points", (int)stats.hp, inline: true);
            stats_page.AddField("💥 Attack", (int)stats.atk, inline: true);
            stats_page.AddField("🛡 Defense", (int)stats.def, inline: true);
            stats_page.AddField("💨 Speed", (int)stats.spd, inline: true);

            await ReplyAsync("", false, stats_page.Build());



        }

        [Command("moves"), Alias("moveset")]
        public async Task Moves() {

            // Get this user's gotchi.

            Gotchi gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);

            if (!await GotchiUtils.Reply_ValidateGotchiAsync(Context, gotchi))
                return;

            Species sp = await BotUtils.GetSpeciesFromDb(gotchi.species_id);

            if (!await BotUtils.ReplyAsync_ValidateSpecies(Context, sp))
                return;

            // Get moveset for this gotchi.
            // If the user is currently in battle, get their moveset from the battle state instead.

            GotchiMoveset set;
            GotchiBattleState battle_state = GotchiBattleState.GetBattleStateByUserId(Context.User.Id);

            if (!(battle_state is null))
                set = battle_state.GetGotchiMoveset(gotchi);
            else
                set = await GotchiMoveset.GetMovesetAsync(gotchi);


            // Create the embed.

            EmbedBuilder set_page = new EmbedBuilder();
            LuaGotchiStats stats = await GotchiStatsUtils.CalculateStats(gotchi);

            set_page.WithTitle(string.Format("{0}'s {2}, **Level {1}** (Age {3})", Context.User.Username, stats.level, sp.GetShortName(), gotchi.Age()));
            set_page.WithThumbnailUrl(sp.pics);
            set_page.WithFooter(string.Format("{0} experience points until next level", GotchiStatsUtils.ExperienceRequired(stats)));

            int move_index = 1;

            foreach (GotchiMove move in set.moves)
                set_page.AddField(string.Format("Move {0}: **{1}** ({2}/{3} PP)", move_index++, StringUtils.ToTitleCase(move.info.name), move.pp, move.info.pp), move.info.description);

            await ReplyAsync("", false, set_page.Build());

        }

        [Command("battle"), Alias("challenge", "duel")]
        public async Task Battle(IUser user) {

            // Cannot challenge oneself.

            if (!(user is null) && user.Id == Context.User.Id) {

                await BotUtils.ReplyAsync_Error(Context, "You cannot challenge yourself.");

                return;

            }

            // Get this user's gotchi.

            Gotchi gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);

            if (!await _replyValidateChallengerGotchiForBattleAsync(Context, gotchi))
                return;

            // Get the opponent's gotchi.
            // If the opponent is null, assume the user is training. A random gotchi will be generated for them to battle against.

            Gotchi opposing_gotchi = null;

            if (!(user is null)) {

                opposing_gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(user);

                if (!await _replyValidateOpponentGotchiForBattleAsync(Context, opposing_gotchi))
                    return;

            }

            // If the user is involved in an existing battle (in progress), do not permit them to start another.

            if (!await _replyVerifyChallengerAvailableForBattleAsync(Context))
                return;

            // If the other user is involved in a battle, do not permit them to start another.

            if (!(user is null) && !await _replyVerifyOpponentAvailableForBattleAsync(Context, user))
                return;

            // Challenge the user to a battle.

            await GotchiBattleState.RegisterBattleAsync(Context, gotchi, opposing_gotchi);

            if (!(user is null)) {

                // If the user is battling another user, show a message challenging them to battle.
                // Otherwise, the battle state will be shown automatically when calling RegisterBattleAsync.

                await ReplyAsync(string.Format("{0}, **{1}** is challenging you to a battle! Use `{2}gotchi accept` or `{2}gotchi deny` to respond to their challenge.",
                    user.Mention,
                    Context.User.Username,
                    OurFoodChainBot.GetInstance().GetConfig().prefix));

            }

        }

        [Command("train")]
        public async Task Train() {

            // Get this user's gotchi.

            Gotchi gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);

            if (!await GotchiUtils.Reply_ValidateGotchiAsync(Context, gotchi))
                return;

            // Users can train their gotchi by battling random gotchis 3 times every 15 minutes.

            long training_left = 0;
            long training_ts = 0;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT training_left, training_ts FROM Gotchi WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$id", gotchi.id);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null)) {

                    training_left = row.IsNull("training_left") ? 0 : row.Field<long>("training_left");
                    training_ts = row.IsNull("training_ts") ? 0 : row.Field<long>("training_ts");

                }

            }

            // If it's been more than 15 minutes since the training timestamp was updated, reset the training count.

            long minutes_elapsed = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - training_ts) / 60;

            if (minutes_elapsed >= 15) {

                training_left = 3;
                training_ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            }

            // If the user has no more training attempts left, exit. 

            if (training_left <= 0) {

                long minutes_left = (15 - minutes_elapsed);

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** is feeling tired from all the training... Try again in {1} minute{2}.",
                    StringUtils.ToTitleCase(gotchi.name),
                    minutes_left <= 1 ? "a" : minutes_left.ToString(),
                    minutes_left > 1 ? "s" : ""));

                return;

            }

            // Update the user's training data.

            --training_left;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET training_left=$training_left, training_ts=$training_ts WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$id", gotchi.id);
                cmd.Parameters.AddWithValue("$training_left", training_left);
                cmd.Parameters.AddWithValue("$training_ts", training_ts);

                await Database.ExecuteNonQuery(cmd);

            }

            await Battle(null);

        }

        [Command("accept")]
        public async Task Accept() {

            // This command is used to respond to battle or trade requests.
            // Battle requests will take priority over trade requests to help prevent users from tricking others into trading their gotchis.

            // Get the battle state that the user is involved with (if applicable).

            GotchiBattleState battle_state = GotchiBattleState.GetBattleStateByUserId(Context.User.Id);
            bool battle_state_is_valid = !(battle_state is null ||
                (await battle_state.GetOtherPlayerAsync(Context, Context.User.Id)) is null || // user who issued the challenge should still be in server
                (await battle_state.GetPlayer2UserAsync(Context)).Id != Context.User.Id); // users cannot respond to challenges directed to others

            if (battle_state_is_valid) {

                // Accept the battle.

                if (!battle_state.accepted) {

                    battle_state.accepted = true;

                    await ReplyAsync(string.Format("{0}, **{1}** has accepted your challenge!",
                       (await battle_state.GetOtherPlayerAsync(Context, Context.User.Id)).Mention,
                       Context.User.Username));

                    await GotchiBattleState.ShowBattleStateAsync(Context, battle_state);

                    return;

                }

            }
            else {

                // Get the trade that the user is involved with (if applicable).

                Gotchi gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);
                GotchiTradeRequest trade_request = GotchiTradeRequest.GetTradeRequest(gotchi);
                bool trade_request_is_valid = !(trade_request is null);

                if (trade_request_is_valid) {

                    if (!await trade_request.IsValid(Context))
                        await BotUtils.ReplyAsync_Info(Context, "The trade request has expired, or is invalid.");

                    else {

                        // The trade is valid, so perform the trade.

                        await trade_request.ExecuteRequest(Context);

                        await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** successfully traded gotchis with **{1}**. Take good care of them!",
                            Context.User.Username,
                            (await Context.Guild.GetUserAsync(trade_request.requesterGotchi.owner_id)).Username));

                        return;

                    }

                }

            }

            // The user has no open requests.
            await BotUtils.ReplyAsync_Info(Context, "You have no open requests to respond to.");

        }

        [Command("deny")]
        public async Task Deny() {

            // This command is used to respond to battle or trade requests.

            GotchiBattleState battle_state = GotchiBattleState.GetBattleStateByUserId(Context.User.Id);
            bool battle_state_is_valid = !(battle_state is null ||
                (await battle_state.GetOtherPlayerAsync(Context, Context.User.Id)) is null || // user who issued the challenge should still be in server
                (await battle_state.GetPlayer2UserAsync(Context)).Id != Context.User.Id); // users cannot respond to challenges directed to others

            if (battle_state_is_valid) {

                // Deny the battle.

                GotchiBattleState.DeregisterBattle(Context.User.Id);

                await ReplyAsync(string.Format("{0}, **{1}** has denied your challenge.",
                   (await battle_state.GetOtherPlayerAsync(Context, Context.User.Id)).Mention,
                   Context.User.Username));

                return;

            }
            else {

                // Get the trade that the user is involved with (if applicable).

                Gotchi gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);
                GotchiTradeRequest trade_request = GotchiTradeRequest.GetTradeRequest(gotchi);
                bool trade_request_is_valid = !(trade_request is null);

                if (trade_request_is_valid && await trade_request.IsValid(Context)) {

                    await ReplyAsync(string.Format("{0}, **{1}** has denied your trade request.",
                        (await Context.Guild.GetUserAsync(trade_request.requesterGotchi.owner_id)).Mention,
                        Context.User.Username));

                    return;

                }

            }

            // The user has no open requests.
            await BotUtils.ReplyAsync_Info(Context, "You have no open requests to respond to.");

        }

        [Command("move"), Alias("use")]
        public async Task Move(string moveIdentifier) {

            // Get the battle state that the user is involved with.

            GotchiBattleState state = GotchiBattleState.GetBattleStateByUserId(Context.User.Id);

            // If the state is null, the user has not been challenged to a battle.

            if (state is null || (!state.IsBattlingCpu() && (await state.GetOtherPlayerAsync(Context, Context.User.Id)) is null)) {

                await BotUtils.ReplyAsync_Error(Context, "You have not been challenged to a battle.");

                return;

            }

            // If the other user hasn't accepted the battle yet, don't allow the user to select a move.

            if (!state.accepted) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** has not accepted your challenge yet.",
                    await state.GetPlayer2UsernameAsync(Context)));

                return;

            }

            // Attempt to select the user's chosen move.
            // If anything goes wrong, the battle state will alert the user.
            await state.SelectMoveAsync(Context, moveIdentifier);

        }

        [Command("forfeit")]
        public async Task Forfeit() {

            // Get the battle state that the user is involved with.
            GotchiBattleState state = GotchiBattleState.GetBattleStateByUserId(Context.User.Id);

            if (!await _validateGotchiBattleState(Context, state))
                await BotUtils.ReplyAsync_Error(Context, "You are not currently in battle.");

            else {

                // Deregister the battle state.
                GotchiBattleState.DeregisterBattle(Context.User.Id);

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** has forfeited the battle.", Context.User.Username));

            }

        }

        [Command("help"), Alias("h")]
        public async Task Help() {

            await HelpCommands.ShowHelpCategory(Context, "help/gotchi", "gotchi");

        }
        [Command("help"), Alias("h")]
        public async Task Help(string command) {

            await HelpCommands.ShowHelp(Context, "gotchi", command);

        }

        [Command("trade")]
        public async Task Trade(IUser user) {

            // Cannot trade with oneself.

            if (Context.User.Id == user.Id) {

                await BotUtils.ReplyAsync_Info(Context, "You cannot trade with yourself.");

            }
            else {

                Gotchi gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);
                Gotchi partnerGotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(user);

                // Submit the trade request.

                switch (await GotchiTradeRequest.MakeTradeRequest(Context, gotchi, partnerGotchi)) {

                    case GotchiTradeRequest.GotchiTradeRequestResult.Success:

                        await ReplyAsync(string.Format("{0}, **{1}** wants to trade gotchis with you! Use `{2}gotchi accept` or `{2}gotchi deny` to respond to their trade request.",
                            user.Mention,
                            Context.User.Username,
                            OurFoodChainBot.GetInstance().GetConfig().prefix));

                        break;

                    case GotchiTradeRequest.GotchiTradeRequestResult.RequestPending:

                        await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** currently has another pending trade request. Please try again later.",
                            user.Username));

                        break;

                    case GotchiTradeRequest.GotchiTradeRequestResult.Invalid:

                        await BotUtils.ReplyAsync_Info(Context, "Trade request is invalid.");

                        break;

                }

            }

        }

        [Command("shop")]
        public async Task Shop() {

            GotchiUser user_data = await GotchiUtils.GetGotchiUserAsync(Context.User);

            // List all gotchi items available for sale.

            // Generate a field for each item.

            GotchiItem[] items = GotchiUtils.GetAllGotchiItems();
            List<EmbedFieldBuilder> item_fields = new List<EmbedFieldBuilder>();

            foreach (GotchiItem item in items) {

                // (Tank expansions cost extra the more the user has already.)

                if (item.id == 1)
                    item.cost *= user_data.GotchiLimit;

                // Build the field.

                item_fields.Add(new EmbedFieldBuilder {
                    Name = string.Format("{0}. {1} {2} — {3}", item.id, item.icon, item.Name, item.cost <= 0 ? "Not Available" : (item.cost.ToString("n0") + "G")),
                    Value = item.description
                });

            }

            // Create the embed.

            PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder();

            embed.AddPages(EmbedUtils.FieldsToEmbedPages(item_fields));
            embed.SetTitle("🛒 Gotchi Shop");
            embed.SetDescription(string.Format("Welcome to the Gotchi Shop! Purchase an item with `{0}gotchi buy <item>`.",
               OurFoodChainBot.GetInstance().GetConfig().prefix));
            embed.SetFooter(string.Format("You currently have {0:n0}G.", user_data.G));
            embed.SetColor(Color.LightOrange);
            embed.AddPageNumbers();

            await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, embed.Build());

        }

        [Command("buy")]
        public async Task Buy(string itemIdentifier, params string[] arguments) {

            GotchiItem item = GotchiUtils.GetGotchiItemByNameOrId(itemIdentifier);

            if (item is null || item.id == GotchiItem.NULL_ITEM_ID) {

                await BotUtils.ReplyAsync_Error(Context, "Invalid item selection.");

            }
            else {

                GotchiUser user_data = await GotchiUtils.GetGotchiUserAsync(Context.User);

                // (Tank expansions cost extra the more the user has already.)

                if (item.id == 1)
                    item.cost *= user_data.GotchiLimit;

                bool item_failed = false;

                if (item.cost > (ulong)user_data.G) {

                    await BotUtils.ReplyAsync_Error(Context, string.Format("You don't have enough G to afford this item ({0:n0}G).", item.cost));

                }
                else {

                    switch (item.id) {

                        case 1: // tank expansion

                            user_data.GotchiLimit += 1;

                            await BotUtils.ReplyAsync_Success(Context, string.Format("Your Gotchi limit has been increased to {0}!", user_data.GotchiLimit));

                            break;

                        case 2: // evo stone
                        case 3: // glowing evo stone
                            {

                                Gotchi gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);
                                string desired_evo = item.id == 3 ? string.Join(" ", arguments) : string.Empty;

                                if (item.id == 3 && string.IsNullOrEmpty(desired_evo)) {

                                    await BotUtils.ReplyAsync_Error(Context,
                                        string.Format("Please specify the desired species when buying this item.\n\nEx: `{0}gotchi buy 3 asperum`", OurFoodChainBot.GetInstance().GetConfig().prefix));

                                    item_failed = true;

                                }
                                else {

                                    if (await GotchiUtils.Reply_ValidateGotchiAsync(Context, gotchi) && await GotchiUtils.EvolveAndUpdateGotchiAsync(gotchi, desired_evo)) {

                                        await BotUtils.ReplyAsync_Success(Context, string.Format("Congratulations, your Gotchi has evolved into **{0}**!",
                                            (await BotUtils.GetSpeciesFromDb(gotchi.species_id)).GetShortName()));

                                    }
                                    else {

                                        item_failed = true;

                                        await BotUtils.ReplyAsync_Error(Context, "Your Gotchi is not able to evolve at the current time.");

                                    }

                                }

                            }

                            break;

                        case 4: // alarm clock
                            {

                                Gotchi gotchi = await GotchiUtils.GetPrimaryGotchiByUserAsync(Context.User);

                                if (await GotchiUtils.Reply_ValidateGotchiAsync(Context, gotchi) && gotchi.IsSleeping()) {

                                    using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET born_ts = $born_ts WHERE id = $id")) {

                                        long born_ts = gotchi.born_ts;
                                        born_ts -= gotchi.HoursOfSleepLeft() * 60 * 60;

                                        cmd.Parameters.AddWithValue("$id", gotchi.id);
                                        cmd.Parameters.AddWithValue("$born_ts", born_ts);

                                        await Database.ExecuteNonQuery(cmd);

                                    }

                                    await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** woke up! Its sleep schedule has been reset.",
                                        StringUtils.ToTitleCase(gotchi.name)));

                                }
                                else {

                                    item_failed = true;

                                    await BotUtils.ReplyAsync_Error(Context, "Your Gotchi is already awake.");

                                }

                            }

                            break;

                    }

                }

                // If using the item was successful, subtract the cost of the item from the user's balance.

                if (!item_failed)
                    user_data.G -= (long)item.cost;

                // Update the user.
                await GotchiUtils.UpdateGotchiUserAsync(user_data);

            }
        }

        [Command("list")]
        public async Task List() {

            // List all Gotchis belonging to the current user.

            if (await GotchiUtils.GetGotchiCountAsync(Context.User) <= 0) {

                await GotchiUtils.Reply_ValidateGotchiAsync(Context, null);

            }
            else {

                List<string> gotchi_list = new List<string>();
                int index = 1;

                foreach (Gotchi i in await GotchiUtils.GetGotchisByUserIdAsync(Context.User.Id)) {

                    gotchi_list.Add(string.Format("{0}. **{1}** ({2}), Lv. {3}",
                        index,
                        StringUtils.ToTitleCase(i.name),
                        (await BotUtils.GetSpeciesFromDb(i.species_id)).GetShortName(),
                        (await GotchiStatsUtils.CalculateStats(i)).level));

                    ++index;

                }

                GotchiUser user_data = await GotchiUtils.GetGotchiUserAsync(Context.User);

                PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder();
                embed.AddPages(EmbedUtils.ListToEmbedPages(gotchi_list, fieldName: string.Format("{0}'s Gotchis ({1}/{2})",
                    Context.User.Username,
                    gotchi_list.Count,
                    user_data.GotchiLimit)));

                await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, embed.Build());

            }

        }

        [Command("set"), Alias("primary", "pick", "choose")]
        public async Task Set(string nameOrIndex) {

            // Set the user's primary gotchi.

            if (await GotchiUtils.GetGotchiCountAsync(Context.User) <= 0) {

                await GotchiUtils.Reply_ValidateGotchiAsync(Context, null);

                return;

            }

            long gotchi_id = OurFoodChain.gotchi.Gotchi.NULL_GOTCHI_ID;
            string name = "";

            if (StringUtils.IsNumeric(nameOrIndex)) {

                Gotchi[] gotchis = await GotchiUtils.GetGotchisByUserIdAsync(Context.User.Id);
                long index = long.Parse(nameOrIndex) - 1;

                if (index >= 0 && index < gotchis.Length) {

                    gotchi_id = gotchis[index].id;
                    name = gotchis[index].name;

                }

            }

            if (gotchi_id == OurFoodChain.gotchi.Gotchi.NULL_GOTCHI_ID) {

                Gotchi gotchi = await GotchiUtils.GetGotchiByNameAsync(Context.User.Id, nameOrIndex.ToLower());

                if (!(gotchi is null)) {

                    gotchi_id = gotchi.id;
                    name = gotchi.name;

                }

            }

            if (gotchi_id == OurFoodChain.gotchi.Gotchi.NULL_GOTCHI_ID) {

                await BotUtils.ReplyAsync_Error(Context, string.Format("No Gotchi with the name \"{0}\" exists.", nameOrIndex));

            }
            else {

                GotchiUser user_data = await GotchiUtils.GetGotchiUserAsync(Context.User);

                user_data.PrimaryGotchiId = gotchi_id;

                await GotchiUtils.UpdateGotchiUserAsync(user_data);

                await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully set primary Gotchi to **{0}**.", StringUtils.ToTitleCase(name)));

            }

        }

        private static async Task<bool> _validateGotchiBattleState(ICommandContext context, GotchiBattleState battleState) {

            if (battleState is null)
                return false;

            // Even in CPU battles, the first participant must be a human player still in the server.
            if (await battleState.GetPlayer1UserAsync(context) is null)
                return false;

            // If not battling a CPU, the opponent must still be present in the server.
            if (!battleState.IsBattlingCpu() && await battleState.GetPlayer2UserAsync(context) is null)
                return false;

            return true;

        }
        private static async Task<bool> _replyValidateChallengerGotchiForBattleAsync(ICommandContext context, Gotchi gotchi) {

            if (!await GotchiUtils.Reply_ValidateGotchiAsync(context, gotchi))
                return false;

            if (gotchi.IsDead()) {

                await BotUtils.ReplyAsync_Info(context, "Your gotchi has died, and is unable to battle.");

                return false;

            }

            return true;

        }
        private static async Task<bool> _replyValidateOpponentGotchiForBattleAsync(ICommandContext context, Gotchi gotchi) {

            if (!GotchiUtils.ValidateGotchi(gotchi)) {

                await BotUtils.ReplyAsync_Info(context, "Your opponent doesn't have a gotchi yet.");

                return false;

            }

            if (gotchi.IsDead()) {

                await BotUtils.ReplyAsync_Info(context, "Your opponent's gotchi has died, and is unable to battle.");

                return false;

            }

            return true;

        }
        private static async Task<bool> _replyVerifyChallengerAvailableForBattleAsync(ICommandContext context) {

            GotchiBattleState state = GotchiBattleState.GetBattleStateByUserId(context.User.Id);

            if (!(state is null) && state.accepted) {

                ulong other_user_id = state.player1.gotchi.owner_id == context.User.Id ? state.player2.gotchi.owner_id : state.player1.gotchi.owner_id;
                IUser other_user = await context.Guild.GetUserAsync(other_user_id);

                // We won't lock them into the battle if the other user has left the server.

                if (!(other_user is null) || state.IsBattlingCpu()) {

                    await BotUtils.ReplyAsync_Info(context, string.Format("You are already battling **{0}**. You must finish the battle (or forfeit) before beginning a new one.",
                        state.IsBattlingCpu() ? await state.GetPlayer2UsernameAsync(context) : other_user.Mention));

                    return false;

                }

            }

            return true;

        }
        private static async Task<bool> _replyVerifyOpponentAvailableForBattleAsync(ICommandContext context, IUser user) {

            GotchiBattleState state = GotchiBattleState.GetBattleStateByUserId(user.Id);

            if (!(state is null) && state.accepted) {

                ulong other_user_id = state.player1.gotchi.owner_id == context.User.Id ? state.player2.gotchi.owner_id : state.player1.gotchi.owner_id;
                IUser other_user = await context.Guild.GetUserAsync(other_user_id);

                // We won't lock them into the battle if the other user has left the server.

                if (!(other_user is null)) {

                    await BotUtils.ReplyAsync_Info(context, string.Format("**{0}** is currently battling someone else. Challenge them again later when they have finished.", other_user.Mention));

                    return false;

                }

            }

            return true;

        }

    }

}