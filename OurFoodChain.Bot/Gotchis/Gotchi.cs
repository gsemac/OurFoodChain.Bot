using OurFoodChain.Common.Utilities;
using System;

namespace OurFoodChain.Gotchis {

    public class Gotchi {

        // Public members

        public const long NullGotchiId = -1;
        public static readonly long HoursPerDay = 24;

        public long Id { get; set; } = NullGotchiId;
        public long? SpeciesId { get; set; } 
        public string Name {
            get {
                return StringUtilities.ToTitleCase(_name, TitleOptions.CapitalizeRomanNumerals);
            }
            set {
                _name = value;
            }
        }
        public ulong OwnerId { get; set; } = UserInfo.NullId;
        public long FedTimestamp { get; set; } = DateUtilities.GetCurrentTimestampUtc();
        public long BornTimestamp { get; set; } = DateUtilities.GetCurrentTimestampUtc();
        public long DiedTimestamp { get; set; } = 0;
        public long EvolvedTimestamp { get; set; } = 0;
        public long ViewedTimestamp { get; set; } = 0;

        public int Experience { get; set; } = 0;

        public int Age {
            get {

                long ts = DateUtilities.GetCurrentTimestampUtc();

                return (int)(((DiedTimestamp > 0 ? DiedTimestamp : ts) - BornTimestamp) / 60 / 60 / 24);

            }
        }

        public bool IsAlive {
            get {

                return HoursSinceFed() <= (HoursPerDay * Global.GotchiContext.Config.MaxMissedFeedings);

            }
        }
        public bool IsSleeping {
            get {

                return (HoursSinceBirth() % HoursPerDay) >= (HoursPerDay - Global.GotchiContext.Config.SleepHours);

            }
        }
        public bool IsEating {
            get {

                return HoursSinceFed() < 1;

            }
        }
        public bool IsHungry {
            get {

                return HoursSinceFed() > 12;

            }
        }
        public bool IsEvolved {
            get {

                // Returns true if the gotchi has evolved since the last time it was viewed.
                // The gotchi must have been viewed at least once before (to prevent new gotchis from appearing to have evolved).

                return ViewedTimestamp > 0 && ViewedTimestamp < EvolvedTimestamp;

            }
        }

        public bool CanEvolve {
            get {

                return IsAlive && HoursSinceEvolved() >= 7 * 24;

            }
        }

        public GotchiStates State {
            get {

                if (!IsAlive)
                    return GotchiStates.Dead;
                else if (IsSleeping)
                    return GotchiStates.Sleeping;
                else if (IsEvolved)
                    return GotchiStates.Evolved;
                else if (IsHungry)
                    return GotchiStates.Hungry;
                else if (IsEating)
                    return GotchiStates.Eating;
                else if (HoursSinceLastSlept() < 1)
                    return GotchiStates.Energetic;
                else if (HoursUntilSleep() <= 1)
                    return GotchiStates.Tired;

                return GotchiStates.Happy;


            }
        }

        public long HoursOfSleepLeft() {

            if (!IsSleeping)
                return 0;

            return HoursPerDay - (HoursSinceBirth() % HoursPerDay);

        }
        public long HoursSinceLastSlept() {

            if (IsSleeping)
                return 0;

            return HoursSinceBirth() % HoursPerDay;

        }
        public long HoursUntilSleep() {

            if (IsSleeping)
                return 0;

            return (HoursPerDay - Global.GotchiContext.Config.SleepHours) - (HoursSinceBirth() % HoursPerDay);

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

        // Private members

        private string _name;

    }

}