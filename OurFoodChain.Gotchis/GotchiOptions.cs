namespace OurFoodChain.Gotchis {

    public class GotchiOptions :
        IGotchiOptions {

        public int SleepHours { get; set; } = 8;
        public int MaxMissedFeedings { get; set; } = 3;
        public int TrainingLimit { get; set; } = 3;
        public int TrainingCooldown { get; set; } = 15;
        public bool ImageWhitelistEnabled { get; set; } = true;

    }

}