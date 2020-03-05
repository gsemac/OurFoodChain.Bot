using OurFoodChain.Common;
using OurFoodChain.Debug;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies {

    public interface ITrophyScanner {

        event EventHandler<LogMessage> Log;
        event EventHandler<IUnlockedTrophyInfo> TrophyUnlocked;

        void RegisterTrophy(ITrophy trophy);
        void RegisterTrophies();
        IEnumerable<ITrophy> GetTrophies();

        Task EnqueueAsync(ICreator creator, bool scanImmediately = false);

    }

}