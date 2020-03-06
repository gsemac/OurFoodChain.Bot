using Discord.Commands;
using OurFoodChain.Data;
using OurFoodChain.Discord.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class OfcModuleBase :
        ModuleBase {

        public IDatabaseService DatabaseService { get; set; }

        public async Task<SQLiteDatabase> GetDatabaseAsync() => await DatabaseService.GetDatabaseAsync(Context.Guild.Id);

    }

}