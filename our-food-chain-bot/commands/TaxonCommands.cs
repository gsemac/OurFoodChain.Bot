using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class TaxonCommands :
        ModuleBase {

        [Command("genus"), Alias("g", "genera")]
        public async Task Genus(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Genus, name);
        }
        [Command("addgenus")]
        public async Task AddGenus(string genus, string description = "") {

            // Make sure that the genus doesn't already exist.

            if (!(await BotUtils.GetGenusFromDb(genus) is null)) {

                await BotUtils.ReplyAsync_Warning(Context, string.Format("The genus **{0}** already exists.", StringUtils.ToTitleCase(genus)));

                return;

            }

            Genus genus_info = new Genus();
            genus_info.name = genus;
            genus_info.description = description;

            await BotUtils.AddGenusToDb(genus_info);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully created new genus, **{0}**.", StringUtils.ToTitleCase(genus)));

        }
        [Command("setgenus")]
        public async Task SetGenus(string genus, string species, string newGenus = "") {

            // If there is no argument for "newGenus", assume the user omitted the original genus.
            // e.g.: setgenus <species> <newGenus>

            if (string.IsNullOrEmpty(newGenus)) {
                newGenus = species;
                species = genus;
                genus = string.Empty;
            }

            // Get the specified species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Get the specified genus.

            Genus genus_info = await BotUtils.GetGenusFromDb(newGenus);

            if (!await BotUtils.ReplyAsync_ValidateGenus(Context, genus_info))
                return;

            // Update the species.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET genus_id=$genus_id WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$genus_id", genus_info.id);
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has successfully been assigned to the genus **{1}**.", sp.GetShortName(), StringUtils.ToTitleCase(genus_info.name)));

        }
        [Command("setgenuspic"), Alias("setgpic")]
        public async Task SetGenusPic(string genus, string picUrl) {

            Genus genus_info = await BotUtils.GetGenusFromDb(genus);

            if (!await BotUtils.ReplyAsync_ValidateGenus(Context, genus_info))
                return;

            if (!await BotUtils.ReplyAsync_ValidateImageUrl(Context, picUrl))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Genus SET pics=$url WHERE id=$genus_id;")) {

                cmd.Parameters.AddWithValue("$url", picUrl);
                cmd.Parameters.AddWithValue("$genus_id", genus_info.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully added a picture for **{0}**.", StringUtils.ToTitleCase(genus_info.name)));

        }
        [Command("setgenusdescription"), Alias("setgenusdesc", "setgdesc")]
        public async Task SetGenusDescription(string genus, string description) {

            Genus genus_info = await BotUtils.GetGenusFromDb(genus);

            if (!await BotUtils.ReplyAsync_ValidateGenus(Context, genus_info))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Genus SET description=$description WHERE id=$genus_id;")) {

                cmd.Parameters.AddWithValue("$description", description);
                cmd.Parameters.AddWithValue("$genus_id", genus_info.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated description for genus **{0}**.", StringUtils.ToTitleCase(genus_info.name)));

        }

        [Command("family"), Alias("f", "families")]
        public async Task Family(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Family, name);
        }
        [Command("addfamily")]
        public async Task AddFamily(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, TaxonType.Family, name, description);
        }
        [Command("setfamily")]
        public async Task SetFamily(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, TaxonType.Family, child, parent);
        }
        [Command("setfamilydesc"), Alias("setfamilydescription", "setfdesc")]
        public async Task SetFamilyDesc(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Family, name, description);
        }
        [Command("setfamilycommonname"), Alias("setfamilycommon", "setfcommon")]
        public async Task SetFamilyCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Family, name, commonName);
        }

        [Command("order"), Alias("o", "orders")]
        public async Task Order(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Order, name);
        }
        [Command("addorder")]
        public async Task AddOrder(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, TaxonType.Order, name, description);
        }
        [Command("setorder")]
        public async Task SetOrder(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, TaxonType.Order, child, parent);
        }
        [Command("setorderdesc"), Alias("setorderdescription", "setodesc")]
        public async Task SetOrderDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Order, name, description);
        }
        [Command("setordercommonname"), Alias("setordercommon", "setocommon")]
        public async Task SetOrderCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Order, name, commonName);
        }

        [Command("class"), Alias("c", "classes")]
        public async Task Class(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Class, name);
        }
        [Command("addclass")]
        public async Task AddClass(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, TaxonType.Class, name, description);
        }
        [Command("setclass")]
        public async Task SetClass(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, TaxonType.Class, child, parent);
        }
        [Command("setclassdesc"), Alias("setclassdescription", "setcdesc")]
        public async Task SetClassDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Class, name, description);
        }
        [Command("setclasscommonname"), Alias("setclasscommon", "setccommon")]
        public async Task SetClassCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Class, name, commonName);
        }

        [Command("phylum"), Alias("p", "phyla")]
        public async Task Phylum(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Phylum, name);
        }
        [Command("addphylum")]
        public async Task AddPhylum(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, TaxonType.Phylum, name, description);
        }
        [Command("setphylum")]
        public async Task SetPhylum(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, TaxonType.Phylum, child, parent);
        }
        [Command("setphylumdesc"), Alias("setphylumdescription", "setpdesc")]
        public async Task SetPhylumDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Phylum, name, description);
        }
        [Command("setphylumcommonname"), Alias("setphylumcommon", "setpcommon")]
        public async Task SetPhylumCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Phylum, name, commonName);
        }

        [Command("kingdom"), Alias("k", "kingdoms")]
        public async Task Kingdom(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Kingdom, name);
        }
        [Command("addkingdom")]
        public async Task AddKingdom(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, TaxonType.Kingdom, name, description);
        }
        [Command("setkingdom")]
        public async Task SetKingdom(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, TaxonType.Kingdom, child, parent);
        }
        [Command("setkingdomdesc"), Alias("setkingdomdescription", "setkdesc")]
        public async Task SetKingdomDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Kingdom, name, description);
        }
        [Command("setkingdomcommonname"), Alias("setkingdomcommon", "setkcommon")]
        public async Task SetKingdomCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Kingdom, name, commonName);
        }

        [Command("domain"), Alias("d", "domains")]
        public async Task Domain(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Domain, name);
        }
        [Command("adddomain")]
        public async Task AddDomain(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, TaxonType.Domain, name, description);
        }
        [Command("setdomain")]
        public async Task SetDomain(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, TaxonType.Domain, child, parent);
        }
        [Command("setdomaindesc"), Alias("setdomaindescription", "setddesc")]
        public async Task SetDomainDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Domain, name, description);
        }
        [Command("setdomaincommonname"), Alias("setdomaincommon", "setdcommon")]
        public async Task SetDomainCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Domain, name, commonName);
        }

    }

}