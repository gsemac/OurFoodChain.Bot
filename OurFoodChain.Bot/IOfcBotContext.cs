using Discord.Commands;
using OurFoodChain.Bot;
using OurFoodChain.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public interface IOfcBotContext {

        ICommandContext CommandContext { get; }
        IOfcBotConfiguration Configuration { get; }
        SQLiteDatabase Database { get; }

    }

}