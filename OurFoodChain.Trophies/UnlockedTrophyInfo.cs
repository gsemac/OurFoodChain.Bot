using OurFoodChain.Common;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Trophies {

    public class UnlockedTrophyInfo :
        IUnlockedTrophyInfo {

        public IUser Creator { get; }
        public ITrophy Trophy { get; }
        public int TimesUnlocked { get; set; } = 1;
        public DateTimeOffset DateUnlocked { get; set; } = DateUtilities.GetCurrentDateUtc();

        public UnlockedTrophyInfo(IUser creator, ITrophy trophy) {

            this.Creator = creator;
            this.Trophy = trophy;

        }

    }

}