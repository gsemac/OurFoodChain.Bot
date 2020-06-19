using OurFoodChain.Common.Utilities;
using System;

namespace OurFoodChain.Gotchis {

    public class Gotchi :
        IGotchi {

        public long? Id { get; set; }
        public long? SpeciesId { get; set; }
        public long? UserId { get; set; }

        public string Name { get; set; }

        public DateTimeOffset? FedTimestamp { get; set; } = DateUtilities.GetCurrentDateUtc();
        public DateTimeOffset? BornTimestamp { get; set; } = DateUtilities.GetCurrentDateUtc();
        public DateTimeOffset? EvolvedTimestamp { get; set; }
        public DateTimeOffset? ViewedTimestamp { get; set; }

        public double Experience { get; set; } = 0.0;

    }

}