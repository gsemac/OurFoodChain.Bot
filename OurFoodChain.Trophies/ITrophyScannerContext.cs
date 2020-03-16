using Discord.Commands;
using OurFoodChain.Common;
using OurFoodChain.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Trophies {

    public interface ITrophyScannerContext {

        ICommandContext CommandContext { get; }
        ICreator Creator { get; }
        SQLiteDatabase Database { get; }

    }

}