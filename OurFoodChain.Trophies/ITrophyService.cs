using OurFoodChain.Debug;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies {

    public interface ITrophyService {

        event Func<ILogMessage, Task> Log;

        void RegisterTrophy(ITrophy trophy);
        Task RegisterTrophiesAsync(RegisterTrophiesOptions options = RegisterTrophiesOptions.None);
        IEnumerable<ITrophy> GetTrophies();

    }

}