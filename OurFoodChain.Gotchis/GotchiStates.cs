using System;

namespace OurFoodChain.Gotchis {

    [Flags]
    public enum GotchiStates {
        Happy = 1,
        Hungry = 2,
        Eating = 4,
        Dead = 8,
        Energetic = 16,
        Sleeping = 32,
        Tired = 64,
        Evolved = 128,
        ReadyToEvolve = 256
    }

}