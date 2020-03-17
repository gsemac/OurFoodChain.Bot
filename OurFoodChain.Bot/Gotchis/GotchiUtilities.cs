using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using OurFoodChain.Bot;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Bots;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public static class GotchiUtilities {

        // Public members

        public static async Task<GotchiItem[]> GetGotchiItemsAsync() {

            List<GotchiItem> items = new List<GotchiItem>();
            string[] files = System.IO.Directory.GetFiles(Constants.GotchiItemsDirectory, "*.json");

            foreach (string file in files) {

                GotchiItem item = JsonConvert.DeserializeObject<GotchiItem>(System.IO.File.ReadAllText(file));

                items.Add(item);

            }

            return await Task.FromResult(items.ToArray());

        }
        public static async Task<GotchiItem> GetGotchiItemAsync(long identifier) {
            return await GetGotchiItemAsync(identifier.ToString());
        }
        public static async Task<GotchiItem> GetGotchiItemAsync(string identifier) {

            GotchiItem[] items = await GetGotchiItemsAsync();
            long id = -1;

            if (StringUtilities.IsNumeric(identifier))
                id = long.Parse(identifier);

            foreach (GotchiItem item in items)
                if (item.Name.ToLower() == identifier.ToLower() || (id != GotchiItem.NullId && item.Id == id))
                    return item;

            return null;

        }

        public static async Task<bool> ValidateTradeRequestAsync(ICommandContext context, SQLiteDatabase db, GotchiTradeRequest tradeRequest) {

            // The request is invalid if:
            // - Either user involved in the trade has gotten a new gotchi since the trade was initiated
            // - Either gotchi has died since the trade was initiated
            // - The request has expired

            if (tradeRequest.IsExpired || tradeRequest.OfferedGotchi is null || tradeRequest.ReceivedGotchi is null)
                return false;

            IUser user1 = await context.Guild.GetUserAsync(tradeRequest.OfferedGotchi.OwnerId);
            Gotchi gotchi1 = user1 is null ? null : await db.GetGotchiAsync(user1.ToCreator());

            if (gotchi1 is null || !gotchi1.IsAlive || gotchi1.Id != tradeRequest.OfferedGotchi.Id)
                return false;

            IUser user2 = await context.Guild.GetUserAsync(tradeRequest.ReceivedGotchi.OwnerId);
            Gotchi gotchi2 = user2 is null ? null : await db.GetGotchiAsync(user2.ToCreator());

            if (gotchi2 is null || !gotchi1.IsAlive || gotchi2.Id != tradeRequest.ReceivedGotchi.Id)
                return false;

            return true;

        }
        public static async Task ExecuteTradeRequestAsync(ICommandContext context, SQLiteDatabase db, GotchiTradeRequest tradeRequest) {

            // Get both users and their gotchis.

            IUser user1 = await context.Guild.GetUserAsync(tradeRequest.OfferedGotchi.OwnerId);
            Gotchi gotchi1 = await db.GetGotchiAsync(user1.ToCreator());
            GotchiUserInfo userInfo1 = await db.GetUserInfoAsync(user1.ToCreator());

            IUser user2 = await context.Guild.GetUserAsync(tradeRequest.ReceivedGotchi.OwnerId);
            Gotchi gotchi2 = await db.GetGotchiAsync(user2.ToCreator());
            GotchiUserInfo userInfo2 = await db.GetUserInfoAsync(user2.ToCreator());

            // Swap the owners of the gotchis.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET owner_id = $owner_id WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$owner_id", user1.Id);
                cmd.Parameters.AddWithValue("$id", gotchi2.Id);

                await db.ExecuteNonQueryAsync(cmd);

            }

            userInfo1.PrimaryGotchiId = gotchi2.Id;

            await db.UpdateUserInfoAsync(userInfo1);

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Gotchi SET owner_id = $owner_id WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$owner_id", user2.Id);
                cmd.Parameters.AddWithValue("$id", gotchi1.Id);

                await db.ExecuteNonQueryAsync(cmd);

            }

            userInfo2.PrimaryGotchiId = gotchi1.Id;

            await db.UpdateUserInfoAsync(userInfo2);

            // Remove all existing trade requests involving either user.
            _trade_requests.RemoveAll(x => x.OfferedGotchi.OwnerId == user1.Id || x.ReceivedGotchi.OwnerId == user2.Id);

        }
        public static async Task<GotchiTradeRequestResult> MakeTradeRequestAsync(ICommandContext context, SQLiteDatabase db, Gotchi offeredGotchi, Gotchi recievedGotchi) {

            // If either gotchi passed in is null, the request is invalid.

            if (offeredGotchi is null || recievedGotchi is null)
                return GotchiTradeRequestResult.Invalid;

            // If the user has made previous trade requests, remove them.
            _trade_requests.RemoveAll(x => x.OfferedGotchi.OwnerId == offeredGotchi.OwnerId);

            // If their partner already has an open trade request that hasn't been accepted, don't allow a new trade request to be made.
            // This is so users cannot make a new trade request right before one is accepted and snipe the trade.

            GotchiTradeRequest request = GetTradeRequest(recievedGotchi);

            if (!(request is null)) {

                if (request.IsExpired)
                    _trade_requests.RemoveAll(x => x.ReceivedGotchi.OwnerId == recievedGotchi.OwnerId);
                else
                    return GotchiTradeRequestResult.RequestPending;

            }

            request = new GotchiTradeRequest {
                OfferedGotchi = offeredGotchi,
                ReceivedGotchi = recievedGotchi
            };

            if (!await ValidateTradeRequestAsync(context, db, request))
                return GotchiTradeRequestResult.Invalid;

            _trade_requests.Add(request);

            return GotchiTradeRequestResult.Success;

        }
        public static GotchiTradeRequest GetTradeRequest(Gotchi recievedGotchi) {

            // Returns the trade request that this user is a partner in.
            // If the partner has changed gotchis since the request was initiated, the request is invalid and thus not returned.

            foreach (GotchiTradeRequest request in _trade_requests)
                if (request.ReceivedGotchi.OwnerId == recievedGotchi.OwnerId && request.ReceivedGotchi.Id == recievedGotchi.Id)
                    return request;

            return null;

        }

        public static bool TryReadGotchiConfigurationFromFile(out GotchiConfiguration result) {

            List<string> filePaths = new List<string> {
                "gotchi-config.json", // file name used in past versions
                "gotchi_config.json"
            };

            foreach (string path in filePaths) {

                if (System.IO.File.Exists(path)) {

                    result = ConfigurationBase.Open<GotchiConfiguration>(path);

                    return true;

                }

            }

            result = null;

            return false;

        }

        // Private members

        private static readonly List<GotchiTradeRequest> _trade_requests = new List<GotchiTradeRequest>();

    }

}