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

            ideas.AddRange(await GetEmptyZoneIdeasAsync());
            ideas.AddRange(await GetSmallGeneraIdeasAsync());
            ideas.AddRange(await GetEmptyLineageIdeasAsync());

            if (ideas.Count() > 0)
                await BotUtils.ReplyAsync_Info(Context, string.Format("💡 {0}", ideas[new Random().Next(ideas.Count())]));
            else
                await BotUtils.ReplyAsync_Info(Context, "I don't have any good ideas right now.");

        }

        // Checks for empty zones
        private async Task<string[]> GetEmptyZoneIdeasAsync() {

            List<string> ideas = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones WHERE id NOT IN (SELECT zone_id FROM SpeciesZones);"))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows)
                    ideas.Add(string.Format("**{0}** does not contain any species yet. Why not make one?", Zone.FromDataRow(row).GetFullName()));

            return ideas.ToArray();

        }
        // Checks for genera with only one species
        private async Task<string[]> GetSmallGeneraIdeasAsync() {

            List<string> ideas = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Genus WHERE id IN (SELECT genus_id FROM(SELECT genus_id, COUNT(genus_id) AS c FROM (SELECT * FROM Species WHERE id NOT IN (SELECT species_id FROM Extinctions)) GROUP BY genus_id) WHERE c <= 1);"))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows)
                    ideas.Add(string.Format("Genus **{0}** only has one species in it. Why not make another?", Taxon.FromDataRow(row, TaxonType.Genus).GetName()));

            return ideas.ToArray();

        }
        // Checks for species with empty lineage
        private async Task<string[]> GetEmptyLineageIdeasAsync() {

            List<string> ideas = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id NOT IN (SELECT species_id FROM Ancestors) AND id NOT IN (SELECT ancestor_id FROM Ancestors) AND id NOT IN (SELECT species_id FROM Extinctions);"))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows)
                    ideas.Add(string.Format("Species **{0}** does not have any descendants. Why not derive one?", (await Species.FromDataRow(row)).GetShortName()));

            return ideas.ToArray();

        }

    }

}
