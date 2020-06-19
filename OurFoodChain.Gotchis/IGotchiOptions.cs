namespace OurFoodChain.Gotchis {

    public interface IGotchiOptions {

        int SleepHours { get; set; }
        int MaxMissedFeedings { get; set; }
        int TrainingLimit { get; set; }
        int TrainingCooldown { get; set; }
        bool ImageWhitelistEnabled { get; set; }

    }

}