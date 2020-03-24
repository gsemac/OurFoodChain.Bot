using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public abstract class TaxonFormatterBase :
        ITaxonFormatter {

        // Public members

        public ExtinctNameFormat ExtinctNameFormat { get; set; } = ExtinctNameFormat.Default;

        public virtual string GetString(ISpecies species) {

            return GetString((ITaxon)species, species.IsExtinct());

        }
        public abstract string GetString(ITaxon taxon);

        public virtual string GetString(ISpecies species, bool isExtinct) {

            return GetString((ITaxon)species, isExtinct);

        }
        public virtual string GetString(ITaxon taxon, bool isExtinct) {

            string name = GetString(taxon);

            if (isExtinct)
                name = GetExtinctString(name);

            return name;

        }

        // Protected members

        protected string GetExtinctString(string name) {

            switch (ExtinctNameFormat) {

                default:
                case ExtinctNameFormat.Strikethrough:
                    return string.Format("~~{0}~~", name);

                case ExtinctNameFormat.Dagger:
                    return string.Format("†{0}", name);

            }

        }

    }

}