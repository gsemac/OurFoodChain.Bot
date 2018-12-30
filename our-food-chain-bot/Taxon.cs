using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    enum TaxonType {
        Species = 1,
        Genus,
        Family,
        Order,
        Class,
        Phylum,
        Kingdom,
        Domain
    }

    class Taxon {

        public Taxon(TaxonType type) {

            id = -1;
            parent_id = -1;
            this.type = type;

        }

        public long id;
        public long parent_id; // For genera, this is the family_id, etc.
        public string name;
        public string description;
        public string pics;
        public TaxonType type;

        public string GetName() {

            return StringUtils.ToTitleCase(name);

        }
        public string GetTypeName(bool plural = false) {
            return TypeToName(type);
        }
        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return BotUtils.DEFAULT_DESCRIPTION;

            return description;

        }
        public TaxonType GetParentType() {
            return TypeToParentType(type);
        }
        public TaxonType GetChildType() {
            return TypeToChildType(type);
        }

        public static Taxon FromDataRow(DataRow row, TaxonType type) {

            Taxon taxon = new Taxon(type) {
                id = row.Field<long>("id"),
                name = row.Field<string>("name"),
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
        public static string TypeToName(TaxonType type, bool plural = false) {

            switch (type) {
                case TaxonType.Species:
                    return plural ? "species" : "species";
                case TaxonType.Genus:
                    return plural ? "genera" : "genus";
                case TaxonType.Family:
                    return plural ? "families" : "family";
                case TaxonType.Order:
                    return plural ? "orders" : "order";
                case TaxonType.Class:
                    return plural ? "classes" : "class";
                case TaxonType.Phylum:
                    return plural ? "phyla" : "phylum";
                case TaxonType.Kingdom:
                    return plural ? "kingdoms" : "kingdom";
                case TaxonType.Domain:
                    return plural ? "domains" : "domain";
                default:
                    return string.Empty;
            }

        }
        public static TaxonType TypeToParentType(TaxonType type) {

            switch (type) {
                case TaxonType.Species:
                    return TaxonType.Genus;
                case TaxonType.Genus:
                    return TaxonType.Family;
                case TaxonType.Family:
                    return TaxonType.Order;
                case TaxonType.Order:
                    return TaxonType.Class;
                case TaxonType.Class:
                    return TaxonType.Phylum;
                case TaxonType.Phylum:
                    return TaxonType.Kingdom;
                case TaxonType.Kingdom:
                    return TaxonType.Domain;
                default:
                    return 0;
            }

        }
        public static TaxonType TypeToChildType(TaxonType type) {

            switch (type) {
                case TaxonType.Species:
                    return 0;
                case TaxonType.Genus:
                    return TaxonType.Species;
                case TaxonType.Family:
                    return TaxonType.Genus;
                case TaxonType.Order:
                    return TaxonType.Family;
                case TaxonType.Class:
                    return TaxonType.Order;
                case TaxonType.Phylum:
                    return TaxonType.Class;
                case TaxonType.Kingdom:
                    return TaxonType.Phylum;
                case TaxonType.Domain:
                    return TaxonType.Kingdom;
                default:
                    return 0;
            }

        }
        public static string TypeToDatabaseTableName(TaxonType type) {

            string table_name = StringUtils.ToTitleCase(TypeToName(type));

            if (table_name == "Order")
                table_name = "Ord";

            return table_name;

        }
        public static string TypeToDatabaseColumnName(TaxonType type) {

            if (type == TaxonType.Domain)
                return string.Empty;

            return string.Format("{0}_id", TypeToName(type));

        }

    }

}