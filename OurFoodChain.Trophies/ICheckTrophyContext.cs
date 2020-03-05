using OurFoodChain.Common;
using OurFoodChain.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Trophies {

    public interface ICheckTrophyContext {

        ICreator Creator { get; }
        SQLiteDatabase Database { get; }

    }

}