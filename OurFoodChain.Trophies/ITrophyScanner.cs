using OurFoodChain.Common;
using OurFoodChain.Debug;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies {

    public enum RegisterTrophiesOptions {
        None = 0,
        RegisterDefaultTrophies = 1
    }

    public interface ITrophyScanner {

        event Func<ILogMessage, Task> Log;
        event Func<IUnlockedTrophyInfo, Task> TrophyUnlocked;

        void RegisterTrophy(ITrophy trophy);
        Task RegisterTrophiesAsync(RegisterTrophiesOptions options = RegisterTrophiesOptions.None);
        IEnumerable<ITrophy> GetTrophies();

        Task<bool> EnqueueAsync(ICreator creator, bool scanImmediately = false);

    }

}