using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class GotchiItem {

        public const long NULL_ITEM_ID = -1;
        public const string DEFAULT_NAME = "item";
        public const string DEFAULT_DESCRIPTION = "No description provided.";
        
        public long id = NULL_ITEM_ID;
        public string icon = "";
        public string name = DEFAULT_NAME;
        public string description = DEFAULT_DESCRIPTION;
        public ulong cost = 0;

        public string Name {
            get {
                return StringUtils.ToTitleCase(name);
            }
            set {
                name = value;
            }
        }

    }

}