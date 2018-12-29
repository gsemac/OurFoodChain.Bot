using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    enum TaxonType {
        Genus = 1,
        Family,
        Order,
        Class,
        Phylum,
        Kingdom,
        Domain
    }

    class Taxon {

        public long id;
        public long parent_id; // For genera, this is the family_id, etc.
        public string name;
        public string description;
        public string pics;

        public string GetName() {

            return StringUtils.ToTitleCase(name);

        }
        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return BotUtils.DEFAULT_DESCRIPTION;

            return description;

        }

        public static Taxon FromDataRow(DataRow row, TaxonType type) {

            Taxon result = new Taxon {
                id = row.Field<long>("id"),
                name = row.Field<string>("name"),
                description = row.Field<string>("description"),
                pics = row.Field<string>("pics")
            };

            string parent_id_name = _getParentIdColumnNameFromTaxonType(type);

            if (!string.IsNullOrEmpty(parent_id_name))
                result.parent_id = (row[parent_id_name] == DBNull.Value) ? 0 : row.Field<long>(parent_id_name);
            else
                result.parent_id = -1;

            return result;

        }
        public static string TypeToName(TaxonType type, bool plural = false) {

            switch (type) {
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

        private static string _getParentIdColumnNameFromTaxonType(TaxonType type) {

            if (type == TaxonType.Domain)
                return string.Empty;

            return string.Format("{0}_id", TypeToName(TypeToParentType(type)));

        }

    }

}