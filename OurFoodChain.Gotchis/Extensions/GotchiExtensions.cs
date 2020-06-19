using OurFoodChain.Common.Utilities;
using System;
using System.Diagnostics;

namespace OurFoodChain.Gotchis.Extensions {

    public static class GotchiExtensions {

        // Public members

        public static long GetAge(this IGotchi gotchi) {

            Debug.Assert(gotchi.BornTimestamp.HasValue);

            DateTimeOffset ts = DateUtilities.GetCurrentDateUtc();

            return (long)Math.Floor((ts - gotchi.BornTimestamp.Value).TotalDays);

        }

        // Private members

        public const long HoursPerDay = 24;

        private static bool IsAlive(this IGotchi gotchi, int maxMissedFeedings) {

            return gotchi.HoursSinceFed() <= (HoursPerDay * maxMissedFeedings);

        }
        private static bool IsSleeping(this IGotchi gotchi, int sleepHours) {

            return (gotchi.HoursSinceBirth() % HoursPerDay) >= (HoursPerDay - sleepHours);

        }
        private static bool IsEating(this IGotchi gotchi) {

            return gotchi.HoursSinceFed() < 1;

        }
        private static bool IsHungry(this IGotchi gotchi) {

            return gotchi.HoursSinceFed() > 12;

        }
        private static bool IsEvolved(this IGotchi gotchi) {

            // Returns true if the gotchi has evolved since the last time it was viewed.
            // The gotchi must have been viewed at least once before (to prevent new gotchis from appearing to have evolved).

            return gotchi.ViewedTimestamp.HasValue && gotchi.ViewedTimestamp < gotchi.EvolvedTimestamp;

        }
        private static bool CanEvolve(this IGotchi gotchi, int maxMissedFeedings) {

            return gotchi.IsAlive(maxMissedFeedings) && gotchi.HoursSinceEvolved() >= 7 * 24;

        }

        private static long HoursOfSleepLeft(this IGotchi gotchi, int sleepHours) {

            if (!gotchi.IsSleeping(sleepHours))
                return 0;

            return HoursPerDay - (gotchi.HoursSinceBirth() % HoursPerDay);

        }
        private static long HoursSinceLastSlept(this IGotchi gotchi, int sleepHours) {

            if (gotchi.IsSleeping(sleepHours))
                return 0;

            return gotchi.HoursSinceBirth() % HoursPerDay;

        }
        private static long HoursUntilSleep(this IGotchi gotchi, int sleepHours) {

            if (gotchi.IsSleeping(sleepHours))
                return 0;

            return HoursPerDay - sleepHours - (gotchi.HoursSinceBirth() % HoursPerDay);

        }
        private static long HoursSinceBirth(this IGotchi gotchi) {

            if (!gotchi.BornTimestamp.HasValue)
                return 0;

            DateTimeOffset currentTimestamp = DateUtilities.GetCurrentDateUtc();

            TimeSpan timeSinceBirth = currentTimestamp - gotchi.BornTimestamp.Value;
            long hoursSinceBirth = (long)Math.Floor(timeSinceBirth.TotalHours);

            return hoursSinceBirth;

        }
        private static long HoursSinceFed(this IGotchi gotchi) {

            if (!gotchi.FedTimestamp.HasValue)
                return gotchi.HoursSinceBirth();

            DateTimeOffset currentTimestamp = DateUtilities.GetCurrentDateUtc();

            TimeSpan timeSinceFed = currentTimestamp - gotchi.FedTimestamp.Value;
            long hoursSinceFed = (long)Math.Floor(timeSinceFed.TotalHours);

            return hoursSinceFed;

        }
        private static long HoursSinceEvolved(this IGotchi gotchi) {

            if (!gotchi.EvolvedTimestamp.HasValue)
                return gotchi.HoursSinceBirth();

            DateTimeOffset currentTimestamp = DateUtilities.GetCurrentDateUtc();

            TimeSpan timeSinceEvolved = currentTimestamp - gotchi.EvolvedTimestamp.Value;
            long hoursSinceEvolved = (long)Math.Floor(timeSinceEvolved.TotalHours);

            return hoursSinceEvolved;

        }

    }

}