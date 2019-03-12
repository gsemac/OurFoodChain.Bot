using Discord;
using Discord.Commands;
using MoonSharp.Interpreter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public class GotchiBattleState {

        public const ulong WILD_GOTCHI_USER_ID = 0;
        public const long WILD_GOTCHI_ID = -1;

        private static ConcurrentDictionary<ulong, GotchiBattleState> _battle_states = new ConcurrentDictionary<ulong, GotchiBattleState>();

        public class PlayerState {
            public Gotchi gotchi;
            public LuaGotchiStats stats;
            public GotchiMoveset moves;
            public LuaGotchiMove selectedMove = null;
        }

        public PlayerState player1;
        public PlayerState player2;

        public bool accepted = false;
        public int turnCount = 0;
        public string battleText = "";

        public async Task<IUser> GetPlayer1UserAsync(ICommandContext context) {

            return await context.Guild.GetUserAsync(player1.gotchi.owner_id);

        }
        public async Task<IUser> GetPlayer2UserAsync(ICommandContext context) {

            return await context.Guild.GetUserAsync(player2.gotchi.owner_id);

        }
        public async Task<string> GetPlayer1UsernameAsync(ICommandContext context) {

            if (player1.gotchi.owner_id == WILD_GOTCHI_USER_ID)
                return player1.gotchi.name;

            return (await GetPlayer1UserAsync(context)).Username;

        }
        public async Task<string> GetPlayer2UsernameAsync(ICommandContext context) {

            if (player2.gotchi.owner_id == WILD_GOTCHI_USER_ID)
                return player2.gotchi.name;

            return (await GetPlayer2UserAsync(context)).Username;

        }
        public async Task<IUser> GetOtherPlayerAsync(ICommandContext context, ulong userId) {

            ulong other_user_id = GetOtherPlayerUserId(userId);
            IUser other_user = await context.Guild.GetUserAsync(other_user_id);

            return other_user;

        }
        public ulong GetOtherPlayerUserId(ulong userId) {

            ulong other_user_id = player1.gotchi.owner_id == userId ? player2.gotchi.owner_id : player1.gotchi.owner_id;

            return other_user_id;

        }

        public Gotchi GetPlayersGotchi(ulong userId) {

            return userId == player1.gotchi.owner_id ? player1.gotchi : player2.gotchi;

        }
        public LuaGotchiStats GetGotchiStats(Gotchi gotchi) {

            if (gotchi.id == player1.gotchi.id)
                return player1.stats;
            else if (gotchi.id == player2.gotchi.id)
                return player2.stats;

            return null;

        }

        public async Task SelectMoveAsync(ICommandContext context, string moveIdentifier) {

            PlayerState player = context.User.Id == player1.gotchi.owner_id ? player1 : player2;
            PlayerState other_player = context.User.Id == player1.gotchi.owner_id ? player2 : player1;

            if (!(player.selectedMove is null)) {

                // If the player has already selected a move, don't allow them to change it.

                await BotUtils.ReplyAsync_Info(context, string.Format("You have already selected a move for this turn. Awaiting **{0}**'s move.",
                    (await GetOtherPlayerAsync(context, context.User.Id)).Username));

            }
            else {

                LuaGotchiMove move = player.moves.GetMove(moveIdentifier);

                if (move is null) {

                    // Warn the player if they select an invalid move.
                    await BotUtils.ReplyAsync_Error(context, "The move you have selected is invalid. Please select a valid move.");

                }
                else {

                    // Lock in the selected move.
                    player.selectedMove = move;

                    if (!IsBattlingCpu() && (other_player.selectedMove is null)) {

                        // If the other user hasn't locked in a move yet, await their move.

                        await BotUtils.ReplyAsync_Info(context, string.Format("Move locked in! Awaiting **{0}**'s move.",
                            (await GetOtherPlayerAsync(context, context.User.Id)).Username));

                    }
                    else {

                        // If the player is battling a CPU, select a move for them now.

                        if (IsBattlingCpu())
                            _pickCpuMove(player2);

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

            // The faster gotchi goes first.
            // If both gotchis have the same speed, the first attacker is randomly selected.

            PlayerState first, second;

            if (player1.stats.spd > player2.stats.spd) {
                first = player1;
                second = player2;
            }
            else if (player2.stats.spd > player1.stats.spd) {
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

            // Execute the first player's move.
            await _useMoveOnAsync(context, first, second);

            if (IsBattleOver())
                await _endBattle(context);
            else {

                // Execute the second player's move.
                await _useMoveOnAsync(context, second, first);

                if (IsBattleOver())
                    await _endBattle(context);

            }

            // Apply status problems for both users.

            if (!IsBattleOver()) {

                _applyStatusProblems(context, first);

                if (IsBattleOver())
                    await _endBattle(context);

                else {

                    _applyStatusProblems(context, second);

                    if (IsBattleOver())
                        await _endBattle(context);

                }

            }

            // Show the battle state.
            await ShowBattleStateAsync(context, this);

            // Reset the battle text and each user's selected moves.

            battleText = string.Empty;
            player1.selectedMove = null;
            player2.selectedMove = null;

            ++turnCount;

        }

        public bool IsBattleOver() {

            return player1.stats.hp <= 0.0 || player2.stats.hp <= 0.0;

        }
        public bool IsBattlingCpu() {

            if (!(player2 is null))
                return player2.gotchi.owner_id == WILD_GOTCHI_USER_ID;

            return false;

        }
        public bool IsCpuGotchi(Gotchi gotchi) {

            return gotchi.owner_id == WILD_GOTCHI_USER_ID;

        }
        public static bool IsUserCurrentlyBattling(ulong userId) {

            if (_battle_states.ContainsKey(userId))
                return true;

            return false;


        }

        public static async Task RegisterBattleAsync(ICommandContext context, Gotchi gotchi1, Gotchi gotchi2) {

            // Initialize the battle state.

            GotchiBattleState state = new GotchiBattleState();

            // Initialize both participants.

            // Initialize Player 1 (which must be a human player).

            state.player1 = new PlayerState {
                gotchi = gotchi1,
                moves = await GotchiMoveset.GetMovesetAsync(gotchi1),
                stats = await GotchiStatsUtils.CalculateStats(gotchi1)
            };

            // Initialize Player 2 (which may be a human player, or a CPU).

            if (!(gotchi2 is null)) {

                // If an opponent was provided, use that opponent.

                state.player2 = new PlayerState {
                    gotchi = gotchi2,
                    moves = await GotchiMoveset.GetMovesetAsync(gotchi2),
                    stats = await GotchiStatsUtils.CalculateStats(gotchi2)
                };

            }
            else {

                // Otherwise, generate an opponent for the user.
                await state._generateOpponentAsync();

                // Since the user is battling a CPU, accept the battle immediately.
                state.accepted = true;

            }

            // Register the battle state in the battle state collection.

            _battle_states[gotchi1.owner_id] = state;

            if (state.player2.gotchi.owner_id != WILD_GOTCHI_USER_ID)
                _battle_states[state.player2.gotchi.owner_id] = state;

            // Set the initial message displayed when the battle starts.

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("The battle has begun!");
            sb.AppendLine();
            sb.AppendLine(string.Format("Pick a move with `{0}gotchi move`.\nSee your gotchi's moveset with `{0}gotchi moveset`.",
                OurFoodChainBot.GetInstance().GetConfig().prefix));

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

            GotchiUtils.GotchiGifCreatorParams p1 = new GotchiUtils.GotchiGifCreatorParams {
                gotchi = state.player1.gotchi,
                x = 50,
                y = 150,
                state = state.player1.stats.hp > 0 ? (state.player2.stats.hp <= 0 ? GotchiState.Happy : GotchiState.Energetic) : GotchiState.Dead,
                auto = false
            };

            GotchiUtils.GotchiGifCreatorParams p2 = new GotchiUtils.GotchiGifCreatorParams {
                gotchi = state.player2.gotchi,
                x = 250,
                y = 150,
                state = state.player2.stats.hp > 0 ? (state.player1.stats.hp <= 0 ? GotchiState.Happy : GotchiState.Energetic) : GotchiState.Dead,
                auto = false
            };

            gif_url = await GotchiUtils.Reply_GenerateAndUploadGotchiGifAsync(context, new GotchiUtils.GotchiGifCreatorParams[] { p1, p2 }, new GotchiUtils.GotchiGifCreatorExtraParams {
                backgroundFileName = "home_battle.png",
                overlay = (Graphics gfx) => {

                    // Draw health bars.

                    _drawHealthBar(gfx, p1.x, 180, state.player1.stats.hp / state.player1.stats.max_hp);
                    _drawHealthBar(gfx, p2.x, 180, state.player2.stats.hp / state.player2.stats.max_hp);

                }
            });

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithTitle(string.Format("**{0}** vs. **{1}** (Turn {2})",
                StringUtils.ToTitleCase(state.player1.gotchi.name),
                StringUtils.ToTitleCase(state.player2.gotchi.name),
                state.turnCount));
            embed.WithImageUrl(gif_url);
            embed.WithDescription(state.battleText);
            if (state.IsBattleOver())
                embed.WithFooter("The battle has ended!");
            else if (state.turnCount == 0) {
                embed.WithFooter("The battle has begun. Pick your move!");
            }
            else
                embed.WithFooter(string.Format("Beginning Turn {0}. Pick your move!", state.turnCount + 1));

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }

        private async Task _useMoveOnAsync(ICommandContext context, PlayerState user, PlayerState target) {

            // Check role match-up to see if the move is super-effective.
            // #todo Role match-ups should be defined in an external file.

            Role[] target_roles = await BotUtils.GetRolesFromDbBySpecies(target.gotchi.species_id);
            double weakness_multiplier = user.selectedMove.can_matchup ? _getWeaknessMultiplier(user.selectedMove.role, target_roles) : 1.0;
            Species target_species = await BotUtils.GetSpeciesFromDb(target.gotchi.species_id);

            // Execute the selected move.

            StringBuilder battle_text = new StringBuilder();
            battle_text.AppendLine(battleText);
            battle_text.AppendLine();

            if (!string.IsNullOrEmpty(user.selectedMove.script_path)) {

                // Create, initialize, and execute the script associated with this move.

                Script script = new Script();
                LuaUtils.InitializeScript(script);

                script.DoFile(user.selectedMove.script_path);

                // Initialize the callback args.

                LuaGotchiMoveCallbackArgs args = new LuaGotchiMoveCallbackArgs {
                    user = user.stats,
                    target = target.stats,
                    target_roles = target_roles,
                    target_species = target_species
                };

                // Initialize the move state (required for only certain moves).

                if (!(script.Globals["init"] is null))
                    await script.CallAsync(script.Globals["init"], args);

                // It's possible for a move to be used more than once in a turn (e.g., multi-hit moves).
                // Each time will trigger the callback to be called and display a new message.

                for (int i = 0; i < Math.Max(1, args.times); ++i) {

                    // Check if this was a critical hit, or if the move missed.

                    bool is_hit = !user.selectedMove.can_miss || (BotUtils.RandomInteger(0, 20 + 1) < 20 * user.selectedMove.hit_rate * Math.Max(0.1, user.stats.accuracy));
                    bool is_critical = user.selectedMove.can_critial && (BotUtils.RandomInteger(0, (int)(10 / user.selectedMove.critical_rate)) == 0);

                    if (is_hit) {

                        // Set additional parameters in the callback.

                        args.matchup_multiplier = weakness_multiplier;
                        args.bonus_multiplier = user.selectedMove.multiplier;

                        if (is_critical)
                            args.bonus_multiplier *= 1.5;

                        // Clone each user's stats before triggering the callback, so we can compare them before and after.

                        LuaGotchiStats user_clone = user.stats.clone();
                        LuaGotchiStats target_clone = target.stats.clone();

                        // Trigger the callback.

                        try {

                            if (!(script.Globals["callback"] is null))
                                await script.CallAsync(script.Globals["callback"], args);

                        }
                        catch (Exception) {
                            args.text = "but something went wrong";
                        }

                        // Show the battle text.
                        // If the move doesn't specify a text, choose one automatically (where possible).

                        string text = args.text;

                        // If the gotchi healed but is heal-blocked, undo the heal and update the message.
                        if (user.stats.hp > user_clone.hp && user.stats.status == "heal block") {

                            user.stats.hp = user_clone.hp;
                            text = "but it failed";

                        }

                        if (string.IsNullOrEmpty(text)) {

                            if (target.stats.hp < target_clone.hp) {
                                text = "dealing {target:damage} damage";
                                user.selectedMove.Type = GotchiMoveType.Offensive;
                            }

                            else if (target.stats.atk < target_clone.atk) {
                                text = "lowering its opponent's ATK by {target:atk%}";
                                user.selectedMove.Type = GotchiMoveType.Buff;
                            }
                            else if (target.stats.def < target_clone.def) {
                                text = "lowering its opponent's DEF by {target:def%}";
                                user.selectedMove.Type = GotchiMoveType.Buff;
                            }
                            else if (target.stats.spd < target_clone.spd) {
                                text = "lowering its opponent's SPD by {target:spd%}";
                                user.selectedMove.Type = GotchiMoveType.Buff;
                            }
                            else if (target.stats.accuracy < target_clone.accuracy) {
                                text = "lowering its opponent's accuracy by {target:acc%}";
                                user.selectedMove.Type = GotchiMoveType.Buff;
                            }

                            else if (user.stats.hp > user_clone.hp) {
                                text = "recovering {user:recovered} HP";
                                user.selectedMove.Type = GotchiMoveType.Recovery;
                            }
                            else if (user.stats.atk > user_clone.atk) {
                                text = "boosting its ATK by {user:atk%}";
                                user.selectedMove.Type = GotchiMoveType.Buff;
                            }
                            else if (user.stats.def > user_clone.def) {
                                text = "boosting its DEF by {user:def%}";
                                user.selectedMove.Type = GotchiMoveType.Buff;
                            }
                            else if (user.stats.spd > user_clone.spd) {
                                text = "boosting its SPD by {user:spd%}";
                                user.selectedMove.Type = GotchiMoveType.Buff;
                            }
                            else if (user.stats.accuracy > user_clone.accuracy) {
                                text = "boosting its accuracy by {user:acc%}";
                                user.selectedMove.Type = GotchiMoveType.Buff;
                            }

                            else {
                                text = "but nothing happened?";
                                is_critical = false;
                                weakness_multiplier = 1.0;
                            }

                        }

                        // Various replacements are allowed, which the user can specify in the move's battle text.

                        text = Regex.Replace(text, @"\{([^\}]+)\}", m => {

                            switch (m.Groups[1].Value.ToLower()) {

                                case "damage":
                                case "target:damage":
                                    return string.Format("{0:0.0}", target_clone.hp - target.stats.hp);

                                case "target:atk%":
                                    return string.Format("{0}%", (Math.Abs(target_clone.atk - target.stats.atk) / target_clone.atk) * 100.0);
                                case "target:def%":
                                    return string.Format("{0}%", (Math.Abs(target_clone.def - target.stats.def) / target_clone.def) * 100.0);
                                case "target:spd%":
                                    return string.Format("{0}%", (Math.Abs(target_clone.spd - target.stats.spd) / target_clone.spd) * 100.0);
                                case "target:acc%":
                                    return string.Format("{0}%", (Math.Abs(target_clone.accuracy - target.stats.accuracy) / target_clone.accuracy) * 100.0);

                                case "user:atk%":
                                    return string.Format("{0}%", (Math.Abs(user_clone.atk - user.stats.atk) / user_clone.atk) * 100.0);
                                case "user:def%":
                                    return string.Format("{0}%", (Math.Abs(user_clone.def - user.stats.def) / user_clone.def) * 100.0);
                                case "user:spd%":
                                    return string.Format("{0}%", (Math.Abs(user_clone.spd - user.stats.spd) / user_clone.spd) * 100.0);
                                case "user:acc%":
                                    return string.Format("{0}%", (Math.Abs(user_clone.accuracy - user.stats.accuracy) / user_clone.accuracy) * 100.0);

                                case "user:recovered":
                                    return string.Format("{0:0.0}", user.stats.hp - user_clone.hp);

                                default:
                                    return "???";

                            }

                        });

                        battle_text.Append(string.Format("{0} **{1}** used **{2}**, {3}!",
                            user.selectedMove.icon(),
                            StringUtils.ToTitleCase(user.gotchi.name),
                            StringUtils.ToTitleCase(user.selectedMove.name),
                            text));

                        if (user.selectedMove.can_matchup && weakness_multiplier > 1.0)
                            battle_text.Append(" It's super effective!");

                        if (user.selectedMove.can_critial && is_critical)
                            battle_text.Append(" Critical hit!");

                        if (i + 1 < args.times)
                            battle_text.AppendLine();

                        // Normalize state changes (i.e. make sure no stats ended up being negative).
                        // Do this after the message has been shown so things like damage higher than the target's HP can still be shown correctly.

                        user.stats.normalize();
                        target.stats.normalize();

                    }
                    else {

                        // If the move missed, so display a failure message.
                        battle_text.Append(string.Format("{0} **{1}** used **{2}**, but it missed!",
                            user.selectedMove.icon(),
                            StringUtils.ToTitleCase(user.gotchi.name),
                            StringUtils.ToTitleCase(user.selectedMove.name)));

                    }

                }

                if (args.times > 1)
                    battle_text.Append(string.Format(" Hit {0} times!", args.times));

            }
            else {

                // If there is no Lua script associated with the given move, display a failure message.
                battle_text.Append(string.Format("{0} **{1}** used **{2}**, but it forgot how!",
                    user.selectedMove.icon(),
                    StringUtils.ToTitleCase(user.gotchi.name),
                    StringUtils.ToTitleCase(user.selectedMove.name)));

            }

            battleText = battle_text.ToString();

        }
        private void _applyStatusProblems(ICommandContext context, PlayerState user) {

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(battleText);

            if (user.stats.status == "poisoned") {

                // If the user is poisoned, apply poison damage (1/16th of max HP).

                user.stats.hp = Math.Max(0.0, user.stats.hp - (user.stats.max_hp / 16.0));

                sb.Append(string.Format("\n⚡ **{0}** is damaged by poison!", StringUtils.ToTitleCase(user.gotchi.name)));

            }
            else if (user.stats.status == "rooted") {

                // If the user is rooted, heal some HP (1/10th of max HP).

                user.stats.hp = Math.Min(user.stats.max_hp, user.stats.hp + (user.stats.max_hp / 10.0));

                sb.Append(string.Format("\n❤ **{0}** absorbed nutrients from its roots!", StringUtils.ToTitleCase(user.gotchi.name)));

            }
            else if (user.stats.status == "vine-wrapped") {

                // If the user is wrapped in vines, apply poison damage (1/16th of max HP).

                user.stats.hp = Math.Max(0.0, user.stats.hp - (user.stats.max_hp / 16.0));

                sb.Append(string.Format("\n⚡ **{0}** is hurt by vines!", StringUtils.ToTitleCase(user.gotchi.name)));

            }
            else if (user.stats.status == "thorn-surrounded") {

                // #todo Damage should only be incurred when the user uses a damaging move.

                // If the user is surrounded by thorns, apply thorn damage (1/10th of max HP).
                // Only damages the user if they are attacking the opponent.

                user.stats.hp = Math.Max(0.0, user.stats.hp - (user.stats.max_hp / 10.0));

                sb.Append(string.Format("\n⚡ **{0}** is hurt by thorns!", StringUtils.ToTitleCase(user.gotchi.name)));

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

            exp = (opponent.id == player1.gotchi.id ? player1.stats.level : player2.stats.level) * 10.0;

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
        private async Task _generateOpponentAsync() {

            player2 = new PlayerState();

            Gotchi opp = new Gotchi {
                born_ts = player1.gotchi.born_ts,
                died_ts = player1.gotchi.died_ts,
                evolved_ts = player1.gotchi.evolved_ts,
                fed_ts = player1.gotchi.fed_ts,
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

            for (int i = 0; i < player1.stats.level / 10; ++i)
                if (!await GotchiUtils.EvolveGotchiAsync(opp))
                    break;

            // Calculate stats.

            LuaGotchiStats opp_stats = new LuaGotchiStats {
                level = Math.Max(1, player1.stats.level + BotUtils.RandomInteger(-3, 4)), // up to 3 levels in either direction
                exp = player1.stats.exp
            };

            opp_stats = await GotchiStatsUtils.CalculateStats(opp, opp_stats);

            // Name the gotchi.

            Species opp_species = await BotUtils.GetSpeciesFromDb(opp.species_id);
            opp.name = (opp_species is null ? "Wild Gotchi" : opp_species.GetShortName()) + string.Format(" (Lv. {0})", opp_stats.level);

            // Set the opponent.

            player2.gotchi = opp;
            player2.stats = opp_stats;
            player2.moves = await GotchiMoveset.GetMovesetAsync(player2.gotchi, player2.stats);

        }
        private void _pickCpuMove(PlayerState player) {

            LuaGotchiMove move = player.moves.GetRandomMove();

            player.selectedMove = move;

        }
        private async Task _endBattle(ICommandContext context) {

            PlayerState winner = player1.stats.hp <= 0.0 ? player2 : player1;
            PlayerState loser = player1.stats.hp <= 0.0 ? player1 : player2;

            // Calculate the amount of EXP awarded to the winner.
            // The loser will get 50% of the winner's EXP.

            double exp = _getExpEarned(winner.gotchi, loser.gotchi, won: true);

            double exp1 = player2.stats.hp <= 0.0 ? exp : exp * .5;
            double exp2 = player1.stats.hp <= 0.0 ? exp : exp * .5;

            long levels1 = GotchiStatsUtils.LeveUp(player1.stats, exp1);
            long levels2 = GotchiStatsUtils.LeveUp(player2.stats, exp2);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(battleText);
            sb.AppendLine();

            // Show the winner's accomplishments, then the loser's.

            if (!IsCpuGotchi(winner.gotchi)) {

                double winner_exp = winner.gotchi.id == player1.gotchi.id ? exp1 : exp2;
                long winner_levels = winner.gotchi.id == player1.gotchi.id ? levels1 : levels2;

                sb.AppendLine(string.Format("🏆 **{0}** won the battle! Earned **{1} EXP**.", StringUtils.ToTitleCase(winner.gotchi.name), winner_exp));

                if (winner_levels > 0)
                    sb.AppendLine(string.Format("🆙 **{0}** leveled up to level **{1}**!", StringUtils.ToTitleCase(winner.gotchi.name), winner.stats.level));

                if (((winner.stats.level - levels1) / 10) < (winner.stats.level / 10))
                    if (await GotchiUtils.EvolveGotchiAsync(winner.gotchi)) {

                        Species sp = await BotUtils.GetSpeciesFromDb(winner.gotchi.species_id);

                        sb.AppendLine(string.Format("🚩 Congratulations, **{0}** evolved into **{1}**!", StringUtils.ToTitleCase(winner.gotchi.name), sp.GetShortName()));

                    }

                sb.AppendLine();

            }

            if (!IsCpuGotchi(loser.gotchi)) {

                double loser_exp = loser.gotchi.id == player1.gotchi.id ? exp1 : exp2;
                long loser_levels = loser.gotchi.id == player1.gotchi.id ? levels1 : levels2;

                sb.AppendLine(string.Format("💀 **{0}** lost the battle... Earned **{1} EXP**.", StringUtils.ToTitleCase(loser.gotchi.name), loser_exp));

                if (loser_levels > 0)
                    sb.AppendLine(string.Format("🆙 **{0}** leveled up to level **{1}**!", StringUtils.ToTitleCase(loser.gotchi.name), loser.stats.level));

                if (((loser.stats.level - levels1) / 10) < (loser.stats.level / 10))
                    if (await GotchiUtils.EvolveGotchiAsync(loser.gotchi)) {

                        Species sp = await BotUtils.GetSpeciesFromDb(loser.gotchi.species_id);

                        sb.AppendLine(string.Format("🚩 Congratulations, **{0}** evolved into **{1}**!", StringUtils.ToTitleCase(loser.gotchi.name), sp.GetShortName()));

                    }

            }

            // Update level/exp in the database.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET level=$level, exp=$exp WHERE id=$id;")) {

                cmd.Parameters.AddWithValue("$id", player1.gotchi.id);
                cmd.Parameters.AddWithValue("$level", player1.stats.level);
                cmd.Parameters.AddWithValue("$exp", player1.stats.exp);

                await Database.ExecuteNonQuery(cmd);

            }

            if (!IsCpuGotchi(player2.gotchi)) {

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET level=$level, exp=$exp WHERE id=$id;")) {

                    cmd.Parameters.AddWithValue("$id", player2.gotchi.id);
                    cmd.Parameters.AddWithValue("$level", player2.stats.level);
                    cmd.Parameters.AddWithValue("$exp", player2.stats.exp);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

            // Deregister the battle state.

            DeregisterBattle(context.User.Id);

            battleText = sb.ToString();

        }

    }

}