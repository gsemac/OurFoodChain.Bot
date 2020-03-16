using OurFoodChain.Common;
using OurFoodChain.Debug;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies {

    public enum RegisterTrophiesOptions {
        None = 0,
        ExcludeDefaultTrophies = 1
    }

    public interface ITrophyScanner {

        event Func<ILogMessage, Task> Log;
        event Func<TrophyUnlockedArgs, Task> TrophyUnlocked;

        Task<bool> EnqueueAsync(ITrophyScannerContext context, bool scanImmediately = false);

    }

}