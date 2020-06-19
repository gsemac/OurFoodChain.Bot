using OurFoodChain.Common;
using OurFoodChain.Common.Taxa;

namespace OurFoodChain.Gotchis {

    public interface IGotchiNameGenerator {

        string GetName(IUser owner, ISpecies species);

    }

}