using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Zones {

    public class SpeciesZoneInfo :
        ISpeciesZoneInfo {

        // Public members

        public IZone Zone { get; set; }
        public string Notes {
            get => notes;
            set => notes = value?.Trim().ToLowerInvariant() ?? "";
        }
        public DateTimeOffset? Date { get; set; }

        // Private members

        private string notes = "";

    }

}