using OurFoodChain.Common;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Utilities {

    public class SpeciesAdapter :
        SpeciesBase {

        // Public members

        public override ITaxon Genus {
            get => new Common.Taxa.Taxon(species.GenusName, TaxonRankType.Genus);
            set {

                species.GenusName = value.Name;
                species.GenusId = value.Id.GetValueOrDefault(Taxon.NullId);

            }
        }
        public override ICreator Creator {
            get => new Creator((ulong)species.OwnerUserId, species.OwnerName);
            set {

                species.OwnerUserId = (long)value.UserId.GetValueOrDefault(UserInfo.NullId);
                species.OwnerName = value.Name;

            }
        }
        public override DateTimeOffset CreationDate {
            get => DateUtilities.TimestampToOffset(species.Timestamp);
            set => species.Timestamp = DateUtilities.OffsetToTimestamp(value);
        }
        public override string Description {
            get => species.Description;
            set => species.Description = value;
        }
        public override IConservationStatus Status {
            get => new ConservationStatus() { ExtinctionDate = DateTimeOffset.MinValue };
            set => species.IsExtinct = value.IsExinct;
        }
        public override long? Id {
            get => species.Id;
            set => species.Id = value.GetValueOrDefault(Species.NullId);
        }
        public override string Name {
            get => species.Name;
            set => species.Name = value;
        }
        public override IPicture Picture {
            get => string.IsNullOrEmpty(species.Picture) ? null : new Picture(species.Picture) { Artist = new Creator(species.OwnerName) };
            set => species.Picture = value?.Url ?? string.Empty;
        }

        public SpeciesAdapter(Species species) {

            this.species = species;

        }

        // Private members

        readonly Species species;

    }

}