using OurFoodChain.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies {

    public enum TrophyFlags {
        Hidden = 1,
        OneTime = 2
    }

    public class Trophy {

        public const string HIDDEN_TROPHY_DESCRIPTION = "This is a hidden trophy. Unlock it for details!";

        public Trophy(string name, string description, Func<TrophyScanner.ScannerQueueItem, Task<bool>> checkUnlocked) {

            this.name = name;
            _description = description;
            _checkUnlocked = checkUnlocked;
            Flags = 0;

        }
        public Trophy(string name, string description, TrophyFlags flags, Func<TrophyScanner.ScannerQueueItem, Task<bool>> checkUnlocked) :
            this(name, description, checkUnlocked) {

            Flags = flags;

        }

        public string GetName() {
            return StringUtilities.ToTitleCase(name);
        }
        public string GetIdentifier() {
            return name.ToLower().Replace(' ', '_');
        }
        public string GetDescription() {
            return _description;
        }
        public string GetIcon() {

            string icon = "🏆";

            if (Flags.HasFlag(TrophyFlags.OneTime))
                icon = "🥇";
            else if (Flags.HasFlag(TrophyFlags.Hidden))
                icon = "❓";

            return icon;

        }
        public async Task<bool> IsUnlocked(TrophyScanner.ScannerQueueItem item) {

            if (_checkUnlocked is null)
                return false;

            return await _checkUnlocked(item);

        }

        public TrophyFlags Flags { get; }

        public string name;

        private readonly string _description;
        private Func<TrophyScanner.ScannerQueueItem, Task<bool>> _checkUnlocked;

    }

}