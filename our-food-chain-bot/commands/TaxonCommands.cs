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

        // Genus

        [Command("genus"), Alias("g", "genera")]
        public async Task Genus(string name = "") {
            await BotUtils.Command_ShowTaxon(Context, TaxonType.Genus, name);
        }
        [Command("addgenus")]
        public async Task AddGenus(string genus, string description = "") {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator))
                return;

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

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator))
                return;

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
        [Command("setgenusdescription"), Alias("setgenusdesc", "setgdesc")]
        public async Task SetGenusDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Genus, name);
        }
        [Command("setgenusdescription"), Alias("setgenusdesc", "setgdesc")]
        public async Task SetGenusDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Genus, name, description);
        }
        [Command("setgenuspic"), Alias("setgpic")]
        public async Task SetGenusPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, TaxonType.Genus, name, url);
        }
        [Command("setgenuscommonname"), Alias("setgenuscommon", "setgcommon")]
        public async Task SetGenuscommonName(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Genus, name, commonName);
        }

        // Family

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
        public async Task SetFamilyDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Family, name);
        }
        [Command("setfamilydesc"), Alias("setfamilydescription", "setfdesc")]
        public async Task SetFamilyDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Family, name, description);
        }
        [Command("setfamilypic"), Alias("setfpic")]
        public async Task SetFamilyPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, TaxonType.Family, name, url);
        }
        [Command("setfamilycommonname"), Alias("setfamilycommon", "setfcommon")]
        public async Task SetFamilyCommonName(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Family, name, commonName);
        }

        // Order

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
        public async Task SetOrderDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Order, name);
        }
        [Command("setorderdesc"), Alias("setorderdescription", "setodesc")]
        public async Task SetOrderDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Order, name, description);
        }
        [Command("setorderpic"), Alias("setopic")]
        public async Task SetOrderPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, TaxonType.Order, name, url);
        }
        [Command("setordercommonname"), Alias("setordercommon", "setocommon")]
        public async Task SetOrderCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Order, name, commonName);
        }

        // Class

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
        public async Task SetClassDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Class, name);
        }
        [Command("setclassdesc"), Alias("setclassdescription", "setcdesc")]
        public async Task SetClassDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Class, name, description);
        }
        [Command("setclasspic"), Alias("setcpic")]
        public async Task SetClassPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, TaxonType.Class, name, url);
        }
        [Command("setclasscommonname"), Alias("setclasscommon", "setccommon")]
        public async Task SetClassCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Class, name, commonName);
        }

        // Phylum

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
        public async Task SetPhylumDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Phylum, name);
        }
        [Command("setphylumdesc"), Alias("setphylumdescription", "setpdesc")]
        public async Task SetPhylumDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Phylum, name, description);
        }
        [Command("setphylumpic"), Alias("setppic")]
        public async Task SetPhylumPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, TaxonType.Phylum, name, url);
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
        public async Task SetKingdomDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Kingdom, name);
        }
        [Command("setkingdomdesc"), Alias("setkingdomdescription", "setkdesc")]
        public async Task SetKingdomDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Kingdom, name, description);
        }
        [Command("setkingdompic"), Alias("setkpic")]
        public async Task SetKingdomPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, TaxonType.Kingdom, name, url);
        }
        [Command("setkingdomcommonname"), Alias("setkingdomcommon", "setkcommon")]
        public async Task SetKingdomCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Kingdom, name, commonName);
        }

        // Domain

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
        public async Task SetDomainDescription(string name) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Domain, name);
        }
        [Command("setdomaindesc"), Alias("setdomaindescription", "setddesc")]
        public async Task SetDomainDescription(string name, string description) {
            await BotUtils.Command_SetTaxonDescription(Context, TaxonType.Domain, name, description);
        }
        [Command("setdomainpic"), Alias("setdpic")]
        public async Task SetDomainPic(string name, string url) {
            await BotUtils.Command_SetTaxonPic(Context, TaxonType.Domain, name, url);
        }
        [Command("setdomaincommonname"), Alias("setdomaincommon", "setdcommon")]
        public async Task SetDomainCommon(string name, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, TaxonType.Domain, name, commonName);
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
                    await SetSpeciesDescription("", speciesOrGenus, descriptionOrSpecies);
                else
                    // The user passed in a blank genus and a non-existent species, so reply with some suggestions.
                    await BotUtils.ReplyAsync_SpeciesSuggestions(Context, speciesOrGenus, descriptionOrSpecies);

            }
            else if (await BotUtils.ReplyAsync_ValidateSpecies(Context, species_list)) {

                // A species exists with the given genus/species, so initiate a two-part command.

                // Ensure that the user has necessary privileges to use this command.
                if (!await BotUtils.ReplyAsync_CheckPrivilegeOrOwnership(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator, species_list[0]))
                    return;

                MultistageCommand p = new MultistageCommand(Context) {
                    OriginalArguments = new string[] { speciesOrGenus, descriptionOrSpecies },
                    Callback = async (MultistageCommandCallbackArgs args) => {

                        Species[] species = await BotUtils.GetSpeciesFromDb(args.Command.OriginalArguments[0], args.Command.OriginalArguments[1]);

                        if (await BotUtils.ReplyAsync_ValidateSpecies(args.Command.Context, species))
                            await _setSpeciesDescription(species[0], args.MessageContent);

                    }
                };

                await MultistageCommand.SendAsync(p,
                    string.Format("Reply with the description for **{0}**.\nTo cancel the update, reply with \"cancel\".", species_list[0].GetShortName()));

            }

        }
        [Command("setspeciesdesc"), Alias("setspeciesdescription", "setsdesc")]
        public async Task SetSpeciesDescription(string genus, string species, string description) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            await _setSpeciesDescription(sp, description);

        }
        [Command("setspeciescommonname"), Alias("setspeciescommon", "setscommon")]
        private async Task SetSpeciesCommonName(string species, string commonName) {
            await SetSpeciesCommonName("", species, commonName);
        }
        [Command("setspeciescommonname"), Alias("setspeciescommon", "setscommon")]
        private async Task SetSpeciesCommonName(string genus, string species, string commonName) {

            Species species_info = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (species_info is null)
                return;

            if (string.IsNullOrWhiteSpace(commonName)) {

                // If the given common name is empty, erase all common names associated with this species.
                await SpeciesUtils.RemoveCommonNames(species_info);

                await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully removed common name from **{0}**.",
                    species_info.GetShortName()));

            }
            else {

                // Otherwise, add the common name to the database.

                // The difference between this and the "+common" command is that this one overwrites the value stored in the "Species" table.
                // This field is pretty much deprected at this point, but it is still accessed through some generic taxon commands.

                await SpeciesUtils.AddCommonName(species_info, commonName, overwriteSpeciesTable: true);

                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** is now commonly known as the **{1}**.",
                    species_info.GetShortName(),
                    StringUtils.ToTitleCase(commonName)));

            }

        }

        [Command("+commonname"), Alias("+common")]
        private async Task PlusCommonName(string species, string commonName) {
            await PlusCommonName("", species, commonName);
        }
        [Command("+commonname"), Alias("+common")]
        private async Task PlusCommonName(string genus, string species, string commonName) {

            Species species_info = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (species_info is null)
                return;

            if (string.IsNullOrWhiteSpace(commonName)) {

                await BotUtils.ReplyAsync_Error(Context, "Common name cannot be empty.");

            }
            else {

                await SpeciesUtils.AddCommonName(species_info, commonName, overwriteSpeciesTable: false);

                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** is now commonly known as the **{1}**.",
                    species_info.GetShortName(),
                    StringUtils.ToTitleCase(commonName)));

            }

        }
        [Command("-commonname"), Alias("-common")]
        private async Task MinusCommonName(string species, string commonName) {
            await MinusCommonName("", species, commonName);
        }
        [Command("-commonname"), Alias("-common")]
        private async Task MinusCommonName(string genus, string species, string commonName) {

            Species species_info = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (species_info is null)
                return;

            CommonName[] common_names = await SpeciesUtils.GetCommonNamesAsync(species_info);

            if(!common_names.Any(x => x.Value.ToLower() == commonName.ToLower())) {

                // Check if the species actually has this common name before attempting to remove it (for the sake of clarity to the user).

                await BotUtils.ReplyAsync_Warning(Context, string.Format("The common name **{0}** has already been removed.",
                      StringUtils.ToTitleCase(commonName)));

            }
            else {

                await SpeciesUtils.RemoveCommonName(species_info, commonName);

                await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** is no longer known as the **{1}**.",
                    species_info.GetShortName(),
                    StringUtils.ToTitleCase(commonName)));

            }

        }

        private async Task _setSpeciesDescription(Species species, string description) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilegeOrOwnership(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator, species))
                return;

            await BotUtils.UpdateSpeciesDescription(species, description);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated the description for **{0}**.", species.GetShortName()));

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
                await _setTaxonDescription(taxa[0]);

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
                    await BotUtils.ReplyAsync_FindSpecies(Context, "", taxonNameOrGenus);

                else {

                    // Make sure we have one, and only one taxon to update.

                    if (!await BotUtils.ReplyAsync_ValidateTaxa(Context, taxa))
                        return;

                    Taxon taxon = taxa[0];

                    if (taxon.type == TaxonType.Species)

                        // If the taxon is a species, use the species update procedure.
                        await _setSpeciesDescription(await BotUtils.GetSpeciesFromDb(taxon.id), descriptionOrSpecies);

                    else

                        // Update the taxon in the DB.           
                        await BotUtils.Command_SetTaxonDescription(Context, taxon, descriptionOrSpecies);

                }

            }
            else if (await BotUtils.ReplyAsync_ValidateSpecies(Context, species_list)) {

                // A species exists with the given genus/species, so initiate a two-part command.

                // Ensure that the user has necessary privileges to use this command.
                if (!await BotUtils.ReplyAsync_CheckPrivilegeOrOwnership(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator, species_list[0]))
                    return;

                MultistageCommand p = new MultistageCommand(Context) {
                    OriginalArguments = new string[] { taxonNameOrGenus, descriptionOrSpecies },
                    Callback = async (MultistageCommandCallbackArgs args) => {

                        Species[] species = await BotUtils.GetSpeciesFromDb(args.Command.OriginalArguments[0], args.Command.OriginalArguments[1]);

                        if (await BotUtils.ReplyAsync_ValidateSpecies(args.Command.Context, species))
                            await _setSpeciesDescription(species[0], args.MessageContent);

                    }
                };

                await MultistageCommand.SendAsync(p,
                    string.Format("Reply with the description for **{0}**.\nTo cancel the update, reply with \"cancel\".", species_list[0].GetShortName()));

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
            else if (await BotUtils.ReplyAsync_ValidateSpecies(Context, species_list)) {

                // Initialize a multistage update for the given species.

                Species species = species_list[0];

                await _appendDescription(species);

            }

        }
        [Command("appenddescription"), Alias("appenddesc", "+desc", "+description")]
        public async Task AppendDescription(string genus, string species, string description) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            await _appendDescription(sp, description);

        }

        [Command("setcommonname"), Alias("setcommon")]
        public async Task SetCommonName(string taxon, string commonName) {

            // Get all taxa with the specified name.
            Taxon[] taxa = await BotUtils.GetTaxaFromDb(taxon);

            if (taxa.Count() <= 0) {

                // If there is no such taxon, default to showing species suggestions.
                await BotUtils.ReplyAsync_SpeciesSuggestions(Context, "", taxon);

            }
            else if (taxa.Count() == 1 && taxa[0].type == TaxonType.Species) {

                // If there's a single result, and it's a species, use the species-specific update procedure.
                // (The generic one works fine, but this currently shows a unique confirmation message.)
                await SetSpeciesCommonName(taxon, commonName);

            }
            else if (await BotUtils.ReplyAsync_ValidateTaxa(Context, taxa)) {

                // If we got a single taxon, update the common name for that taxon.
                await _setTaxonCommonName(taxa[0], commonName);

            }

        }
        [Command("setcommonname"), Alias("setcommon")]
        public async Task SetCommonName(string genus, string species, string commonName) {
            await SetSpeciesCommonName(genus, species, commonName);
        }

        private async Task _setTaxonDescription(Taxon taxon) {

            if (taxon.type == TaxonType.Species)
                await SetSpeciesDescription(taxon.name);
            else if (await BotUtils.ReplyAsync_CheckPrivilege(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator)) { // moderator use only

                MultistageCommand p = new MultistageCommand(Context) {
                    OriginalArguments = new string[] { taxon.name },
                    Callback = async (MultistageCommandCallbackArgs args) => {

                        await BotUtils.Command_SetTaxonDescription(args.Command.Context, taxon, args.MessageContent);

                    }
                };

                await MultistageCommand.SendAsync(p,
                    string.Format("Reply with the description for {0} **{1}**.\nTo cancel the update, reply with \"cancel\".", taxon.GetTypeName(), taxon.GetName()));

            }

        }
        public async Task _appendDescription(Species species) {

            MultistageCommand p = new MultistageCommand(Context) {
                Callback = async (MultistageCommandCallbackArgs args) => {

                    if (await BotUtils.ReplyAsync_ValidateSpecies(args.Command.Context, species))
                        await _appendDescription(species, args.MessageContent);

                }
            };

            await MultistageCommand.SendAsync(p,
                string.Format("Reply with the text to append to the description for **{0}**.\nTo cancel the update, reply with \"cancel\".", species.GetShortName()));

        }
        public async Task _appendDescription(Species species, string description) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyAsync_CheckPrivilegeOrOwnership(Context, (IGuildUser)Context.User, PrivilegeLevel.ServerModerator, species))
                return;

            // Always start appended test on a new paragraph.
            description = Environment.NewLine + Environment.NewLine + description.Trim();

            // Ensure the decription is of a reasonable size.

            const int MAX_DESCRIPTION_LENGTH = 10000;

            if (species.description.Length + description.Length > MAX_DESCRIPTION_LENGTH) {

                await BotUtils.ReplyAsync_Error(Context, string.Format("The description length exceeds the maximum allowed length ({0} characters).", MAX_DESCRIPTION_LENGTH));

                return;

            }

            // Append text to the existing description.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET description = $description WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", species.id);
                cmd.Parameters.AddWithValue("$description", (species.description + description).Trim());

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated the description for **{0}**.", species.GetShortName()));

        }
        private async Task _setTaxonCommonName(Taxon taxon, string commonName) {
            await BotUtils.Command_SetTaxonCommonName(Context, taxon.type, taxon.name, commonName);
        }

    }

}