using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data {

    public class SpeciesAmbiguityResolver :
        ISpeciesAmbiguityResolver {

        // Public members

        public SpeciesAmbiguityResolver(SQLiteDatabase database) {

            this.database = database;

        }

        public async Task<ISpeciesAmbiguityResolverResult> ResolveAsync(string arg0, string arg1, string arg2) {

            // <genus> <species> <species>

            IEnumerable<ISpecies> matchingSpecies = await database.GetSpeciesAsync(arg0, arg1);
            ISpecies species1 = null;
            ISpecies species2 = null;
            //IEnumerable<ISpecies> species2AmbiguousMatches = null;

            if (matchingSpecies.Count() > 1) {

                // If the first species is ambiguous even with the genus, it will be without as well.

                //await ReplyValidateSpeciesAsync(context, query_result);

                return new SpeciesAmbiguityResolverResult(species1, species2);

            }
            else if (matchingSpecies.Count() == 1) {

                species1 = matchingSpecies.First();

                matchingSpecies = await database.GetSpeciesAsync(arg2);

                if (matchingSpecies.Count() > 1) {

                    // If the second species is ambiguous, store the query result to show later.
                    // It's possible that it won't be ambiguous on the second attempt, so we won't show it for now.

                    //species2AmbiguousMatches = matchingSpecies;

                }
                else if (matchingSpecies.Count() == 1) {

                    species2 = matchingSpecies.First();

                    if (species1 != null && species2 != null)
                        return new SpeciesAmbiguityResolverResult(species1, species2);

                }

            }

            // <species> <genus> <species>

            matchingSpecies = await database.GetSpeciesAsync(arg0);

            if (matchingSpecies.Count() > 1) {

                // If the first species is ambiguous, there's nothing we can do.

                //await ReplyValidateSpeciesAsync(context, query_result);

                return new SpeciesAmbiguityResolverResult(species1, species2);

            }
            else if (matchingSpecies.Count() == 1) {

                // In this case, we will show if the second species is ambiguous, as there are no further cases to check.

                species1 = matchingSpecies.First();

                matchingSpecies = await database.GetSpeciesAsync(arg1, arg2);

                if (matchingSpecies.Count() > 1) {

                    //await ReplyValidateSpeciesAsync(context, query_result);

                    return new SpeciesAmbiguityResolverResult(species1, species2);

                }
                else if (matchingSpecies.Count() == 1) {

                    species2 = matchingSpecies.First();

                    return new SpeciesAmbiguityResolverResult(species1, species2);

                }

            }

            // If we get here, we were not able to unambiguously figure out what the intended species are, or one of them didn't exist.

            //if (species_1 is null && species_2 is null)
            //    await ReplyAsync_Error(context, "The given species could not be determined.");
            //else if (species_1 is null)
            //    await ReplyAsync_Error(context, "The first species could not be determined.");
            //else if (species_2 is null) {

            //    if (species_2_ambiguous_matches != null)
            //        await ReplyValidateSpeciesAsync(context, species_2_ambiguous_matches);
            //    else
            //        await ReplyAsync_Error(context, "The second species could not be determined.");

            //}

            return new SpeciesAmbiguityResolverResult(species1, species2);

        }

        // Private members

        private readonly SQLiteDatabase database;

    }

}