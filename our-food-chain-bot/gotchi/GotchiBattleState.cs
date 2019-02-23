using Discord;
using Discord.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public class GotchiBattleState {

        private static ConcurrentDictionary<ulong, GotchiBattleState> BATTLE_STATES = new ConcurrentDictionary<ulong, GotchiBattleState>();

        public Gotchi gotchi1;
        public Gotchi gotchi2;
        public GotchiStats stats1;
        public GotchiStats stats2;

        public double maxHp1 = 0;
        public double maxHp2 = 0;

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

                double exp1 = _getExpEarned(gotchi1, gotchi2, stats2.hp <= 0.0);
                double exp2 = _getExpEarned(gotchi2, gotchi1, stats1.hp <= 0.0);

                reply.AppendLine(string.Format("**{0}** won the battle! Earned **{1} EXP**.\n**{2}** earned **{3} EXP**.",
                    StringUtils.ToTitleCase(winner.name),
                    winner.id == gotchi1.id ? exp1 : exp2,
                     StringUtils.ToTitleCase(loser.name),
                     loser.id == gotchi1.id ? exp1 : exp2
                    ));

                stats1.LeveUp(exp1);
                stats2.LeveUp(exp2);

                // Update level/exp in the database.

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET level=$level, exp=$exp WHERE id=$id;")) {

                    cmd.Parameters.AddWithValue("$id", gotchi1.id);
                    cmd.Parameters.AddWithValue("$level", stats1.level);
                    cmd.Parameters.AddWithValue("$exp", stats1.exp);

                    await Database.ExecuteNonQuery(cmd);

                }

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET level=$level, exp=$exp WHERE id=$id;")) {

                    cmd.Parameters.AddWithValue("$id", gotchi2.id);
                    cmd.Parameters.AddWithValue("$level", stats2.level);
                    cmd.Parameters.AddWithValue("$exp", stats2.exp);

                    await Database.ExecuteNonQuery(cmd);

                }

                // Deregister the battle state.

                DeregisterBattle(context.User.Id);

            }
            else {

                reply.AppendLine(string.Format("It's {1}'s turn! Pick a move with `{0}gotchi move`. \nSee your gotchi's moveset with `{0}gotchi moveset`.",
                    OurFoodChainBot.GetInstance().GetConfig().prefix,
                    (await GetOtherUserAsync(context, context.User.Id)).Mention));

            }

            await BotUtils.ReplyAsync_Info(context, reply.ToString());

        }
        public bool IsBattleOver() {

            return stats1.hp <= 0.0 || stats2.hp <= 0.0;

        }

        public static async Task RegisterBattleAsync(Gotchi gotchi1, Gotchi gotchi2) {

            GotchiBattleState state = new GotchiBattleState {
                gotchi1 = gotchi1,
                gotchi2 = gotchi2,
                stats1 = await GotchiStats.CalculateStats(gotchi1),
                stats2 = await GotchiStats.CalculateStats(gotchi2)
            };

            if (state.stats1.spd != state.stats2.spd)
                state.currentTurn = state.stats1.spd > state.stats2.spd ? 1 : 2;
            else
                state.currentTurn = BotUtils.RandomInteger(1, 3);

            state.maxHp1 = state.stats1.hp;
            state.maxHp2 = state.stats2.hp;

            BATTLE_STATES[gotchi1.owner_id] = state;
            BATTLE_STATES[gotchi2.owner_id] = state;

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

                    _drawHealthBar(gfx, p1.x, 180, state.stats1.hp / state.maxHp1);
                    _drawHealthBar(gfx, p2.x, 180, state.stats2.hp / state.maxHp2);

                }
            });

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithTitle(string.Format("**{0}** vs. **{1}** (Turn {2})", StringUtils.ToTitleCase(state.gotchi1.name), StringUtils.ToTitleCase(state.gotchi2.name), state.turnCount));
            embed.WithImageUrl(gif_url);
            //embed.WithDescription(state.message);
            if (!state.IsBattleOver())
                embed.WithFooter(string.Format("It is now {0}'s turn.", state.currentTurn == 1 ? (await state.GetUser1Async(context)).Username : (await state.GetUser2Async(context)).Username));
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

            switch (move.type) {

                case MoveType.Attack:

                    // Have a chance of missing.

                    if (BotUtils.RandomInteger(0, 10) == 0) {

                        message = string.Format("💥 **{0}** used **{1}**, but it missed!",
                          StringUtils.ToTitleCase(user.name),
                          StringUtils.ToTitleCase(move.name));

                    }
                    else {

                        bool critical = BotUtils.RandomInteger(0, 10) == 0;
                        double damage = Math.Max(1, (user_stats.atk * move.factor) - target_stats.def) * type_multiplier;

                        if (critical)
                            damage *= 1.5;

                        target_stats.hp -= damage;

                        string bonus_messages = "";

                        if (type_multiplier > 1.0)
                            bonus_messages += " It's super effective!";

                        if (critical)
                            bonus_messages += " Critical hit!";

                        message = string.Format("💥 **{0}** used **{1}**, dealing {2:0.0} damage!{3}",
                            StringUtils.ToTitleCase(user.name),
                            StringUtils.ToTitleCase(move.name),
                            damage,
                            bonus_messages);

                    }

                    break;

                case MoveType.Recovery:

                    double recovered = Math.Max(1, user_stats.atk * move.factor) * type_multiplier;
                    target_stats.hp = Math.Min(target_stats.hp + recovered, target.id == gotchi1.id ? maxHp1 : maxHp2);

                    message = string.Format("❤ **{0}** used **{1}**, recovering {2:0.0} hit points!",

                        StringUtils.ToTitleCase(user.name),
                        StringUtils.ToTitleCase(move.name),
                        recovered,
                        type_multiplier > 1.0 ? " It's super effective!" : "");

                    break;

                case MoveType.StatBoost:

                    target_stats.BoostByFactor(move.factor);

                    message = string.Format("🛡 **{0}** used **{1}**, boosting their stats by {2}%!",
                        StringUtils.ToTitleCase(user.name),
                        StringUtils.ToTitleCase(move.name),
                        (move.factor - 1.0) * 100.0);

                    break;

            }

            await ShowBattleStateAsync(context, this);

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

            using (Brush brush = new SolidBrush(amount < 0.5 ? System.Drawing.Color.Red : System.Drawing.Color.Green))
                gfx.FillRectangle(brush, new RectangleF(x - hp_bar_width / 2, y, hp_bar_width * (float)amount, 10));

            using (Brush brush = new SolidBrush(System.Drawing.Color.Black))
            using (Pen pen = new Pen(brush))
                gfx.DrawRectangle(pen, new Rectangle((int)(x - hp_bar_width / 2), (int)y, (int)hp_bar_width, 10));

        }

    }

}