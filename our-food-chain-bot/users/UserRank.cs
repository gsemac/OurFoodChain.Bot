using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class UserRank {

        public UserInfo User { get; set; } = new UserInfo();

        public long Rank { get; set; } = 0;
        public string Icon {
            get {

                switch (Rank) {

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

}