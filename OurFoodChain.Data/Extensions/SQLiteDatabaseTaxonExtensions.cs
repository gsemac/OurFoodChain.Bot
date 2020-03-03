using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Extensions {

    public static class SQLiteDatabaseTaxonExtensions {

        // Public members

        public static async Task<ITaxon> GetTaxonAsync(this SQLiteDatabase database, long id, TaxonRankType rank) {

            string tableName = GetTableNameForRank(rank);

            if (string.IsNullOrEmpty(tableName))
                return null;

            ITaxon taxon = null;

            using (SQLiteCommand cmd = new SQLiteCommand(string.Format("SELECT * FROM {0} WHERE id = $id", tableName))) {

                cmd.Parameters.AddWithValue("$id", id);

                DataRow row = await database.GetRowAsync(cmd);

                if (row != null)
                    taxon = CreateTaxonFromDataRow(row, rank);

            }

            return taxon;

        }

        // Private members

        private static string GetTableNameForRank(TaxonRankType rank) {

            string tableName = TaxonUtilities.GetNameFromRank(rank).ToTitle();

            if (tableName.Equals("Order"))
                tableName = "Ord";

            return tableName;

        }
        private static string GetFieldNameForRank(TaxonRankType rank) {

            if (rank <= 0)
                return string.Empty;

            return string.Format("{0}_id", TaxonUtilities.GetNameFromRank(rank).ToLowerInvariant());

        }
        private static ITaxon CreateTaxonFromDataRow(DataRow row, TaxonRankType rank) {

            ITaxon taxon = new Taxon(row.Field<string>("name"), rank) {
                Id = row.Field<long>("id"),
                Description = row.Field<string>("description")
            };

            if (!row.IsNull("common_name") && !string.IsNullOrWhiteSpace(row.Field<string>("common_name")))
                taxon.CommonNames.Add(row.Field<string>("common_name"));

            if (!row.IsNull("pics") && !string.IsNullOrWhiteSpace(row.Field<string>("pics")))
                taxon.Pictures.Add(new Picture(row.Field<string>("pics")));

            string parentIdFieldName = GetFieldNameForRank(TaxonUtilities.GetParentRank(rank));

            if (!string.IsNullOrEmpty(parentIdFieldName) && !row.IsNull(parentIdFieldName))
                taxon.ParentId = row.Field<long>(parentIdFieldName);

            return taxon;

        }

    }

}