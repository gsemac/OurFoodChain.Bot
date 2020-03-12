using Discord.Commands;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class IdeaModule :
        OfcModuleBase {

        // Public members

        public SQLiteDatabase Db { get; set; }

        [Command("idea")]
        public async Task Idea() {

            List<string> ideas = new List<string>();

            ideas.AddRange(await GetEmptyZoneIdeasAsync());
            ideas.AddRange(await GetSmallGenusIdeasAsync());
            ideas.AddRange(await GetEmptyLineageIdeasAsync());
            ideas.AddRange(await GetNoPredatorIdeasAsync());
            ideas.AddRange(await GetMissingRolesInZoneIdeasAsync());

            if (ideas.Count() > 0)
                await BotUtils.ReplyAsync_Info(Context, string.Format("💡 {0}", ideas[new Random().Next(ideas.Count())]));
            else
                await BotUtils.ReplyAsync_Info(Context, "I don't have any good ideas right now.");

        }

        // Private members

        private async Task<IEnumerable<string>> GetEmptyZoneIdeasAsync() {

            // Checks for empty zones

            List<string> ideas = new List<string>();

            foreach (IZone zone in await Db.GetZonesAsync()) {

                if ((await Db.GetSpeciesAsync(zone)).Count() <= 0)
                    ideas.Add(string.Format("**{0}** does not contain any species yet. Why not make one?", zone.GetFullName()));

            }

            return ideas;

        }
        private async Task<IEnumerable<string>> GetSmallGenusIdeasAsync() {

            // Checks for genera with only one species

            List<string> ideas = new List<string>();

            foreach (ITaxon genus in await Db.GetTaxaAsync(TaxonRankType.Genus)) {

                if ((await Db.GetSubtaxaAsync(genus)).Count() <= 0)
                    ideas.Add(string.Format("Genus **{0}** only has one species in it. Why not make another?", genus.Name.ToTitle()));

            }

            return ideas;

        }
        private async Task<IEnumerable<string>> GetEmptyLineageIdeasAsync() {

            // Checks for species with empty lineage

            List<string> ideas = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id NOT IN (SELECT species_id FROM Ancestors) AND id NOT IN (SELECT ancestor_id FROM Ancestors) AND id NOT IN (SELECT species_id FROM Extinctions)"))
                foreach (DataRow row in await Db.GetRowsAsync(cmd))
                    ideas.Add(string.Format("Species **{0}** does not have any descendants. Why not derive one?", (await SpeciesUtils.SpeciesFromDataRow(row)).ShortName));

            return ideas.ToArray();

        }
        private async Task<IEnumerable<string>> GetNoPredatorIdeasAsync() {

            // Checks for species with no predators (that aren't themselves predators)

            List<string> ideas = new List<string>();

            string query = @"SELECT * FROM Species WHERE 
	            id NOT IN (SELECT species_id FROM Extinctions) AND  
	            id NOT IN (SELECT eats_id FROM Predates) AND 
	            id NOT IN (SELECT species_id FROM Predates)";

            using (SQLiteCommand cmd = new SQLiteCommand(query))
                foreach (DataRow row in await Db.GetRowsAsync(cmd)) {

                    ISpecies species = await SpeciesUtils.SpeciesFromDataRow(row);

                    ideas.Add(string.Format("There are no species that feed on **{0}**. Why not make one?", species.GetShortName()));

                }

            return ideas.ToArray();

        }
        private async Task<IEnumerable<string>> GetMissingRolesInZoneIdeasAsync() {

            // Checks for roles that are unfulfilled for a given zone

            List<string> ideas = new List<string>();

            string query = @"SELECT Zones.id AS zone_id1, Zones.name AS zone_name, Roles.id AS role_id1, Roles.name AS role_name FROM Zones, Roles WHERE
	            NOT EXISTS(SELECT * FROM SpeciesRoles WHERE role_id = role_id1 AND species_id IN (SELECT species_id FROM SpeciesZones WHERE zone_id = zone_id1));";

            using (SQLiteCommand cmd = new SQLiteCommand(query))
                foreach (DataRow row in await Db.GetRowsAsync(cmd)) {

                    string zone_name = row.Field<string>("zone_name");
                    string role_name = row.Field<string>("role_name");

                    ideas.Add(string.Format("**{0}** does not have any **{1}s**. Why not fill this role?",
                        ZoneUtilities.GetFullName(zone_name),
                        StringUtilities.ToTitleCase(role_name)));

                }

            return ideas.ToArray();

        }

    }

}