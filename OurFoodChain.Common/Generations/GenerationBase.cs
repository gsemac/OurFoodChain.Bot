using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Generations {

    public abstract class GenerationBase :
        IGeneration {

        public long? Id { get; set; }

        public string Name => string.Format("Gen {0}", Number);
        public int Number { get; set; } = 0;

        public DateTimeOffset StartDate { get; set; } = DateTimeOffset.MinValue;
        public DateTimeOffset EndDate { get; set; } = DateTimeOffset.MinValue;

    }

}