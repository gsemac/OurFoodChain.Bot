using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Generations {

    public interface IGeneration {

        long? Id { get; set; }

        string Name { get; }
        int Number { get; set; }

        DateTimeOffset StartDate { get; set; }
        DateTimeOffset EndDate { get; set; }

    }

}