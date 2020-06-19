using System;

namespace OurFoodChain.Gotchis {

    public interface IGotchi {

        long? Id { get; set; }
        long? SpeciesId { get; set; }
        long? UserId { get; set; }

        string Name { get; set; }

        DateTimeOffset? FedTimestamp { get; set; }
        DateTimeOffset? BornTimestamp { get; set; }
        DateTimeOffset? EvolvedTimestamp { get; set; }
        DateTimeOffset? ViewedTimestamp { get; set; }

        double Experience { get; set; }

    }

}