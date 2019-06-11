using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class HelpUtils {

        public class CommandInfo {

            public string name = "";
            public string description = "";
            public string category = "uncategorized";
            public string[] aliases;
            public string[] examples;

            public string ExamplesToString(string prefix) {

                StringBuilder sb = new StringBuilder();

                foreach (string example in examples)
                    sb.AppendLine(string.Format("`{0}{1}`", prefix, example));

                return sb.ToString();

            }

        }

        public static CommandInfo GetCommandInfo(string command) {

            // Look inside the help directory, and read all JSON files containing command documentation.
            // We can't just go by filename because the command might have aliases.            

            if (!System.IO.Directory.Exists(Constants.HELP_DIRECTORY))
                return null;

            List<CommandInfo> command_info = new List<CommandInfo>();

            string[] fnames = System.IO.Directory.GetFiles(Constants.HELP_DIRECTORY, "*.json", System.IO.SearchOption.TopDirectoryOnly);

            foreach (string fname in fnames)
                command_info.Add(JsonConvert.DeserializeObject<CommandInfo>(System.IO.File.ReadAllText(fname)));

            // Find the given command.

            command = command.ToLower();

            foreach (CommandInfo c in command_info)
                if (c.name == command || c.aliases.Contains(command))
                    return c;

            return null;

        }

    }

}