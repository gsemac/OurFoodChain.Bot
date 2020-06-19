using OurFoodChain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Trophies {

    public interface IUnlockedTrophyInfo {

        IUser Creator { get; }
        ITrophy Trophy { get; }
        int TimesUnlocked { get; set; }
        DateTimeOffset DateUnlocked { get; set; }

    }

}