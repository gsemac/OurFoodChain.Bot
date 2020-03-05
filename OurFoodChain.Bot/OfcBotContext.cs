using Discord.Commands;
using OurFoodChain.Bot;
using OurFoodChain.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class OfcBotContext :
        IOfcBotContext {

        public ICommandContext CommandContext { get; }
        public IOfcBotConfiguration Configuration { get; }
        public SQLiteDatabase Database { get; }

        public OfcBotContext(ICommandContext commandContext, IOfcBotConfiguration configuration, SQLiteDatabase database) {

            this.CommandContext = commandContext;
            this.Configuration = configuration;
            this.Database = database;

        }

    }

}