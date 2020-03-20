using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    public class TaxaModule :
        OfcModuleBase {

        // Public members

        // Genus

        [Command("genus", RunMode = RunMode.Async), Alias("g", "genera")]
        public async Task Genus(string taxonName = "") {

            await ReplyTaxonOrTypeAsync(TaxonRankType.Genus, taxonName);

        }

        [Command("addgenus", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddGenus(string taxonName, string description = "") {

            await ReplyAddTaxonAsync(TaxonRankType.Genus, taxonName, description);

        }

        [Command("setgenus", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetGenus(string speciesName, string newGenusName) {

            await SetGenus(string.Empty, speciesName, newGenusName);

        }

        [Command("setgenus", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetGenus(string genusName, string speciesName, string newGenusName) {

            // Get the specified species.

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                // Get the specified genus.

                ITaxon genus = await GetTaxonOrReplyAsync(TaxonRankType.Genus, newGenusName);

                if (genus.IsValid()) {

                    // Update the species.

                    species.Genus = genus;

                    await Db.UpdateSpeciesAsync(species);

                    await ReplySuccessAsync($"**{species.GetShortName()}** has successfully been assigned to the genus **{genus.GetName().ToTitle()}**.");

                }

            }

        }

        [Command("setgenusdescription", RunMode = RunMode.Async), Alias("setgenusdesc", "setgdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetGenusDescription(string taxonName) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Genus, taxonName);

        }

        [Command("setgenusdescription", RunMode = RunMode.Async), Alias("setgenusdesc", "setgdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetGenusDescription(string taxonName, string description) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Genus, taxonName, description);

        }

        [Command("setgenuspic", RunMode = RunMode.Async), Alias("setgpic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetGenusPic(string taxonName, string imageUrl) {

            await ReplySetTaxonPictureAsync(TaxonRankType.Genus, taxonName, imageUrl);

        }

        [Command("setgenuscommonname", RunMode = RunMode.Async), Alias("setgenuscommon", "setgcommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetGenusCommonName(string taxonName, string commonName) {

            await ReplySetTaxonCommonNameAsync(TaxonRankType.Genus, taxonName, commonName);

        }

        [Command("deletegenus", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteGenus(string taxonName) {

            await ReplyDeleteTaxonAsync(TaxonRankType.Genus, taxonName);

        }

        // Family

        [Command("family", RunMode = RunMode.Async), Alias("f", "families")]
        public async Task Family(string taxonName = "") {

            await ReplyTaxonOrTypeAsync(TaxonRankType.Family, taxonName);

        }

        [Command("addfamily", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddFamily(string taxonName, string description = "") {

            await ReplyAddTaxonAsync(TaxonRankType.Family, taxonName, description);

        }

        [Command("setfamily", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetFamily(string childTaxonName, string parentTaxonName) {

            await ReplySetTaxonParentAsync(TaxonRankType.Family, childTaxonName, parentTaxonName);

        }

        [Command("setfamilydesc", RunMode = RunMode.Async), Alias("setfamilydescription", "setfdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetFamilyDescription(string taxonName) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Family, taxonName);

        }

        [Command("setfamilydesc", RunMode = RunMode.Async), Alias("setfamilydescription", "setfdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetFamilyDescription(string taxonName, string description) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Family, taxonName, description);

        }

        [Command("setfamilypic", RunMode = RunMode.Async), Alias("setfpic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetFamilyPic(string taxonName, string imageUrl) {

            await ReplySetTaxonPictureAsync(TaxonRankType.Family, taxonName, imageUrl);

        }

        [Command("setfamilycommonname", RunMode = RunMode.Async), Alias("setfamilycommon", "setfcommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetFamilyCommonName(string taxonName, string commonName) {

            await ReplySetTaxonCommonNameAsync(TaxonRankType.Family, taxonName, commonName);

        }

        [Command("deletefamily", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteFamily(string taxonName) {

            await ReplyDeleteTaxonAsync(TaxonRankType.Family, taxonName);

        }

        // Order

        [Command("order", RunMode = RunMode.Async), Alias("o", "orders")]
        public async Task Order(string taxonName = "") {

            await ReplyTaxonOrTypeAsync(TaxonRankType.Order, taxonName);

        }

        [Command("addorder", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddOrder(string taxonName, string description = "") {

            await ReplyAddTaxonAsync(TaxonRankType.Order, taxonName, description);

        }

        [Command("setorder", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOrder(string childTaxonName, string parentTaxonName) {

            await ReplySetTaxonParentAsync(TaxonRankType.Order, childTaxonName, parentTaxonName);

        }

        [Command("setorderdesc", RunMode = RunMode.Async), Alias("setorderdescription", "setodesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOrderDescription(string taxonName) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Order, taxonName);

        }

        [Command("setorderdesc", RunMode = RunMode.Async), Alias("setorderdescription", "setodesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOrderDescription(string taxonName, string description) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Order, taxonName, description);

        }

        [Command("setorderpic", RunMode = RunMode.Async), Alias("setopic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOrderPic(string taxonName, string imageUrl) {

            await ReplySetTaxonPictureAsync(TaxonRankType.Order, taxonName, imageUrl);

        }

        [Command("setordercommonname", RunMode = RunMode.Async), Alias("setordercommon", "setocommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOrderCommonName(string taxonName, string commonName) {

            await ReplySetTaxonCommonNameAsync(TaxonRankType.Order, taxonName, commonName);

        }

        [Command("deleteorder", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteOrder(string taxonName) {

            await ReplyDeleteTaxonAsync(TaxonRankType.Order, taxonName);

        }

        // Class

        [Command("class", RunMode = RunMode.Async), Alias("c", "classes")]
        public async Task Class(string taxonName = "") {

            await ReplyTaxonOrTypeAsync(TaxonRankType.Class, taxonName);

        }

        [Command("addclass", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddClass(string taxonName, string description = "") {

            await ReplyAddTaxonAsync(TaxonRankType.Class, taxonName, description);

        }

        [Command("setclass", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetClass(string childTaxonName, string parentTaxonName) {

            await ReplySetTaxonParentAsync(TaxonRankType.Class, childTaxonName, parentTaxonName);

        }

        [Command("setclassdesc", RunMode = RunMode.Async), Alias("setclassdescription", "setcdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetClassDescription(string taxonName) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Class, taxonName);

        }

        [Command("setclassdesc", RunMode = RunMode.Async), Alias("setclassdescription", "setcdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetClassDescription(string taxonName, string description) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Class, taxonName, description);

        }

        [Command("setclasspic", RunMode = RunMode.Async), Alias("setcpic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetClassPic(string taxonName, string imageUrl) {

            await ReplySetTaxonPictureAsync(TaxonRankType.Class, taxonName, imageUrl);

        }

        [Command("setclasscommonname", RunMode = RunMode.Async), Alias("setclasscommon", "setccommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetClassCommonName(string taxonName, string commonName) {

            await ReplySetTaxonCommonNameAsync(TaxonRankType.Class, taxonName, commonName);

        }

        [Command("deleteclass", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteClass(string taxonName) {

            await ReplyDeleteTaxonAsync(TaxonRankType.Class, taxonName);

        }

        // Phylum

        [Command("phylum", RunMode = RunMode.Async), Alias("p", "phyla")]
        public async Task Phylum(string taxonName = "") {

            await ReplyTaxonOrTypeAsync(TaxonRankType.Phylum, taxonName);

        }

        [Command("addphylum", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddPhylum(string taxonName, string description = "") {

            await ReplyAddTaxonAsync(TaxonRankType.Phylum, taxonName, description);

        }

        [Command("setphylum", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetPhylum(string childTaxonName, string parentTaxonName) {

            await ReplySetTaxonParentAsync(TaxonRankType.Phylum, childTaxonName, parentTaxonName);

        }

        [Command("setphylumdesc", RunMode = RunMode.Async), Alias("setphylumdescription", "setpdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetPhylumDescription(string taxonName) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Phylum, taxonName);

        }

        [Command("setphylumdesc", RunMode = RunMode.Async), Alias("setphylumdescription", "setpdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetPhylumDescription(string taxonName, string description) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Phylum, taxonName, description);

        }

        [Command("setphylumpic", RunMode = RunMode.Async), Alias("setppic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetPhylumPic(string taxonName, string imageUrl) {

            await ReplySetTaxonPictureAsync(TaxonRankType.Phylum, taxonName, imageUrl);

        }

        [Command("setphylumcommonname", RunMode = RunMode.Async), Alias("setphylumcommon", "setpcommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetPhylumCommonName(string taxonName, string commonName) {

            await ReplySetTaxonCommonNameAsync(TaxonRankType.Phylum, taxonName, commonName);

        }

        [Command("deletephylum", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeletePhylum(string taxonName) {

            await ReplyDeleteTaxonAsync(TaxonRankType.Phylum, taxonName);

        }

        // Kingdom

        [Command("kingdom", RunMode = RunMode.Async), Alias("k", "kingdoms")]
        public async Task Kingdom(string taxonName = "") {

            await ReplyTaxonOrTypeAsync(TaxonRankType.Kingdom, taxonName);

        }

        [Command("addkingdom", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddKingdom(string taxonName, string description = "") {

            await ReplyAddTaxonAsync(TaxonRankType.Kingdom, taxonName, description);

        }

        [Command("setkingdom", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetKingdom(string childTaxonName, string parentTaxonName) {

            await ReplySetTaxonParentAsync(TaxonRankType.Kingdom, childTaxonName, parentTaxonName);

        }

        [Command("setkingdomdesc", RunMode = RunMode.Async), Alias("setkingdomdescription", "setkdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetKingdomDescription(string taxonName) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Kingdom, taxonName);

        }

        [Command("setkingdomdesc", RunMode = RunMode.Async), Alias("setkingdomdescription", "setkdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetKingdomDescription(string taxonName, string description) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Kingdom, taxonName, description);

        }

        [Command("setkingdompic", RunMode = RunMode.Async), Alias("setkpic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetKingdomPic(string taxonName, string imageUrl) {

            await ReplySetTaxonPictureAsync(TaxonRankType.Kingdom, taxonName, imageUrl);

        }

        [Command("setkingdomcommonname", RunMode = RunMode.Async), Alias("setkingdomcommon", "setkcommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetKingdomCommonName(string taxonName, string commonName) {

            await ReplySetTaxonCommonNameAsync(TaxonRankType.Kingdom, taxonName, commonName);

        }

        [Command("deletekingdom", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteKingdom(string taxonName) {

            await ReplyDeleteTaxonAsync(TaxonRankType.Kingdom, taxonName);

        }

        // Domain

        [Command("domain", RunMode = RunMode.Async), Alias("d", "domains")]
        public async Task Domain(string taxonName = "") {

            await ReplyTaxonOrTypeAsync(TaxonRankType.Domain, taxonName);

        }

        [Command("adddomain", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddDomain(string taxonName, string description = "") {

            await ReplyAddTaxonAsync(TaxonRankType.Domain, taxonName, description);

        }

        [Command("setdomain", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetDomain(string childTaxonName, string parentTaxonName) {

            await ReplySetTaxonParentAsync(TaxonRankType.Domain, childTaxonName, parentTaxonName);

        }

        [Command("setdomaindesc", RunMode = RunMode.Async), Alias("setdomaindescription", "setddesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetDomainDescription(string taxonName) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Domain, taxonName);

        }

        [Command("setdomaindesc", RunMode = RunMode.Async), Alias("setdomaindescription", "setddesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetDomainDescription(string taxonName, string description) {

            await ReplySetTaxonDescriptionAsync(TaxonRankType.Domain, taxonName, description);

        }

        [Command("setdomainpic", RunMode = RunMode.Async), Alias("setdpic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetDomainPic(string taxonName, string imageUrl) {

            await ReplySetTaxonPictureAsync(TaxonRankType.Domain, taxonName, imageUrl);

        }

        [Command("setdomaincommonname", RunMode = RunMode.Async), Alias("setdomaincommon", "setdcommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetDomainCommon(string taxonName, string commonName) {

            await ReplySetTaxonCommonNameAsync(TaxonRankType.Domain, taxonName, commonName);

        }

        [Command("deletedomain", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteDomain(string taxonName) {

            await ReplyDeleteTaxonAsync(TaxonRankType.Domain, taxonName);

        }

        // Species

        [Command("setspeciesdesc", RunMode = RunMode.Async), Alias("setspeciesdescription", "setsdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetSpeciesDescription(string speciesName) {

            await SetSpeciesDescription(string.Empty, speciesName);

        }
        [Command("setspeciesdesc", RunMode = RunMode.Async), Alias("setspeciesdescription", "setsdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetSpeciesDescription(string arg0, string arg1) {

            // Possible cases:
            // 1. <genus> <species>
            // 2. <species> <description>

            IEnumerable<ISpecies> matchingSpecies = await Db.GetSpeciesAsync(arg0, arg1);

            if (matchingSpecies.Count() <= 0) {

                // We either have case (1) and the species does not exist, or case (2).

                ISpecies species = await GetSpeciesOrReplyAsync(string.Empty, arg0);

                if (species.IsValid()) {

                    // The first argument was a valid species name, so we have case (2).

                    await ReplySetTaxonDescriptionAsync(species, arg1);

                }

            }
            else {

                ISpecies species = await ReplyValidateSpeciesAsync(matchingSpecies);

                if (species.IsValid()) {

                    // We have case (1).

                    await ReplySetTaxonDescriptionAsync(species);

                }

            }

        }
        [Command("setspeciesdesc", RunMode = RunMode.Async), Alias("setspeciesdescription", "setsdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetSpeciesDescription(string genusName, string speciesName, string description) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid())
                await ReplySetTaxonDescriptionAsync(species, description);

        }

        [Command("setspeciescommonname", RunMode = RunMode.Async), Alias("setspeciescommon", "setscommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetSpeciesCommonName(string speciesName, string commonName) {

            await SetSpeciesCommonName(string.Empty, speciesName, commonName);

        }
        [Command("setspeciescommonname", RunMode = RunMode.Async), Alias("setspeciescommon", "setscommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetSpeciesCommonName(string genusName, string speciesName, string commonName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                if (string.IsNullOrWhiteSpace(commonName)) {

                    // If the given common name is empty, erase all common names associated with this species.

                    species.CommonNames.Clear();

                    await Db.UpdateSpeciesAsync(species);

                    await ReplySuccessAsync($"Successfully removed all common names from **{species.GetShortName()}**.");

                }
                else {

                    // Otherwise, add the common name to the database.
                    // The difference between this and the "+common" command is that this one overwrites the value stored in the "Species" table with the first common name.
                    // This field is deprected at this point.

                    List<string> commonNames = new List<string> {
                        commonName
                    };

                    commonNames.AddRange(species.CommonNames);

                    species.CommonNames.Clear();
                    species.CommonNames.AddRange(commonNames);

                    await Db.UpdateSpeciesAsync(species);

                    await ReplySuccessAsync($"**{species.GetShortName()}** is now commonly known as the **{species.GetCommonName().ToTitle()}**.");

                }

            }

        }

        [Command("+commonname", RunMode = RunMode.Async), Alias("+common"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task PlusCommonName(string speciesName, string commonName) {

            await PlusCommonName(string.Empty, speciesName, commonName);

        }
        [Command("+commonname", RunMode = RunMode.Async), Alias("+common"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task PlusCommonName(string genusName, string speciesName, string commonName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (string.IsNullOrWhiteSpace(commonName)) {

                await ReplyErrorAsync("Common name cannot be empty.");

            }
            else if (species.IsValid()) {

                if (species.CommonNames.Any(name => name.Equals(commonName, StringComparison.OrdinalIgnoreCase))) {

                    await ReplyWarningAsync($"{species.GetShortName().ToBold()} is already known as the {species.CommonNames.Last().ToTitle().ToBold()}.");

                }
                else {

                    species.CommonNames.Add(commonName);

                    await Db.UpdateSpeciesAsync(species);

                    await ReplySuccessAsync($"{species.GetShortName().ToBold()} is now commonly known as the {species.CommonNames.Last().ToTitle().ToBold()}.");

                }

            }

        }

        [Command("-commonname", RunMode = RunMode.Async), Alias("-common"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusCommonName(string speciesName, string commonName) {

            await MinusCommonName(string.Empty, speciesName, commonName);

        }
        [Command("-commonname", RunMode = RunMode.Async), Alias("-common"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusCommonName(string genusName, string speciesName, string commonName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                if (!species.CommonNames.Any(name => name.Equals(commonName, StringComparison.OrdinalIgnoreCase))) {

                    // The species does not have the given common name.

                    await ReplyWarningAsync($"{species.GetShortName().ToBold()} does not have the common name {commonName.ToTitle().ToBold()}.");

                }
                else {

                    // Remove the common name.

                    species.CommonNames.Remove(species.CommonNames.Where(name => name.Equals(commonName, StringComparison.OrdinalIgnoreCase)).First());

                    await Db.UpdateSpeciesAsync(species);

                    await ReplySuccessAsync($"**{species.GetShortName()}** is no longer known as the **{commonName.ToTitle()}**.");

                }

            }

        }

        // Generic

        [Command("setdescription", RunMode = RunMode.Async), Alias("setdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetTaxonDescription(string taxonName) {

            IEnumerable<ITaxon> taxa = await Db.GetTaxaAsync(taxonName);

            if (taxa.Count() <= 0) {

                // We did not get any matching taxa.
                // In this case, show species suggestions.

                // If there is no such taxon, default to showing species suggestions.

                ISpecies species = await ReplySpeciesSuggestionAsync(string.Empty, taxonName);

                if (species.IsValid())
                    await ReplySetTaxonDescriptionAsync(species);

            }
            else {

                // We got one or more matching taxa.

                ITaxon taxon = await ReplyValidateTaxaAsync(taxa);

                if (taxon.IsValid())
                    await ReplySetTaxonDescriptionAsync(taxon);

            }

        }
        [Command("setdescription", RunMode = RunMode.Async), Alias("setdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetTaxonDescription(string arg0, string arg1) {

            // Possible cases:
            // 1. <taxon> <description>
            // 2. <genus> <species>

            IEnumerable<ISpecies> matchingSpecies = await Db.GetSpeciesAsync(arg0, arg1);

            if (matchingSpecies.Count() <= 0) {

                // No such species exists, so we have case (1). Note that this might still be a species (<species> <description>).

                IEnumerable<ITaxon> taxa = await Db.GetTaxaAsync(arg0);

                if (taxa.Count() <= 0) {

                    // If there are no matching taxa, show species suggestions.

                    ISpecies species = await ReplySpeciesSuggestionAsync(string.Empty, arg0);

                    if (species.IsValid())
                        await ReplySetTaxonDescriptionAsync(species, arg1);

                }
                else {

                    // There is at least one matching taxon.

                    ITaxon taxon = await ReplyValidateTaxaAsync(taxa);

                    if (taxon.IsValid())
                        await ReplySetTaxonDescriptionAsync(taxon, arg1);

                }

            }
            else {

                // There is at least one matching species.

                ISpecies species = await ReplyValidateSpeciesAsync(matchingSpecies);

                if (species.IsValid())
                    await ReplySetTaxonDescriptionAsync(species);

            }

        }
        [Command("setdescription", RunMode = RunMode.Async), Alias("setdesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetTaxonDescription(string genusName, string speciesName, string description) {

            await SetSpeciesDescription(genusName, speciesName, description);

        }

        [Command("appenddescription", RunMode = RunMode.Async), Alias("appenddesc", "+desc", "+description"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AppendDescription(string speciesName) {

            await AppendDescription(string.Empty, speciesName);

        }
        [Command("appenddescription", RunMode = RunMode.Async), Alias("appenddesc", "+desc", "+description"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AppendDescription(string arg0, string arg1) {

            // Possible cases:
            // 1. <genus> <species>
            // 2. <species> <description>

            IEnumerable<ISpecies> matchingSpecies = await Db.GetSpeciesAsync(arg0, arg1);

            if (matchingSpecies.Count() <= 0) {

                // We did not get any matching species, so we must have case (2) (or invalid genus/species).

                await AppendDescription(string.Empty, arg0, arg1);

            }
            else {

                // We got at least one matching species, so we have case (1).

                ISpecies species = await ReplyValidateSpeciesAsync(matchingSpecies);

                if (species.IsValid())
                    await ReplyAppendDescriptionAsync(species);

            }

        }
        [Command("appenddescription", RunMode = RunMode.Async), Alias("appenddesc", "+desc", "+description"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AppendDescription(string genusName, string speciesName, string description) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid())
                await ReplyAppendDescriptionAsync(species, description);

        }

        [Command("setcommonname", RunMode = RunMode.Async), Alias("setcommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetCommonName(string taxonName, string commonName) {

            IEnumerable<ITaxon> taxa = await Db.GetTaxaAsync(taxonName);

            if (taxa.Count() <= 0) {

                // We did not get any matching taxa.

                ISpecies species = await ReplySpeciesSuggestionAsync(string.Empty, taxonName);

                if (species.IsValid())
                    await SetSpeciesCommonName(string.Empty, taxonName);

            }
            else {

                // We got at least one matching taxon.

                ITaxon taxon = await ReplyValidateTaxaAsync(taxa);

                if (taxon.IsValid()) {

                    if (taxon.GetRank() == TaxonRankType.Species)
                        await SetSpeciesCommonName(taxonName, commonName); // species are handled differently
                    else
                        await ReplySetTaxonCommonNameAsync(taxon, commonName);

                }

            }

        }
        [Command("setcommonname", RunMode = RunMode.Async), Alias("setcommon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetCommonName(string genusName, string speciesName, string commonName) {

            await SetSpeciesCommonName(genusName, speciesName, commonName);

        }

        [Command("delete", RunMode = RunMode.Async), Alias("deletetaxon"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task DeleteTaxon(string taxonName) {

            await ReplyDeleteTaxonAsync(taxonName);

        }

        // Private members

        private async Task ReplyTaxonOrTypeAsync(TaxonRankType rank, string taxonName) {

            if (string.IsNullOrWhiteSpace(taxonName))
                await ReplyTaxonAsync(rank);
            else {

                ITaxon taxon = await GetTaxonOrReplyAsync(rank, taxonName);

                if (taxon.IsValid())
                    await ReplyTaxonAsync(taxon);
            }

        }

        private async Task ReplyAddTaxonAsync(TaxonRankType rank, string taxonName, string description) {

            // Make sure that the taxon does not already exist before trying to add it.

            ITaxon taxon = (await Db.GetTaxaAsync(taxonName, rank)).FirstOrDefault();

            if (taxon.IsValid()) {

                await ReplyWarningAsync($"The {rank.GetName()} **{taxon.GetName()}** already exists.");

            }
            else {

                taxon = new Common.Taxa.Taxon(rank, taxonName) {
                    Name = taxonName,
                    Description = description
                };

                await Db.AddTaxonAsync(taxon);

                await ReplySuccessAsync($"Successfully created new {rank.GetName()}, **{taxon.GetName()}**.");

            }

        }

        private async Task ReplySetTaxonDescriptionAsync(TaxonRankType rank, string taxonName, string description) {

            ITaxon taxon = await GetTaxonOrReplyAsync(rank, taxonName);

            if (taxon.IsValid())
                await ReplySetTaxonDescriptionAsync(taxon, description);

        }
        private async Task ReplySetTaxonDescriptionAsync(ITaxon taxon, string description) {

            if (taxon.IsValid()) {

                taxon.Description = description;

                await Db.UpdateTaxonAsync(taxon);

                string name = (taxon is ISpecies species) ? species.GetShortName() : taxon.GetName();

                await ReplySuccessAsync($"Successfully updated description for {taxon.GetRank().GetName()} **{name}**.");

            }

        }
        private async Task ReplySetTaxonDescriptionAsync(TaxonRankType rank, string taxonName) {

            ITaxon taxon = await GetTaxonOrReplyAsync(rank, taxonName);

            if (taxon.IsValid())
                await ReplySetTaxonDescriptionAsync(taxon);

        }
        private async Task ReplySetTaxonDescriptionAsync(ITaxon taxon) {

            if (taxon.IsValid()) {

                string name = (taxon is ISpecies species) ? species.GetFullName() : taxon.GetName();

                IMessage message = new Message($"Reply with the description for {taxon.GetRank().GetName()} **{name}**.");
                IResponsiveMessageResponse response = await ResponsiveMessageService.GetResponseAsync(Context, message);

                if (!response.Canceled)
                    await ReplySetTaxonDescriptionAsync(taxon, await GetDescriptionFromMessageAsync(response.Message));

            }

        }

        private async Task ReplySetTaxonPictureAsync(TaxonRankType rank, string taxonName, string imageUrl) {

            ITaxon taxon = await GetTaxonOrReplyAsync(rank, taxonName);

            if (taxon.IsValid() && await ReplyValidateImageUrlAsync(imageUrl)) {

                taxon.Pictures.Clear();
                taxon.Pictures.Add(new Picture(imageUrl));

                await Db.UpdateTaxonAsync(taxon);

                await ReplySuccessAsync($"Successfully set the picture for for {taxon.GetRank().GetName()} **{taxon.GetName().ToTitle()}**.");

            }

        }

        private async Task ReplySetTaxonCommonNameAsync(TaxonRankType rank, string taxonName, string commonName) {

            ITaxon taxon = await GetTaxonOrReplyAsync(rank, taxonName);

            if (taxon.IsValid())
                await ReplySetTaxonCommonNameAsync(taxon, commonName);

        }
        private async Task ReplySetTaxonCommonNameAsync(ITaxon taxon, string commonName) {

            if (taxon.IsValid()) {

                taxon.CommonNames.Clear();
                taxon.CommonNames.Add(commonName);

                await Db.UpdateTaxonAsync(taxon);

                if (taxon is ISpecies species) {

                    await ReplySuccessAsync($"{species.GetShortName().ToBold()} is now commonly known as the {species.GetCommonName().ToTitle().ToBold()}.");

                }
                else {

                    await ReplySuccessAsync($"Members of the {taxon.GetRank().GetName()} {taxon.GetName().ToTitle().ToBold()} are now commonly known as {taxon.GetCommonName().ToTitle().ToBold()}.");

                }

            }

        }

        private async Task ReplySetTaxonParentAsync(TaxonRankType rank, string childTaxonName, string parentTaxonName) {

            ITaxon child = await GetTaxonOrReplyAsync(rank.GetChildRank(), childTaxonName);
            ITaxon parent = child.IsValid() ? await GetTaxonOrReplyAsync(rank, parentTaxonName) : null;

            if (child.IsValid() && parent.IsValid()) {

                child.ParentId = parent.Id;

                await Db.UpdateTaxonAsync(child);

                await ReplySuccessAsync($"{child.GetRank().GetName().ToSentence()} **{child.GetName().ToTitle()}** has sucessfully been placed under the {parent.GetRank().GetName()} **{parent.GetName().ToTitle()}**.");

            }

        }

        private async Task ReplyDeleteTaxonAsync(TaxonRankType rank, string taxonName) {

            ITaxon taxon = await GetTaxonOrReplyAsync(rank, taxonName);

            if (taxon.IsValid())
                await ReplyDeleteTaxonAsync(taxon);

        }
        private async Task ReplyDeleteTaxonAsync(string taxonName) {

            ITaxon taxon = await GetTaxonOrReplyAsync(taxonName);

            if (taxon.IsValid())
                await ReplyDeleteTaxonAsync(taxon);

        }
        private async Task ReplyDeleteTaxonAsync(ITaxon taxon) {

            if (taxon.IsValid()) {

                if (taxon.GetRank() != TaxonRankType.Species && (await Db.GetSpeciesAsync(taxon)).Count() > 0) {

                    // If the taxon still has species underneath of it, don't allow it to be deleted.

                    await ReplyErrorAsync("Taxa containing species cannot be deleted.");

                }
                else if (taxon.GetRank() != TaxonRankType.Species) {

                    // The taxon is empty, so delete the taxon.

                    await Db.DeleteTaxonAsync(taxon);

                    string name = (taxon is ISpecies species) ? species.GetShortName() : taxon.GetName();

                    await ReplySuccessAsync($"{taxon.GetRank().GetName().ToSentence()} **{name}** was successfully deleted.");

                }
                else {

                    // Species cannot currently be deleted through the bot.

                    await ReplyErrorAsync("Species cannot currently be deleted.");

                }

            }

        }

        private async Task ReplyAppendDescriptionAsync(ISpecies species) {

            if (species.IsValid()) {

                IMessage message = new Message($"Reply with the description for {species.GetRank().GetName()} **{species.GetFullName()}**.");
                IResponsiveMessageResponse response = await ResponsiveMessageService.GetResponseAsync(Context, message);

                if (!response.Canceled)
                    await ReplyAppendDescriptionAsync(species, await GetDescriptionFromMessageAsync(response.Message));

            }

        }
        private async Task ReplyAppendDescriptionAsync(ISpecies species, string append) {

            const int maxLength = 10000;

            if (species.IsValid()) {

                StringBuilder sb = new StringBuilder(species.Description.SafeTrim());

                // Add new text as a new paragraph.

                sb.AppendLine();
                sb.AppendLine();
                sb.Append(append.SafeTrim());

                if (sb.Length > maxLength) {

                    // The descrption exceeds the maximum description length.

                    await ReplyErrorAsync($"The description length exceeds the maximum allowed length ({maxLength} characters).");

                }
                else {

                    await ReplySetTaxonDescriptionAsync(species, sb.ToString());

                }


            }

        }

        private async Task<string> GetDescriptionFromMessageAsync(IMessage message) {

            // The user can either provide a message or text attachment.

            string descriptionText = message.Text;

            if (message.Attachments.Any())
                descriptionText = await DiscordUtilities.DownloadTextAttachmentAsync(message.Attachments.First());

            return descriptionText;

        }

    }

}