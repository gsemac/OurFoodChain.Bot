using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.gotchi {

    public class Gotchi {

        public const long NULL_GOTCHI_ID = -1;
        public const long HOURS_OF_SLEEP_PER_DAY = 8;
        public const long HOURS_PER_DAY = 24;
        public const long MAXIMUM_STARVATION_DAYS = 3; // 3 days of no feeding

        public long id = NULL_GOTCHI_ID;
        public long species_id = -1;
        public string name;
        public ulong owner_id = 0;
        public long fed_ts = 0;
        public long born_ts = 0;
        public long died_ts = 0;
        public long evolved_ts = 0;

        public bool IsSleeping() {

            return (HoursSinceBirth() % HOURS_PER_DAY) >= (HOURS_PER_DAY - HOURS_OF_SLEEP_PER_DAY);

        }
        public long HoursOfSleepLeft() {

            if (!IsSleeping())
                return 0;

            return HOURS_PER_DAY - (HoursSinceBirth() % HOURS_PER_DAY);

        }
        public long HoursSinceLastSlept() {

            if (IsSleeping())
                return 0;

            return HoursSinceBirth() % HOURS_PER_DAY;

        }
        public long HoursUntilSleep() {

            if (IsSleeping())
                return 0;

            return (HOURS_PER_DAY - HOURS_OF_SLEEP_PER_DAY) - (HoursSinceBirth() % HOURS_PER_DAY);

        }
        public bool IsEating() {

            return HoursSinceFed() < 1;

        }
        public bool IsHungry() {

            return HoursSinceFed() > 12;

        }
        public bool IsDead() {

            return HoursSinceFed() > (HOURS_PER_DAY * MAXIMUM_STARVATION_DAYS); 

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
        public bool IsReadyToEvolve() {

            return !IsDead() && HoursSinceEvolved() >= 7 * 24;

        }
        public long Age() {

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return ((died_ts > 0 ? died_ts : ts) - born_ts) / 60 / 60 / 24;

        }
        public GotchiState State() {

            if (IsDead())
                return GotchiState.Dead;
            else if (IsSleeping())
                return GotchiState.Sleeping;
            else if (IsReadyToEvolve())
                return GotchiState.ReadyToEvolve;
            else if (IsHungry())
                return GotchiState.Hungry;
            else if (IsEating())
                return GotchiState.Eating;
            else if (HoursSinceLastSlept() < 1)
                return GotchiState.Energetic;
            else if (HoursUntilSleep() <= 1)
                return GotchiState.Tired;

            return GotchiState.Happy;

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