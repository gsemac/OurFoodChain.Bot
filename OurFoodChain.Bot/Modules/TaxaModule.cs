using Discord;
using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    public class TaxaModule :
        ModuleBase {

        // Public members

        public IOfcBotConfiguration BotConfiguration { get; set; }
        public SQLiteDatabase Db { get; set; }

        // Genus

        [Command("genus"), Alias("g", "genera")]
        public async Task Genus(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, BotConfiguration, Db, TaxonRank.Genus, name);
        }
        [Command("addgenus"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddGenus(string genus, string description = "") {

            // Make sure that the genus doesn't already exist.

            if (!(await BotUtils.GetGenusFromDb(genus) is null)) {

                await BotUtils.ReplyAsync_Warning(Context, string.Format("The genus **{0}** already exists.", StringUtilities.ToTitleCase(genus)));

                return;

            }

            Taxon genus_info = new Taxon(TaxonRank.Genus) {
                name = genus,
                description = description
            };

            await BotUtils.AddGenusToDb(genus_info);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully created new genus, **{0}**.", StringUtilities.ToTitleCase(genus)));

        }
        [Command("setgenus"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetGenus(string genus, string species, string newGenus = "") {

            // If there is no argument for "newGenus", assume the user omitted the original genus.
            // e.g.: setgenus <species> <newGenus>

            if (string.IsNullOrEmpty(newGenus)) {
                newGenus = species;
                species = genus;
                genus = string.Empty;
            }

            // Get the specified species.

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            // Get the specified genus.

            Taxon genus_info = await BotUtils.GetGenusFromDb(newGenus);

            if (!await BotUtils.ReplyAsync_ValidateGenus(Context, genus_info))
                return;

            // Update the species.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET genus_id=$genus_id WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$genus_id", genus_info.id);
                cmd.Parameters.AddWithValue("$species_id", sp.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has successfully been assigned to the genus **{1}**.", sp.ShortName, StringUtilities.ToTitleCase(genus_info.name)));

        }
        [Command("setgenusdescription"), Alias("setgenusdesc", "setgdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetGenusDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Genus, name);
        }
        [Command("setgenusdescription"), Alias("setgenusdesc", "setgdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetGenusDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Genus, name, description);
        }
        [Command("setgenuspic"), Alias("setgpic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetGenusPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, BotConfiguration, TaxonRank.Genus, name, url);
        }
        [Command("setgenuscommonname"), Alias("setgenuscommon", "setgcommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetGenuscommonName(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, BotConfiguration, TaxonRank.Genus, name, commonName);
        }
        [Command("deletegenus"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteGenus(string name) {
            await _deleteTaxonAsync(name, TaxonRank.Genus);
        }

        // Family

        [Command("family"), Alias("f", "families")]
        public async Task Family(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, BotConfiguration, Db, TaxonRank.Family, name);
        }
        [Command("addfamily"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddFamily(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, BotConfiguration, TaxonRank.Family, name, description);
        }
        [Command("setfamily"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetFamily(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, BotConfiguration, TaxonRank.Family, child, parent);
        }
        [Command("setfamilydesc"), Alias("setfamilydescription", "setfdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetFamilyDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Family, name);
        }
        [Command("setfamilydesc"), Alias("setfamilydescription", "setfdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetFamilyDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Family, name, description);
        }
        [Command("setfamilypic"), Alias("setfpic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetFamilyPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, BotConfiguration, TaxonRank.Family, name, url);
        }
        [Command("setfamilycommonname"), Alias("setfamilycommon", "setfcommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetFamilyCommonName(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, BotConfiguration, TaxonRank.Family, name, commonName);
        }
        [Command("deletefamily"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteFamily(string name) {
            await _deleteTaxonAsync(name, TaxonRank.Family);
        }

        // Order

        [Command("order"), Alias("o", "orders")]
        public async Task Order(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, BotConfiguration, Db, TaxonRank.Order, name);
        }
        [Command("addorder"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddOrder(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, BotConfiguration, TaxonRank.Order, name, description);
        }
        [Command("setorder"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOrder(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, BotConfiguration, TaxonRank.Order, child, parent);
        }
        [Command("setorderdesc"), Alias("setorderdescription", "setodesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOrderDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Order, name);
        }
        [Command("setorderdesc"), Alias("setorderdescription", "setodesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOrderDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Order, name, description);
        }
        [Command("setorderpic"), Alias("setopic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOrderPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, BotConfiguration, TaxonRank.Order, name, url);
        }
        [Command("setordercommonname"), Alias("setordercommon", "setocommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOrderCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, BotConfiguration, TaxonRank.Order, name, commonName);
        }
        [Command("deleteorder"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteOrder(string name) {
            await _deleteTaxonAsync(name, TaxonRank.Order);
        }

        // Class

        [Command("class"), Alias("c", "classes")]
        public async Task Class(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, BotConfiguration, Db, TaxonRank.Class, name);
        }
        [Command("addclass"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddClass(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, BotConfiguration, TaxonRank.Class, name, description);
        }
        [Command("setclass"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetClass(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, BotConfiguration, TaxonRank.Class, child, parent);
        }
        [Command("setclassdesc"), Alias("setclassdescription", "setcdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetClassDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Class, name);
        }
        [Command("setclassdesc"), Alias("setclassdescription", "setcdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetClassDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Class, name, description);
        }
        [Command("setclasspic"), Alias("setcpic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetClassPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, BotConfiguration, TaxonRank.Class, name, url);
        }
        [Command("setclasscommonname"), Alias("setclasscommon", "setccommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetClassCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, BotConfiguration, TaxonRank.Class, name, commonName);
        }
        [Command("deleteclass"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteClass(string name) {
            await _deleteTaxonAsync(name, TaxonRank.Class);
        }

        // Phylum

        [Command("phylum"), Alias("p", "phyla")]
        public async Task Phylum(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, BotConfiguration, Db, TaxonRank.Phylum, name);
        }
        [Command("addphylum"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddPhylum(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, BotConfiguration, TaxonRank.Phylum, name, description);
        }
        [Command("setphylum"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetPhylum(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, BotConfiguration, TaxonRank.Phylum, child, parent);
        }
        [Command("setphylumdesc"), Alias("setphylumdescription", "setpdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetPhylumDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Phylum, name);
        }
        [Command("setphylumdesc"), Alias("setphylumdescription", "setpdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetPhylumDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Phylum, name, description);
        }
        [Command("setphylumpic"), Alias("setppic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetPhylumPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, BotConfiguration, TaxonRank.Phylum, name, url);
        }
        [Command("setphylumcommonname"), Alias("setphylumcommon", "setpcommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetPhylumCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, BotConfiguration, TaxonRank.Phylum, name, commonName);
        }
        [Command("deletephylum"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeletePhylum(string name) {
            await _deleteTaxonAsync(name, TaxonRank.Phylum);
        }

        [Command("kingdom"), Alias("k", "kingdoms")]
        public async Task Kingdom(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, BotConfiguration, Db, TaxonRank.Kingdom, name);
        }
        [Command("addkingdom"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddKingdom(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, BotConfiguration, TaxonRank.Kingdom, name, description);
        }
        [Command("setkingdom"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetKingdom(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, BotConfiguration, TaxonRank.Kingdom, child, parent);
        }
        [Command("setkingdomdesc"), Alias("setkingdomdescription", "setkdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetKingdomDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Kingdom, name);
        }
        [Command("setkingdomdesc"), Alias("setkingdomdescription", "setkdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetKingdomDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Kingdom, name, description);
        }
        [Command("setkingdompic"), Alias("setkpic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetKingdomPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, BotConfiguration, TaxonRank.Kingdom, name, url);
        }
        [Command("setkingdomcommonname"), Alias("setkingdomcommon", "setkcommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetKingdomCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, BotConfiguration, TaxonRank.Kingdom, name, commonName);
        }
        [Command("deletekingdom"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteKingdom(string name) {
            await _deleteTaxonAsync(name, TaxonRank.Kingdom);
        }

        // Domain

        [Command("domain"), Alias("d", "domains")]
        public async Task Domain(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, BotConfiguration, Db, TaxonRank.Domain, name);
        }
        [Command("adddomain"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddDomain(string name, string description = "") {
            await BotUtils.Command_AddTaxon(Context, BotConfiguration, TaxonRank.Domain, name, description);
        }
        [Command("setdomain"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetDomain(string child, string parent) {
            await BotUtils.Command_SetTaxon(Context, BotConfiguration, TaxonRank.Domain, child, parent);
        }
        [Command("setdomaindesc"), Alias("setdomaindescription", "setddesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetDomainDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Domain, name);
        }
        [Command("setdomaindesc"), Alias("setdomaindescription", "setddesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetDomainDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, TaxonRank.Domain, name, description);
        }
        [Command("setdomainpic"), Alias("setdpic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetDomainPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, BotConfiguration, TaxonRank.Domain, name, url);
        }
        [Command("setdomaincommonname"), Alias("setdomaincommon", "setdcommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetDomainCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, BotConfiguration, TaxonRank.Domain, name, commonName);
        }
        [Command("deletedomain"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteDomain(string name) {
            await _deleteTaxonAsync(name, TaxonRank.Domain);
        }

        // Species

        [Command("setspeciesdesc"), Alias("setspeciesdescription", "setsdesc")]
        public async Task SetSpeciesDescription(string species) {
            await SetSpeciesDescription("", species);
        }
        [Command("setspeciesdesc"), Alias("setspeciesdescription", "setsdesc")]
        public async Task SetSpeciesDescription(string speciesOrGenus, string descriptionOrSpecies) {

            // Either the user provided a species and a description, or they provided a genus and species and want to use the two-part command.
            // If the species exists, we'll use the two-part command version. If it doesn't, we'll assume the user was providing a description directly.

            Species[] species_list = await BotUtils.GetSpeciesFromDb(speciesOrGenus, descriptionOrSpecies);

            if (species_list.Count() <= 0) {

                // No such species exists for the given genus/species. We have two possibilities.

                if (!string.IsNullOrEmpty(speciesOrGenus))
                    // The user passed in a species and a description, so attempt to update the description for that species.
                    await SetSpeciesDescription(string.Empty, speciesOrGenus, descriptionOrSpecies);
                else
                    // The user passed in a blank genus and a non-existent species, so reply with some suggestions.
                    await BotUtils.ReplyAsync_SpeciesSuggestions(Context, speciesOrGenus, descriptionOrSpecies);

            }
            else if (await BotUtils.ReplyValidateSpeciesAsync(Context, species_list)) {

                // A species exists with the given genus/species, so initiate a two-part command.

                // Ensure that the user has necessary privileges to use this command.
                if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator, species_list[0]))
                    return;

                await _setSpeciesDescriptionAsync(species_list[0]);

            }

        }
        [Command("setspeciesdesc"), Alias("setspeciesdescription", "setsdesc")]
        public async Task SetSpeciesDescription(string genusName, string speciesName, string description) {

            Species species = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);

            if (species != null)
                await _setSpeciesDescriptionAsync(species, description);

        }

        [Command("setspeciescommonname"), Alias("setspeciescommon", "setscommon")]
        private async Task SetSpeciesCommonName(string species, string commonName) {
            await SetSpeciesCommonName("", species, commonName);
        }
        [Command("setspeciescommonname"), Alias("setspeciescommon", "setscommon")]
        private async Task SetSpeciesCommonName(string genus, string species, string commonName) {

            Species species_info = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (species_info is null)
                return;

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator, species_info))
                return;

            if (string.IsNullOrWhiteSpace(commonName)) {

                // If the given common name is empty, erase all common names associated with this species.
                await SpeciesUtils.RemoveCommonNamesAsync(species_info);

                await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully removed all common names from **{0}**.",
                    species_info.ShortName));

            }
            else {

                // Otherwise, add the common name to the database.

                // The difference between this and the "+common" command is that this one overwrites the value stored in the "Species" table.
                // This field is pretty much deprected at this point, but it is still accessed through some generic taxon commands.

                await SpeciesUtils.AddCommonNameAsync(species_info, commonName, overwriteSpeciesTable: true);

                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** is now commonly known as the **{1}**.",
                    species_info.ShortName,
                    StringUtilities.ToTitleCase(commonName)));

            }

        }

        [Command("+commonname"), Alias("+common")]
        private async Task PlusCommonName(string species, string commonName) {
            await PlusCommonName("", species, commonName);
        }
        [Command("+commonname"), Alias("+common")]
        private async Task PlusCommonName(string genus, string species, string commonName) {

            Species species_info = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (species_info is null)
                return;

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator, species_info))
                return;

            if (string.IsNullOrWhiteSpace(commonName)) {

                await BotUtils.ReplyAsync_Error(Context, "Common name cannot be empty.");

            }
            else {

                await SpeciesUtils.AddCommonNameAsync(species_info, commonName, overwriteSpeciesTable: false);

                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** is now commonly known as the **{1}**.",
                    species_info.ShortName,
                    StringUtilities.ToTitleCase(commonName)));

            }

        }

        [Command("-commonname"), Alias("-common")]
        private async Task MinusCommonName(string species, string commonName) {
            await MinusCommonName("", species, commonName);
        }
        [Command("-commonname"), Alias("-common")]
        private async Task MinusCommonName(string genus, string species, string commonName) {

            Species species_info = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (species_info is null)
                return;

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator, species_info))
                return;

            CommonName[] common_names = await SpeciesUtils.GetCommonNamesAsync(species_info);

            if (!common_names.Any(x => x.Value.ToLower() == commonName.ToLower())) {

                // Check if the species actually has this common name before attempting to remove it (for the sake of clarity to the user).

                await BotUtils.ReplyAsync_Warning(Context, string.Format("The common name **{0}** has already been removed.",
                      StringUtilities.ToTitleCase(commonName)));

            }
            else {

                await SpeciesUtils.RemoveCommonNameAsync(species_info, commonName);

                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** is no longer known as the **{1}**.",
                    species_info.ShortName,
                    StringUtilities.ToTitleCase(commonName)));

            }

        }

        // Generic

        [Command("setdescription"), Alias("setdesc")]
        public async Task SetTaxonDescription(string taxon) {

            // Get all taxa with the specified name.
            Taxon[] taxa = await BotUtils.GetTaxaFromDb(taxon);

            if (taxa.Count() <= 0) {

                // If there is no such taxon, default to showing species suggestions.
                await BotUtils.ReplyAsync_SpeciesSuggestions(Context, "", taxon);

            }
            else if (await BotUtils.ReplyAsync_ValidateTaxa(Context, taxa)) {

                // If we got a single taxon, begin a multistage update for that taxon.
                await _setTaxonDescriptionAsync(taxa[0]);

            }

        }
        [Command("setdescription"), Alias("setdesc")]
        public async Task SetTaxonDescription(string taxonNameOrGenus, string descriptionOrSpecies) {

            // Either the user provided a taxon and a description, or they provided a genus and species and want to use a two-part command sequence.
            // If the species exists, we'll use the two-part command version. If it doesn't, we'll assume the user was providing a description directly.

            Species[] species_list = await BotUtils.GetSpeciesFromDb(taxonNameOrGenus, descriptionOrSpecies);

            if (species_list.Count() <= 0) {

                // No such species exists for the given genus/species, so look for the taxon instead and try to update its description directly.

                Taxon[] taxa = await BotUtils.GetTaxaFromDb(taxonNameOrGenus);

                // If we didn't get any matches, show the user species suggestions.

                if (taxa.Count() <= 0)
                    await BotUtils.ReplyFindSpeciesAsync(Context, "", taxonNameOrGenus);

                else {

                    // Make sure we have one, and only one taxon to update.

                    if (!await BotUtils.ReplyAsync_ValidateTaxa(Context, taxa))
                        return;

                    Taxon taxon = taxa[0];

                    if (taxon.type == TaxonRank.Species)

                        // If the taxon is a species, use the species update procedure.
                        await _setSpeciesDescriptionAsync(await BotUtils.GetSpeciesFromDb(taxon.id), descriptionOrSpecies);

                    else

                        // Update the taxon in the DB.           
                        await BotUtils.Command_SetTaxonDescription(Context, BotConfiguration, taxon, descriptionOrSpecies);

                }

            }
            else if (await BotUtils.ReplyValidateSpeciesAsync(Context, species_list)) {

                // A species exists with the given genus/species, so initiate a two-part command.

                // Ensure that the user has necessary privileges to use this command.
                if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator, species_list[0]))
                    return;

                Bot.MultiPartMessage p = new Bot.MultiPartMessage(Context) {
                    UserData = new string[] { taxonNameOrGenus, descriptionOrSpecies },
                    Callback = async (args) => {

                        Species[] species = await BotUtils.GetSpeciesFromDb(args.Message.UserData[0], args.Message.UserData[1]);

                        if (await BotUtils.ReplyValidateSpeciesAsync(args.Message.Context, species))
                            await _setSpeciesDescriptionAsync(species[0], args.ResponseContent);

                    }
                };

                await Bot.DiscordUtils.SendMessageAsync(Context, p,
                    string.Format("Reply with the description for **{0}**.\nTo cancel the update, reply with \"cancel\".", species_list[0].ShortName));

            }

        }
        [Command("setdescription"), Alias("setdesc")]
        public async Task SetTaxonDescription(string genus, string species, string description) {
            await SetSpeciesDescription(genus, species, description);
        }

        [Command("appenddescription"), Alias("appenddesc", "+desc", "+description")]
        public async Task AppendDescription(string species) {
            await AppendDescription("", species);
        }
        [Command("appenddescription"), Alias("appenddesc", "+desc", "+description")]
        public async Task AppendDescription(string arg0, string arg1) {

            // Possible input:
            // +desc <genus> <species>
            // +desc <species> <description>

            Species[] species_list = await BotUtils.GetSpeciesFromDb(arg0, arg1);

            if (species_list.Count() <= 0) {

                // We weren't able to find a species with the given genus and name, so perhaps the user provided a species and description.
                // The following method call will handle this situation, as well as the situation where the species was invalid.
                await AppendDescription("", species: arg0, description: arg1);

            }
            else if (await BotUtils.ReplyValidateSpeciesAsync(Context, species_list)) {

                // Initialize a multistage update for the given species.

                Species species = species_list[0];

                // Ensure that the user has necessary privileges to use this command.
                if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator, species))
                    return;

                await _appendDescriptionAsync(species);

            }

        }
        [Command("appenddescription"), Alias("appenddesc", "+desc", "+description")]
        public async Task AppendDescription(string genus, string species, string description) {

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator, sp))
                return;

            await _appendDescriptionAsync(sp, description);

        }

        [Command("setcommonname"), Alias("setcommon")]
        public async Task SetCommonName(string taxon, string commonName) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator))
                return;

            // Get all taxa with the specified name.
            Taxon[] taxa = await BotUtils.GetTaxaFromDb(taxon);

            if (taxa.Count() <= 0) {

                // If there is no such taxon, default to showing species suggestions.
                await BotUtils.ReplyAsync_SpeciesSuggestions(Context, "", taxon);

            }
            else if (taxa.Count() == 1 && taxa[0].type == TaxonRank.Species) {

                // If there's a single result, and it's a species, use the species-specific update procedure.
                // (The generic one works fine, but this currently shows a unique confirmation message.)
                await SetSpeciesCommonName(taxon, commonName);

            }
            else if (await BotUtils.ReplyAsync_ValidateTaxa(Context, taxa)) {

                // If we got a single taxon, update the common name for that taxon.
                await _setTaxonCommonNameAsync(taxa[0], commonName);

            }

        }
        [Command("setcommonname"), Alias("setcommon")]
        public async Task SetCommonName(string genus, string species, string commonName) {
            await SetSpeciesCommonName(genus, species, commonName);
        }

        [Command("delete"), Alias("deletetaxon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteTaxon(string name) {

            Taxon[] taxa = await TaxonUtils.GetTaxaAsync(name);

            await _deleteTaxonAsync(taxa);

        }

        // Private members

        private async Task _setSpeciesDescriptionAsync(Species species) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator, species))
                return;

            Bot.MultiPartMessage p = new Bot.MultiPartMessage(Context) {
                Callback = async (args) => {

                    if (await BotUtils.ReplyValidateSpeciesAsync(args.Message.Context, species))
                        await _setSpeciesDescriptionAsync(species, args.ResponseContent);

                }
            };

            await Bot.DiscordUtils.SendMessageAsync(Context, p,
                string.Format("Reply with the description for **{0}**.\nTo cancel the update, reply with \"cancel\".", species.ShortName));

        }
        private async Task _setSpeciesDescriptionAsync(Species species, string description) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator, species))
                return;

            await BotUtils.UpdateSpeciesDescription(species, description);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated the description for **{0}**.", species.ShortName));

        }

        private async Task _setTaxonDescriptionAsync(Taxon taxon) {

            if (taxon.type == TaxonRank.Species)
                await _setSpeciesDescriptionAsync(await SpeciesUtils.GetSpeciesAsync(taxon.id));

            else if (await BotUtils.ReplyHasPrivilegeAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator)) { // moderator use only

                Bot.MultiPartMessage p = new Bot.MultiPartMessage(Context) {
                    UserData = new string[] { taxon.name },
                    Callback = async (args) => {

                        await BotUtils.Command_SetTaxonDescription(args.Message.Context, BotConfiguration, taxon, args.ResponseContent);

                    }
                };

                await Bot.DiscordUtils.SendMessageAsync(Context, p,
                    string.Format("Reply with the description for {0} **{1}**.\nTo cancel the update, reply with \"cancel\".", taxon.GetTypeName(), taxon.GetName()));

            }

        }
        private async Task _appendDescriptionAsync(Species species) {

            Bot.MultiPartMessage p = new Bot.MultiPartMessage(Context) {
                Callback = async (args) => {

                    if (await BotUtils.ReplyValidateSpeciesAsync(args.Message.Context, species))
                        await _appendDescriptionAsync(species, args.ResponseContent);

                }
            };

            await Bot.DiscordUtils.SendMessageAsync(Context, p,
                string.Format("Reply with the text to append to the description for **{0}**.\nTo cancel the update, reply with \"cancel\".", species.ShortName));

        }
        private async Task _appendDescriptionAsync(Species species, string description) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator, species))
                return;

            // Always start appended test on a new paragraph.
            description = Environment.NewLine + Environment.NewLine + description.Trim();

            // Ensure the decription is of a reasonable size.

            const int MAX_DESCRIPTION_LENGTH = 10000;

            if (species.Description.Length + description.Length > MAX_DESCRIPTION_LENGTH) {

                await BotUtils.ReplyAsync_Error(Context, string.Format("The description length exceeds the maximum allowed length ({0} characters).", MAX_DESCRIPTION_LENGTH));

                return;

            }

            // Append text to the existing description.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET description = $description WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", species.Id);
                cmd.Parameters.AddWithValue("$description", (species.Description + description).Trim());

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated the description for **{0}**.", species.ShortName));

        }
        private async Task _setTaxonCommonNameAsync(Taxon taxon, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, BotConfiguration, taxon.type, taxon.name, commonName);
        }
        private async Task _deleteTaxonAsync(string name, TaxonRank rank) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator))
                return;

            Taxon[] taxa = await TaxonUtils.GetTaxaAsync(name, rank);

            await _deleteTaxonAsync(taxa);

        }
        private async Task _deleteTaxonAsync(Taxon[] taxa) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator))
                return;

            if (await BotUtils.ReplyAsync_ValidateTaxa(Context, taxa)) {

                if ((await TaxonUtils.GetSpeciesAsync(taxa[0])).Count() > 0) {

                    // If the taxon still has species underneath of it, don't allow it to be deleted.
                    await BotUtils.ReplyAsync_Error(Context, "Taxa containing species cannot be deleted.");

                }
                else {

                    // The taxon is empty, so delete the taxon.

                    await TaxonUtils.DeleteTaxonAsync(taxa[0]);

                    await BotUtils.ReplyAsync_Success(Context, string.Format("{0} **{1}** was successfully deleted.", StringUtilities.ToTitleCase(taxa[0].GetTypeName()), taxa[0].GetName()));

                }

            }

        }

    }

}