using OurFoodChain.Common;
using OurFoodChain.Trophies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Extensions {

    public static class OfcModuleBaseTrophyExtensions {

        public static async Task ScanTrophiesAsync(this OfcModuleBase moduleBase, IUser creator, bool scanImmediately = false) {

            if (moduleBase.TrophyScanner != null && moduleBase.Config.TrophiesEnabled) {

                ITrophyScannerContext context = new TrophyScannerContext(moduleBase.Context, creator, await moduleBase.GetDatabaseAsync());

                await moduleBase.TrophyScanner.EnqueueAsync(context, scanImmediately);

            }

        }

    }

}