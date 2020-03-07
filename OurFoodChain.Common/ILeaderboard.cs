using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public interface ILeaderboard :
        IEnumerable<ILeaderboardItem> {

        string Title { get; set; }

        void Add(string name, long score);

    }

}