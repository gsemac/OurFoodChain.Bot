using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChainWikiBot {

    class Program {

        static void Main(string[] args)
             => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args) {

            _log("loading configuration");

            Config config = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText("wikibot-config.json"));

            _log("initializing mediawiki client");

            MediaWikiClient client = new MediaWikiClient {
                Protocol = config.Protocol,
                Server = config.Server,
                ApiPath = config.ApiPath,
                UserAgent = config.UserAgent
            };

            if (client.Login(config.Username, config.Password).Success) {

                _log("generating link dictionary");

                Dictionary<string, string> link_dictionary = await _generateLinkDictionary();

                _log("synchronizing species");
                _log("getting species from database");

                OurFoodChain.Species[] speciesList = await OurFoodChain.SpeciesUtils.GetSpeciesAsync();

                _log(string.Format("got {0} results", speciesList.Count()));

                foreach (OurFoodChain.Species species in speciesList) {

                    _log(string.Format("synchronizing species {0}", species.GetShortName()));

                    // Pages are created based on the first/primary common name (where available).
                    // The full species name is added as a redirect.

                    string page_title = string.Empty;
                    bool create_redirect = true;

                    OurFoodChain.CommonName[] commonNames = await OurFoodChain.SpeciesUtils.GetCommonNamesAsync(species);

                    if (!string.IsNullOrWhiteSpace(species.CommonName))
                        page_title = species.CommonName;
                    else if (commonNames.Count() > 0)
                        page_title = commonNames[0].Value;

                    if (string.IsNullOrEmpty(page_title)) {

                        page_title = species.GetFullName();

                        create_redirect = false;

                    }

                    // Attempt to upload the species' picture.

                    string picture_filename = _generateSpeciesPictureFileName(species);

                    if (!string.IsNullOrEmpty(species.pics)) {

                        UploadParameters upload_parameters = new UploadParameters {
                            FileName = picture_filename,
                            FilePath = species.pics
                        };

                        _log(string.Format("uploading {0}", upload_parameters.FilePath));

                        try {

                            MediaWikiApiRequestResult result = client.Upload(upload_parameters);

                            if (!result.Success)
                                _log(result.ErrorMessage);

                            if (result.ErrorCode == ErrorCode.VerificationError) {

                                // This means that the file extension didn't match (e.g., filename has ".png" when the file format is actually ".jpg").
                                // Try changing the file extension and reuploading.

                                string ext = System.IO.Path.GetExtension(picture_filename);
                                ext = (ext == ".png") ? ".jpg" : ".png";

                                picture_filename = System.IO.Path.ChangeExtension(picture_filename, ext);

                                upload_parameters.FileName = picture_filename;

                                result = client.Upload(upload_parameters);

                                if (!result.Success)
                                    _log(result.ErrorMessage);

                            }

                            if (!result.Success && result.ErrorCode != ErrorCode.FileExistsNoChange)
                                picture_filename = string.Empty;

                        }
                        catch (Exception ex) {

                            picture_filename = string.Empty;

                            _log(ex.ToString());

                        }

                    }
                    else
                        _log("no picture to upload");

                    // Generate page content.

                    string page_content = new SpeciesPageGenerator().Generate(new SpeciesPageData {
                        Species = species,
                        AllSpecies = speciesList,
                        PictureFileName = picture_filename,
                        LinkDictionary = link_dictionary
                    });

                    // Upload page content.

                    _log(string.Format("creating page \"{0}\"", page_title));

                    _editPage(client, page_title, page_content);

                    // Attempt to create the redirect page for the species (if applicable).

                    if (create_redirect) {

                        string redirect_page_title = species.GetFullName();

                        _editPage(client, redirect_page_title, string.Format("#REDIRECT [[{0}]]", page_title) + "\n" + BOT_FLAG_STRING);

                    }

                    _log(string.Format("finished synchronizing species {0}", species.GetShortName()));

                }

            }
            else
                _log("mediawiki login failed");

            _log("synchronizing complete");

            await Task.Delay(-1);

        }

        private const string BOT_FLAG_STRING = "{{BotGenerated}}";

        private static void _log(string message) {

            Console.WriteLine(new LogMessage {
                Source = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                Message = message
            });

        }

        private static string _generateSpeciesPictureFileName(OurFoodChain.Species species) {

            if (string.IsNullOrEmpty(species.pics))
                return string.Empty;

            string image_url = species.pics;

            if (image_url.Contains("?"))
                image_url = image_url.Substring(0, image_url.LastIndexOf("?"));

            return string.Format("{0}{1}", species.GetFullName().ToLower().Replace(' ', '_'), System.IO.Path.GetExtension(image_url).ToLower());

        }
        private static async Task<Dictionary<string, string>> _generateLinkDictionary() {

            // Returns a dictionary of substrings that should be turned into page links in page content.

            Dictionary<string, string> dict = new Dictionary<string, string>();

            // Add species names to the dictionary.

            OurFoodChain.Species[] species_list = await OurFoodChain.SpeciesUtils.GetSpeciesAsync();

            foreach (OurFoodChain.Species species in species_list) {

                dict[species.GetShortName().ToLower()] = species.GetFullName();
                dict[species.GetFullName().ToLower()] = species.GetFullName();
                dict[species.name.ToLower()] = species.GetFullName();

                if (!string.IsNullOrEmpty(species.CommonName))
                    dict[species.CommonName.ToLower()] = species.GetFullName();

            }

            // Add zone names to the dictionary.

            OurFoodChain.Zone[] zones_list = await OurFoodChain.ZoneUtils.GetZonesAsync();

            foreach (OurFoodChain.Zone zone in zones_list) {

                dict.Add(zone.GetFullName().ToLower(), zone.GetFullName());

            }

            return dict;

        }

        private static void _editPage(MediaWikiClient client, string pageTitle, string pageContent) {

            _log(string.Format("parsing page \"{0}\"", pageTitle));

            // Get existing page content.
            // This allows us to make sure that no one has removed the "{{BotGenerated}}" flag.
            // If it has been removed, do not modify the page.

            MediaWikiApiParseRequestResult parse_result = client.Parse(pageTitle, new ParseParameters());

            if (parse_result.ErrorCode == ErrorCode.MissingTitle || parse_result.Text.Contains(BOT_FLAG_STRING)) {

                if (parse_result.ErrorCode == ErrorCode.MissingTitle)
                    _log(string.Format("creating page \"{0}\"", pageTitle));
                else
                    _log(string.Format("editing page \"{0}\"", pageTitle));

                try {

                    client.Edit(pageTitle, new EditParameters {
                        Action = EditAction.Text,
                        Text = pageContent
                    });

                }
                catch (Exception ex) {
                    _log(ex.ToString());
                }

            }
            else {

                _log(string.Format("skipping page \"{0}\" (manually edited)", pageTitle));

            }

        }

    }

}