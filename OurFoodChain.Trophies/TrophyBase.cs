using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies {

    public abstract class TrophyBase :
        ITrophy {

        // Public members

        public const string HiddenTrophyDescription = "This is a hidden trophy. Unlock it for details!";

        public string Icon => GetIcon();
        public string Name { get; }
        public string Description { get; }
        public TrophyFlags Flags { get; }

        public string Identifier => GetIdentifier();

        public virtual async Task<bool> CheckTrophyAsync(ICheckTrophyContext context) {

            return await Task.FromResult(false);

        }

        // Protected members

        protected TrophyBase(string name, string description, TrophyFlags flags = TrophyFlags.None) {

            this.Name = name.ToTitle();
            this.Description = StringUtilities.ToSentenceCase(description);
            this.Flags = flags;

        }

        // Private members

        private string GetIcon() {

            if (Flags.HasFlag(TrophyFlags.OneTime))
                return "🥇";
            else if (Flags.HasFlag(TrophyFlags.Hidden))
                return "❓";
            else
                return "🏆";

        }
        private string GetIdentifier() {

            return Name.ToLowerInvariant().Replace(' ', '_');

        }

    }

}