using Discord;
using Discord.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public class GotchiBattleState {

        public const ulong WILD_GOTCHI_USER_ID = 0;
        public const long WILD_GOTCHI_ID = -1;

        private static ConcurrentDictionary<ulong, GotchiBattleState> BATTLE_STATES = new ConcurrentDictionary<ulong, GotchiBattleState>();

        public Gotchi gotchi1;
        public Gotchi gotchi2;
        public GotchiStats stats1;
        public GotchiStats stats2;

        public bool accepted = false;
        public string message = "";
        public int turnCount = 1;
        public int currentTurn = 1;

        public async Task<IUser> GetUser1Async(ICommandContext context) {

            return await context.Guild.GetUserAsync(gotchi1.owner_id);

        }
        public async Task<IUser> GetUser2Async(ICommandContext context) {

            return await context.Guild.GetUserAsync(gotchi2.owner_id);

        }
        public async Task<string> GetUsername1Async(ICommandContext context) {

            if (gotchi1.owner_id == WILD_GOTCHI_USER_ID)
                return gotchi1.name;

            return (await GetUser1Async(context)).Username;

        }
        public async Task<string> GetUsername2Async(ICommandContext context) {

            if (gotchi2.owner_id == WILD_GOTCHI_USER_ID)
                return gotchi2.name;

            return (await GetUser2Async(context)).Username;

        }
        public async Task<IUser> GetOtherUserAsync(ICommandContext context, ulong userId) {

            ulong other_user_id = GetOtherUserId(userId);
            IUser other_user = await context.Guild.GetUserAsync(other_user_id);

            return other_user;

        }
        public ulong GetOtherUserId(ulong userId) {

            ulong other_user_id = gotchi1.owner_id == userId ? gotchi2.owner_id : gotchi1.owner_id;

            return other_user_id;

        }
        public bool IsTurn(ulong userId) {

            return (currentTurn == 1 && userId == gotchi1.owner_id) || (currentTurn == 2 && userId == gotchi2.owner_id);

        }
        public Gotchi GetGotchi(ulong userId) {

            return userId == gotchi1.owner_id ? gotchi1 : gotchi2;

        }
        public GotchiStats GetStats(Gotchi gotchi) {

            if (gotchi.id == gotchi1.id)
                return stats1;
            else if (gotchi.id == gotchi2.id)
                return stats2;

            return null;

        }
        public async Task UseMoveAsync(ICommandContext context, GotchiMove move) {

            Gotchi user = currentTurn == 1 ? gotchi1 : gotchi2;
            Gotchi other = currentTurn == 1 ? gotchi2 : gotchi1;

            ++turnCount;
            currentTurn = currentTurn == 1 ? 2 : 1;

            if (move.target == MoveTarget.Self)
                await _useMoveOnGotchiAsync(context, move, user, user);
            else
                await _useMoveOnGotchiAsync(context, move, user, other);

            StringBuilder reply = new StringBuilder();

            reply.AppendLine(message);
            reply.AppendLine();

            // Check if either gotchi has fainted. If so, the battle is over, and EXP will be awarded.

            if (stats1.hp <= 0.0 || stats2.hp <= 0.0) {

                Gotchi winner = stats1.hp <= 0.0 ? gotchi2 : gotchi1;
                Gotchi loser = stats1.hp <= 0.0 ? gotchi1 : gotchi2;

                // Calculate the amount of EXP awarded to the winner.
                // The loser will get 50% of the winner's EXP.

                double exp = _getExpEarned(winner, loser, won: true);

                double exp1 = stats2.hp <= 0.0 ? exp : exp * .5;
                double exp2 = stats1.hp <= 0.0 ? exp : exp * .5;

                if (!IsCpuGotchi(winner))
                    reply.AppendLine(string.Format("**{0}** won the battle! Earned **{1} EXP**.", StringUtils.ToTitleCase(winner.name), winner.id == gotchi1.id ? exp1 : exp2));
                else
                    reply.AppendLine(string.Format("**{0}** lost the battle...", StringUtils.ToTitleCase(loser.name)));

                if (!IsCpuGotchi(loser))
                    reply.AppendLine(string.Format("**{0}** earned **{1} EXP**.", StringUtils.ToTitleCase(loser.name), loser.id == gotchi1.id ? exp1 : exp2));

                reply.AppendLine();

                long levels1 = stats1.LeveUp(exp1);
                long levels2 = stats2.LeveUp(exp2);

                if (levels1 > 0)
                    reply.AppendLine(string.Format("**{0}** leveled up to level **{1}**!", StringUtils.ToTitleCase(gotchi1.name), stats1.level));

                if (!IsCpuGotchi(gotchi2) && levels2 > 0)
                    reply.AppendLine(string.Format("**{0}** leveled up to level **{1}**!", StringUtils.ToTitleCase(gotchi2.name), stats2.level));

                // If the gotchi has leveled up to multiple 10, evolve randomly.
                // If multiple sets of 10 are passed, it will still only evolve once.

                reply.AppendLine();

                if (((stats1.level - levels1) / 10) < (stats1.level / 10))
                    if (await GotchiUtils.EvolveGotchiAsync(gotchi1)) {

                        Species sp = await BotUtils.GetSpeciesFromDb(gotchi1.species_id);

                        reply.AppendLine(string.Format("Congratulations, **{0}** evolved into **{1}**!", StringUtils.ToTitleCase(gotchi1.name), sp.GetShortName()));

                    }

                if (!IsCpuGotchi(gotchi2) && (((stats2.level - levels2) / 10) < (stats2.level / 10)))
                    if (await GotchiUtils.EvolveGotchiAsync(gotchi2)) {

                        Species sp = await BotUtils.GetSpeciesFromDb(gotchi2.species_id);

                        reply.AppendLine(string.Format("Congratulations, **{0}** evolved into **{1}**!", StringUtils.ToTitleCase(gotchi2.name), sp.GetShortName()));

                    }

                // Update level/exp in the database.

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET level=$level, exp=$exp WHERE id=$id;")) {

                    cmd.Parameters.AddWithValue("$id", gotchi1.id);
                    cmd.Parameters.AddWithValue("$level", stats1.level);
                    cmd.Parameters.AddWithValue("$exp", stats1.exp);

                    await Database.ExecuteNonQuery(cmd);

                }

                if (!IsCpuGotchi(gotchi2)) {

                    using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET level=$level, exp=$exp WHERE id=$id;")) {

                        cmd.Parameters.AddWithValue("$id", gotchi2.id);
                        cmd.Parameters.AddWithValue("$level", stats2.level);
                        cmd.Parameters.AddWithValue("$exp", stats2.exp);

                        await Database.ExecuteNonQuery(cmd);

                    }

                }

                // Deregister the battle state.

                DeregisterBattle(context.User.Id);

            }
            else {

                string user_mention = "";

                if (currentTurn == 1)
                    user_mention = (await GetUser1Async(context)).Mention;
                else if (currentTurn == 2 && !(gotchi2.owner_id == WILD_GOTCHI_USER_ID))
                    user_mention = (await GetUser2Async(context)).Mention;

                if (!string.IsNullOrEmpty(user_mention)) {

                    reply.AppendLine(string.Format("It's {1}'s turn! Pick a move with `{0}gotchi move`. \nSee your gotchi's moveset with `{0}gotchi moveset`.",
                        OurFoodChainBot.GetInstance().GetConfig().prefix, user_mention));

                }

            }

            message = reply.ToString();

            // If battling the CPU, have them retaliate before showing the battle state.
            if (IsBattlingCpu() && user.owner_id != WILD_GOTCHI_USER_ID && !IsBattleOver())
                await UseMoveAsync(context, (await GotchiMoveset.GetMovesetAsync(gotchi2)).GetRandomMove());
            else
                await ShowBattleStateAsync(context, this);

        }
        public bool IsBattleOver() {

            return stats1.hp <= 0.0 || stats2.hp <= 0.0;

        }
        public bool IsBattlingCpu() {

            if (!(gotchi2 is null))
                return gotchi2.owner_id == WILD_GOTCHI_USER_ID;

            return false;

        }
        public bool IsCpuGotchi(Gotchi gotchi) {

            return gotchi.owner_id == WILD_GOTCHI_USER_ID;

        }

        public static async Task RegisterBattleAsync(ICommandContext context, Gotchi gotchi1, Gotchi gotchi2) {

            GotchiBattleState state = new GotchiBattleState {
                gotchi1 = gotchi1,
                stats1 = await GotchiStats.CalculateStats(gotchi1)
            };

            if (!(gotchi2 is null)) {

                // If an opponent was provided, use that opponent.

                state.gotchi2 = gotchi2;
                state.stats2 = await GotchiStats.CalculateStats(gotchi2);

            }
            else {

                // Otherwise, generate an opponent for the user.

                await state._generateOpponent();

                state.accepted = true;

            }

            if (state.stats1.spd != state.stats2.spd)
                state.currentTurn = state.stats1.spd > state.stats2.spd ? 1 : 2;
            else
                state.currentTurn = BotUtils.RandomInteger(1, 3);

            BATTLE_STATES[gotchi1.owner_id] = state;

            if (state.gotchi2.owner_id != WILD_GOTCHI_USER_ID)
                BATTLE_STATES[state.gotchi2.owner_id] = state;

            // Set the initial message displayed when the battle starts.

            StringBuilder message_builder = new StringBuilder();

            if (state.stats1.spd != state.stats2.spd) {

                message_builder.AppendLine(string.Format("The battle has begun! **{0}** is faster, so {1} goes first.",
                StringUtils.ToTitleCase(state.stats1.spd > state.stats2.spd ? state.gotchi1.name : state.gotchi2.name),
                state.stats1.spd > state.stats2.spd ? await state.GetUsername1Async(context) : await state.GetUsername2Async(context)));

            }
            else {

                message_builder.AppendLine(string.Format("The battle has begun! {0} has been randomly selected to go first.",
                state.currentTurn == 1 ? await state.GetUsername1Async(context) : await state.GetUsername2Async(context)));

            }

            if (!(state.currentTurn == 2 && state.gotchi2.owner_id == WILD_GOTCHI_USER_ID)) {

                // Only show the "pick a move" message if the next move isn't the CPU.

                message_builder.AppendLine();
                message_builder.AppendLine(string.Format("Pick a move with `{0}gotchi move`.\nSee your gotchi's moveset with `{0}gotchi moveset`.",
                    OurFoodChainBot.GetInstance().GetConfig().prefix));

                state.message = message_builder.ToString();

                if (state.IsBattlingCpu())
                    await ShowBattleStateAsync(context, state);

            }
            else {

                message_builder.AppendLine();
                state.message = message_builder.ToString();

                // Otherwise, have the CPU select and use a move.
                await state.UseMoveAsync(context, (await GotchiMoveset.GetMovesetAsync(state.gotchi2)).GetRandomMove());

            }

        }
        public static void DeregisterBattle(ulong userId) {

            GotchiBattleState state;

            if (BATTLE_STATES.TryRemove(userId, out state))
                BATTLE_STATES.TryRemove(state.GetOtherUserId(userId), out state);

        }
        public static bool IsCurrentlyBattling(ulong userId) {

            if (BATTLE_STATES.ContainsKey(userId))
                return true;

            return false;


        }
        public static GotchiBattleState GetBattleStateByUser(ulong userId) {

            if (!BATTLE_STATES.ContainsKey(userId))
                return null;

            return BATTLE_STATES[userId];

        }
        public static async Task ShowBattleStateAsync(ICommandContext context, GotchiBattleState state) {

            // Get an image of the battle.

            string gif_url = "";

            GotchiUtils.GotchiGifCreatorParams p1 = new GotchiUtils.GotchiGifCreatorParams {
                gotchi = state.gotchi1,
                x = 50,
                y = 150,
                state = state.stats1.hp > 0 ? (state.stats2.hp <= 0 ? GotchiState.Happy : GotchiState.Energetic) : GotchiState.Dead,
                auto = false
            };

            GotchiUtils.GotchiGifCreatorParams p2 = new GotchiUtils.GotchiGifCreatorParams {
                gotchi = state.gotchi2,
                x = 250,
                y = 150,
                state = state.stats2.hp > 0 ? (state.stats1.hp <= 0 ? GotchiState.Happy : GotchiState.Energetic) : GotchiState.Dead,
                auto = false
            };

            gif_url = await GotchiUtils.Reply_GenerateAndUploadGotchiGifAsync(context, new GotchiUtils.GotchiGifCreatorParams[] { p1, p2 }, new GotchiUtils.GotchiGifCreatorExtraParams {
                backgroundFileName = "home_battle.png",
                overlay = (Graphics gfx) => {

                    // Draw health bars.

                    _drawHealthBar(gfx, p1.x, 180, state.stats1.hp / state.stats1.maxHp);
                    _drawHealthBar(gfx, p2.x, 180, state.stats2.hp / state.stats2.maxHp);

                }
            });

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithTitle(string.Format("**{0}** vs. **{1}** (Turn {2})", StringUtils.ToTitleCase(state.gotchi1.name), StringUtils.ToTitleCase(state.gotchi2.name), state.turnCount));
            embed.WithImageUrl(gif_url);
            embed.WithDescription(state.message);
            if (!state.IsBattleOver())
                embed.WithFooter(string.Format("It is now {0}'s turn.", state.currentTurn == 1 ? await state.GetUsername1Async(context) : await state.GetUsername2Async(context)));
            else
                embed.WithFooter("The battle has ended!");

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }

        private async Task _useMoveOnGotchiAsync(ICommandContext context, GotchiMove move, Gotchi user, Gotchi target) {

            GotchiStats user_stats = GetStats(user);
            GotchiStats target_stats = GetStats(target);

            // Check roles to see if the move is super-effective.

            Role[] target_roles = await BotUtils.GetRolesFromDbBySpecies(target.species_id);
            double type_multiplier = _getTypeMultiplier(move.role, target_roles);
            StringBuilder message_builder = new StringBuilder();
            string message_end = "";

            switch (move.type) {

                case MoveType.Attack:

                    // Have a chance of missing.

                    if (BotUtils.RandomInteger(0, (int)(10 * move.hitRate)) == 0) {

                        message_builder.Append(string.Format("💥 **{0}** used **{1}**, but it missed!",
                          StringUtils.ToTitleCase(user.name),
                          StringUtils.ToTitleCase(move.name)));

                    }
                    else {

                        double damage = user_stats.atk;

                        if (!(move.callback is null)) {

                            GotchiMoveCallbackArgs args = new GotchiMoveCallbackArgs {
                                state = this,
                                user = user,
                                userStats = user_stats,
                                target = target,
                                targetStats = target_stats,
                                value = damage,
                                move = move
                            };

                            await move.callback(args);

                            message_end = args.messageFormat;
                            damage = args.value;

                        }

                        for (int i = 0; i < move.times; ++i) {

                            bool critical = BotUtils.RandomInteger(0, (int)(10 / move.criticalRate)) == 0;
                            damage = Math.Max(1.0, (damage * move.multiplier) - target_stats.def) * type_multiplier;

                            if (critical)
                                damage *= 1.5;

                            string bonus_messages = "";

                            if (type_multiplier > 1.0)
                                bonus_messages += " It's super effective!";

                            if (critical)
                                bonus_messages += " Critical hit!";

                            target_stats.hp -= damage;

                            if (string.IsNullOrEmpty(message_end))
                                message_end = "dealing {0:0.0} damage";

                            message_end = string.Format(message_end, damage);

                            message_builder.Append(string.Format("💥 **{0}** used **{1}**, {4}!{3}",
                                StringUtils.ToTitleCase(user.name),
                                StringUtils.ToTitleCase(move.name),
                                damage,
                                bonus_messages,
                                message_end));

                        }

                    }

                    break;

                case MoveType.Recovery:

                    double recovered = Math.Max(1, user_stats.maxHp * move.multiplier) * type_multiplier;

                    if (!(move.callback is null)) {

                        GotchiMoveCallbackArgs args = new GotchiMoveCallbackArgs {
                            state = this,
                            user = user,
                            userStats = user_stats,
                            target = target,
                            targetStats = target_stats,
                            value = recovered,
                            move = move
                        };

                        await move.callback(args);

                        recovered = args.value;

                    }

                    target_stats.hp = Math.Min(target_stats.hp + recovered, target.id == gotchi1.id ? stats1.maxHp : stats2.maxHp);

                    message_builder.Append(string.Format("❤ **{0}** used **{1}**, recovering {2:0.0} hit points!",
                        StringUtils.ToTitleCase(user.name),
                        StringUtils.ToTitleCase(move.name),
                        recovered,
                        type_multiplier > 1.0 ? " It's super effective!" : ""));

                    break;

                case MoveType.StatBoost:

                    if (!(move.callback is null)) {

                        GotchiMoveCallbackArgs args = new GotchiMoveCallbackArgs {
                            state = this,
                            user = user,
                            userStats = user_stats,
                            target = target,
                            targetStats = target_stats,
                            value = move.multiplier,
                            move = move
                        };

                        await move.callback(args);

                        message_end = args.messageFormat;

                    }
                    else
                        target_stats.BoostByFactor(move.multiplier);

                    if (string.IsNullOrEmpty(message_end))
                        message_end = string.Format("{0} its {1}stats by ",
                            move.multiplier > 1.0 ? "boosting" : "lowering",
                            move.target == MoveTarget.Self ? "" : "opponent's ") + "{0}";

                    message_end = string.Format(message_end, Math.Abs((move.multiplier - 1.0) * 100.0));

                    message_builder.Append(string.Format("🛡 **{0}** used **{1}**, {2}%!",
                        StringUtils.ToTitleCase(user.name),
                        StringUtils.ToTitleCase(move.name),
                        message_end));

                    break;

            }

            if (user.owner_id == WILD_GOTCHI_USER_ID)
                message += message_builder.ToString();
            else
                message = message_builder.ToString();

        }
        private double _getTypeMultiplier(string moveRole, Role[] target_roles) {

            double mult = 1.0;

            /*
             parasite -> predators, base-consumers
             decomposer, scavenger, detritvore -> producers
             predator -> predator, base-conumers; -/> producers
             base-consumer -> producer
             */

            foreach (Role role in target_roles) {

                switch (moveRole.ToLower()) {

                    case "parasite":
                        if (role.name.ToLower() == "predator" || role.name.ToLower() == "base-consumer")
                            mult *= 1.2;
                        break;

                    case "decomposer":
                    case "scavenger":
                    case "detritvore":
                        if (role.name.ToLower() == "producer")
                            mult *= 1.2;
                        break;

                    case "predator":
                        if (role.name.ToLower() == "predator" || role.name.ToLower() == "base-consumer")
                            mult *= 1.2;
                        else if (role.name.ToLower() == "producers")
                            mult *= 0.8;
                        break;

                    case "base-consumer":
                        if (role.name.ToLower() == "producer")
                            mult *= 1.2;
                        break;

                }

            }

            return mult;

        }
        private double _getExpEarned(Gotchi gotchi, Gotchi opponent, bool won) {

            double exp = 0.0;

            exp = (opponent.id == gotchi1.id ? stats1.level : stats2.level) * 10.0;

            if (!won)
                exp *= .5;

            return exp;

        }
        private static void _drawHealthBar(Graphics gfx, float x, float y, double amount) {

            float hp_bar_width = 50;

            using (Brush brush = new SolidBrush(System.Drawing.Color.White))
                gfx.FillRectangle(brush, new RectangleF(x - hp_bar_width / 2, y, hp_bar_width, 10));

            using (Brush brush = new SolidBrush(amount < 0.5 ? (amount < 0.2 ? System.Drawing.Color.Red : System.Drawing.Color.Orange) : System.Drawing.Color.Green))
                gfx.FillRectangle(brush, new RectangleF(x - hp_bar_width / 2, y, hp_bar_width * (float)amount, 10));

            using (Brush brush = new SolidBrush(System.Drawing.Color.Black))
            using (Pen pen = new Pen(brush))
                gfx.DrawRectangle(pen, new Rectangle((int)(x - hp_bar_width / 2), (int)y, (int)hp_bar_width, 10));

        }
        private async Task _generateOpponent() {

            Gotchi opp = new Gotchi {
                born_ts = gotchi1.born_ts,
                died_ts = gotchi1.died_ts,
                evolved_ts = gotchi1.evolved_ts,
                fed_ts = gotchi1.fed_ts,
                owner_id = WILD_GOTCHI_USER_ID,
                id = WILD_GOTCHI_ID
            };

            // Pick a random base species for the user to battle.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id NOT IN (SELECT species_id FROM Ancestors) ORDER BY RANDOM() LIMIT 1;")) {

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    opp.species_id = (await Species.FromDataRow(row)).id;

            }

            // Evolve it to the same point as the user's gotchi.

            for (int i = 0; i < stats1.level / 10; ++i)
                if (!await GotchiUtils.EvolveGotchiAsync(opp))
                    break;

            // Calculate stats.

            GotchiStats opp_stats = new GotchiStats {
                level = Math.Max(1, stats1.level + BotUtils.RandomInteger(-3, 4)), // up to 3 levels in either direction
                exp = stats1.exp
            };

            opp_stats = await GotchiStats.CalculateStats(opp, opp_stats);

            // Name the gotchi.

            Species opp_species = await BotUtils.GetSpeciesFromDb(opp.species_id);
            opp.name = (opp_species is null ? "Wild Gotchi" : opp_species.GetShortName()) + string.Format(" (Lv. {0})", opp_stats.level);

            // Set the opponent.

            gotchi2 = opp;
            stats2 = opp_stats;

        }

    }

}