using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Utilities;
using OurFoodChain.Extensions;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class RoleModule :
        OfcModuleBase {

        // Public members

        [Command("addrole"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddRole(string roleName, string description = "") {

            Common.Roles.IRole role = new Common.Roles.Role {
                Name = roleName,
                Description = description
            };

            await Db.AddRoleAsync(role);

            await ReplySuccessAsync($"Succesfully created the new role **{roleName.ToTitle()}**.");

        }

        [Command("+role", RunMode = RunMode.Async), Alias("setrole"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetRole(string speciesName, string roleName) {

            await SetRole(string.Empty, speciesName, roleName, string.Empty);

        }
        [Command("+role", RunMode = RunMode.Async), Alias("setrole"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetRole(string genusName, string speciesName, string roleName, string notes = "") {

            // Get the species.

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                // Get the role.

                Common.Roles.IRole role = await Db.GetRoleAsync(roleName);

                if (await this.ReplyValidateRoleAsync(role)) {

                    // Update the species.

                    role.Notes = notes;

                    await Db.AddRoleAsync(species, role);

                    await ReplySuccessAsync($"{species.GetShortName().ToBold()} has successfully been assigned the role of {role.GetName().ToBold()}.");

                }

            }

        }

        [Command("-role", RunMode = RunMode.Async), Alias("unsetrole"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task RemoveRole(string speciesName, string roleName) {

            await RemoveRole(string.Empty, speciesName, roleName);

        }
        [Command("-role", RunMode = RunMode.Async), Alias("unsetrole"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task RemoveRole(string genusName, string speciesName, string roleName) {

            // Get the species.

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                // Get the role.

                Common.Roles.IRole role = await Db.GetRoleAsync(roleName);

                if (await this.ReplyValidateRoleAsync(role)) {

                    // Update the species.

                    await Db.RemoveRoleAsync(species, role);

                    await ReplySuccessAsync($"Role {role.GetName().ToBold()} has successfully been unassigned from {species.GetShortName().ToBold()}.");

                }

            }

        }

        [Command("roles"), Alias("role")]
        public async Task Roles() {

            IEnumerable<Common.Roles.IRole> roles = await Db.GetRolesAsync();

            Discord.Messaging.IEmbed embed = new Discord.Messaging.Embed {
                Title = $"All roles ({roles.Count()})"
            };

            foreach (Common.Roles.IRole role in roles) {

                long count = await Db.GetSpeciesCountAsync(role);

                embed.AddField($"{role.GetName()} ({count})", role.GetShortDescription());

            }

            await ReplyAsync(embed);

        }
        [Command("roles", RunMode = RunMode.Async), Alias("role")]
        public async Task Roles(string arg0) {

            // Possible cases:
            // 1. <role>
            // 2. <species>

            // If a role with this name exists, that's what we'll prioritize (users can use the genus + species overload if they need to).
            // If no such role exists, check for a species with this name instead.

            Common.Roles.IRole role = await Db.GetRoleAsync(arg0);

            if (role.IsValid()) {

                // The role is valid.
                // List all extant species with this role.

                IEnumerable<ISpecies> species = (await Db.GetSpeciesAsync(role))
                    .Where(s => !s.IsExtinct())
                    .OrderBy(s => s.GetShortName());

                IEnumerable<Discord.Messaging.IEmbed> pages =
                    EmbedUtilities.CreateEmbedPages($"Extant species with this role ({species.Count()}):", species, options: EmbedPaginationOptions.AddPageNumbers);

                foreach (Discord.Messaging.IEmbed page in pages) {

                    page.Title = $"Role: {role.GetName()}";
                    page.Description = role.GetDescriptionOrDefault();

                }

                await ReplyAsync(new Discord.Messaging.PaginatedMessage(pages));

            }
            else {

                // The role is not valid.

                IEnumerable<ISpecies> matchingSpecies = await Db.GetSpeciesAsync(string.Empty, arg0);

                if (matchingSpecies.Count() > 0) {

                    ISpecies species = await ReplyValidateSpeciesAsync(matchingSpecies);

                    if (species.IsValid())
                        await ReplyRolesAsync(matchingSpecies.First());

                }
                else {

                    // There were no matching species, so just say that the role is invalid.

                    await this.ReplyValidateRoleAsync(role);

                }

            }

        }
        [Command("roles", RunMode = RunMode.Async), Alias("role")]
        public async Task Roles(string genusName, string speciesName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid())
                await ReplyRolesAsync(species);

        }

        [Command("setroledescription"), Alias("setroledesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetRoleDescription(string roleName, string description) {

            Common.Roles.IRole role = await this.GetRoleOrReplyAsync(roleName);

            if (role.IsValid()) {

                role.Description = description;

                await Db.UpdateRoleAsync(role);

                await ReplySuccessAsync($"Successfully updated description for role {role.GetName().ToBold()}.");

            }

        }

        // Private members

        private async Task ReplyRolesAsync(ISpecies species) {

            // Get the role(s) assigned to this species.

            IEnumerable<Common.Roles.IRole> roles = await Db.GetRolesAsync(species);

            if (roles.Count() > 0) {

                // Display the role(s) to the user.

                StringBuilder lines = new StringBuilder();

                foreach (Common.Roles.IRole role in roles) {

                    lines.Append(role.GetName());

                    if (!string.IsNullOrEmpty(role.Notes))
                        lines.Append(string.Format(" ({0})", role.Notes));

                    lines.AppendLine();

                }

                Discord.Messaging.IEmbed embed = new Discord.Messaging.Embed {
                    Title = $"{species.GetShortName()}'s role(s) ({roles.Count()})",
                    Description = lines.ToString()
                };

                await ReplyAsync(embed);

            }
            else {

                await ReplyInfoAsync($"{species.GetShortName().ToBold()} has not been assigned any roles.");

            }

        }

    }

}