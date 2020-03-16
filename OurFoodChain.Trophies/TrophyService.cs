using OurFoodChain.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies {

    public class TrophyService :
         ITrophyService {

        // Public members

        public event Func<ILogMessage, Task> Log;

        public IEnumerable<ITrophy> GetTrophies() {

            return trophies;

        }
        public async Task RegisterTrophiesAsync(RegisterTrophiesOptions options = RegisterTrophiesOptions.None) {

            // Register all trophies in the assembly.

            await OnLogAsync(LogSeverity.Info, "Registering trophies");

            Assembly currentAssembly = Assembly.GetExecutingAssembly();

            IEnumerable<Type> trophyTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !options.HasFlag(RegisterTrophiesOptions.ExcludeDefaultTrophies) || assembly != currentAssembly)
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsAbstract && typeof(ITrophy).IsAssignableFrom(type))
                .Where(type => type.GetConstructor(Type.EmptyTypes) != null);

            foreach (Type type in trophyTypes) {

                ITrophy instance = (ITrophy)Activator.CreateInstance(type);

                trophies.Add(instance);

            }

            if (!options.HasFlag(RegisterTrophiesOptions.ExcludeDefaultTrophies)) {

                trophies.Add(new Trophy("To Infinity And Beyond", "Own a species that spreads to another zone."));
                trophies.Add(new Trophy("A New World", "Create a species that spreads across an ocean body."));
                trophies.Add(new Trophy("One To Rule Them All", "Create a species that turns into an apex predator."));
                trophies.Add(new Trophy("I Am Selection", "Create a species that is the direct cause of another species extinction."));

                trophies.Add(new Trophy("Colonization", "Be the first to create a eusocial species.", TrophyFlags.Hidden | TrophyFlags.OneTime));
                trophies.Add(new Trophy("Let There Be Light", "Be the first to create a species that makes light.", TrophyFlags.Hidden | TrophyFlags.OneTime));
                trophies.Add(new Trophy("Master Of The Skies", "Be the first to create a species capable of flight.", TrophyFlags.Hidden | TrophyFlags.OneTime));
                trophies.Add(new Trophy("Did You Hear That?", "Be the first to make a species that makes noise.", TrophyFlags.Hidden | TrophyFlags.OneTime));
                trophies.Add(new Trophy("Double Trouble", "Be the first to make a species with two legs.", TrophyFlags.Hidden | TrophyFlags.OneTime));
                trophies.Add(new Trophy("Can We Keep It?", "Be the first to create a species with fur.", TrophyFlags.Hidden | TrophyFlags.OneTime));
                trophies.Add(new Trophy("Turn On The AC!", "Be the first to create a warm-blooded species.", TrophyFlags.Hidden | TrophyFlags.OneTime));
                trophies.Add(new Trophy("Do You See What I See?", "Be the first to create a species with developed eyes.", TrophyFlags.Hidden | TrophyFlags.OneTime));
                trophies.Add(new Trophy("Imposter", "Be the first to create a species that mimics another species.", TrophyFlags.Hidden | TrophyFlags.OneTime));

            }

            await OnLogAsync(LogSeverity.Info, string.Format("Registered {0} trophies", trophies.Count()));

        }
        public void RegisterTrophy(ITrophy trophy) {

            trophies.Add(trophy);

        }

        // Protected members

        protected async Task OnLogAsync(ILogMessage message) {

            if (Log != null)
                await Log(message);

        }
        protected async Task OnLogAsync(LogSeverity severity, string message) {

            if (Log != null)
                await OnLogAsync(new LogMessage(severity, "Trophies", message));

        }

        // Private members

        private readonly List<ITrophy> trophies = new List<ITrophy>();

    }

}