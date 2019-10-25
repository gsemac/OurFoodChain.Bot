using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class IdeasCommands :
        ModuleBase {

        [Command("idea")]
        public async Task Idea() {

            List<string> ideas = new List<string>();

            ideas.AddRange(await _getEmptyZoneIdeasAsync());
            ideas.AddRange(await _getSmallGeneraIdeasAsync());
            ideas.AddRange(await _getEmptyLineageIdeasAsync());
            ideas.AddRange(await _getNoPredatorIdeasAsync());
            ideas.AddRange(await _getMissingRolesInZoneIdeasAsync());

            if (ideas.Count() > 0)
                await BotUtils.ReplyAsync_Info(Context, string.Format("💡 {0}", ideas[new Random().Next(ideas.Count())]));
            else
                await BotUtils.ReplyAsync_Info(Context, "I don't have any good ideas right now.");

        }

        // Checks for empty zones
        private async Task<string[]> _getEmptyZoneIdeasAsync() {

            List<string> ideas = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones WHERE id NOT IN (SELECT zone_id FROM SpeciesZones);"))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows)
                    ideas.Add(string.Format("**{0}** does not contain any species yet. Why not make one?", ZoneUtils.ZoneFromDataRow(row).GetFullName()));

            return ideas.ToArray();

        }
        // Checks for genera with only one species
        private async Task<string[]> _getSmallGeneraIdeasAsync() {

            List<string> ideas = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Genus WHERE id IN (SELECT genus_id FROM(SELECT genus_id, COUNT(genus_id) AS c FROM (SELECT * FROM Species WHERE id NOT IN (SELECT species_id FROM Extinctions)) GROUP BY genus_id) WHERE c <= 1);"))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows)
                    ideas.Add(string.Format("Genus **{0}** only has one species in it. Why not make another?", Taxon.FromDataRow(row, TaxonRank.Genus).GetName()));

            return ideas.ToArray();

        }
        // Checks for species with empty lineage
        private async Task<string[]> _getEmptyLineageIdeasAsync() {

            List<string> ideas = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id NOT IN (SELECT species_id FROM Ancestors) AND id NOT IN (SELECT ancestor_id FROM Ancestors) AND id NOT IN (SELECT species_id FROM Extinctions);"))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows)
                    ideas.Add(string.Format("Species **{0}** does not have any descendants. Why not derive one?", (await Species.FromDataRow(row)).GetShortName()));

            return ideas.ToArray();

        }
        // Checks for species with no predators (that aren't themselves predators)
        private async Task<string[]> _getNoPredatorIdeasAsync() {

            List<string> ideas = new List<string>();

            string query = @"SELECT * FROM Species WHERE 
	            id NOT IN (SELECT species_id FROM Extinctions) AND  
	            id NOT IN (SELECT eats_id FROM Predates) AND 
	            id NOT IN (SELECT species_id FROM Predates)";

            using (SQLiteCommand cmd = new SQLiteCommand(query))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows) {

                    Species species = await Species.FromDataRow(row);

                    ideas.Add(string.Format("There are no species that feed on **{0}**. Why not make one?", species.GetShortName()));

                }

            return ideas.ToArray();

        }
        // Checks for roles that are unfulfilled for a given zone
        private async Task<string[]> _getMissingRolesInZoneIdeasAsync() {

            List<string> ideas = new List<string>();

            string query = @"SELECT Zones.id AS zone_id1, Zones.name AS zone_name, Roles.id AS role_id1, Roles.name AS role_name FROM Zones, Roles WHERE
	            NOT EXISTS(SELECT * FROM SpeciesRoles WHERE role_id = role_id1 AND species_id IN (SELECT species_id FROM SpeciesZones WHERE zone_id = zone_id1));";

            using (SQLiteCommand cmd = new SQLiteCommand(query))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows) {

                    string zone_name = row.Field<string>("zone_name");
                    string role_name = row.Field<string>("role_name");

                    ideas.Add(string.Format("**{0}** does not have any **{1}s**. Why not fill this role?",
                        ZoneUtils.FormatZoneName(zone_name),
                        StringUtils.ToTitleCase(role_name)));

                }

            return ideas.ToArray();

        }

    }

}
