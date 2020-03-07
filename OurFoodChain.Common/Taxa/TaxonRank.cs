using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using OurFoodChain.Common.Extensions;
using System.Linq;
using OurFoodChain.Common.Utilities;

namespace OurFoodChain.Common.Taxa {

    public class TaxonRank :
        ITaxonRank {

        // Public members

        public TaxonRankType Type { get; }
        public string Name {
            get {

                if (!string.IsNullOrEmpty(_rankName))
                    return _rankName;

                return TaxonUtilities.GetNameForRank(Type);

            }
        }
        public string PluralName {
            get {

                if (!string.IsNullOrEmpty(_rankName))
                    return TaxonUtilities.GetPluralName(_rankName);

                return TaxonUtilities.GetNameForRank(Type, true);

            }
        }
        public TaxonPrefix Prefix {
            get {

                return TaxonUtilities.GetPrefixFromRankName(Name);

            }
        }
        public string Suffix {
            get {

                string name = Name;
                string prefix = Prefix.ToString();

                if (name.StartsWith(prefix))
                    return name.After(prefix);
                else
                    return name;

            }
        }

        public TaxonRank() :
            this(TaxonRankType.None) {
        }
        public TaxonRank(TaxonRankType rankType) {

            Type = rankType;

        }
        public TaxonRank(string rankName) :
           this(TaxonUtilities.GetRankFromRankName(rankName)) {

            if (!string.IsNullOrEmpty(rankName))
                _rankName = rankName.Trim().ToLower();

        }

        public int CompareTo(ITaxonRank other) {

            if (Type != TaxonRankType.Custom && other.Type != TaxonRankType.Custom) {

                // The order of the values in the enum corresponds to their hierarchical order.

                return Type.CompareTo(other.Type);

            }
            else if (Suffix == other.Suffix) {

                // For custom ranks, we need to infer the hierarchical order based on the name of the rank.
                // This requires that both rank suffixes are "known". If they're not, we can't compare them.

                return Prefix.CompareTo(other.Prefix);

            }
            else
                // The comparison could not be performed.
                return 0;

        }

        public override string ToString() {

            return Name;

        }

        // Private members

        private readonly string _rankName;

    }

}