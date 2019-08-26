using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class CommandHelpInfo {

        [JsonProperty("name")]
        public string Name { get; set; } = "";
        [JsonProperty("description")]
        public string Description { get; set; } = "";
        [JsonProperty("category")]
        public string Category { get; set; } = "uncategorized";
        [JsonProperty("aliases")]
        public string[] Aliases { get; set; } = new string[] { };
        [JsonProperty("examples")]
        public string[] Examples { get; set; } = new string[] { };

        public string NestedUnder { get; set; } = "/";

        public string ExamplesToString(string prefix) {

            StringBuilder sb = new StringBuilder();

            foreach (string example in Examples)
                sb.AppendLine(string.Format("`{0}{1}`", prefix, example));

            return sb.ToString();

        }

        public static CommandHelpInfo FromJson(string json) {

            return JsonConvert.DeserializeObject<CommandHelpInfo>(json);

        }
        public static CommandHelpInfo FromFile(string filePath) {

            return FromJson(System.IO.File.ReadAllText(filePath));

        }

    }

    public class CommandHelpInfoCollection :
        IEnumerable<CommandHelpInfo> {

        // Public members

        public CommandHelpInfoCollection(IEnumerable<CommandHelpInfo> commandHelpInfos) {
            _info = new List<CommandHelpInfo>(commandHelpInfos);
        }

        public CommandHelpInfo FindCommandByName(string commandName) {

            if (!string.IsNullOrEmpty(commandName)) {

                commandName = commandName.ToLower();

                foreach (CommandHelpInfo info in _info)
                    if (info.Name == commandName || info.Aliases.Contains(commandName))
                        return info;

            }

            return null;

        }

        public IEnumerator<CommandHelpInfo> GetEnumerator() {
            return _info.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return _info.GetEnumerator();
        }

        // Private members

        private List<CommandHelpInfo> _info;
    }

    public class HelpUtils {

        public static readonly string HELP_DIRECTORY = Global.DATA_DIRECTORY + "help/";
        public static readonly string DEFAULT_COMMAND_CATEGORY = "uncategorized";

        public static CommandHelpInfo GetCommandInfo(string commandName) {

            return GetAllCommandInfo().FindCommandByName(commandName);

        }
        public static CommandHelpInfoCollection GetAllCommandInfo() {

            return GetCommandInfoFromDirectory(HELP_DIRECTORY, System.IO.SearchOption.TopDirectoryOnly);

        }
        public static CommandHelpInfoCollection GetCommandInfoFromDirectory(string directory, System.IO.SearchOption searchOption = System.IO.SearchOption.TopDirectoryOnly) {

            if (string.IsNullOrEmpty(directory) || !System.IO.Directory.Exists(directory))
                directory = HELP_DIRECTORY;

            List<CommandHelpInfo> result = new List<CommandHelpInfo>();

            if (System.IO.Directory.Exists(directory)) {

                string[] filenames = System.IO.Directory.GetFiles(directory, "*.json", searchOption);

                foreach (string filename in filenames) {

                    CommandHelpInfo info = CommandHelpInfo.FromFile(filename);

                    result.Add(info);

                }

            }

            return new CommandHelpInfoCollection(result);

        }

    }

}