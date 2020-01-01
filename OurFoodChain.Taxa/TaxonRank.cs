using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using OurFoodChain.Common.Extensions;
using System.Linq;

namespace OurFoodChain.Taxa {

    public class TaxonRank :
        ITaxonRank {

        // Public members

        public TaxonRankType Type { get; }
        public string Name {
            get {

                if (!string.IsNullOrEmpty(_rankName))
                    return _rankName;

                return RankTypeToRankName(Type);

            }
        }
        public string PluralName {
            get {

                if (!string.IsNullOrEmpty(_rankName))
                    return PluralizeRankName(_rankName);

                return PluralizeRankName(RankTypeToRankName(Type));

            }
        }
        public TaxonPrefix Prefix {
            get {

                return GetPrefixFromRankName(Name);

            }
        }
        public string Suffix {
            get {

                string name = Name;
                string prefix = Prefix.ToString();

                if (name.StartsWith(prefix))
                    return name.AfterSubstring(prefix);
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
           this(RankNameToRankType(rankName)) {

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

        private static TaxonRankType RankNameToRankType(string rankName) {

            if (string.IsNullOrEmpty(rankName))
                return TaxonRankType.None;

            TaxonRankType result;

            if (string.IsNullOrEmpty(rankName))
                result = TaxonRankType.None;

            else if (!Enum.TryParse(rankName.Trim().ToLowerInvariant(), out result))
                result = TaxonRankType.Custom;

            return result;

        }
        private static string RankTypeToRankName(TaxonRankType rankType) {

            string result;

            if (rankType == TaxonRankType.None)
                result = string.Empty;

            else if (rankType != TaxonRankType.Custom)
                result = rankType.ToString().ToLowerInvariant();

            else
                throw new ArgumentOutOfRangeException(nameof(rankType));

            return result;

        }
        private static string PluralizeRankName(string rankName) {

            if (string.IsNullOrEmpty(rankName))
                return string.Empty;

            rankName = rankName.Trim().ToLowerInvariant();

            if (rankName.EndsWith("phylum"))
                return rankName.ReplaceLastSubstring("phylum", "phyla");

            else if (rankName.EndsWith("genus"))
                return rankName.ReplaceLastSubstring("genus", "genera");

            else if (rankName.EndsWith("ss"))
                return rankName + "es"; // e.g. class -> classes

            else if (rankName.EndsWith("s"))
                return rankName; // e.g. species -> species

            else if (rankName.EndsWith("y"))
                return rankName.ReplaceLastSubstring("y", "ies"); // e.g. family -> families

            else
                return rankName + "s"; // e.g. domain -> domains

        }
        private static TaxonPrefix GetPrefixFromRankName(string rankName) {

            if (string.IsNullOrEmpty(rankName))
                return TaxonPrefix.None;

            foreach (string prefix in Enum.GetNames(typeof(TaxonPrefix))
                .Select(p => p.ToLowerInvariant())
                .OrderByDescending(p => p.Length)) {

                if (rankName.StartsWith(prefix))
                    return (TaxonPrefix)Enum.Parse(typeof(TaxonPrefix), prefix);

            }

            return TaxonPrefix.None;

        }

    }

}