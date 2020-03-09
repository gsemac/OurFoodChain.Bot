using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using OurFoodChain.Adapters;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Trophies;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OurFoodChain.Trophies.Extensions;
using OurFoodChain.Common;

namespace OurFoodChain.Bot.Modules {

    public class GeneralModule :
        OfcModuleBase {

        [Command("roll")]
        public async Task Roll() {

            await Roll(6);

        }
        [Command("roll")]
        public async Task Roll(int max) {

            if (max < 1)
                await ReplyErrorAsync("Value must be greater than or equal 1.");

            else
                await Roll(1, max);

        }
        [Command("roll")]
        public async Task Roll(int min, int max) {

            if (min < 0 || max < 0)
                await ReplyErrorAsync("Values must be greater than 1.");
            if (min > max + 1)
                await ReplyErrorAsync("Minimum value must be less than or equal to the maximum value.");
            else {

                int result = NumberUtilities.GetRandomInteger(min, max + 1);

                await ReplyAsync(result.ToString());

            }

        }

    }

}