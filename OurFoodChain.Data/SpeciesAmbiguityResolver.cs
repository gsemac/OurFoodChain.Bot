using OurFoodChain.Common;
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

        public async Task<ISpeciesAmbiguityResolverResult> ResolveAsync(string arg0, string arg1, string arg2, AmbiguityResolverOptions options = AmbiguityResolverOptions.None) {

            // 1. <genus> <species> <species>

            IEnumerable<ISpecies> matchingSpecies = await database.GetSpeciesAsync(arg0, arg1);
            IEnumerable<ISpecies> species1 = Enumerable.Empty<ISpecies>();
            IEnumerable<ISpecies> species2 = Enumerable.Empty<ISpecies>();
            string suggestionHint = string.Empty;

            if (matchingSpecies.Count() > 1) {

                // We got multiple matches for the first species (including the genus). 
                // We will fail immediately as there is no way for this to be unambiguous.

                return new SpeciesAmbiguityResolverResult(matchingSpecies, species2, suggestionHint);

            }
            else if (matchingSpecies.Count() == 1) {

                // We got a single match for the first species (including the genus).

                species1 = matchingSpecies;

                matchingSpecies = await database.GetSpeciesAsync(arg2);

                if (matchingSpecies.Count() > 1) {

                    // We got multiple matches for the second species (without the genus).
                    // We won't fail immediately, because we might get an unambiguous match by including the genus.

                    species2 = matchingSpecies;

                }
                else if (matchingSpecies.Count() == 1) {

                    // We got a single match for the second species (without the genus).
                    // We have unambiguously determined both species, so return the result.

                    species2 = matchingSpecies;

                    if (species1 != null && species2 != null)
                        return new SpeciesAmbiguityResolverResult(species1, species2, suggestionHint);

                }
                else
                    suggestionHint = arg2;

            }

            // 2. <species> <genus> <species>

            matchingSpecies = await database.GetSpeciesAsync(arg0);

            if (matchingSpecies.Count() > 1) {

                // We got multiple matches for the first species (without the genus). 
                // We will fail immediately as there is no way for this to be unambiguous.

                return new SpeciesAmbiguityResolverResult(matchingSpecies, species2, suggestionHint);

            }
            else if (matchingSpecies.Count() == 1) {

                // We got a single match for the first species (without the genus).
                // This leaves us with two possible cases for the second species.

                species1 = matchingSpecies;

                // 2.1. <species> <genus> <species>

                matchingSpecies = await database.GetSpeciesAsync(arg1, arg2);
                species2 = matchingSpecies;

                if (matchingSpecies.Count() == 1) {

                    // We got a single match for the second species (with the genus).
                    // We have unambiguously determined both species, so return the result.

                    species2 = matchingSpecies;

                    return new SpeciesAmbiguityResolverResult(species1, species2, suggestionHint);

                }
                else if (matchingSpecies.Count() <= 0 && options.HasFlag(AmbiguityResolverOptions.AllowExtra)) {

                    // 2.2. <species> <species> <extra>

                    // We didn't get any matches for the second species by including the genus, so try to get a match with just the species.

                    matchingSpecies = await database.GetSpeciesAsync(arg1);
                    species2 = matchingSpecies;

                    if (matchingSpecies.Count() == 1) {

                        // We got a single match for the second species (without the genus).
                        // We have unambiguously determined both species, so return the result.

                        species2 = matchingSpecies;

                        return new SpeciesAmbiguityResolverResult(species1, species2, suggestionHint, arg2);

                    }

                }
                else
                    suggestionHint = BinomialName.Parse(arg1, arg2).ToString();

            }

            // If we get here, we were not able to unambiguously figure out what the intended species are, or one of them didn't exist.

            return new SpeciesAmbiguityResolverResult(species1, species2, suggestionHint);

        }
        public async Task<ISpeciesAmbiguityResolverResult> ResolveAsync(string arg0, string arg1, string arg2, string arg3, AmbiguityResolverOptions options = AmbiguityResolverOptions.None) {

            // 1. <genus> <species> <?> <?>

            IEnumerable<ISpecies> matchingSpecies = await database.GetSpeciesAsync(arg0, arg1);
            IEnumerable<ISpecies> species1 = Enumerable.Empty<ISpecies>();
            IEnumerable<ISpecies> species2 = Enumerable.Empty<ISpecies>();
            string suggestionHint = string.Empty;

            if (matchingSpecies.Count() > 1) {

                // We got multiple matches for the first species (including the genus). 
                // We will fail immediately as there is no way for this to be unambiguous.

                return new SpeciesAmbiguityResolverResult(matchingSpecies, species2, suggestionHint);

            }
            else if (matchingSpecies.Count() == 1) {

                // We got a single match for the first species (including the genus).

                species1 = matchingSpecies;

                // 1.2. <genus> <species> <genus> <species>

                matchingSpecies = await database.GetSpeciesAsync(arg2, arg3);

                if (matchingSpecies.Count() > 1) {

                    // We got multiple matches for the second species (including the genus). 
                    // We will fail immediately as there is no way for this to be unambiguous.

                    return new SpeciesAmbiguityResolverResult(species1, matchingSpecies, suggestionHint);

                }
                else if (matchingSpecies.Count() == 1) {

                    // We got a single match for the second species (including the genus).
                    // We have unambiguously determined both species, so return the result.

                    species2 = matchingSpecies;

                    return new SpeciesAmbiguityResolverResult(species1, species2, suggestionHint);

                }
                else if (matchingSpecies.Count() <= 0 && options.HasFlag(AmbiguityResolverOptions.AllowExtra)) {

                    // 1.3. <genus> <species> <species> <extra>

                    // We didn't get any matches for the second species (including the genus).

                    matchingSpecies = await database.GetSpeciesAsync(arg2);

                    if (matchingSpecies.Count() == 1) {

                        // We got a single match for the second species (without the genus).
                        // We have unambiguously determined both species, so return the result.

                        species2 = matchingSpecies;

                        return new SpeciesAmbiguityResolverResult(species1, species2, arg3);

                    }
                    else if (matchingSpecies.Count() > 1) {

                        // We got multiple matches for the second species (without the genus). 

                        return new SpeciesAmbiguityResolverResult(species1, matchingSpecies, suggestionHint);

                    }

                }
                else
                    suggestionHint = BinomialName.Parse(arg2, arg3).ToString();

            }
            else {

                // We didn't get any match for the first species (including the genus).

                matchingSpecies = await database.GetSpeciesAsync(arg0);

                if (matchingSpecies.Count() > 1) {

                    // If this is ambiguous, we can't determine the first species at all.

                    return new SpeciesAmbiguityResolverResult(matchingSpecies, species2, suggestionHint);

                }
                else if (matchingSpecies.Count() == 1) {

                    // We must have something of the following form:
                    // <species> <genus> <species> <notes>

                    species1 = matchingSpecies;
                    species2 = await database.GetSpeciesAsync(arg1, arg2);

                    return new SpeciesAmbiguityResolverResult(species1, species2, suggestionHint, arg3);

                }

            }

            // If we get here, we were not able to unambiguously figure out what the intended species are, or one of them didn't exist.

            return new SpeciesAmbiguityResolverResult(species1, species2, suggestionHint);

        }

        // Private members

        private readonly SQLiteDatabase database;

    }

}