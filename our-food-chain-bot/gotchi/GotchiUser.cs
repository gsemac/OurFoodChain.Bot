using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public class GotchiUser {

        // Public methods

        public GotchiUser(ulong userId) {

            UserId = userId;

        }

        public static GotchiUser FromDataRow(DataRow row) {

            return new GotchiUser((ulong)row.Field<long>("user_id")) {
                G = row.Field<long>("g"),
                GotchiLimit = (ulong)row.Field<long>("gotchi_limit"),
                PrimaryGotchiId = row.Field<long>("primary_gotchi_id")
            };

        }

        // Public properties

        /// <summary>
        /// The amount of currency (G) owned by the user.
        /// </summary>
        public long G { get; set; } = 0;
        /// <summary>
        /// The Discord user ID of the user.
        /// </summary>
        public ulong UserId { get; } = 0;
        /// <summary>
        /// The number of Gotchis the user is allowed to have at one time.
        /// </summary>
        public ulong GotchiLimit { get; set; } = 1;
        /// <summary>
        /// The ID of the user's primary gotchi.
        /// </summary>
        public long PrimaryGotchiId { get; set; } = Gotchi.NULL_GOTCHI_ID;

    }

}