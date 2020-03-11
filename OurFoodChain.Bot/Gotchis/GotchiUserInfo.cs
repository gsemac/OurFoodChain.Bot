using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public class GotchiUserInfo {

        // Public methods

        public GotchiUserInfo(ulong? userId) {

            if (!userId.HasValue)
                throw new ArgumentNullException(nameof(userId));

            UserId = userId.Value;

        }

        public static GotchiUserInfo FromDataRow(DataRow row) {

            return new GotchiUserInfo((ulong)row.Field<long>("user_id")) {
                G = row.Field<long>("g"),
                GotchiLimit = row.Field<long>("gotchi_limit"),
                PrimaryGotchiId = row.Field<long>("primary_gotchi_id")
            };

        }

        // Public properties

        /// <summary>
        /// The amount of currency (G) owned by the user.
        /// </summary>
        public long G {
            get {
                return _g;
            }
            set {
                _g = Math.Max(0, value);
            }
        }
        /// <summary>
        /// The Discord user ID of the user.
        /// </summary>
        public ulong UserId { get; } = 0;
        /// <summary>
        /// The number of Gotchis the user is allowed to have at one time.
        /// </summary>
        public long GotchiLimit { get; set; } = 1;
        /// <summary>
        /// The ID of the user's primary gotchi.
        /// </summary>
        public long PrimaryGotchiId { get; set; } = Gotchi.NullGotchiId;

        // Private members

        private long _g = 0;

    }

}