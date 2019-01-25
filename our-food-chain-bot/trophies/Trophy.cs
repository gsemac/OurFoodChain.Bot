using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.trophies {

    public class Trophy {

        public Trophy(string name, string description, Func<ulong, Task<bool>> checkUnlocked) {

            _name = name;
            _description = description;
            _checkUnlocked = checkUnlocked;
            Secret = false;

        }

        public string GetName() {
            return StringUtils.ToTitleCase(_name);
        }
        public string GetIdentifier() {
            return _name.ToLower().Replace(' ', '_');
        }
        public string GetDescription() {
            return _description;
        }
        public async Task<bool> IsUnlocked(ulong userId) {

            if (_checkUnlocked is null)
                return false;

            return await _checkUnlocked(userId);

        }

        public bool Secret { get; }

        private string _name;
        private string _description;
        private Func<ulong, Task<bool>> _checkUnlocked;

    }

}