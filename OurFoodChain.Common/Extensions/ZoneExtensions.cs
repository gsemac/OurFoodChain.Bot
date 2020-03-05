using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public enum ZoneListToStringOptions {
        None = 0,
        DoNotCollapseRanges = 1,
        DoNotGroupComments = 2
    }

    public static class ZoneExtensions {

        public static string GetShortName(this IZone zone) {

            return ZoneUtilities.GetShortName(zone.Name);

        }
        public static string GetFullName(this IZone zone) {

            return ZoneUtilities.GetFullName(zone.Name);

        }
        public static string GetShortDescription(this IZone zone) {

            return zone.GetDescriptionOrDefault().GetFirstSentence();

        }
        public static string GetDescriptionOrDefault(this IZone zone) {

            if (zone is null || string.IsNullOrWhiteSpace(zone.Description))
                return Constants.DefaultDescription;

            return zone.Description;

        }

        public static string ToString(this IEnumerable<ISpeciesZoneInfo> zones, ZoneListToStringOptions options, int maxLength = 0) {

            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength));

            string resultString = string.Empty;

            if (!options.HasFlag(ZoneListToStringOptions.DoNotGroupComments)) {

                // We'll group zone lists according to those that have identical comments.
                // Ex: "A, B, C (notes); D, E (other notes)

                List<string> sb = new List<string>();

                zones.GroupBy(x => string.IsNullOrEmpty(x.Notes) ? "" : x.Notes)
                    .OrderBy(x => x.Key)
                    .ToList().ForEach(keyValuePair => {

                        // Generate a array of all zones corresponding to this key (notes), sorted by name.

                        List<string> zonesArray = keyValuePair.Select(speciesZone => speciesZone.Zone.GetShortName()).ToList();

                        zonesArray.Sort((lhs, rhs) => new NaturalStringComparer().Compare(lhs, rhs));

                        string zonesString = string.Join(", ", zonesArray);

                        if (options.HasFlag(SpeciesZoneInfoCollectionToStringOptions.CollapseRanges))
                            zonesString = StringUtilities.CollapseAlphanumericList(zonesString);

                        if (string.IsNullOrEmpty(keyValuePair.Key))
                            sb.Add(zonesString);
                        else
                            sb.Add(string.Format("{0} ({1})", zonesString, keyValuePair.Key.ToLower()));

                    });

                resultString = string.Join("; ", sb);

            }
            else {

                // We won't group zone lists at all, and simply list them all in one list along with their notes.
                // Ex: "A (notes), B (notes), C (notes), D (other notes), E (other notes)"

                string[] zonesList = zones.OrderBy(x => x.Zone.GetShortName())
                    .Select(x => string.Format("{0} ({1})", x.Zone, x.Notes.ToLower()))
                    .ToArray();

                string zonesString = string.Join(", ", zonesList);

                if (options.HasFlag(SpeciesZoneInfoCollectionToStringOptions.CollapseRanges))
                    zonesString = StringUtilities.CollapseAlphanumericList(zonesString);

                resultString = zonesString;

            }

            // Generate the final string and make sure it meets our length requirements.
            // If it doesn't, we'll trim off zones until it's the correct length.

            if (maxLength > 0) {

                int zonesDropped = 0;

                // #todo This is an incredibly inefficient way of handling this, but it shouldn't need many iterations in most cases.

                while (resultString.Length > maxLength) {

                    string[] shorterZoneList = resultString.Split(new string[] { ", " }, StringSplitOptions.None);

                    ++zonesDropped;

                    resultString = string.Format("{0} (+{1})", string.Join(", ", shorterZoneList.Take(shorterZoneList.Count() - 1)).Trim(',', ';'), zonesDropped);

                }

            }

            return resultString;

        }

        public static bool IsValid(this IZone zone) {

            return zone != null && zone.Id.HasValue && zone.Id >= 0;

        }

    }

}