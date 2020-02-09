using OurFoodChain.Common;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Taxa {

    public class Species :
        SpeciesBase {

        public override ITaxon Genus { get; set; }
        public override ICreator Creator { get; set; }
        public override DateTimeOffset CreationDate { get; set; } = DateUtilities.GetCurrentUtcDate();
        public override string Description { get; set; }
        public override IConservationStatus Status { get; set; } = new ConservationStatus();
        public override long? Id { get; set; }
        public override string Name { get; set; }
        public override IPicture Picture { get; set; }

    }

}