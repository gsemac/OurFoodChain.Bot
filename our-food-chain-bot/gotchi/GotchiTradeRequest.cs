using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public enum GotchiTradeRequestResult {
        Success,
        Invalid,
        RequestPending
    }

    public class GotchiTradeRequest {

        public const long MINUTES_UNTIL_EXPIRED = 5;

        public GotchiTradeRequest() {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public Gotchi OfferedGotchi { get; set; }
        public Gotchi ReceivedGotchi { get; set; }
        public long Timestamp { get; set; }

        public bool IsExpired {
            get {
                return ((DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Timestamp) / 60) > MINUTES_UNTIL_EXPIRED;
            }
        }

    }

}