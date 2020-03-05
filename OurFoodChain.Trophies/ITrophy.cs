using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies {

    public enum TrophyFlags {
        None = 0,
        Hidden = 1,
        OneTime = 2
    }

    public interface ITrophy {

        string Icon { get; }
        string Name { get; }
        string Description { get; }
        TrophyFlags Flags { get; }

        string Identifier { get; }

        Task<bool> CheckTrophyAsync(ICheckTrophyContext context);

    }

}