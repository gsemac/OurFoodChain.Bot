using Discord.Commands;
using Newtonsoft.Json;
using OurFoodChain.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public class HelpService :
        IHelpService {

        // Public members

        public HelpService(
            IBotConfiguration botConfiguration,
            IServiceProvider serviceProvider,
            CommandService commandService
            ) {

            _botConfiguration = botConfiguration;
            _serviceProvider = serviceProvider;
            _commandService = commandService;

        }

        public async Task<ICommandHelpInfo> GetCommandHelpInfoAsync(string commandName) {

            return FindHelpInfo(await GetCommandHelpInfoAsync(), commandName.Trim());

        }
        public async Task<IEnumerable<ICommandHelpInfo>> GetCommandHelpInfoAsync() {

            List<ICommandHelpInfo> commandHelpInfos = new List<ICommandHelpInfo>();

            // Get command information from the registered modules.

            foreach (CommandInfo commandInfo in _commandService.Commands) {

                commandHelpInfos.Add(new CommandHelpInfo {
                    Name = GetFullCommandName(commandInfo),
                    Aliases = commandInfo.Aliases,
                    Summary = commandInfo.Summary
                });

            }

            // Match the commands to files in the help directory, adding/replacing any missing metadata.

            string helpDirectory = _botConfiguration.HelpDirectory;

            if (System.IO.Directory.Exists(helpDirectory)) {

                foreach (string helpInfoFilePath in System.IO.Directory.GetFiles(helpDirectory, "*.json", System.IO.SearchOption.AllDirectories)) {

                    ICommandHelpInfo fileHelpInfo = JsonConvert.DeserializeObject<CommandHelpInfo>(System.IO.File.ReadAllText(helpInfoFilePath));

                    string group = string.Join(" ",
                          helpInfoFilePath
                          .AfterSubstring(_botConfiguration.HelpDirectory)
                          .Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
                          .Where(p => !string.IsNullOrWhiteSpace(p))
                          .SkipLast(1));

                    if (!string.IsNullOrEmpty(group))
                        fileHelpInfo.Name = group + " " + fileHelpInfo.Name;

                    ICommandHelpInfo helpInfo = FindHelpInfo(commandHelpInfos, fileHelpInfo.Name);

                    if (helpInfo != null) {

                        helpInfo.Name = (fileHelpInfo.Name ?? helpInfo.Name).ToLower();
                        helpInfo.Aliases = helpInfo.Aliases
                            .Union(fileHelpInfo.Aliases)
                            .Where(i => !i.Equals(helpInfo.Name, StringComparison.OrdinalIgnoreCase))
                            .Distinct()
                            .OrderBy(i => i);
                        helpInfo.Summary = fileHelpInfo.Summary ?? helpInfo.Summary;
                        helpInfo.Examples = fileHelpInfo.Examples;
                        helpInfo.Category = fileHelpInfo.Category ?? helpInfo.Category;

                    }

                }

            }

            // Return the result.

            return await Task.FromResult(commandHelpInfos);

        }
        public async Task<IEnumerable<ICommandHelpInfo>> GetCommandHelpInfoAsync(ICommandContext context) {

            IEnumerable<ICommandHelpInfo> helpInfos = await GetCommandHelpInfoAsync();

            // Filter out the commands that the user does not have access to.

            List<ICommandHelpInfo> filteredHelpInfos = new List<ICommandHelpInfo>();

            foreach (ICommandHelpInfo helpInfo in helpInfos) {

                if (await IsCommandAvailableAsync(context, helpInfo.Name))
                    filteredHelpInfos.Add(helpInfo);

            }

            return filteredHelpInfos;

        }
        public async Task<bool> IsCommandAvailableAsync(ICommandContext context, string commandName) {

            CommandInfo commandInfo = _commandService.Commands
                .Where(i => i.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) || i.Aliases.Any(a => a.Equals(commandName, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault();

            if (commandInfo is null)
                return false;

            if (!(await commandInfo.CheckPreconditionsAsync(context, _serviceProvider)).IsSuccess)
                return false;

            return true;

        }

        // Private members

        private readonly IBotConfiguration _botConfiguration;
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandService _commandService;

        private string GetFullCommandName(CommandInfo commandInfo) {

            string commandName = commandInfo.Name;
            ModuleInfo moduleInfo = commandInfo.Module;

            while (moduleInfo != null) {

                if (!string.IsNullOrEmpty(moduleInfo.Group))
                    commandName = moduleInfo.Group + " " + commandName;

                moduleInfo = moduleInfo.IsSubmodule ? moduleInfo.Parent : null;

            }

            return commandName.ToLower();

        }
        private ICommandHelpInfo FindHelpInfo(IEnumerable<ICommandHelpInfo> helpInfo, string commandName) {

            if (!string.IsNullOrEmpty(commandName))
                commandName = commandName.Trim();

            // Attempt to find an exact match for the name of the command.

            ICommandHelpInfo result = helpInfo
                .Where(i => i.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) || i.Aliases.Any(a => a.Equals(commandName, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault();

            // Attempt to find the command nested inside of a group.

            if (result is null) {

                result = helpInfo
                    .Where(i => i.Name.Split(' ').Last().Equals(commandName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

            }

            return result;

        }

    }

}