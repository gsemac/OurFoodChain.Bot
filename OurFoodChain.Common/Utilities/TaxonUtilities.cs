﻿using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public static class TaxonUtilities {

        // Public members

        public static string GetNameForRank(TaxonRankType rank, bool plural = false) {

            string result;

            if (plural) {

                result = GetPluralForRank(rank);

            }
            else {

                if (rank == TaxonRankType.None)
                    result = string.Empty;

                else if (rank != TaxonRankType.Custom)
                    result = rank.ToString().ToLowerInvariant();

                else
                    throw new ArgumentOutOfRangeException(nameof(rank));

            }

            return result;

        }
        public static string GetPluralName(string rankName) {

            if (string.IsNullOrEmpty(rankName))
                return string.Empty;

            rankName = rankName.Trim().ToLowerInvariant();

            if (rankName.EndsWith("phylum"))
                return rankName.ReplaceLast("phylum", "phyla");

            else if (rankName.EndsWith("genus"))
                return rankName.ReplaceLast("genus", "genera");

            else if (rankName.EndsWith("ss"))
                return rankName + "es"; // e.g. class -> classes

            else if (rankName.EndsWith("s"))
                return rankName; // e.g. species -> species

            else if (rankName.EndsWith("y"))
                return rankName.ReplaceLast("y", "ies"); // e.g. family -> families

            else
                return rankName + "s"; // e.g. domain -> domains

        }

        public static TaxonRankType GetRankFromRankName(string rankName) {

            if (string.IsNullOrEmpty(rankName))
                return TaxonRankType.None;

            TaxonRankType result;

            if (string.IsNullOrEmpty(rankName))
                result = TaxonRankType.None;

            else if (!Enum.TryParse(rankName.Trim().ToLowerInvariant(), out result))
                result = TaxonRankType.Custom;

            return result;

        }
        public static TaxonPrefix GetPrefixFromRankName(string rankName) {

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

        public static TaxonRankType GetParentRank(TaxonRankType rank) {

            switch (rank) {

                case TaxonRankType.Species:
                    return TaxonRankType.Genus;
                case TaxonRankType.Genus:
                    return TaxonRankType.Family;
                case TaxonRankType.Family:
                    return TaxonRankType.Order;
                case TaxonRankType.Order:
                    return TaxonRankType.Class;
                case TaxonRankType.Class:
                    return TaxonRankType.Phylum;
                case TaxonRankType.Phylum:
                    return TaxonRankType.Kingdom;
                case TaxonRankType.Kingdom:
                    return TaxonRankType.Domain;
                default:
                    return TaxonRankType.None;

            }

        }
        public static TaxonRankType GetChildRank(TaxonRankType rank) {

            switch (rank) {

                case TaxonRankType.Species:
                    return TaxonRankType.None;
                case TaxonRankType.Genus:
                    return TaxonRankType.Species;
                case TaxonRankType.Family:
                    return TaxonRankType.Genus;
                case TaxonRankType.Order:
                    return TaxonRankType.Family;
                case TaxonRankType.Class:
                    return TaxonRankType.Order;
                case TaxonRankType.Phylum:
                    return TaxonRankType.Class;
                case TaxonRankType.Kingdom:
                    return TaxonRankType.Phylum;
                case TaxonRankType.Domain:
                    return TaxonRankType.Kingdom;
                default:
                    return TaxonRankType.None;

            }

        }

        // Private members

        private static string GetPluralForRank(TaxonRankType rank) {

            return GetPluralName(GetNameForRank(rank));

        }

    }
}