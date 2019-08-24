using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies {

    public class TrophyUser {

        public TrophyUser(ulong userId, long earnedTimestamp) {

            UserId = userId;
            EarnedTimestamp = earnedTimestamp;

        }

        public ulong UserId { get; }
        public long EarnedTimestamp { get; }
        public DateTime EarnedDate {
            get {
                return DateTimeOffset.FromUnixTimeSeconds(EarnedTimestamp).DateTime.ToUniversalTime();
            }
        }

    }
}