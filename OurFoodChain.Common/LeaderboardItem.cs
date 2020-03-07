using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public class LeaderboardItem :
        ILeaderboardItem {

        // Public members

        public string Name { get; set; }
        public long Rank { get; set; }
        public long Score { get; set; } = 0;

        public string Icon => GetRankIcon(Rank);

        // Private members

        public static string GetRankIcon(long rank) {

            switch (rank) {

                case 1:
                    return "👑";

                case 2:
                    return "🥈";

                case 3:
                    return "🥉";

                default:
                    return "➖";

            }

        }

    }

}