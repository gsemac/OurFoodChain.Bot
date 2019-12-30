using OurFoodChain.Utilities;
using System;
using System.Data;

namespace OurFoodChain {

    public enum TaxonRank {
        Species = 1,
        Genus,
        Family,
        Order,
        Class,
        Phylum,
        Kingdom,
        Domain,
        Any
    }

    class TaxonSet {

        public Taxon Domain;
        public Taxon Kingdom;
        public Taxon Phylum;
        public Taxon Class;
        public Taxon Order;
        public Taxon Family;
        public Taxon Genus;
        public Taxon Species = null;

        public bool Contains(string name) {

            return Contains(name, TaxonRank.Domain) ||
                Contains(name, TaxonRank.Kingdom) ||
                Contains(name, TaxonRank.Phylum) ||
                Contains(name, TaxonRank.Class) ||
                Contains(name, TaxonRank.Order) ||
                Contains(name, TaxonRank.Family) ||
                Contains(name, TaxonRank.Genus) ||
                Contains(name, TaxonRank.Species);

        }
        public bool Contains(string name, TaxonRank type) {

            if (string.IsNullOrEmpty(name))
                return false;

            name = name.Trim().ToLower();

            switch (type) {

                case TaxonRank.Species:
                    return Species != null && Species.name.ToLower() == name;
                case TaxonRank.Genus:
                    return Genus != null && Genus.name.ToLower() == name;
                case TaxonRank.Family:
                    return Family != null && Family.name.ToLower() == name;
                case TaxonRank.Order:
                    return Order != null && Order.name.ToLower() == name;
                case TaxonRank.Class:
                    return Class != null && Class.name.ToLower() == name;
                case TaxonRank.Phylum:
                    return Phylum != null && Phylum.name.ToLower() == name;
                case TaxonRank.Kingdom:
                    return Kingdom != null && Kingdom.name.ToLower() == name;
                case TaxonRank.Domain:
                    return Domain != null && Domain.name.ToLower() == name;

            }

            return false;

        }

    }

    public class Taxon {

        public const long NullId = -1;

        public Taxon(TaxonRank type) {

            id = -1;
            parent_id = -1;
            this.type = type;

        }

        public long id = NullId;
        public long parent_id = NullId; // For genera, this is the family_id, etc.
        public string name = "";
        public string CommonName {
            get {

                if (string.IsNullOrEmpty(_common_name))
                    return "";

                return StringUtilities.ToTitleCase(_common_name);

            }
            set {
                _common_name = value;
            }
        }
        public string description = "";
        public string pics = "";
        public TaxonRank type = 0;

        public string GetName() {

            return StringUtilities.ToTitleCase(name);

        }
        public string GetCommonName() {
            return CommonName;
        }
        public string GetTypeName(bool plural = false) {
            return GetRankName(type);
        }
        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return BotUtils.DEFAULT_DESCRIPTION;

            return description;

        }
        public TaxonRank GetParentType() {
            return TypeToParentType(type);
        }
        public TaxonRank GetChildRank() {
            return TypeToChildType(type);
        }

        public static Taxon FromDataRow(DataRow row, TaxonRank type) {

            Taxon taxon = new Taxon(type) {
                id = row.Field<long>("id"),
                name = row.Field<string>("name"),
                CommonName = row.Field<string>("common_name"),
                description = row.Field<string>("description"),
                pics = row.Field<string>("pics")
            };

            string parent_id_name = TypeToDatabaseColumnName(taxon.GetParentType());

            if (!string.IsNullOrEmpty(parent_id_name))
                taxon.parent_id = (row[parent_id_name] == DBNull.Value) ? 0 : row.Field<long>(parent_id_name);
            else
                taxon.parent_id = -1;

            return taxon;

        }
        public static string GetRankName(TaxonRank type, bool plural = false) {

            switch (type) {
                case TaxonRank.Species:
                    return plural ? "species" : "species";
                case TaxonRank.Genus:
                    return plural ? "genera" : "genus";
                case TaxonRank.Family:
                    return plural ? "families" : "family";
                case TaxonRank.Order:
                    return plural ? "orders" : "order";
                case TaxonRank.Class:
                    return plural ? "classes" : "class";
                case TaxonRank.Phylum:
                    return plural ? "phyla" : "phylum";
                case TaxonRank.Kingdom:
                    return plural ? "kingdoms" : "kingdom";
                case TaxonRank.Domain:
                    return plural ? "domains" : "domain";
                default:
                    return string.Empty;
            }

        }
        public static TaxonRank TypeToParentType(TaxonRank type) {

            switch (type) {
                case TaxonRank.Species:
                    return TaxonRank.Genus;
                case TaxonRank.Genus:
                    return TaxonRank.Family;
                case TaxonRank.Family:
                    return TaxonRank.Order;
                case TaxonRank.Order:
                    return TaxonRank.Class;
                case TaxonRank.Class:
                    return TaxonRank.Phylum;
                case TaxonRank.Phylum:
                    return TaxonRank.Kingdom;
                case TaxonRank.Kingdom:
                    return TaxonRank.Domain;
                default:
                    return 0;
            }

        }
        public static TaxonRank TypeToChildType(TaxonRank type) {

            switch (type) {
                case TaxonRank.Species:
                    return 0;
                case TaxonRank.Genus:
                    return TaxonRank.Species;
                case TaxonRank.Family:
                    return TaxonRank.Genus;
                case TaxonRank.Order:
                    return TaxonRank.Family;
                case TaxonRank.Class:
                    return TaxonRank.Order;
                case TaxonRank.Phylum:
                    return TaxonRank.Class;
                case TaxonRank.Kingdom:
                    return TaxonRank.Phylum;
                case TaxonRank.Domain:
                    return TaxonRank.Kingdom;
                default:
                    return 0;
            }

        }
        public static string TypeToDatabaseTableName(TaxonRank type) {

            string table_name = StringUtilities.ToTitleCase(GetRankName(type));

            if (table_name == "Order")
                table_name = "Ord";

            return table_name;

        } // deprecated
        public static string TypeToDatabaseColumnName(TaxonRank type) {

            if (type <= 0)
                return string.Empty;

            return string.Format("{0}_id", GetRankName(type));

        } // deprecated

        private string _common_name = "";

    }

}