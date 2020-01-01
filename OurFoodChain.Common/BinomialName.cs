using OurFoodChain.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common {

    public enum BinomialNameFormat {
        Full,
        Abbreviated
    }

    public class BinomialName {

        public string GenusName { get; }
        public string SpeciesName { get; }

        public BinomialName(string genusName, string speciesName) {

            GenusName = genusName.ToTitleCase();
            SpeciesName = speciesName.ToLowerInvariant();

        }

        public override string ToString() {

            return ToString(BinomialNameFormat.Full);

        }
        public string ToString(BinomialNameFormat format) {

            switch (format) {

                case BinomialNameFormat.Abbreviated:
                    return string.Format("{0}. {1}", GenusName.First(), SpeciesName);

                default:
                    return string.Format("{0} {1}", GenusName, SpeciesName);

            }

        }

    }

}