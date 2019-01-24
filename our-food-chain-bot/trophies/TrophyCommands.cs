using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.trophies {

    public class TrophyCommands :
        ModuleBase {

        [Command("trophy")]
        public async Task Trophy() {

            await TrophyScanner.AddToQueueAsync(Context, Context.User.Id);

        }

    }

}