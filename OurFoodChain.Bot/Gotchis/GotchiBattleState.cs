using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MoonSharp.Interpreter;
using OurFoodChain.Bot;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public class GotchiBattleState {

        public const ulong WildGotchiUserId = 0;
        public const long WildGotchiId = -1;

        public bool BattleIsOver {
            get {
                return player1.Gotchi.Stats.Hp <= 0.0 || player2.Gotchi.Stats.Hp <= 0.0;
            }
        }

        private static ConcurrentDictionary<ulong, GotchiBattleState> _battle_states = new ConcurrentDictionary<ulong, GotchiBattleState>();

        public class PlayerState {
            public BattleGotchi Gotchi { get; set; } = null;
            public GotchiMove SelectedMove { get; set; } = null;
        }

        public PlayerState player1;
        public PlayerState player2;

        public bool accepted = false;
        public int turnCount = 0;
        public string battleText = "";

        public GotchiBattleState(IOfcBotConfiguration botConfiguration, DiscordSocketClient discordClient, SQLiteDatabase database) {

            _botConfiguration = botConfiguration;
            _discordClient = discordClient;
            this.database = database;

        }

        public async Task<IUser> GetPlayer1UserAsync(ICommandContext context) {

            return await context.Guild.GetUserAsync(player1.Gotchi.Gotchi.OwnerId);

        }
        public async Task<IUser> GetPlayer2UserAsync(ICommandContext context) {

            return await context.Guild.GetUserAsync(player2.Gotchi.Gotchi.OwnerId);

        }
        public async Task<string> GetPlayer1UsernameAsync(ICommandContext context) {

            if (player1.Gotchi.Gotchi.OwnerId == WildGotchiUserId)
                return player1.Gotchi.Gotchi.Name;

            return (await GetPlayer1UserAsync(context)).Username;

        }
        public async Task<string> GetPlayer2UsernameAsync(ICommandContext context) {

            if (player2.Gotchi.Gotchi.OwnerId == WildGotchiUserId)
                return player2.Gotchi.Gotchi.Name;

            return (await GetPlayer2UserAsync(context)).Username;

        }
        public async Task<IUser> GetOtherPlayerAsync(ICommandContext context, ulong userId) {

            ulong other_user_id = GetOtherPlayerUserId(userId);
            IUser other_user = await context.Guild.GetUserAsync(other_user_id);

            return other_user;

        }
        public ulong GetOtherPlayerUserId(ulong userId) {

            ulong other_user_id = player1.Gotchi.Gotchi.OwnerId == userId ? player2.Gotchi.Gotchi.OwnerId : player1.Gotchi.Gotchi.OwnerId;

            return other_user_id;

        }

        public Gotchi GetPlayersGotchi(ulong userId) {

            return userId == player1.Gotchi.Gotchi.OwnerId ? player1.Gotchi.Gotchi : player2.Gotchi.Gotchi;

        }
        public GotchiStats GetGotchiStats(Gotchi gotchi) {

            if (gotchi.Id == player1.Gotchi.Gotchi.Id)
                return player1.Gotchi.Stats;
            else if (gotchi.Id == player2.Gotchi.Gotchi.Id)
                return player2.Gotchi.Stats;

            return null;

        }
        public GotchiMoveSet GetGotchiMoveset(Gotchi gotchi) {

            if (gotchi.Id == player1.Gotchi.Gotchi.Id)
                return player1.Gotchi.Moves;
            else if (gotchi.Id == player2.Gotchi.Gotchi.Id)
                return player2.Gotchi.Moves;

            return null;

        }

        public async Task SelectMoveAsync(ICommandContext context, string moveIdentifier) {

            PlayerState player = context.User.Id == player1.Gotchi.Gotchi.OwnerId ? player1 : player2;
            PlayerState other_player = context.User.Id == player1.Gotchi.Gotchi.OwnerId ? player2 : player1;

            if (player.SelectedMove != null) {

                // If the player has already selected a move, don't allow them to change it.

                await BotUtils.ReplyAsync_Info(context, string.Format("You have already selected a move for this turn. Awaiting **{0}**'s move.",
                    (await GetOtherPlayerAsync(context, context.User.Id)).Username));

            }
            else {

                GotchiMove move = player.Gotchi.Moves.GetMove(moveIdentifier);

                if (move is null) {

                    // Warn the player if they select an invalid move.
                    await BotUtils.ReplyAsync_Error(context, "The move you have selected is invalid. Please select a valid move.");

                }
                else if (move.PP <= 0 && player.Gotchi.Moves.HasPP) {

                    // The selected move cannot be used because it is out of PP.
                    await BotUtils.ReplyAsync_Error(context, "The selected move has no PP left. Please select a different move.");

                }
                else {

                    // Lock in the selected move.
                    player.SelectedMove = move;

                    // If the selected move does not have any PP, silently select the "struggle" move (used when no moves have any PP).
                    if (player.SelectedMove.PP <= 0)
                        player.SelectedMove = await Global.GotchiContext.MoveRegistry.GetMoveAsync("desperation");

                    if (!IsBattlingCpu() && other_player.SelectedMove is null) {

                        // If the other user hasn't locked in a move yet, await their move.

                        await BotUtils.ReplyAsync_Info(context, string.Format("Move locked in! Awaiting **{0}**'s move.",
                            (await GetOtherPlayerAsync(context, context.User.Id)).Username));

                    }
                    else {

                        // If the player is battling a CPU, select a move for them now.

                        if (IsBattlingCpu())
                            await _pickCpuMoveAsync(player2);

                        // Update the battle state.
                        await ExecuteTurnAsync(context);

                    }

                }

            }

        }
        public async Task ExecuteTurnAsync(ICommandContext context) {

            battleText = string.Empty;

            if (turnCount == 0)
                turnCount = 1;

            Tuple<PlayerState, PlayerState> turnOrder = _getTurnOrder();

            // Execute both players' moves.

            if (!BattleIsOver)
                await _useMoveOnAsync(context, turnOrder.Item1, turnOrder.Item2);

            if (!BattleIsOver)
                await _useMoveOnAsync(context, turnOrder.Item2, turnOrder.Item1);

            // Apply status problems.

            if (!BattleIsOver)
                await _applyStatusProblemsAsync(turnOrder.Item1, turnOrder.Item2);

            if (!BattleIsOver)
                await _applyStatusProblemsAsync(turnOrder.Item2, turnOrder.Item1);

            // Check if the battle has ended, and handle the situation accordingly.

            if (BattleIsOver)
                await _endBattle(context);

            // Show the battle state.
            await ShowBattleStateAsync(context, this);

            // Reset the battle text and each user's selected moves.

            battleText = string.Empty;
            player1.SelectedMove.PP -= 1;
            player2.SelectedMove.PP -= 1;
            player1.SelectedMove = null;
            player2.SelectedMove = null;

            ++turnCount;

        }

        public bool IsBattlingCpu() {

            if (!(player2 is null))
                return player2.Gotchi.Gotchi.OwnerId == WildGotchiUserId;

            return false;

        }
        public bool IsCpuGotchi(Gotchi gotchi) {

            return gotchi.OwnerId == WildGotchiUserId;

        }
        public static bool IsUserCurrentlyBattling(ulong userId) {

            if (_battle_states.ContainsKey(userId))
                return true;

            return false;


        }

        public static async Task RegisterBattleAsync(ICommandContext context, IOfcBotConfiguration botConfiguration, DiscordSocketClient discordClient, SQLiteDatabase database, Gotchi gotchi1, Gotchi gotchi2) {

            // Initialize the battle state.

            GotchiBattleState state = new GotchiBattleState(botConfiguration, discordClient, database) {

                // Initialize Player 1 (which must be a human player).

                player1 = new PlayerState {
                    Gotchi = new BattleGotchi {
                        Gotchi = gotchi1,
                        Moves = await Global.GotchiContext.MoveRegistry.GetMoveSetAsync(gotchi1),
                        Stats = await new GotchiStatsCalculator(Global.GotchiContext).GetStatsAsync(gotchi1),
                        Types = await Global.GotchiContext.TypeRegistry.GetTypesAsync(gotchi1)
                    }
                }

            };

            if (gotchi2 != null) {

                // Initialize Player 2 (which may be a human player, or a CPU).

                state.player2 = new PlayerState {
                    Gotchi = new BattleGotchi {
                        Gotchi = gotchi2,
                        Moves = await Global.GotchiContext.MoveRegistry.GetMoveSetAsync(gotchi2),
                        Stats = await new GotchiStatsCalculator(Global.GotchiContext).GetStatsAsync(gotchi2),
                        Types = await Global.GotchiContext.TypeRegistry.GetTypesAsync(gotchi2)
                    }
                };

            }
            else {

                // Otherwise, generate an opponent for the user.
                await state._generateOpponentAsync();

                // If the opponent is null (no species available as opponents), abort.

                if (state.player2.Gotchi.Gotchi is null) {

                    await BotUtils.ReplyAsync_Info(context, "There are no opponents available.");

                    return;

                }

                // Since the user is battling a CPU, accept the battle immediately.
                state.accepted = true;

            }

            state.player1.Gotchi.Context = Global.GotchiContext;
            state.player2.Gotchi.Context = Global.GotchiContext;

            // Register the battle state in the battle state collection.

            _battle_states[gotchi1.OwnerId] = state;

            if (state.player2.Gotchi.Gotchi.OwnerId != WildGotchiUserId)
                _battle_states[state.player2.Gotchi.Gotchi.OwnerId] = state;

            // Set the initial message displayed when the battle starts.

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("The battle has begun!");
            sb.AppendLine();
            sb.AppendLine(string.Format("Pick a move with `{0}gotchi move`.\nSee your gotchi's moveset with `{0}gotchi moveset`.",
                botConfiguration.Prefix));

            state.battleText = sb.ToString();

            // If the user is battling a CPU, show the battle state immediately.
            // Otherwise, it will be shown when the other user accepts the battle challenge.

            if (state.IsBattlingCpu())
                await ShowBattleStateAsync(context, state);

        }
        public static void DeregisterBattle(ulong userId) {

            if (_battle_states.TryRemove(userId, out GotchiBattleState state))
                _battle_states.TryRemove(state.GetOtherPlayerUserId(userId), out state);

        }
        public static GotchiBattleState GetBattleStateByUserId(ulong userId) {

            if (!_battle_states.ContainsKey(userId))
                return null;

            return _battle_states[userId];

        }

        public static async Task ShowBattleStateAsync(ICommandContext context, GotchiBattleState state) {

            // Get an image of the battle.

            string gif_url = "";

            GotchiGifCreatorParams p1 = new GotchiGifCreatorParams {
                gotchi = state.player1.Gotchi.Gotchi,
                x = 50,
                y = 150,
                state = state.player1.Gotchi.Stats.Hp > 0 ? (state.player2.Gotchi.Stats.Hp <= 0 ? GotchiState.Happy : GotchiState.Energetic) : GotchiState.Dead,
                auto = false
            };

            GotchiGifCreatorParams p2 = new GotchiGifCreatorParams {
                gotchi = state.player2.Gotchi.Gotchi,
                x = 250,
                y = 150,
                state = state.player2.Gotchi.Stats.Hp > 0 ? (state.player1.Gotchi.Stats.Hp <= 0 ? GotchiState.Happy : GotchiState.Energetic) : GotchiState.Dead,
                auto = false
            };

            gif_url = await GotchiUtils.GenerateAndUploadGotchiGifAndReplyAsync(context, state._botConfiguration, state._discordClient, new GotchiGifCreatorParams[] { p1, p2 }, new GotchiGifCreatorExtraParams {
                backgroundFileName = await GotchiUtils.GetGotchiBackgroundFileNameAsync(state.database, state.player2.Gotchi.Gotchi, "home_battle.png"),
                overlay = (Graphics gfx) => {

                    // Draw health bars.

                    _drawHealthBar(gfx, p1.x, 180, (double)state.player1.Gotchi.Stats.Hp / state.player1.Gotchi.Stats.MaxHp);
                    _drawHealthBar(gfx, p2.x, 180, (double)state.player2.Gotchi.Stats.Hp / state.player2.Gotchi.Stats.MaxHp);

                }
            });

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithTitle(string.Format("**{0}** vs. **{1}** (Turn {2})",
                state.player1.Gotchi.Gotchi.Name,
                state.player2.Gotchi.Gotchi.Name,
                state.turnCount));
            embed.WithImageUrl(gif_url);
            embed.WithDescription(state.battleText);
            if (state.BattleIsOver)
                embed.WithFooter("The battle has ended!");
            else if (state.turnCount == 0) {
                embed.WithFooter("The battle has begun. Pick your move!");
            }
            else
                embed.WithFooter(string.Format("Beginning Turn {0}. Pick your move!", state.turnCount + 1));

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }

        private readonly IOfcBotConfiguration _botConfiguration;
        private readonly DiscordSocketClient _discordClient;
        private readonly SQLiteDatabase database;

        private async Task _useMoveOnAsync(ICommandContext context, PlayerState user, PlayerState target) {

            // Execute the selected move.

            StringBuilder battle_text = new StringBuilder();
            battle_text.AppendLine(battleText);

            if (!string.IsNullOrEmpty(user.SelectedMove.LuaScriptFilePath)) {

                // Create, initialize, and execute the script associated with this move.

                GotchiMoveLuaScript moveScript = new GotchiMoveLuaScript(user.SelectedMove.LuaScriptFilePath);

                GotchiMoveCallbackArgs args = await _createCallbackArgsAsync(user, target);

                // Call the move's "OnInit" callback (only applicable for some moves).
                await moveScript.OnInitAsync(args);

                // It's possible for a move to be used more than once in a turn (e.g., multi-hit moves).
                // Each time will trigger the callback to be called and display a new message.

                for (int i = 0; i < Math.Max(1, args.Times); ++i) {

                    // Check if this was a critical hit, or if the move missed.

                    bool is_hit = user.SelectedMove.IgnoreAccuracy || (BotUtils.RandomInteger(0, 20 + 1) < 20 * user.SelectedMove.Accuracy * Math.Max(0.1, user.Gotchi.Stats.Acc - target.Gotchi.Stats.Eva));
                    args.IsCritical =
                        !user.SelectedMove.IgnoreCritical &&
                        (BotUtils.RandomInteger(0, (int)(10 / user.SelectedMove.CriticalRate)) == 0 ||
                        (await SpeciesUtils.GetPreyAsync(user.Gotchi.Gotchi.SpeciesId)).Any(x => x.Prey.Id == target.Gotchi.Gotchi.Id));

                    if (is_hit) {

                        // Clone each user's stats before triggering the callback, so we can compare them before and after.

                        GotchiStats user_before = user.Gotchi.Stats.Clone();
                        GotchiStats target_before = target.Gotchi.Stats.Clone();

                        // Invoke the callback.

                        try {

                            if (!await moveScript.OnMoveAsync(args))
                                args.DealDamage(user.SelectedMove.Power);

                        }
                        catch (Exception) {

                            args.Text = "but something went wrong";

                        }

                        // Apply the target's status if a new status was acquired.

                        if (target.Gotchi.StatusChanged && target.Gotchi.HasStatus) {

                            await new GotchiStatusLuaScript(target.Gotchi.Status.LuaScriptFilePath).OnAcquireAsync(args);

                            target.Gotchi.StatusChanged = false;

                        }

                        // Prevent the target from fainting if they're able to endure the hit.

                        if (target.Gotchi.HasStatus && target.Gotchi.Status.Endure)
                            target.Gotchi.Stats.Hp = Math.Max(1, target.Gotchi.Stats.Hp);

                        // If the user is heal-blocked, prevent recovery by resetting the HP back to what it was before recovery.

                        if (user.Gotchi.HasStatus && !user.Gotchi.Status.AllowRecovery && user.Gotchi.Stats.Hp > user_before.Hp)
                            user.Gotchi.Stats.Hp = user_before.Hp;

                        // Show the battle text.
                        // If the move doesn't specify a text, choose one automatically (where possible).

                        string text = args.Text;

                        if (string.IsNullOrEmpty(text)) {

                            if (target.Gotchi.Stats.Hp < target_before.Hp) {
                                text = "dealing {target:damage} damage";
                                //user.SelectedMove.info.Type = GotchiMoveType.Offensive;
                            }

                            else if (target.Gotchi.Stats.Atk < target_before.Atk) {
                                text = "lowering its opponent's ATK by {target:atk%}";
                                //user.SelectedMove.info.Type = GotchiMoveType.Buff;
                            }
                            else if (target.Gotchi.Stats.Def < target_before.Def) {
                                text = "lowering its opponent's DEF by {target:def%}";
                                //user.SelectedMove.info.Type = GotchiMoveType.Buff;
                            }
                            else if (target.Gotchi.Stats.Spd < target_before.Spd) {
                                text = "lowering its opponent's SPD by {target:spd%}";
                                //user.SelectedMove.info.Type = GotchiMoveType.Buff;
                            }
                            else if (target.Gotchi.Stats.Acc < target_before.Acc) {
                                text = "lowering its opponent's accuracy by {target:acc%}";
                                //user.SelectedMove.info.Type = GotchiMoveType.Buff;
                            }
                            else if (target.Gotchi.Stats.Eva < target_before.Eva) {
                                text = "lowering its opponent's evasion by {target:eva%}";
                                //user.SelectedMove.info.Type = GotchiMoveType.Buff;
                            }

                            else if (user.Gotchi.Stats.Hp > user_before.Hp) {
                                text = "recovering {user:recovered} HP";
                                //user.SelectedMove.info.Type = GotchiMoveType.Recovery;
                            }
                            else if (user.Gotchi.Stats.Atk > user_before.Atk) {
                                text = "boosting its ATK by {user:atk%}";
                                //user.SelectedMove.info.Type = GotchiMoveType.Buff;
                            }
                            else if (user.Gotchi.Stats.Def > user_before.Def) {
                                text = "boosting its DEF by {user:def%}";
                                //user.SelectedMove.info.Type = GotchiMoveType.Buff;
                            }
                            else if (user.Gotchi.Stats.Spd > user_before.Spd) {
                                text = "boosting its SPD by {user:spd%}";
                                //user.SelectedMove.info.Type = GotchiMoveType.Buff;
                            }
                            else if (user.Gotchi.Stats.Acc > user_before.Acc) {
                                text = "boosting its accuracy by {user:acc%}";
                                //user.SelectedMove.info.Type = GotchiMoveType.Buff;
                            }
                            else if (user.Gotchi.Stats.Eva > user_before.Eva) {
                                text = "boosting its evasion by {user:eva%}";
                                //user.SelectedMove.info.Type = GotchiMoveType.Buff;
                            }

                            else {
                                text = "but nothing happened?";
                            }

                        }

                        // Various replacements are allowed, which the user can specify in the move's battle text.

                        text = Regex.Replace(text, @"\{([^\}]+)\}", m => {

                            switch (m.Groups[1].Value.ToLower()) {

                                case "damage":
                                case "target:damage":
                                    return string.Format("{0:0.#}", target_before.Hp - target.Gotchi.Stats.Hp);

                                case "target:atk%":
                                    return string.Format("{0:0.#}%", Math.Abs(target_before.Atk - target.Gotchi.Stats.Atk) / (double)target_before.Atk * 100.0);
                                case "target:def%":
                                    return string.Format("{0:0.#}%", Math.Abs(target_before.Def - target.Gotchi.Stats.Def) / (double)target_before.Def * 100.0);
                                case "target:spd%":
                                    return string.Format("{0:0.#}%", Math.Abs(target_before.Spd - target.Gotchi.Stats.Spd) / (double)target_before.Spd * 100.0);
                                case "target:acc%":
                                    return string.Format("{0:0.#}%", Math.Abs(target_before.Acc - target.Gotchi.Stats.Acc) / target_before.Acc * 100.0);
                                case "target:eva%":
                                    return string.Format("{0:0.#}%", Math.Abs(target_before.Eva - target.Gotchi.Stats.Eva) / target_before.Eva * 100.0);

                                case "user:atk%":
                                    return string.Format("{0:0.#}%", Math.Abs(user_before.Atk - user.Gotchi.Stats.Atk) / (double)user_before.Atk * 100.0);
                                case "user:def%":
                                    return string.Format("{0:0.#}%", Math.Abs(user_before.Def - user.Gotchi.Stats.Def) / (double)user_before.Def * 100.0);
                                case "user:spd%":
                                    return string.Format("{0:0.#}%", Math.Abs(user_before.Spd - user.Gotchi.Stats.Spd) / (double)user_before.Spd * 100.0);
                                case "user:acc%":
                                    return string.Format("{0:0.#}%", Math.Abs(user_before.Acc - user.Gotchi.Stats.Acc) / user_before.Acc * 100.0);
                                case "user:eva%":
                                    return string.Format("{0:0.#}%", (user_before.Eva == 0.0 ? user.Gotchi.Stats.Eva : (Math.Abs(user_before.Eva - user.Gotchi.Stats.Eva) / user_before.Eva)) * 100.0);

                                case "user:recovered":
                                    return string.Format("{0:0.#}", user.Gotchi.Stats.Hp - user_before.Hp);

                                default:
                                    return "???";

                            }

                        });

                        battle_text.Append(string.Format("{0} **{1}** used **{2}**, {3}!",
                            "💥", //user.SelectedMove.info.Icon(),
                            user.Gotchi.Gotchi.Name,
                            user.SelectedMove.Name,
                            text));

                        if (args.IsCritical && target.Gotchi.Stats.Hp < target_before.Hp)
                            battle_text.Append(" Critical hit!");

                        if (args.MatchupMultiplier > 1.0)
                            battle_text.Append(" It's super effective!");
                        else if (args.MatchupMultiplier < 1.0)
                            battle_text.Append(" It's not very effective...");

                        battle_text.AppendLine();

                    }
                    else {

                        // If the move missed, so display a failure message.
                        battle_text.AppendLine(string.Format("{0} **{1}** used **{2}**, but it missed!",
                             "💥", //user.SelectedMove.info.Icon(),
                            user.Gotchi.Gotchi.Name,
                            user.SelectedMove.Name));

                    }

                }

                if (args.Times > 1)
                    battle_text.Append(string.Format(" Hit {0} times!", args.Times));

            }
            else {

                // If there is no Lua script associated with the given move, display a failure message.
                battle_text.Append(string.Format("{0} **{1}** used **{2}**, but it forgot how!",
                     "💥", //user.SelectedMove.info.Icon(),
                    user.Gotchi.Gotchi.Name,
                    user.SelectedMove.Name));

            }

            battleText = battle_text.ToString();

        }
        private async Task _applyStatusProblemsAsync(PlayerState affectedUser, PlayerState otherUser) {

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat(battleText);

            if (affectedUser.Gotchi.HasStatus) {

                GotchiStatus status = affectedUser.Gotchi.Status;
                GotchiStatusLuaScript script = new GotchiStatusLuaScript(status.LuaScriptFilePath);
                GotchiMoveCallbackArgs args = await _createCallbackArgsAsync(otherUser, affectedUser);

                // Call the "OnTurnEnd" callback.
                await script.OnTurnEndAsync(args);

                // Apply other properties.

                affectedUser.Gotchi.Stats.Hp -= status.SlipDamage;
                affectedUser.Gotchi.Stats.Hp -= (int)(status.SlipDamagePercent * affectedUser.Gotchi.Stats.MaxHp);

                // Show status text.

                if (!string.IsNullOrEmpty(args.Text))
                    sb.Append(string.Format("\n⚡ **{0}** {1}!", affectedUser.Gotchi.Gotchi.Name, args.Text));

                // Decrement the duration of the status, clearing it if the duration has been completed.

                if (!status.Permanent && --status.Duration <= 0)
                    affectedUser.Gotchi.ClearStatus();

            }

            battleText = sb.ToString();

        }
        private double _getWeaknessMultiplier(string moveRole, Role[] target_roles) {

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
                        else if (role.name.ToLower() == "producer")
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

            exp = (opponent.Id == player1.Gotchi.Gotchi.Id ? player1.Gotchi.Stats.Level : player2.Gotchi.Stats.Level) * 10.0;

            if (!won)
                exp *= .5;

            return exp;

        }
        private static void _drawHealthBar(Graphics gfx, float x, float y, double amount) {

            int hpBarWidth = 50;
            int hpBarHeight = 9;
            int hpBarRadius = 4;

            Rectangle bounds = new Rectangle((int)x - hpBarWidth / 2, (int)y, hpBarWidth, hpBarHeight);
            System.Drawing.Color color = System.Drawing.Color.Green;

            if (amount >= 0.5)
                color = GraphicsUtils.Blend(System.Drawing.Color.Orange, System.Drawing.Color.Green, (1.0 - amount) / 0.5);
            else
                color = GraphicsUtils.Blend(System.Drawing.Color.Red, System.Drawing.Color.Orange, (0.5 - amount) / 0.5);

            using (Brush brush = new SolidBrush(System.Drawing.Color.White))
                GraphicsUtils.FillRoundedRectangle(gfx, brush, bounds, hpBarRadius);

            gfx.SetClip(GraphicsUtils.RoundedRect(bounds, hpBarRadius), CombineMode.Replace);

            using (Brush brush = new SolidBrush(color))
                gfx.FillRectangle(brush, new Rectangle((int)x - hpBarWidth / 2, (int)y, (int)(hpBarWidth * amount), hpBarHeight));

            gfx.ResetClip();

            using (Brush brush = new SolidBrush(System.Drawing.Color.Black))
            using (Pen pen = new Pen(brush))
                GraphicsUtils.DrawRoundedRectangle(gfx, pen, new Rectangle((int)(x - hpBarWidth / 2), (int)y, hpBarWidth, hpBarHeight), hpBarRadius);

        }
        private async Task _generateOpponentAsync() {

            // Pick a random species from the same zone as the player's gotchi.

            List<ISpecies> species_list = new List<ISpecies>();

            foreach (ISpeciesZoneInfo zone in await database.GetZonesAsync(await database.GetSpeciesAsync(player1.Gotchi.Gotchi.SpeciesId)))
                species_list.AddRange((await database.GetSpeciesAsync(zone.Zone)).Where(x => !x.Status.IsExinct));

            player2 = new PlayerState();

            if (species_list.Count() > 0) {

                player2.Gotchi = await database.GenerateGotchiAsync(new GotchiGenerationParameters {
                    Base = player1.Gotchi.Gotchi,
                    Species = species_list[BotUtils.RandomInteger(species_list.Count())],
                    MinLevel = player1.Gotchi.Stats.Level - 3,
                    MaxLevel = player1.Gotchi.Stats.Level + 3,
                    GenerateMoveset = true,
                    GenerateStats = true
                });

            }

            // Set the opponent.

            if (player2.Gotchi != null) {

                player2.Gotchi.Gotchi.OwnerId = WildGotchiUserId;
                player2.Gotchi.Gotchi.Id = WildGotchiId;

            }

        }
        private async Task _pickCpuMoveAsync(PlayerState player) {

            GotchiMove move = player.Gotchi.Moves.GetRandomMove();

            if (move is null)
                move = await Global.GotchiContext.MoveRegistry.GetMoveAsync("desperation");

            player.SelectedMove = move;

        }
        private async Task _endBattle(ICommandContext context) {

            PlayerState winner = player1.Gotchi.Stats.Hp <= 0.0 ? player2 : player1;
            PlayerState loser = player1.Gotchi.Stats.Hp <= 0.0 ? player1 : player2;

            // Calculate the amount of EXP awarded to the winner.
            // The loser will get 50% of the winner's EXP.

            double exp = _getExpEarned(winner.Gotchi.Gotchi, loser.Gotchi.Gotchi, won: true);

            double exp1 = player2.Gotchi.Stats.Hp <= 0.0 ? exp : exp * .5;
            double exp2 = player1.Gotchi.Stats.Hp <= 0.0 ? exp : exp * .5;

            long levels1 = player1.Gotchi.Stats.AddExperience((int)exp1);
            long levels2 = player2.Gotchi.Stats.AddExperience((int)exp2);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(battleText);

            // Show the winner's accomplishments, then the loser's.

            if (!IsCpuGotchi(winner.Gotchi.Gotchi)) {

                double winner_exp = winner.Gotchi.Gotchi.Id == player1.Gotchi.Gotchi.Id ? exp1 : exp2;
                long winner_levels = winner.Gotchi.Gotchi.Id == player1.Gotchi.Gotchi.Id ? levels1 : levels2;
                long winner_g = (long)Math.Round(loser.Gotchi.Stats.Level * (BotUtils.RandomInteger(150, 200) / 100.0));

                sb.AppendLine(string.Format("🏆 **{0}** won the battle! Earned **{1} EXP** and **{2}G**.",
                    winner.Gotchi.Gotchi.Name,
                    winner_exp,
                    winner_g));

                if (winner_levels > 0)
                    sb.AppendLine(string.Format("🆙 **{0}** leveled up to level **{1}**!", winner.Gotchi.Gotchi.Name, winner.Gotchi.Stats.Level));

                if (((winner.Gotchi.Stats.Level - winner_levels) / 10) < (winner.Gotchi.Stats.Level / 10))
                    if (await database.EvolveAndUpdateGotchiAsync(winner.Gotchi.Gotchi)) {

                        Species sp = await BotUtils.GetSpeciesFromDb(winner.Gotchi.Gotchi.SpeciesId);

                        sb.AppendLine(string.Format("🚩 Congratulations, **{0}** evolved into **{1}**!", winner.Gotchi.Gotchi.Name, sp.ShortName));

                    }

                // Update the winner's G.

                GotchiUserInfo user_data = await database.GetUserInfoAsync(winner.Gotchi.Gotchi.OwnerId);

                user_data.G += winner_g;

                await database.UpdateUserInfoAsync(user_data);

                sb.AppendLine();

            }

            if (!IsCpuGotchi(loser.Gotchi.Gotchi)) {

                double loser_exp = loser.Gotchi.Gotchi.Id == player1.Gotchi.Gotchi.Id ? exp1 : exp2;
                long loser_levels = loser.Gotchi.Gotchi.Id == player1.Gotchi.Gotchi.Id ? levels1 : levels2;

                sb.AppendLine(string.Format("💀 **{0}** lost the battle... Earned **{1} EXP**.", loser.Gotchi.Gotchi.Name, loser_exp));

                if (loser_levels > 0)
                    sb.AppendLine(string.Format("🆙 **{0}** leveled up to level **{1}**!", loser.Gotchi.Gotchi.Name, loser.Gotchi.Stats.Level));

                if (((loser.Gotchi.Stats.Level - loser_levels) / 10) < (loser.Gotchi.Stats.Level / 10))
                    if (await database.EvolveAndUpdateGotchiAsync(loser.Gotchi.Gotchi)) {

                        Species sp = await BotUtils.GetSpeciesFromDb(loser.Gotchi.Gotchi.SpeciesId);

                        sb.AppendLine(string.Format("🚩 Congratulations, **{0}** evolved into **{1}**!", loser.Gotchi.Gotchi.Name, sp.ShortName));

                    }

            }

            // Update exp in the database.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET level=$level, exp=$exp WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$id", player1.Gotchi.Gotchi.Id);
                cmd.Parameters.AddWithValue("$level", DBNull.Value);
                cmd.Parameters.AddWithValue("$exp", player1.Gotchi.Stats.Experience);

                await database.ExecuteNonQueryAsync(cmd);

            }

            if (!IsCpuGotchi(player2.Gotchi.Gotchi)) {

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET level=$level, exp=$exp WHERE id=$id;")) {

                    cmd.Parameters.AddWithValue("$id", player2.Gotchi.Gotchi.Id);
                    cmd.Parameters.AddWithValue("$level", DBNull.Value);
                    cmd.Parameters.AddWithValue("$exp", player2.Gotchi.Stats.Experience);

                    await database.ExecuteNonQueryAsync(cmd);

                }

            }

            // Deregister the battle state.

            DeregisterBattle(context.User.Id);

            battleText = sb.ToString();

        }

        private Tuple<PlayerState, PlayerState> _getTurnOrder() {

            /* Determine which player gets to move first at the start of the turn.
             * 
             * Move priority is considered first, followed by speed, and then random selection.
             * 
             * */

            PlayerState first, second;

            if (player1.SelectedMove.Priority > player2.SelectedMove.Priority) {
                first = player1;
                second = player2;
            }
            else if (player2.SelectedMove.Priority > player1.SelectedMove.Priority) {
                first = player2;
                second = player1;
            }
            else if (player1.Gotchi.Stats.Spd > player2.Gotchi.Stats.Spd) {
                first = player1;
                second = player2;
            }
            else if (player2.Gotchi.Stats.Spd > player1.Gotchi.Stats.Spd) {
                first = player2;
                second = player1;
            }
            else {

                if (BotUtils.RandomInteger(0, 2) == 0) {
                    first = player1;
                    second = player2;
                }
                else {
                    first = player2;
                    second = player1;
                }

            }

            return new Tuple<PlayerState, PlayerState>(first, second);

        }

        private static async Task<GotchiMoveCallbackArgs> _createCallbackArgsAsync(PlayerState userState, PlayerState targetState) {

            return new GotchiMoveCallbackArgs {
                User = userState.Gotchi,
                Target = targetState.Gotchi,
                Move = userState.SelectedMove,
                MoveTypes = await Global.GotchiContext.TypeRegistry.GetTypesAsync(userState.SelectedMove.Types)
            };

        }

    }

}