using Discord.Commands;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class IdeaModule :
        OfcModuleBase {

        // Public members

        [Command("idea")]
        public async Task Idea() {

            List<string> ideas = new List<string>();

            ideas.AddRange(await GetEmptyZoneIdeasAsync());
            ideas.AddRange(await GetSmallGenusIdeasAsync());
            ideas.AddRange(await GetEmptyLineageIdeasAsync());
            ideas.AddRange(await GetNoPredatorIdeasAsync());
            ideas.AddRange(await GetMissingRolesInZoneIdeasAsync());

            if (ideas.Count() > 0)
                await ReplyInfoAsync(string.Format("💡 {0}", ideas[new Random().Next(ideas.Count())]));
            else
                await ReplyInfoAsync("I don't have any good ideas right now.");

        }

        // Private members

        private async Task<IEnumerable<string>> GetEmptyZoneIdeasAsync() {

            // Checks for empty zones

            List<string> ideas = new List<string>();

            IEnumerable<IZone> zones = (await Db.GetZonesAsync())
                .Where(zone => !zone.Flags.HasFlag(ZoneFlags.Retired));

            foreach (IZone zone in zones) {

                if ((await Db.GetSpeciesAsync(zone, GetSpeciesOptions.Fast)).Count() <= 0)
                    ideas.Add($"{zone.GetFullName().ToBold()} does not contain any species yet. Why not make one?");

            }

            return ideas;

        }
        private async Task<IEnumerable<string>> GetSmallGenusIdeasAsync() {

            // Checks for genera with only one species

            List<string> ideas = new List<string>();

            foreach (ITaxon genus in await Db.GetTaxaAsync(TaxonRankType.Genus)) {

                if ((await Db.GetSubtaxaAsync(genus)).Count() <= 0)
                    ideas.Add($"Genus {genus.Name.ToTitle().ToBold()} only has one species in it. Why not make another?");

            }

            return ideas;

        }
        private async Task<IEnumerable<string>> GetEmptyLineageIdeasAsync() {

            // Checks for species with empty lineage

            List<string> ideas = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id NOT IN (SELECT species_id FROM Ancestors) AND id NOT IN (SELECT ancestor_id FROM Ancestors) AND id NOT IN (SELECT species_id FROM Extinctions)"))
                foreach (DataRow row in await Db.GetRowsAsync(cmd))
                    ideas.Add(string.Format("Species **{0}** does not have any descendants. Why not derive one?", TaxonFormatter.GetString(await Db.CreateSpeciesFromDataRowAsync(row))));

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

                    ISpecies species = await Db.CreateSpeciesFromDataRowAsync(row);

                    ideas.Add($"There are no species that feed on {TaxonFormatter.GetString(species).ToBold()}. Why not make one?");

                }

            return ideas.ToArray();

        }
        private async Task<IEnumerable<string>> GetMissingRolesInZoneIdeasAsync() {

            // Checks for roles that are unfulfilled for a given zone

            List<string> ideas = new List<string>();

            string query = @"SELECT Zones.id AS zone_id1, Zones.name AS zone_name, Zones.flags AS zone_flags, Roles.id AS role_id1, Roles.name AS role_name FROM Zones, Roles WHERE
	            NOT EXISTS(SELECT * FROM SpeciesRoles WHERE role_id = role_id1 AND species_id IN (SELECT species_id FROM SpeciesZones WHERE zone_id = zone_id1));";

            using (SQLiteCommand cmd = new SQLiteCommand(query)) {

                foreach (DataRow row in await Db.GetRowsAsync(cmd)) {

                    ZoneFlags zoneFlags = ZoneFlags.None;

                    if (!row.IsNull("zone_flags"))
                        zoneFlags = (ZoneFlags)row.Field<long>("zone_flags");

                    if (!zoneFlags.HasFlag(ZoneFlags.Retired)) {

                        string zoneName = row.Field<string>("zone_name");
                        string roleName = row.Field<string>("role_name");

                        ideas.Add($"{ZoneUtilities.GetFullName(zoneName).ToBold()} does not have any {roleName.ToTitle().ToBold()}s. Why not fill this role?");

                    }

                }

            }

            return ideas.ToArray();

        }

    }

}