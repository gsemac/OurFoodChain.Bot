using OurFoodChain.Common.Roles;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Extensions {

    public static class SQLiteDatabaseRoleExtensions {

        // Public members

        public static async Task<IEnumerable<IRole>> GetRolesAsync(this SQLiteDatabase database, long? speciesId) {

            // Return all roles assigned to the given species.

            List<IRole> roles = new List<IRole>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Roles WHERE id IN (SELECT role_id FROM SpeciesRoles WHERE species_id=$species_id) ORDER BY name ASC;")) {

                cmd.Parameters.AddWithValue("$species_id", speciesId);

                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    roles.Add(CreateRoleFromDataRow(row));

            }

            // Get role notes.
            // #todo Get the roles and notes using a single query.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM SpeciesRoles WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", speciesId);

                foreach (DataRow row in await database.GetRowsAsync(cmd)) {

                    long roleId = row.Field<long>("role_id");
                    string notes = row.Field<string>("notes");

                    IRole role = roles.Where(r => r.Id == roleId).FirstOrDefault();

                    if (role != null)
                        role.Notes = notes;

                }

            }

            return roles;

        }
        public static async Task<IEnumerable<IRole>> GetRolesAsync(this SQLiteDatabase database, ISpecies species) {

            return await database.GetRolesAsync(species.Id);

        }

        // Private members

        private static IRole CreateRoleFromDataRow(DataRow row) {

            IRole role = new Role {
                Id = row.Field<long>("id"),
                Name = row.Field<string>("name"),
                Description = row.Field<string>("description")
            };

            return role;

        }

    }

}