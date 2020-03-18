using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Exceptions {

    public class CycleException :
        Exception {

        // Public members

        public ISpecies Species { get; }

        public CycleException() {
        }
        public CycleException(string message) :
            base(message) {
        }
        public CycleException(string message, Exception innerException) :
            base(message, innerException) {
        }

        public CycleException(ISpecies species) :
            base(string.Format("This species' evolutionary line contains a cycle ({0} appeared twice).",
                species != null ? (species.IsValid() ? string.Format("**{0}**", species.GetShortName()) : species.Id.ToString()) : "a species")) {

            this.Species = species;

        }

    }

}