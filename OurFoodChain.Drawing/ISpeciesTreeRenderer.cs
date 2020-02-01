using OurFoodChain.Common.Collections;
using OurFoodChain.Taxa;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OurFoodChain.Drawing {

    public interface ISpeciesTreeRenderer :
        ITreeRenderer<ISpecies> {

        ISpecies HighlightedSpecies { get; set; }

    }

}