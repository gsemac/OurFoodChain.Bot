using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Commands {

    public interface ICommandHelpInfo {

        string Name { get; set; }
        string Summary { get; set; }
        string Category { get; set; }
        IEnumerable<string> Aliases { get; set; }
        IEnumerable<string> Examples { get; set; }

        string Group { get; }
        bool IsTopLevel { get; }

    }

}