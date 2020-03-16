using Discord.Commands;
using OurFoodChain.Common;
using OurFoodChain.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Trophies {

    public class TrophyScannerContext :
        ITrophyScannerContext {

        public ICommandContext CommandContext { get; }
        public ICreator Creator { get; }
        public SQLiteDatabase Database { get; }

        public TrophyScannerContext(ICommandContext commandContext, ICreator creator, SQLiteDatabase database) {

            this.CommandContext = commandContext;
            this.Creator = creator;
            this.Database = database;

        }

    }

}