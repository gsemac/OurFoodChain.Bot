using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.trophies {

    public class TrophyRegistry {

        public static async Task InitializeAsync() {

            await _registerAllAsync();

        }

        public static IReadOnlyCollection<Trophy> Trophies => _registry.AsReadOnly();


        private static List<Trophy> _registry = new List<Trophy>();

        private static async Task _registerAllAsync() {

            // Don't bother if we've already registered the trophies.
            if (_registry.Count > 0)
                return;

            await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Info, "Trophies", "Registering trophies");

            _registry.Add(new Trophy("Super Special Trophy", "This trophy is meaningless, and only exists for testing purposes. You're not special.", async (ulong userId) => {
                return true;
            }));

        }


    }

}