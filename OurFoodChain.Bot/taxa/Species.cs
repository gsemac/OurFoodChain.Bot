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

    public class Species :
      IComparable<Species> {

        public const long NullSpeciesId = -1;

        public long id;
        public long genusId;
        public string name;
        public string description;
        public string owner;
        public long user_id;
        public long timestamp;
        public string pics;
        public string commonName;

        // fields that stored directly in the table
        public string genus;
        public bool isExtinct;

        public string GenusName {
            get {
                return StringUtils.ToTitleCase(genus);
            }
        }

        public string CommonName {
            get {
                return new CommonName(commonName).Value;
            }
        }
        public string FullName {
            get {
                return GetFullName();
            }
        }
        public string ShortName {
            get {
                return GetShortName();
            }
        }
        public string Name {
            get {

                if (string.IsNullOrEmpty(name))
                    return "";

                return name.ToLower();

            }
        }

        public static async Task<Species> FromDataRow(DataRow row, Taxon genusInfo) {

            Species species = new Species {
                id = row.Field<long>("id"),
                genusId = row.Field<long>("genus_id"),
                name = row.Field<string>("name"),
                // The genus should never be null, but there was instance where a user manually edited the database and the genus ID was invalid.
                // We should at least try to handle this situation gracefully.
                genus = genusInfo is null ? "?" : genusInfo.name,
                description = row.Field<string>("description"),
                owner = row.Field<string>("owner"),
                timestamp = (long)row.Field<decimal>("timestamp"),
                commonName = row.Field<string>("common_name"),
                pics = row.Field<string>("pics")
            };

            species.user_id = row.IsNull("user_id") ? -1 : row.Field<long>("user_id");
            species.isExtinct = false;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);

                if (!(await Database.GetRowAsync(cmd) is null))
                    species.isExtinct = true;

            }

            return species;

        }
        public static async Task<Species> FromDataRow(DataRow row) {

            long genus_id = row.Field<long>("genus_id");
            Taxon genus_info = await BotUtils.GetGenusFromDb(genus_id);

            return await FromDataRow(row, genus_info);

        }

        public string GetShortName() {

            return BotUtils.GenerateSpeciesName(this);

        }
        public string GetFullName() {

            return string.Format("{0} {1}", StringUtils.ToTitleCase(genus), name.ToLower());

        }
        public string GetTimeStampAsDateString() {
            return DateUtils.TimestampToShortDateString(timestamp);
        }
        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return BotUtils.DEFAULT_SPECIES_DESCRIPTION;

            return description;

        }
        public async Task<string> GetOwnerOrDefault(ICommandContext context) {

            string result = owner;

            if (!(context is null || context.Guild is null) && user_id > 0) {

                IUser user = await context.Guild.GetUserAsync((ulong)user_id);

                if (!(user is null))
                    result = user.Username;

            }

            if (string.IsNullOrEmpty(result))
                result = "?";

            return result;

        }

        public int CompareTo(Species other) {

            return GetShortName().CompareTo(other.GetShortName());

        }
    }

}
