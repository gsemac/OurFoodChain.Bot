using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Trophies {

    public class TrophyUnlockedArgs {

        public ITrophyScannerContext Context { get; }
        public IUnlockedTrophyInfo TrophyInfo { get; }

        public TrophyUnlockedArgs(ITrophyScannerContext context, IUnlockedTrophyInfo trophyInfo) {

            this.Context = context;
            this.TrophyInfo = trophyInfo;

        }


    }

}