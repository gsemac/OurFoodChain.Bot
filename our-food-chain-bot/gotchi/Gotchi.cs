using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    class Gotchi {

        public long id = -1;
        public long species_id = -1;
        public string name;
        public ulong owner_id = 0;
        public long fed_ts = 0;
        public long born_ts = 0;
        public long died_ts = 0;
        public long evolved_ts = 0;

        public bool IsSleeping() {

            return (HoursSinceBirth() / 12) % 2 == 1;

        }
        public bool IsEating() {

            return HoursSinceFed() < 1;

        }
        public bool IsHungry() {

            return HoursSinceFed() > 12;

        }
        public bool IsDead() {

            return HoursSinceFed() > 48;

        }
        public long HoursSinceBirth() {

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            long ts_diff = ts - born_ts;
            long hours_diff = ts_diff / 60 / 60;

            return hours_diff;

        }
        public long HoursSinceFed() {

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            long ts_diff = ts - fed_ts;
            long hours_diff = ts_diff / 60 / 60;

            return hours_diff;

        }
        public long HoursSinceEvolved() {

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            long ts_diff = ts - evolved_ts;
            long hours_diff = ts_diff / 60 / 60;

            return hours_diff;

        }
        public long Age() {

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return (ts - born_ts) / 60 / 60 / 24;

        }

        public static Gotchi FromDataRow(DataRow row) {

            Gotchi result = new Gotchi() {
                id = row.Field<long>("id"),
                species_id = row.Field<long>("species_id"),
                name = row.Field<string>("name"),
                owner_id = (ulong)row.Field<long>("owner_id"),
                fed_ts = row.Field<long>("fed_ts"),
                born_ts = row.Field<long>("born_ts"),
                died_ts = row.Field<long>("died_ts"),
                evolved_ts = row.Field<long>("evolved_ts")
            };

            return result;

        }

    }

}