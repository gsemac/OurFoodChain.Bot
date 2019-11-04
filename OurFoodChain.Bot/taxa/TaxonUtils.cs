using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace OurFoodChain {

    public static class TaxonUtils {

        public static async Task<Taxon[]> GetTaxaAsync(string name) {

            // Return all taxa that have the given name.

            List<Taxon> result = new List<Taxon>();

            foreach (TaxonRank type in new TaxonRank[] { TaxonRank.Domain, TaxonRank.Kingdom, TaxonRank.Phylum, TaxonRank.Class, TaxonRank.Order, TaxonRank.Family, TaxonRank.Genus, TaxonRank.Species })
                result.AddRange(await GetTaxaAsync(name, type));

            return result.ToArray();

        }
        public static async Task<Taxon[]> GetTaxaAsync(string name, TaxonRank rank) {

            List<Taxon> taxa = new List<Taxon>();
            string table_name = _getRankTableName(rank);

            if (string.IsNullOrEmpty(table_name))
                return null;

            using (SQLiteCommand cmd = new SQLiteCommand(string.Format("SELECT * FROM {0} WHERE name = $name OR common_name = $name", table_name))) {

                cmd.Parameters.AddWithValue("$name", name.ToLower());

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows)
                        taxa.Add(Taxon.FromDataRow(row, rank));

            }

            return taxa.ToArray();

        }
        public static async Task<Species[]> GetSpeciesAsync(Taxon taxon) {

            List<Species> species = new List<Species>();

            if (taxon.type == TaxonRank.Species) {

                // Return all species with the same name as the taxon.
                species.AddRange(await SpeciesUtils.GetSpeciesAsync("", taxon.name));

            }
            else if (taxon.type == TaxonRank.Genus) {

                // Return all species within this genus (rather than recursively calling this function for each species).

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE genus_id = $genus_id")) {

                    cmd.Parameters.AddWithValue("$genus_id", taxon.id);

                    using (DataTable table = await Database.GetRowsAsync(cmd))
                        foreach (DataRow row in table.Rows)
                            species.Add(await SpeciesUtils.SpeciesFromDataRow(row));

                }

            }
            else {

                // Get all subtaxa and call this function recursively to get the species from each of them.

                Taxon[] subtaxa = await GetSubtaxaAsync(taxon);

                foreach (Taxon t in subtaxa)
                    species.AddRange(await GetSpeciesAsync(t));

            }

            return species.ToArray();

        }
        public static async Task<Taxon[]> GetSubtaxaAsync(Taxon taxon) {

            List<Taxon> result = new List<Taxon>();

            string table_name = Taxon.TypeToDatabaseTableName(taxon.GetChildRank());
            string parent_column_name = Taxon.TypeToDatabaseColumnName(taxon.type);

            if (string.IsNullOrEmpty(table_name))
                return result.ToArray();

            string query = "SELECT * FROM {0} WHERE {1} = $parent_id";

            using (SQLiteCommand cmd = new SQLiteCommand(string.Format(query, table_name, parent_column_name))) {

                cmd.Parameters.AddWithValue("$parent_id", taxon.id);

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        result.Add(Taxon.FromDataRow(row, taxon.GetChildRank()));

            }

            // Sort taxa alphabetically by name.
            result.Sort((lhs, rhs) => lhs.name.CompareTo(rhs.name));

            return result.ToArray();


        }

        public static async Task DeleteTaxonAsync(Taxon taxon) {

            string table_name = _getRankTableName(taxon.type);
            string subtaxa_table_name = _getRankTableName(taxon.GetChildRank());
            string subtaxa_column_name = _getRankColumnName(taxon.type);

            // Set to NULL any references subtaxa have to this taxon.
            // Note that this can also happen automatically if the foreign key is set up correctly when creating the database.

            if (!string.IsNullOrEmpty(table_name) && !string.IsNullOrEmpty(subtaxa_column_name)) {

                using (SQLiteCommand cmd = new SQLiteCommand(string.Format("UPDATE {0} SET {1} = NULL WHERE {1} = $id", subtaxa_table_name, subtaxa_column_name))) {

                    cmd.Parameters.AddWithValue("$id", taxon.id);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

            // Delete the taxon.

            if (!string.IsNullOrEmpty(table_name)) {

                using (SQLiteCommand cmd = new SQLiteCommand(string.Format("DELETE FROM {0} WHERE id = $id", table_name))) {

                    cmd.Parameters.AddWithValue("$id", taxon.id);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

        }

        private static string _getRankTableName(TaxonRank rank) {

            string table_name = StringUtils.ToTitleCase(Taxon.GetRankName(rank));

            if (table_name == "Order")
                table_name = "Ord";

            return table_name;

        }
        private static string _getRankColumnName(TaxonRank rank) {

            if (rank <= 0)
                return string.Empty;

            return string.Format("{0}_id", Taxon.GetRankName(rank));

        }

    }

}