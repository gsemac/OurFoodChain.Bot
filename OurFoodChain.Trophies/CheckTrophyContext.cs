using OurFoodChain.Common;
using OurFoodChain.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Trophies {

    public class CheckTrophyContext :
        ICheckTrophyContext {

        public SQLiteDatabase Database { get; }
        public ICreator Creator { get; }

        public CheckTrophyContext(SQLiteDatabase database, ICreator creator) {

            this.Database = database;
            this.Creator = creator;

        }

    }

}