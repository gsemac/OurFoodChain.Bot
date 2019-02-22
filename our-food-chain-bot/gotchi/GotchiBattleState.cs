using Discord;
using Discord.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public bool accepted = false;
        public string message = "";
        public int turn = 1;
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

            return (turn == 1 && userId == gotchi1.owner_id) || (turn == 2 && userId == gotchi2.owner_id);

        }
        public Gotchi GetGotchi(ulong userId) {

            return userId == gotchi1.owner_id ? gotchi1 : gotchi2;

        }

        public static async Task RegisterBattleAsync(Gotchi gotchi1, Gotchi gotchi2) {

            GotchiBattleState state = new GotchiBattleState {
                gotchi1 = gotchi1,
                gotchi2 = gotchi2,
                stats1 = await GotchiStats.CalculateStats(gotchi1),
                stats2 = await GotchiStats.CalculateStats(gotchi2),
            };

            state.currentTurn = state.stats1.spd > state.stats2.spd ? 1 : 2;

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

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithTitle(string.Format("**{0}** vs. **{1}**", StringUtils.ToTitleCase(state.gotchi1.name), StringUtils.ToTitleCase(state.gotchi2.name)));
            embed.WithImageUrl("https://via.placeholder.com/300");
            embed.WithDescription(state.message);
            embed.WithFooter(string.Format("Awaiting {0}'s next move.", state.currentTurn == 1 ? (await state.GetUser1Async(context)).Username : (await state.GetUser2Async(context)).Username));

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }

    }

}