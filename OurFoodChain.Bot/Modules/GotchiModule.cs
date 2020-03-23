using Discord;
using Discord.Commands;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Commands;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Utilities;
using OurFoodChain.Extensions;
using OurFoodChain.Gotchis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    [Group("gotchi")]
    public class GotchiModule :
     OfcModuleBase {

        // Public members

        [Command]
        public async Task Gotchi() {

            // Get this user's primary Gotchi.

            Gotchi gotchi = await Db.GetGotchiAsync(Context.User.ToCreator());

            if (!await this.ReplyValidateGotchiAsync(gotchi))
                return;

            // Create the gotchi GIF.

            string gifUrl = await this.ReplyUploadGotchiGifAsync(gotchi);

            if (string.IsNullOrEmpty(gifUrl))
                return;

            // Get the gotchi's species.

            ISpecies species = await Db.GetSpeciesAsync(gotchi.SpeciesId);

            // Pick status text.

            string status = "{0} is feeling happy!";

            switch (gotchi.State) {

                case GotchiState.Dead:
                    status = "Oh no... {0} has died...";
                    break;

                case GotchiState.Evolved:
                    status = "Congratulations, {0} " + string.Format("evolved into {0}!", species.GetShortName());
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

            // Update the viewed timestamp.

            await Db.SetViewedTimestampAsync(gotchi, DateUtilities.GetCurrentTimestampUtc());

            // Send the message.

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithTitle(string.Format("{0}'s \"{1}\"", Context.User.Username, gotchi.Name.ToTitle()));
            embed.WithDescription(string.Format("{0}, age {1}", species.GetShortName(), gotchi.Age));
            embed.WithImageUrl(gifUrl);
            embed.WithFooter(string.Format(status, StringUtilities.ToTitleCase(gotchi.Name)));

            await ReplyAsync(embed: embed.Build());

        }

        [Command("get")]
        public async Task Get() {

            // Select a random base species (no ancestor) for the user to start with.

            IEnumerable<ISpecies> baseSpecies = await Db.GetBaseSpeciesAsync();

            if (baseSpecies.Count() > 0)
                await GetGotchiAsync(baseSpecies.Random());
            else
                await ReplyErrorAsync("There are currently no species available.");

        }
        [Command("get", RunMode = RunMode.Async)]
        public async Task Get(string speciesName) {

            await Get(string.Empty, speciesName);

        }
        [Command("get", RunMode = RunMode.Async)]
        public async Task Get(string genusName, string speciesName) {

            // Get the species that the user specified.

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid())
                await GetGotchiAsync(species);

        }

        [Command("release")]
        public async Task Release() {

            Gotchi gotchi = await Db.GetGotchiAsync(Context.User.ToCreator());

            if (gotchi.IsValid())
                await ReleaseGotchiAsync(gotchi);
            else
                await ReplyErrorAsync("You do not have a gotchi to release.");

        }
        [Command("release")]
        public async Task Release(string name) {

            // Find the Gotchi with the given name.

            Gotchi gotchi = await Db.GetGotchiAsync(Context.User.Id, name);

            if (gotchi.IsValid())
                await ReleaseGotchiAsync(gotchi);
            else
                await ReplyErrorAsync(string.Format("No Gotchi with the name \"{0}\" exists.", name));

        }

        [Command("name")]
        public async Task Name(string name) {

            Gotchi gotchi = await Db.GetGotchiAsync(Context.User.ToCreator());

            if (await this.ReplyValidateGotchiAsync(gotchi)) {

                await Db.SetGotchiNameAsync(gotchi, name);

                await ReplySuccessAsync(string.Format("Sucessfully set {0}'s name to **{1}**.", StringUtilities.ToTitleCase(gotchi.Name), StringUtilities.ToTitleCase(name)));

            }

        }

        [Command("feed")]
        public async Task Feed() {

            Gotchi gotchi = await Db.GetGotchiAsync(Context.User.ToCreator());

            if (await this.ReplyValidateGotchiAsync(gotchi)) {

                if (!gotchi.IsAlive) {

                    await ReplyInfoAsync(string.Format("You went to feed **{0}**, but it looks like it's too late...", StringUtilities.ToTitleCase(gotchi.Name)));

                }
                else if (gotchi.IsSleeping) {

                    await ReplyInfoAsync(string.Format("Shhh, do not disturb! **{0}** is currently asleep. Try feeding them again later.", StringUtilities.ToTitleCase(gotchi.Name)));

                }
                else {

                    await Db.FeedGotchisAsync(Global.GotchiContext, Context.User.Id);

                    if (await Db.GetGotchiCountAsync(Context.User.ToCreator()) > 1)
                        await ReplySuccessAsync("Fed everyone some delicious Suka-Flakes™!");
                    else
                        await ReplySuccessAsync(string.Format("Fed **{0}** some delicious Suka-Flakes™!", StringUtilities.ToTitleCase(gotchi.Name)));

                }

            }

        }

        [Command("stats")]
        public async Task Stats() {

            // Get this user's gotchi.

            Gotchi gotchi = await Db.GetGotchiAsync(Context.User.ToCreator());

            if (await this.ReplyValidateGotchiAsync(gotchi)) {

                ISpecies sp = await Db.GetSpeciesAsync(gotchi.SpeciesId);

                if (sp.IsValid()) {

                    // Calculate stats for this gotchi.
                    // If the user is currently in battle, show their battle stats instead.

                    GotchiStats stats;

                    GotchiBattleState battle_state = GotchiBattleState.GetBattleStateByUserId(Context.User.Id);

                    if (!(battle_state is null))
                        stats = battle_state.GetGotchiStats(gotchi);
                    else
                        stats = await new GotchiStatsCalculator(Db, Global.GotchiContext).GetStatsAsync(gotchi);

                    // Create the embed.

                    EmbedBuilder stats_page = new EmbedBuilder();

                    stats_page.WithTitle(string.Format("{0}'s {2}, **Level {1}** (Age {3})", Context.User.Username, stats.Level, sp.GetShortName(), gotchi.Age));
                    stats_page.WithThumbnailUrl(sp.GetPictureUrl());
                    stats_page.WithFooter(string.Format("{0} experience points until next level", stats.ExperienceToNextLevel));

                    stats_page.AddField("❤ Hit points", stats.Hp, inline: true);
                    stats_page.AddField("💥 Attack", stats.Atk, inline: true);
                    stats_page.AddField("🛡 Defense", stats.Def, inline: true);
                    stats_page.AddField("💨 Speed", stats.Spd, inline: true);

                    await ReplyAsync(embed: stats_page.Build());

                }

            }

        }

        [Command("moves"), Alias("moveset")]
        public async Task Moves() {

            // Get this user's gotchi.

            Gotchi gotchi = await Db.GetGotchiAsync(Context.User.ToCreator());

            if (await this.ReplyValidateGotchiAsync(gotchi)) {

                ISpecies sp = await Db.GetSpeciesAsync(gotchi.SpeciesId);

                if (sp.IsValid()) {

                    // Get moveset for this gotchi.
                    // If the user is currently in battle, get their moveset from the battle state instead.

                    GotchiBattleState battle_state = GotchiBattleState.GetBattleStateByUserId(Context.User.Id);
                    GotchiMoveSet set = await Global.GotchiContext.MoveRegistry.GetMoveSetAsync(Db, gotchi);
                    GotchiMoveSet battle_set = battle_state?.GetGotchiMoveset(gotchi);

                    // Create the embed.

                    EmbedBuilder set_page = new EmbedBuilder();
                    GotchiStats stats = await new GotchiStatsCalculator(Db, Global.GotchiContext).GetStatsAsync(gotchi);

                    set_page.WithTitle(string.Format("{0}'s {2}, **Level {1}** (Age {3})", Context.User.Username, stats.Level, sp.GetShortName(), gotchi.Age));
                    set_page.WithThumbnailUrl(sp.GetPictureUrl());
                    set_page.WithFooter(string.Format("{0} experience points until next level", stats.ExperienceToNextLevel));

                    int move_index = 1;

                    foreach (GotchiMove move in set.Moves)
                        set_page.AddField(string.Format(
                            "Move {0}: **{1}** ({2}/{3} PP)",
                            move_index++,
                            move.Name,
                            battle_set is null ? move.PP : battle_set.GetMove(move.Name).PP,
                            move.PP
                            ), move.Description);

                    await ReplyAsync(embed: set_page.Build());

                }

            }

        }

        [Command("battle"), Alias("challenge", "duel")]
        public async Task Battle(IUser user) {

            // Cannot challenge oneself.

            if (!(user is null) && user.Id == Context.User.Id) {

                await ReplyErrorAsync("You cannot challenge yourself.");

                return;

            }

            // Get this user's gotchi.

            Gotchi gotchi = await Db.GetGotchiAsync(Context.User.ToCreator());

            if (!await _replyValidateChallengerGotchiForBattleAsync(Context, gotchi))
                return;

            // Get the opponent's gotchi.
            // If the opponent is null, assume the user is training. A random gotchi will be generated for them to battle against.

            Gotchi opposing_gotchi = null;

            if (!(user is null)) {

                opposing_gotchi = await Db.GetGotchiAsync(user.ToCreator());

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

            await GotchiBattleState.RegisterBattleAsync(Context, Config, DiscordClient, Db, GotchiService, gotchi, opposing_gotchi);

            if (!(user is null)) {

                // If the user is battling another user, show a message challenging them to battle.
                // Otherwise, the battle state will be shown automatically when calling RegisterBattleAsync.

                await ReplyAsync(string.Format("{0}, **{1}** is challenging you to a battle! Use `{2}gotchi accept` or `{2}gotchi deny` to respond to their challenge.",
                    user.Mention,
                    Context.User.Username,
                    Config.Prefix));

            }

        }

        [Command("train")]
        public async Task Train() {

            // Get this user's gotchi.

            Gotchi gotchi = await Db.GetGotchiAsync(Context.User.ToCreator());

            if (!await this.ReplyValidateGotchiAsync(gotchi))
                return;

            if (Global.GotchiContext.Config.TrainingLimit > 0 && Global.GotchiContext.Config.TrainingCooldown > 0) {

                // Users can train their gotchi by battling random gotchis 3 times every 15 minutes.

                long training_left = 0;
                long training_ts = 0;

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT training_left, training_ts FROM Gotchi WHERE id=$id;")) {

                    cmd.Parameters.AddWithValue("$id", gotchi.Id);

                    DataRow row = await Db.GetRowAsync(cmd);

                    if (!(row is null)) {

                        training_left = row.IsNull("training_left") ? 0 : row.Field<long>("training_left");
                        training_ts = row.IsNull("training_ts") ? 0 : row.Field<long>("training_ts");

                    }

                }

                // If it's been more than 15 minutes since the training timestamp was updated, reset the training count.

                long minutes_elapsed = (DateUtilities.GetCurrentTimestampUtc() - training_ts) / 60;

                if (minutes_elapsed >= Global.GotchiContext.Config.TrainingCooldown) {

                    training_left = Global.GotchiContext.Config.TrainingLimit;
                    training_ts = DateUtilities.GetCurrentTimestampUtc();

                }

                // If the user has no more training attempts left, exit. 

                if (training_left <= 0) {

                    long minutes_left = Global.GotchiContext.Config.TrainingCooldown - minutes_elapsed;

                    await ReplyInfoAsync(string.Format("**{0}** is feeling tired from all the training... Try again in {1} minute{2}.",
                        StringUtilities.ToTitleCase(gotchi.Name),
                        minutes_left <= 1 ? "a" : minutes_left.ToString(),
                        minutes_left > 1 ? "s" : ""));

                    return;

                }

                // Update the user's training data.

                --training_left;

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET training_left=$training_left, training_ts=$training_ts WHERE id=$id;")) {

                    cmd.Parameters.AddWithValue("$id", gotchi.Id);
                    cmd.Parameters.AddWithValue("$training_left", training_left);
                    cmd.Parameters.AddWithValue("$training_ts", training_ts);

                    await Db.ExecuteNonQueryAsync(cmd);

                }

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

                Gotchi gotchi = await Db.GetGotchiAsync(Context.User.ToCreator());
                GotchiTradeRequest trade_request = GotchiUtilities.GetTradeRequest(gotchi);
                bool trade_request_is_valid = !(trade_request is null);

                if (trade_request_is_valid) {

                    if (!await GotchiUtilities.ValidateTradeRequestAsync(Context, Db, trade_request))
                        await ReplyInfoAsync("The trade request has expired, or is invalid.");

                    else {

                        // The trade is valid, so perform the trade.

                        await GotchiUtilities.ExecuteTradeRequestAsync(Context, Db, trade_request);

                        await ReplySuccessAsync(string.Format("**{0}** successfully traded gotchis with **{1}**. Take good care of them!",
                            Context.User.Username,
                            (await Context.Guild.GetUserAsync(trade_request.OfferedGotchi.OwnerId)).Username));

                        return;

                    }

                }

            }

            // The user has no open requests.
            await ReplyInfoAsync("You have no open requests to respond to.");

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

                Gotchi gotchi = await Db.GetGotchiAsync(Context.User.ToCreator());
                GotchiTradeRequest trade_request = GotchiUtilities.GetTradeRequest(gotchi);
                bool trade_request_is_valid = !(trade_request is null);

                if (trade_request_is_valid && await GotchiUtilities.ValidateTradeRequestAsync(Context, Db, trade_request)) {

                    await ReplyAsync(string.Format("{0}, **{1}** has denied your trade request.",
                        (await Context.Guild.GetUserAsync(trade_request.OfferedGotchi.OwnerId)).Mention,
                        Context.User.Username));

                    return;

                }

            }

            // The user has no open requests.
            await ReplyInfoAsync("You have no open requests to respond to.");

        }

        [Command("move"), Alias("use")]
        public async Task Move(string moveIdentifier) {

            // Get the battle state that the user is involved with.

            GotchiBattleState state = GotchiBattleState.GetBattleStateByUserId(Context.User.Id);

            // If the state is null, the user has not been challenged to a battle.

            if (state is null || (!state.IsBattlingCpu() && (await state.GetOtherPlayerAsync(Context, Context.User.Id)) is null)) {

                await ReplyErrorAsync("You have not been challenged to a battle.");

                return;

            }

            // If the other user hasn't accepted the battle yet, don't allow the user to select a move.

            if (!state.accepted) {

                await ReplyInfoAsync(string.Format("**{0}** has not accepted your challenge yet.",
                    await state.GetPlayer2UsernameAsync(Context)));

                return;

            }

            // Attempt to select the user's chosen move.
            // If anything goes wrong, the battle state will alert the user.
            await state.SelectMoveAsync(Context, moveIdentifier);

        }

        [Command("run"), Alias("forfeit", "quit")]
        public async Task Forfeit() {

            // Get the battle state that the user is involved with.
            GotchiBattleState state = GotchiBattleState.GetBattleStateByUserId(Context.User.Id);

            if (!await _validateGotchiBattleState(Context, state))
                await ReplyErrorAsync("You are not currently in battle.");

            else {

                // Deregister the battle state.
                GotchiBattleState.DeregisterBattle(Context.User.Id);

                await ReplyInfoAsync(string.Format("**{0}** has forfeited the battle.", Context.User.Username));

            }

        }

        [Command("help"), Alias("h")]
        public async Task Help() {

            IEnumerable<ICommandHelpInfo> helpInfos = await HelpService.GetCommandHelpInfoAsync(Context);

            await ReplyAsync(embed: EmbedUtilities.BuildCommandHelpInfoEmbed(helpInfos, Config, "gotchi").ToDiscordEmbed());

        }
        [Command("help"), Alias("h")]
        public async Task Help(string commandName) {

            ICommandHelpInfo helpInfo = await HelpService.GetCommandHelpInfoAsync("gotchi " + commandName.Trim());

            await ReplyAsync(embed: EmbedUtilities.BuildCommandHelpInfoEmbed(helpInfo, Config).ToDiscordEmbed());

        }

        [Command("trade")]
        public async Task Trade(IUser user) {

            // Cannot trade with oneself.

            if (Context.User.Id == user.Id) {

                await BotUtils.ReplyAsync_Info(Context, "You cannot trade with yourself.");

            }
            else {

                Gotchi gotchi = await Db.GetGotchiAsync(Context.User.ToCreator());
                Gotchi partnerGotchi = await Db.GetGotchiAsync(user.ToCreator());

                // Submit the trade request.

                switch (await GotchiUtilities.MakeTradeRequestAsync(Context, Db, gotchi, partnerGotchi)) {

                    case GotchiTradeRequestResult.Success:

                        await ReplyAsync(string.Format("{0}, **{1}** wants to trade gotchis with you! Use `{2}gotchi accept` or `{2}gotchi deny` to respond to their trade request.",
                            user.Mention,
                            Context.User.Username,
                            Config.Prefix));

                        break;

                    case GotchiTradeRequestResult.RequestPending:

                        await ReplyInfoAsync(string.Format("**{0}** currently has another pending trade request. Please try again later.",
                            user.Username));

                        break;

                    case GotchiTradeRequestResult.Invalid:

                        await ReplyInfoAsync("Trade request is invalid.");

                        break;

                }

            }

        }

        [Command("shop")]
        public async Task Shop() {

            GotchiUserInfo user_data = await Db.GetUserInfoAsync(Context.User.ToCreator());

            // List all gotchi items available for sale.

            // Generate a field for each item.

            GotchiItem[] items = await GotchiUtilities.GetGotchiItemsAsync();
            List<IEmbedField> item_fields = new List<IEmbedField>();

            foreach (GotchiItem item in items) {

                // (Tank expansions cost extra the more the user has already.)

                //if (item.Id == 1)
                //    item.Price *= user_data.GotchiLimit;

                // Build the field.

                item_fields.Add(new Discord.Messaging.EmbedField {
                    Name = string.Format("{0}. {1} {2} — {3}", item.Id, item.Icon, item.Name, item.Price <= 0 ? "Not Available" : (item.Price.ToString("n0") + "G")),
                    Value = item.Description
                });

            }

            // Create the embed.

            IPaginatedMessage embed = new PaginatedMessage();

            embed.AddFields(item_fields);
            embed.SetTitle("🛒 Gotchi Shop");
            embed.SetDescription($"Welcome to the Gotchi Shop! Purchase an item with `{Config.Prefix}gotchi buy <item>`.");
            embed.SetFooter(string.Format("You currently have {0:n0}G.", user_data.G));
            embed.SetColor(Color.LightOrange);
            embed.AddPageNumbers();

            await ReplyAsync(embed);

        }

        [Command("buy")]
        public async Task Buy(string itemIdentifier, long count = 1) {

            GotchiItem item = await GotchiUtilities.GetGotchiItemAsync(itemIdentifier);
            GotchiUserInfo userInfo = await Db.GetUserInfoAsync(Context.User.ToCreator());

            // Calculate the price of the item.

            long totalPrice = item.Price * count;

            if (item != null && userInfo != null && item.Id != GotchiItem.NullId && count >= 1) {

                if (totalPrice <= userInfo.G) {

                    // The user can afford the item, so purchase it and add it to their inventory.

                    await Db.AddItemToInventoryAsync(userInfo.UserId, item, count);

                    // Update the user.

                    userInfo.G -= totalPrice;

                    if (item.Id == (int)GotchiItemId.TankExpansion)
                        userInfo.GotchiLimit += count;

                    await Db.UpdateUserInfoAsync(userInfo);

                    await BotUtils.ReplyAsync_Success(Context,
                        string.Format("Successfully added **{0}**{1} to {2}'s inventory.", item.Name, count == 1 ? "" : "×" + count, Context.User.Username));

                }
                else {

                    // The user cannot afford the purchase.

                    await BotUtils.ReplyAsync_Error(Context, string.Format("You don't have enough G to afford this purchase ({0:n0}G).", totalPrice));

                }

            }
            else
                await BotUtils.ReplyAsync_Error(Context, "Invalid item selection.");

        }

        [Command("inventory"), Alias("inv", "items", "bag")]
        public async Task Inventory() {

            IUser user = Context.User;
            GotchiUserInfo userInfo = await Db.GetUserInfoAsync(user.ToCreator());
            GotchiInventory inventory = await Db.GetInventoryAsync(user.Id);

            List<string> lines = new List<string>();

            foreach (GotchiInventoryItem item in inventory) {

                lines.Add(string.Format("**`{0}.`** {1} {3} ⨯**{2}**",
                    (lines.Count + 1).ToString("000"),
                    string.IsNullOrEmpty(item.Item.Icon) ? "➖" : item.Item.Icon,
                    item.Count.ToString(),
                    string.IsNullOrEmpty(item.Item.Name) ? "Unknown Item" : item.Item.Name
                ));

            }

            IPaginatedMessage embed = new PaginatedMessage();

            if (!lines.Any())
                embed.AddPage(new Discord.Messaging.Embed());
            else
                embed.AddLines(lines);

            embed.SetTitle($"{user.Username}'s inventory");
            embed.SetFooter(string.Format("You currently have {0:n0}G.", userInfo.G));
            embed.AddPageNumbers();

            if (!lines.Any())
                embed.SetDescription("Your inventory is empty.");

            await ReplyAsync(embed);

        }

        [Command("useitem", RunMode = RunMode.Async)]
        public async Task UseItem(string itemIdentifier) {

            IUser user = Context.User;

            Gotchi gotchi = await Db.GetGotchiAsync(user.ToCreator());
            GotchiInventory inventory = await Db.GetInventoryAsync(user.Id);
            GotchiInventoryItem item = null;

            if (int.TryParse(itemIdentifier, out int itemIndex))
                item = inventory.GetItemByIndex(itemIndex);
            else
                item = inventory.GetItemByIdentifier(itemIdentifier);

            if (item != null && item.Count > 0) {

                switch ((GotchiItemId)item.Item.Id) {

                    case GotchiItemId.EvoStone:
                    case GotchiItemId.GlowingEvoStone: {

                            async Task doUseItem(string desiredEvo) {

                                if (desiredEvo.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                                    return;

                                if (await this.ReplyValidateGotchiAsync(gotchi) && await Db.EvolveAndUpdateGotchiAsync(gotchi, desiredEvo)) {

                                    await Db.AddItemToInventoryAsync(user.Id, item.Item, -1);

                                    await BotUtils.ReplyAsync_Success(Context, string.Format("Congratulations, your Gotchi has evolved into **{0}**!",
                                        (await Db.GetSpeciesAsync(gotchi.SpeciesId)).GetShortName()));

                                }
                                else
                                    await BotUtils.ReplyAsync_Info(Context, "Your Gotchi is not able to evolve at the current time.");
                            }

                            if ((GotchiItemId)item.Item.Id == GotchiItemId.GlowingEvoStone) {

                                // Allow the user to pick the species their gotchi evolves into.

                                Discord.Messaging.IResponsiveMessageResponse response = await ResponsiveMessageService.GetResponseAsync(Context, new Discord.Messaging.Message("Reply with the name of the desired species."));

                                if (!response.Canceled)
                                    await doUseItem(response.Message.Text);

                            }
                            else {

                                // Pick a species at random for the gotchi to evolve into.

                                await doUseItem(string.Empty);

                            }

                        }

                        break;

                    case GotchiItemId.AlarmClock: {

                            if (await this.ReplyValidateGotchiAsync(gotchi) && gotchi.IsSleeping) {

                                gotchi.BornTimestamp -= gotchi.HoursOfSleepLeft() * 60 * 60;

                                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET born_ts = $born_ts WHERE id = $id")) {

                                    cmd.Parameters.AddWithValue("$id", gotchi.Id);
                                    cmd.Parameters.AddWithValue("$born_ts", gotchi.BornTimestamp);

                                    await Db.ExecuteNonQueryAsync(cmd);

                                }

                                await Db.AddItemToInventoryAsync(user.Id, item.Item, -1);

                                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** woke up! Its sleep schedule has been reset.",
                                    StringUtilities.ToTitleCase(gotchi.Name)));

                            }
                            else
                                await BotUtils.ReplyAsync_Info(Context, "Your Gotchi is already awake.");

                        }

                        break;

                    default:
                        await BotUtils.ReplyAsync_Info(Context, "This item cannot be used.");
                        break;

                }

            }
            else
                await BotUtils.ReplyAsync_Error(Context, "This item is not in your inventory.");

        }

        [Command("list")]
        public async Task List() {

            // List all Gotchis belonging to the current user.

            if (await Db.GetGotchiCountAsync(Context.User.ToCreator()) <= 0) {

                await this.ReplyValidateGotchiAsync(null);

            }
            else {

                List<string> gotchi_list = new List<string>();
                int index = 1;

                foreach (Gotchi i in await Db.GetGotchisAsync(Context.User.Id)) {

                    gotchi_list.Add(string.Format("{0}. **{1}** ({2}), Lv. {3}",
                        index,
                        StringUtilities.ToTitleCase(i.Name),
                        (await Db.GetSpeciesAsync(i.SpeciesId)).GetShortName(),
                        GotchiExperienceCalculator.GetLevel(ExperienceGroup.Default, i)));

                    ++index;

                }

                GotchiUserInfo user_data = await Db.GetUserInfoAsync(Context.User.ToCreator());

                IPaginatedMessage embed = new PaginatedMessage();

                embed.AddLines($"{Context.User.Username}'s Gotchis ({gotchi_list.Count}/{user_data.GotchiLimit})", gotchi_list);

                await ReplyAsync(embed);

            }

        }

        [Command("set"), Alias("primary", "pick", "choose")]
        public async Task Set(string nameOrIndex) {

            // Set the user's primary gotchi.

            if (await Db.GetGotchiCountAsync(Context.User.ToCreator()) <= 0) {

                await this.ReplyValidateGotchiAsync(null);

                return;

            }

            long gotchi_id = Gotchis.Gotchi.NullGotchiId;
            string name = "";

            if (StringUtilities.IsNumeric(nameOrIndex)) {

                IEnumerable<Gotchi> gotchis = await Db.GetGotchisAsync(Context.User.Id);
                int index = int.Parse(nameOrIndex) - 1;

                if (index >= 0 && index < gotchis.Count()) {

                    gotchi_id = gotchis.ElementAt(index).Id;
                    name = gotchis.ElementAt(index).Name;

                }

            }

            if (gotchi_id == Gotchis.Gotchi.NullGotchiId) {

                Gotchi gotchi = await Db.GetGotchiAsync(Context.User.Id, nameOrIndex.ToLower());

                if (!(gotchi is null)) {

                    gotchi_id = gotchi.Id;
                    name = gotchi.Name;

                }

            }

            if (gotchi_id == OurFoodChain.Gotchis.Gotchi.NullGotchiId) {

                await BotUtils.ReplyAsync_Error(Context, string.Format("No Gotchi with the name \"{0}\" exists.", nameOrIndex));

            }
            else {

                GotchiUserInfo user_data = await Db.GetUserInfoAsync(Context.User.ToCreator());

                user_data.PrimaryGotchiId = gotchi_id;

                await Db.UpdateUserInfoAsync(user_data);

                await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully set primary Gotchi to **{0}**.", StringUtilities.ToTitleCase(name)));

            }

        }

        [Command("dex")]
        public async Task Dex() {

            // Show the dex entry for the user's current gotchi.

            Gotchi gotchi = await this.GetGotchiOrReplyAsync(Context.User.ToCreator());

            if (gotchi.IsValid()) {

                ISpecies species = await Db.GetSpeciesAsync(gotchi.SpeciesId);

                if (species.IsValid())
                    await ShowDexEntryAsync(species);

            }

        }
        [Command("dex")]
        public async Task Dex(string speciesName) {

            await Dex(string.Empty, speciesName);

        }
        [Command("dex", RunMode = RunMode.Async)]
        public async Task Dex(string genusName, string speciesName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            await ShowDexEntryAsync(species);

        }

        // Private members

        private async Task<bool> _validateGotchiBattleState(ICommandContext context, GotchiBattleState battleState) {

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
        private async Task<bool> _replyValidateChallengerGotchiForBattleAsync(ICommandContext context, Gotchi gotchi) {

            if (!await this.ReplyValidateGotchiAsync(gotchi))
                return false;

            if (!gotchi.IsAlive) {

                await BotUtils.ReplyAsync_Info(context, "Your gotchi has died, and is unable to battle.");

                return false;

            }

            return true;

        }
        private async Task<bool> _replyValidateOpponentGotchiForBattleAsync(ICommandContext context, Gotchi gotchi) {

            if (!gotchi.IsValid()) {

                await BotUtils.ReplyAsync_Info(context, "Your opponent doesn't have a gotchi yet.");

                return false;

            }

            if (!gotchi.IsAlive) {

                await BotUtils.ReplyAsync_Info(context, "Your opponent's gotchi has died, and is unable to battle.");

                return false;

            }

            return true;

        }
        private async Task<bool> _replyVerifyChallengerAvailableForBattleAsync(ICommandContext context) {

            GotchiBattleState state = GotchiBattleState.GetBattleStateByUserId(context.User.Id);

            if (!(state is null) && state.accepted) {

                ulong other_user_id = state.player1.Gotchi.Gotchi.OwnerId == context.User.Id ? state.player2.Gotchi.Gotchi.OwnerId : state.player1.Gotchi.Gotchi.OwnerId;
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

                ulong other_user_id = state.player1.Gotchi.Gotchi.OwnerId == context.User.Id ? state.player2.Gotchi.Gotchi.OwnerId : state.player1.Gotchi.Gotchi.OwnerId;
                IUser other_user = await context.Guild.GetUserAsync(other_user_id);

                // We won't lock them into the battle if the other user has left the server.

                if (!(other_user is null)) {

                    await BotUtils.ReplyAsync_Info(context, string.Format("**{0}** is currently battling someone else. Challenge them again later when they have finished.", other_user.Mention));

                    return false;

                }

            }

            return true;

        }

        private async Task GetGotchiAsync(ISpecies species) {

            // The species must be a base species (e.g., doesn't evolve from anything).

            if (await Db.IsBaseSpeciesAsync(species)) {

                // If the user has already reached their gotchi limit, don't allow them to get any more.

                GotchiUserInfo user_data = await Db.GetUserInfoAsync(Context.User.ToCreator());
                long gotchi_count = await Db.GetGotchiCountAsync(Context.User.ToCreator());

                if (gotchi_count >= user_data.GotchiLimit) {

                    // If the user's primary Gotchi is dead, remove it to be replaced. Otherwise, the new Gotchi cannot be added.

                    Gotchi primary_gotchi = await Db.GetGotchiAsync(Context.User.ToCreator());

                    if (!primary_gotchi.IsAlive)
                        await Db.DeleteGotchiAsync(primary_gotchi.Id);

                    else {

                        await ReplyErrorAsync("You don't have room for any more Gotchis!\n\nYou will need to release one of your Gotchis, or expand your Gotchi tank. See the `gotchi release` and `gotchi shop` commands for more details.");

                        return;

                    }

                }

                // Create a gotchi for this user.

                await Db.AddGotchiAsync(Context.User.ToCreator(), species);

                await ReplySuccessAsync(string.Format("All right **{0}**, take care of your new **{1}**!", Context.User.Username, species.GetShortName()));

            }
            else {

                await ReplyErrorAsync("You must start with a base species (i.e. a species with no ancestor).");

            }

        }
        private async Task ReleaseGotchiAsync(Gotchi gotchi) {

            if (gotchi.IsValid()) {

                Discord.Messaging.IPaginatedMessage message = new Discord.Messaging.PaginatedMessage($"Are you sure you want to release **{gotchi.Name.ToTitle()}**?") {
                    Restricted = true
                };

                message.AddReaction(Discord.Messaging.PaginatedMessageReactionType.Yes, async (args) => {

                    if (args.Reaction == Discord.Messaging.PaginatedMessageReactionType.Yes) {

                        // Delete the Gotchi from the database.

                        await Db.DeleteGotchiAsync(gotchi.Id);

                        // Show confirmation message.

                        if (!gotchi.IsAlive)
                            await ReplySuccessAsync(string.Format("**{0}**'s corpse was successfully flushed. Rest in peace, **{0}**!", gotchi.Name.ToTitle()));
                        else
                            await ReplySuccessAsync(string.Format("**{0}** was successfully released. Take care, **{0}**!", gotchi.Name.ToTitle()));

                    }

                });

                await ReplyAsync(message);

            }

        }

        public async Task ShowDexEntryAsync(ISpecies species) {

            if (species.IsValid()) {

                Gotchi gotchi = new Gotchi() {
                    SpeciesId = species.Id,
                    Experience = GotchiExperienceCalculator.ExperienceToLevel(ExperienceGroup.Default, 100)
                };

                GotchiType[] types = await Global.GotchiContext.TypeRegistry.GetTypesAsync(Db, gotchi);
                IEnumerable<GotchiMove> moves = await Global.GotchiContext.MoveRegistry.GetLearnSetAsync(Db, gotchi);
                GotchiStats stats = await new GotchiStatsCalculator(Db, Global.GotchiContext).GetBaseStatsAsync(gotchi);

                // Create the stats page.

                string typesString = string.Format("**Type(s):** {0}", types.Any() ? string.Join(", ", types.Select(t => t.Name)) : "None");

                StringBuilder descriptionBuilder = new StringBuilder();
                descriptionBuilder.AppendLine(typesString);
                descriptionBuilder.AppendLine();
                descriptionBuilder.AppendLine(string.Format("*{0}*", StringUtilities.GetFirstSentence(species.Description)));
                descriptionBuilder.AppendLine("\u200B");

                Discord.Messaging.IEmbed statsPageBuilder = new Discord.Messaging.Embed {
                    Title = string.Format("{0} (#{1}) — Overview", species.GetShortName(), species.Id),
                    Description = descriptionBuilder.ToString(),
                    ImageUrl = species.GetPictureUrl()
                };

                statsPageBuilder.AddField("❤ Base HP", stats.MaxHp, inline: true);
                statsPageBuilder.AddField("💥 Base Attack", stats.Atk, inline: true);
                statsPageBuilder.AddField("🛡 Base Defense", stats.Def, inline: true);
                statsPageBuilder.AddField("💨 Base Speed", stats.Spd, inline: true);

                // Create the learnset pages.

                List<IEmbedField> moveFields = new List<IEmbedField>();

                foreach (GotchiMove move in moves.OrderBy(m => m.Requires.MinimumLevelValue)) {

                    IEmbedField field = new Discord.Messaging.EmbedField {
                        Name = string.Format("{0}. **{1}** ({2} PP)", moveFields.Count + 1, move.Name, move.PP),
                        Value = move.Description
                    };

                    moveFields.Add(field);

                }

                IPaginatedMessage message = new PaginatedMessage();

                message.AddPage(statsPageBuilder);
                message.AddFields(moveFields);

                foreach (Discord.Messaging.IEmbed embed in message.Skip(1).Select(m => m.Embed))
                    embed.Title = $"{species.GetShortName()} (#{species.Id}) — Learnset";

                message.AddPageNumbers();

                await ReplyAsync(message);

            }

        }

    }

}