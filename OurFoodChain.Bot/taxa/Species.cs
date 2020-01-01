using Discord;
using Discord.Commands;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class Species :
        IComparable<Species> {

        // Public members

        public const long NullId = -1;
        public const string DefaultDescription = "No description provided.";

        public long Id { get; set; } = NullId;
        public long GenusId { get; set; } = Taxon.NullId;
        public string Name {
            get {

                if (string.IsNullOrEmpty(name))
                    return "";

                return name.ToLower();

            }
            set {
                name = value;
            }
        }
        public string Description { get; set; }
        public string OwnerName { get; set; }
        public long OwnerUserId { get; set; }
        public long Timestamp { get; set; }
        public string Picture { get; set; }

        // fields that stored directly in the table

        public bool IsExtinct { get; set; }
        public string GenusName {
            get {
                return StringUtilities.ToTitleCase(genusName);
            }
            set {
                genusName = value;
            }
        }

        public string CommonName {
            get {
                return new CommonName(commonName).Value;
            }
            set {
                commonName = value;
            }
        }
        public string FullName {
            get {
                return string.Format("{0} {1}", StringUtilities.ToTitleCase(genusName), Name.ToLower());
            }
        }
        public string ShortName {
            get {
                return BotUtils.GenerateSpeciesName(this);
            }
        }

        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(Description))
                return DefaultDescription;

            return Description;

        }

        public int CompareTo(Species other) {
            return FullName.CompareTo(other.FullName);
        }

        // Private members

        private string name;
        private string genusName;
        private string commonName;

    }

}