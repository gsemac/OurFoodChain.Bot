using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Wiki.Utilities {

    public static class WikiUtilities {

        public static string GetWikiPageTitle(ISpecies species) {

            // This is the same process as used in SpeciesPageBuilder.BuildTitleAsync.
            // #todo Instead of being copy-pasted, this process should be in its own function used by both classes.

            string title;

            if (!string.IsNullOrWhiteSpace(species.GetCommonName()))
                title = species.GetCommonName();
            else
                title = species.GetFullName();

            title = title.SafeTrim();

            return title;

        }

    }

}
