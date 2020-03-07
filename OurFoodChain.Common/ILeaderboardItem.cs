using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public interface ILeaderboardItem {

        string Name { get; set; }
        long Rank { get; set; }
        long Score { get; set; }

        string Icon { get; }

    }

}