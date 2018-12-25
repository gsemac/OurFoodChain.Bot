using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    class CommandUtils {

        public class PaginatedMessage {

            public PaginatedMessage() {
                index = 0;
            }

            public Embed[] pages;
            public int index;

        }

        public static Dictionary<ulong, PaginatedMessage> PAGINATED_MESSAGES = new Dictionary<ulong, PaginatedMessage>();


    }

}
