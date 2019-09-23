using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class Gotchi {

        // Public members

        public const long NullGotchiId = -1;
        public static readonly long HoursPerDay = 24;

        public long Id { get; set; } = NullGotchiId;
        public long SpeciesId { get; set; } = Species.NullSpeciesId;
        public string Name {
            get {
                return StringUtils.ToTitleCase(_name);
            }
            set {
                _name = value;
            }
        }
        public ulong OwnerId { get; set; } = UserInfo.NullUserId;
        public long FedTimestamp { get; set; } = DateUtils.GetCurrentTimestamp();
        public long BornTimestamp { get; set; } = DateUtils.GetCurrentTimestamp();
        public long DiedTimestamp { get; set; } = 0;
        public long EvolvedTimestamp { get; set; } = 0;

        public int Experience { get; set; } = 0;

        public int Age {
            get {

                long ts = DateUtils.GetCurrentTimestamp();

                return (int)(((DiedTimestamp > 0 ? DiedTimestamp : ts) - BornTimestamp) / 60 / 60 / 24);

            }
        }

        public bool IsSleeping() {

            return (HoursSinceBirth() % HoursPerDay) >= (HoursPerDay - Global.GotchiContext.Config.SleepHours);

        }
        public long HoursOfSleepLeft() {

            if (!IsSleeping())
                return 0;

            return HoursPerDay - (HoursSinceBirth() % HoursPerDay);

        }
        public long HoursSinceLastSlept() {

            if (IsSleeping())
                return 0;

            return HoursSinceBirth() % HoursPerDay;

        }
        public long HoursUntilSleep() {

            if (IsSleeping())
                return 0;

            return (HoursPerDay - Global.GotchiContext.Config.SleepHours) - (HoursSinceBirth() % HoursPerDay);

        }
        public bool IsEating() {

            return HoursSinceFed() < 1;

        }
        public bool IsHungry() {

            return HoursSinceFed() > 12;

        }
        public bool IsDead() {

            return HoursSinceFed() > (HoursPerDay * Global.GotchiContext.Config.MaxMissedFeedings);

        }
        public long HoursSinceBirth() {

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            long ts_diff = ts - BornTimestamp;
            long hours_diff = ts_diff / 60 / 60;

            return hours_diff;

        }
        public long HoursSinceFed() {

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            long ts_diff = ts - FedTimestamp;
            long hours_diff = ts_diff / 60 / 60;

            return hours_diff;

        }
        public long HoursSinceEvolved() {

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            long ts_diff = ts - EvolvedTimestamp;
            long hours_diff = ts_diff / 60 / 60;

            return hours_diff;

        }
        public bool IsReadyToEvolve() {

            return !IsDead() && HoursSinceEvolved() >= 7 * 24;

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

        // Private members

        private string _name;

    }

}