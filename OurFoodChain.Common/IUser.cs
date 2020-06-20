using System;

namespace OurFoodChain.Common {

    public interface IUser {

        long? Id { get; set; }
        ulong? UserId { get; set; }
        string Name { get; set; }

        long SpeciesCount { get; set; }
        DateTimeOffset? FirstSpeciesDate { get; set; }
        DateTimeOffset? LastSpeciesDate { get; set; }


    }

}