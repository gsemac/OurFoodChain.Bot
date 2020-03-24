using Discord;
using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class RelationshipModule :
        OfcModuleBase {

        [Command("relationships"), Alias("relations")]
        public async Task Relationships() {

            // Get all relationships from the database.

            Relationship[] relationships = await GetRelationshipsFromDbAsync();

            // Build the embed.

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithTitle(string.Format("All relationships ({0})", relationships.Count()));

            foreach (Relationship relation in relationships) {

                long count = 0;

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM SpeciesRelationships WHERE relationship_id=$id;")) {

                    cmd.Parameters.AddWithValue("$id", relation.id);

                    count = await Db.GetScalarAsync<long>(cmd);

                }

                embed.AddField(string.Format("{0} ({1})", relation.GetName(), count), relation.description);

            }

            await ReplyAsync("", false, embed.Build());

        }
        [Command("relationships", RunMode = RunMode.Async), Alias("relations", "related")]
        public async Task Relationships([Remainder] string speciesName) {

            // Get the species from the DB.

            ISpecies sp = await GetSpeciesOrReplyAsync(speciesName);

            if (!sp.IsValid())
                return;

            // Get relationships and build the embed.

            SortedDictionary<string, List<string>> items = new SortedDictionary<string, List<string>>();

            // Get relationships where this species is the one acting upon another.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM SpeciesRelationships LEFT JOIN Relationships ON SpeciesRelationships.relationship_id = Relationships.id WHERE species1_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.Id);

                foreach (DataRow row in await Db.GetRowsAsync(cmd)) {

                    long other_species_id = row.Field<long>("species2_id");
                    ISpecies other_species = await Db.GetSpeciesAsync(other_species_id);
                    Relationship relationship = Relationship.FromDataRow(row);

                    if (other_species is null)
                        continue;

                    if (!items.ContainsKey(relationship.BeneficiaryName(plural: true)))
                        items[relationship.BeneficiaryName(plural: true)] = new List<string>();

                    items[relationship.BeneficiaryName(plural: true)].Add(TaxonFormatter.GetString(other_species));

                }

            }

            // Get relationships where this species is the one being acted upon.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM SpeciesRelationships LEFT JOIN Relationships ON SpeciesRelationships.relationship_id = Relationships.id WHERE species2_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.Id);

                foreach (DataRow row in await Db.GetRowsAsync(cmd)) {

                    long other_species_id = row.Field<long>("species1_id");
                    ISpecies other_species = await Db.GetSpeciesAsync(other_species_id);
                    Relationship relationship = Relationship.FromDataRow(row);

                    if (other_species is null)
                        continue;

                    if (!items.ContainsKey(relationship.BenefactorName(plural: true)))
                        items[relationship.BenefactorName(plural: true)] = new List<string>();

                    items[relationship.BenefactorName(plural: true)].Add(TaxonFormatter.GetString(other_species));

                }

            }

            // Get any prey/predator relationships.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Predates WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.Id);

                foreach (DataRow row in await Db.GetRowsAsync(cmd)) {

                    long other_species_id = row.Field<long>("eats_id");
                    ISpecies other_species = await Db.GetSpeciesAsync(other_species_id);

                    if (other_species is null)
                        continue;

                    if (!items.ContainsKey("prey"))
                        items["prey"] = new List<string>();

                    items["prey"].Add(TaxonFormatter.GetString(other_species));

                }

            }

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Predates WHERE eats_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp.Id);

                foreach (DataRow row in await Db.GetRowsAsync(cmd)) {

                    long other_species_id = row.Field<long>("species_id");
                    ISpecies other_species = await Db.GetSpeciesAsync(other_species_id);

                    if (other_species is null)
                        continue;

                    if (!items.ContainsKey("predators"))
                        items["predators"] = new List<string>();

                    items["predators"].Add(TaxonFormatter.GetString(other_species));

                }

            }

            // If the species does not have any relationships with other species, state so.

            if (items.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** does not have any relationships with other species.", sp.GetShortName()));

                return;

            }

            // Build the embed.

            EmbedBuilder embed = new EmbedBuilder();
            int relationship_count = 0;

            foreach (string key in items.Keys) {

                items[key].Sort((lhs, rhs) => lhs.CompareTo(rhs));

                embed.AddField(string.Format("{0} ({1})", StringUtilities.ToTitleCase(key), items[key].Count()), string.Join(Environment.NewLine, items[key]), inline: true);

                relationship_count += items[key].Count();

            }

            embed.WithTitle(string.Format("Relationships involving {0} ({1})", TaxonFormatter.GetString(sp, false), relationship_count));

            await ReplyAsync("", false, embed.Build());

        }

        [Command("addrelationship"), Alias("addrelation"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddRelationship(string name) {

            await AddRelationship(name, "");

        }
        [Command("addrelationship"), Alias("addrelation"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddRelationship(string name, string description) {

            // If the relationship already exists, warn the user and do not modify the database.

            if (!(await GetRelationshipFromDbAsync(name) is null)) {

                await BotUtils.ReplyAsync_Warning(Context, string.Format("The relationship **{0}** already exists.", StringUtilities.ToTitleCase(name)));

                return;

            }

            // Add the relationship to the database.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Relationships(name, description) VALUES($name, $description)")) {

                cmd.Parameters.AddWithValue("$name", name.ToLower());
                cmd.Parameters.AddWithValue("$description", description);

                await Db.ExecuteNonQueryAsync(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully created new relationship, **{0}**.", StringUtilities.ToTitleCase(name)));

        }

        [Command("+relationship", RunMode = RunMode.Async), Alias("+relation"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task PlusRelationship(string species1, string species2, string relationship) {

            await PlusRelationship("", species1, "", species2, relationship);

        }
        [Command("+relationship", RunMode = RunMode.Async), Alias("+relation"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task PlusRelationship(string genus1, string species1, string genus2, string species2, string relationship) {

            // Get the relationship from the DB.

            Relationship relation = await GetRelationshipFromDbAsync(relationship);

            if (!await ReplyValidateRelationshipAsync(Context, relation))
                return;

            // Get the species from the DB.

            ISpecies sp1 = await GetSpeciesOrReplyAsync(genus1, species1);

            if (!sp1.IsValid())
                return;

            ISpecies sp2 = await GetSpeciesOrReplyAsync(genus2, species2);

            if (!sp2.IsValid())
                return;

            if (sp1.Id == sp2.Id) {

                await BotUtils.ReplyAsync_Warning(Context, "A species cannot be in a relationship with itself.");

                return;

            }

            // Create the new relationship.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO SpeciesRelationships(species1_id, species2_id, relationship_id) VALUES($species1_id, $species2_id, $relationship_id)")) {

                cmd.Parameters.AddWithValue("$species1_id", sp1.Id);
                cmd.Parameters.AddWithValue("$species2_id", sp2.Id);
                cmd.Parameters.AddWithValue("$relationship_id", relation.id);

                await Db.ExecuteNonQueryAsync(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has successfully been set as a {1} of **{2}**.",
                sp2.GetShortName(),
                relation.BeneficiaryName(),
                sp1.GetShortName()));

        }
        [Command("-relationship", RunMode = RunMode.Async), Alias("-relation"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusRelationship(string species1, string species2, string relationship) {

            await MinusRelationship("", species1, "", species2, relationship);


        }
        [Command("-relationship", RunMode = RunMode.Async), Alias("-relation"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusRelationship(string genus1, string species1, string genus2, string species2, string relationship) {

            // Get the relationship from the DB.

            Relationship relation = await GetRelationshipFromDbAsync(relationship);

            if (!await ReplyValidateRelationshipAsync(Context, relation))
                return;

            // Get the species from the DB.

            ISpecies sp1 = await GetSpeciesOrReplyAsync(genus1, species1);

            if (!sp1.IsValid())
                return;

            ISpecies sp2 = await GetSpeciesOrReplyAsync(genus2, species2);

            if (!sp2.IsValid())
                return;

            // Check if the requested relationship exists.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM SpeciesRelationships WHERE (species1_id=$species1_id OR species1_id=$species2_id OR species2_id=$species1_id OR species2_id=$species2_id) AND relationship_id=$relationship_id;")) {

                cmd.Parameters.AddWithValue("$species1_id", sp1.Id);
                cmd.Parameters.AddWithValue("$species2_id", sp2.Id);
                cmd.Parameters.AddWithValue("$relationship_id", relation.id);

                if (await Db.GetScalarAsync<long>(cmd) <= 0) {

                    await BotUtils.ReplyAsync_Error(Context, string.Format("No such relationship exists between **{0}** and **{1}**.", sp1.GetShortName(), sp2.GetShortName()));

                    return;

                }

            }

            // Delete the relationship.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesRelationships WHERE (species1_id=$species1_id OR species1_id=$species2_id OR species2_id=$species1_id OR species2_id=$species2_id) AND relationship_id=$relationship_id;")) {

                cmd.Parameters.AddWithValue("$species1_id", sp1.Id);
                cmd.Parameters.AddWithValue("$species2_id", sp2.Id);
                cmd.Parameters.AddWithValue("$relationship_id", relation.id);

                await Db.ExecuteNonQueryAsync(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** and **{1}** no longer have a {2} relationship.", sp1.GetShortName(), sp2.GetShortName(), relation.DescriptorName()));

        }

        private async Task<Relationship> GetRelationshipFromDbAsync(string name) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Relationships WHERE name=$name;")) {

                cmd.Parameters.AddWithValue("$name", name.ToLower());

                DataRow row = await Db.GetRowAsync(cmd);

                return row is null ? null : Relationship.FromDataRow(row);

            }

        }
        private async Task<Relationship[]> GetRelationshipsFromDbAsync() {

            List<Relationship> relationships = new List<Relationship>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Relationships;"))
                foreach (DataRow row in await Db.GetRowsAsync(cmd))
                    relationships.Add(Relationship.FromDataRow(row));

            relationships.Sort((lhs, rhs) => lhs.GetName().CompareTo(rhs.GetName()));

            return relationships.ToArray();

        }
        private async Task<bool> ReplyValidateRelationshipAsync(ICommandContext context, Relationship relationship) {

            if (relationship is null || relationship.id < 0) {

                await BotUtils.ReplyAsync_Error(context, "No such relationship exists.");

                return false;

            }

            return true;

        }

    }

}