using Discord;
using Discord.Commands;
using OurFoodChain.Adapters;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Extensions;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class RoleCommands :
        OfcModuleBase {

        [Command("addrole"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddRole(string roleName, string description = "") {

            Common.Roles.IRole role = new Common.Roles.Role {
                Name = roleName,
                Description = description
            };

            await Db.AddRoleAsync(role);

            await ReplySuccessAsync($"Succesfully created the new role **{roleName.ToTitle()}**.");

        }

        [Command("+role"), Alias("setrole"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetRole(string speciesName, string roleName) {

            await SetRole(string.Empty, speciesName, roleName, string.Empty);

        }
        [Command("+role"), Alias("setrole"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
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

        [Command("-role"), Alias("unsetrole"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task RemoveRole(string speciesName, string roleName) {

            await RemoveRole(string.Empty, speciesName, roleName);

        }
        [Command("-role"), Alias("unsetrole"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
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

            Discord.Messaging.IEmbed embed = new Discord.Messaging.Embed();

            embed.Title = $"All roles ({roles.Count()})";

            foreach (Common.Roles.IRole role in roles) {

                long count = 0;

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT count(*) FROM SpeciesRoles WHERE species_id NOT IN (SELECT species_id FROM Extinctions) AND role_id = $role_id")) {

                    cmd.Parameters.AddWithValue("$role_id", role.id);

                    count = (await Db.GetRowAsync(cmd)).Field<long>("count(*)");

                }

                string title = string.Format("{0} ({1})",
                    StringUtilities.ToTitleCase(role.name),
                    count);

                embed.AddField(title, role.GetShortDescription());

            }

            await ReplyAsync("", false, embed.Build());

        }
        [Command("roles"), Alias("role")]
        public async Task Roles(string nameOrSpecies) {

            // If a role with this name exists, that's what we'll prioritize (users can use the genus + species overload if they need to).
            // If no such role exists, check for a species with this name instead.

            Common.Roles.Role role = await Db.GetRoleAsync(nameOrSpecies);

            if (role is null) {

                // No such role exists, so check if a species exists with the given name instead.

                Species[] matching_species = await BotUtils.GetSpeciesFromDb("", nameOrSpecies);

                if (matching_species.Count() == 1)

                    // If only one species was returned, show the roles assigned to that species.
                    await Roles(matching_species[0]);

                else if (matching_species.Count() > 1)

                    // If multiple species were returned, provide a list of matching species for the user to choose from.
                    await BotUtils.ReplyValidateSpeciesAsync(Context, matching_species);

                if (matching_species.Count() > 0)
                    return;

            }

            // If we got here, the role is eiher not null, or it is null, but no species with the given name exists.
            // In this case, proceed to validate the role, and display its information if possible.

            if (!await BotUtils.ReplyAsync_ValidateRole(Context, role))
                return;

            // List all extant species with this role.

            List<Species> species_list = new List<Species>(await BotUtils.GetSpeciesFromDbByRole(role));

            species_list.RemoveAll(x => x.IsExtinct);
            species_list.Sort((lhs, rhs) => lhs.ShortName.CompareTo(rhs.ShortName));

            Bot.PaginatedMessageBuilder embed = new Bot.PaginatedMessageBuilder(EmbedUtils.SpeciesListToEmbedPages(species_list.Select(s => new SpeciesAdapter(s)),
                fieldName: string.Format("Extant species with this role ({0}):", species_list.Count())));

            embed.SetTitle(string.Format("Role: {0}", StringUtilities.ToTitleCase(role.name)));
            embed.SetDescription(role.GetDescriptionOrDefault());

            await Bot.DiscordUtils.SendMessageAsync(Context, embed.Build());

        }
        [Command("roles"), Alias("role")]
        public async Task Roles(string genus, string species) {

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (!(sp is null))
                await Roles(sp);

        }
        public async Task Roles(Species species) {

            // Get the role(s) assigned to this species.

            Role[] roles = await SpeciesUtils.GetRolesAsync(species);

            if (roles.Count() <= 0) {
                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** has not been assigned any roles.", species.ShortName));
                return;
            }

            // Display the role(s) to the user.

            StringBuilder lines = new StringBuilder();

            foreach (Role i in roles) {

                lines.Append(StringUtilities.ToTitleCase(i.name));

                if (!string.IsNullOrEmpty(i.notes))
                    lines.Append(string.Format(" ({0})", i.notes));

                lines.AppendLine();

            }

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithTitle(string.Format("{0}'s role(s) ({1})", species.ShortName, roles.Count()));
            embed.WithDescription(lines.ToString());

            await ReplyAsync("", false, embed.Build());

        }

        [Command("setroledescription"), Alias("setroledesc"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetRoleDescription(string name, string description) {

            Role role = await BotUtils.GetRoleFromDb(name);

            if (!await BotUtils.ReplyAsync_ValidateRole(Context, role))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Roles SET description=$description WHERE name=$name;")) {

                cmd.Parameters.AddWithValue("$name", role.name);
                cmd.Parameters.AddWithValue("$description", description);

                await Db.ExecuteNonQueryAsync(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated description for role **{0}**.", StringUtilities.ToTitleCase(role.name)));

        }

    }

}