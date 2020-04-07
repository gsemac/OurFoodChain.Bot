using OurFoodChain.Common;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Gotchis {

    public interface IGotchiNameGenerator {

        string GetName(ICreator owner, ISpecies species);

    }

}