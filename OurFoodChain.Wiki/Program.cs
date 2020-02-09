using Newtonsoft.Json;
using OurFoodChain.Common;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Wiki {

    class Program {

        // Public members

        string SpeciesTemplateFilePath { get; } = "data/templates/species_template.txt";
        SQLiteDatabase Db { get; } = SQLiteDatabase.FromFile(Constants.DatabaseFilePath);

        static void Main(string[] args)
             => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args) {

            Log("loading configuration");

            Config config = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText("wikibot-config.json"));

            Log("initializing mediawiki client");

            MediaWikiClient client = new MediaWikiClient {
                Protocol = config.Protocol,
                Server = config.Server,
                ApiPath = config.ApiPath,
                UserAgent = config.UserAgent
            };

            client.Log += Log;

            EditHistory history = new EditHistory();

            if (client.Login(config.Username, config.Password).Success) {

                Log("generating link dictionary");

                WikiLinkList LinkifyList = await _generateLinkifyListAsync();

                Log("synchronizing species");
                Log("getting species from database");

                Species[] speciesList = await SpeciesUtils.GetSpeciesAsync();

                Log(string.Format("got {0} results", speciesList.Count()));

                foreach (Species species in speciesList) {

                    Log(string.Format("synchronizing species {0}", species.ShortName));

                    // Create the page builder.

                    SpeciesPageBuilder pageBuilder = new SpeciesPageBuilder(species, WikiPageTemplate.Open(SpeciesTemplateFilePath)) {
                        AllSpecies = speciesList,
                        LinkList = LinkifyList
                    };

                    // Attempt to upload the species' picture.

                    pageBuilder.PictureFilenames.Add(await UploadSpeciesPictureAsync(client, history, species));
                    pageBuilder.PictureFilenames.AddRange(await UploadSpeciesGalleryAsync(client, history, species));
                    pageBuilder.PictureFilenames.RemoveAll(x => string.IsNullOrWhiteSpace(x));

                    // Generate page content.

                    WikiPage wikiPage = await pageBuilder.BuildAsync();

                    string pageTitle = wikiPage.Title;
                    bool createRedirect = pageTitle != species.FullName;

                    // Upload page content.

                    await _editSpeciesPageAsync(client, history, species, pageTitle, wikiPage.Body);

                    // Attempt to create the redirect page for the species (if applicable).

                    if (createRedirect) {

                        string redirect_page_title = species.FullName;

                        if (await _editPageAsync(client, history, redirect_page_title, string.Format("#REDIRECT [[{0}]]", pageTitle) + "\n" + BotFlag))
                            await history.AddRedirectRecordAsync(redirect_page_title, pageTitle);

                    }

                    Log(string.Format("finished synchronizing species {0}", species.ShortName));

                }

            }
            else
                Log("mediawiki login failed");

            Log("synchronizing complete");

            await Task.Delay(-1);

        }

        // Private members

        private const string BotFlag = "{{BotGenerated}}";
        private static string LogFilePath = "";

        private static void Log(string message) {

            Log(new LogMessage(
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                message
            ));

        }
        private static void Log(ILogMessage message) {

            if (string.IsNullOrEmpty(LogFilePath)) {

                System.IO.Directory.CreateDirectory("log");

                LogFilePath = string.Format("log/wiki_log_{0}.txt", DateTimeOffset.Now.ToUnixTimeSeconds());

            }

            Console.WriteLine(message.ToString());

            System.IO.File.AppendAllText(LogFilePath, message.ToString() + Environment.NewLine);

        }

        private static async Task<WikiLinkList> _generateLinkifyListAsync() {

            // Returns a dictionary of substrings that should be turned into page links in page content.

            WikiLinkList list = new WikiLinkList();

            // Add species names to the dictionary.

            Species[] species_list = await SpeciesUtils.GetSpeciesAsync();

            foreach (Species species in species_list) {

                list.Add(species.ShortName.ToLower(), species.FullName);
                list.Add(species.FullName.ToLower(), species.FullName);
                list.Add(species.Name.ToLower(), species.FullName);

                if (!string.IsNullOrEmpty(species.CommonName))
                    list.Add(species.CommonName.ToLower(), species.FullName);

            }

            foreach (Species species in species_list) {

                // Also linkify binomial names that might be using outdated genera (e.g. Species moved to a new genus since the description was written).
                // Only do this for species that have a unique name-- otherwise, there's no way to know for sure which species to link to!
                // This might create some false-positives, so it could be a good idea to limit matches only to known genera (at the expense of a significantly longer regex).

                if (list.Count(x => x.Value == species.Name.ToLower()) == 1)
                    list.Add(string.Format(WikiPageUtils.UnlinkedWikiTextPatternFormat, @"[A-Z](?:[a-z]+|\.)\s" + Regex.Escape(species.Name.ToLower())), species.FullName, WikiLinkListDataType.Regex);

            }

            // Add zone names to the dictionary.

            Zone[] zones_list = await ZoneUtils.GetZonesAsync();

            foreach (Zone zone in zones_list) {

                list.Add(zone.FullName.ToLower(), zone.FullName);

            }

            return list;

        }

        private static string GeneratePictureFilenameFromSpecies(Species species) {

            if (string.IsNullOrEmpty(species.Picture))
                return string.Empty;

            string pictureFilename = GetPictureFilenameFromPictureUrl(species.Picture);

            return string.Format("{0}{1}", species.FullName.ToLower().Replace(' ', '_'), System.IO.Path.GetExtension(pictureFilename).ToLower());

        }
        private static string GeneratePictureFilenameFromSpecies(Species species, string pictureUrl) {

            string pictureFilename = GetPictureFilenameFromPictureUrl(pictureUrl);

            return string.Format("{0}-{1}{2}", species.FullName.ToLower().Replace(' ', '_'), StringUtilities.GetMD5(pictureUrl), System.IO.Path.GetExtension(pictureFilename).ToLower());

        }
        private static string GetPictureFilenameFromPictureUrl(string pictureUrl) {

            if (string.IsNullOrEmpty(pictureUrl))
                return string.Empty;

            if (pictureUrl.Contains("?"))
                pictureUrl = pictureUrl.Substring(0, pictureUrl.LastIndexOf("?"));

            return System.IO.Path.GetFileName(pictureUrl);

        }
        private static async Task<string> UploadSpeciesPictureAsync(MediaWikiClient client, EditHistory history, Species species) {

            // Generate a filename for the image, which will be the filename when it's uploaded to the wiki.

            string uploadedFilename = GeneratePictureFilenameFromSpecies(species);

            if (!string.IsNullOrEmpty(uploadedFilename)) {

                // Attempt to upload the image.

                UploadParameters uploadParameters = new UploadParameters {
                    UploadFileName = uploadedFilename,
                    FilePath = species.Picture
                };

                return await UploadPictureAsync(client, history, uploadParameters, true);

            }

            return string.Empty;

        }
        private async Task<string[]> UploadSpeciesGalleryAsync(MediaWikiClient client, EditHistory history, Species species) {

            // Upload all images in the given species' gallery.

            IEnumerable<IPicture> pictures = await Db.GetAllPicturesAsync(new Utilities.SpeciesAdapter(species));
            List<string> uploadedFilenames = new List<string>();

            if (pictures != null) {

                foreach (IPicture picture in pictures) {

                    // Skip the image if it's the same as the species' default image, because we would've already uploaded it

                    if (picture.Url == species.Picture)
                        continue;

                    string uploadedFilename = GeneratePictureFilenameFromSpecies(species, picture.Url);

                    if (!string.IsNullOrEmpty(uploadedFilename)) {

                        UploadParameters uploadParameters = new UploadParameters {
                            UploadFileName = uploadedFilename,
                            FilePath = picture.Url,
                            Text = picture.Description
                        };

                        uploadedFilename = await UploadPictureAsync(client, history, uploadParameters, true);

                        if (!string.IsNullOrEmpty(uploadedFilename))
                            uploadedFilenames.Add(uploadedFilename);

                    }
                    else
                        Log(string.Format("Failed to generate filename for picture: {0}", picture.Url));

                }

            }

            return uploadedFilenames.ToArray();

        }
        private static async Task<string> UploadPictureAsync(MediaWikiClient client, EditHistory history, UploadParameters parameters, bool allowRetry) {

            // Check if we've already uploaded this file before. 
            // If we've uploaded it before, return the filename that we uploaded it with.

            UploadRecord record = await history.GetUploadRecordAsync(parameters.FilePath);

            if (record is null) {

                // Get page for the file and check for the bot flag.
                // This prevents us from overwriting images that users uploaded manually.

                MediaWikiApiParseRequestResult page_content = client.Parse(parameters.PageTitle, new ParseParameters());

                if (page_content.ErrorCode == ErrorCode.MissingTitle || page_content.Text.Contains(BotFlag)) {

                    // Attempt to upload the file.

                    try {

                        parameters.Text = BotFlag + '\n' + parameters.Text;

                        MediaWikiApiRequestResult result = client.Upload(parameters);

                        if (!result.Success)
                            Log(result.ErrorMessage);

                        if (result.ErrorCode == ErrorCode.VerificationError && allowRetry) {

                            // This means that the file extension didn't match (e.g., filename has ".png" when the file format is actually ".jpg").
                            // Try changing the file extension and reuploading, because sometimes URLs stored in the bot will have this problem.

                            string ext = System.IO.Path.GetExtension(parameters.UploadFileName);
                            ext = (ext == ".png") ? ".jpg" : ".png";

                            parameters.UploadFileName = System.IO.Path.ChangeExtension(parameters.UploadFileName, ext);

                            Log("file extension didn't match, retrying upload");

                            return await UploadPictureAsync(client, history, parameters, false);

                        }
                        else {

                            if (result.Success || result.ErrorCode == ErrorCode.FileExistsNoChange) {

                                // If the upload succeeded, record the file upload so that we can skip it in the future.
                                await history.AddUploadRecordAsync(parameters.FilePath, parameters.UploadFileName);

                                // Add the bot flag to the page content.
                                // client.Edit(parameters.PageTitle, new EditParameters { Text = BotFlag });

                            }
                            else
                                parameters.UploadFileName = string.Empty;

                        }

                    }
                    catch (Exception ex) {

                        parameters.UploadFileName = string.Empty;

                        Log(ex.ToString());

                    }

                }
                else
                    Log(string.Format("skipping file \"{0}\" (manually edited)", parameters.UploadFileName));

            }
            else {

                // This image has been uploaded previously, so just return its path.

                Log(string.Format("skipping file \"{0}\" (previously uploaded)", parameters.UploadFileName));

                parameters.UploadFileName = record.UploadFileName;

            }

            return parameters.UploadFileName;

        }

        private static async Task _editSpeciesPageAsync(MediaWikiClient client, EditHistory history, Species species, string pageTitle, string pageContent) {

            if (await _editPageAsync(client, history, pageTitle, pageContent)) {

                // If the edit was successful, associated it with this species.

                EditRecord record = await history.GetEditRecordAsync(pageTitle, pageContent);

                if (record != null) {

                    await history.AddEditRecordAsync(species.Id, record);

                    // Because it's possible that the species was renamed, we need to look at past edits to find previous titles of the same page.
                    // Old pages for renamed species will be deleted.

                    EditRecord[] edit_records = (await history.GetEditRecordsAsync(species.Id))
                        .Where(x => x.Id != record.Id && x.Title.ToLower() != record.Title.ToLower())
                        .ToArray();

                    // Delete all created pages where the old title does not match the current title.

                    foreach (EditRecord i in edit_records) {

                        MediaWikiApiParseRequestResult parse_result = client.Parse(i.Title, new ParseParameters());

                        if (parse_result.Text.Contains(BotFlag)) {

                            // Only delete pages that haven't been manually edited. 

                            client.Delete(i.Title, new DeleteParameters {
                                Reason = "species page moved to " + pageTitle
                            });

                            // Add an edit record for this page so that we can restore the content later without it thinking we've already made this edit.
                            // This is important, because this step can delete redirects when a page with redirects is updated. By creating a new edit record, the redirect will be recreated.

                            await history.AddEditRecordAsync(i.Title, string.Empty);

                        }

                    }

                    // We also need to delete any redirect pages that are now invalid (i.e. when specific epithet that points to a common name is changed).
                    // Delete all redirects that point to this page (or one of this page's previous titles).

                    RedirectRecord[] redirect_records = (await history.GetRedirectRecordsAsync())
                        .Where(i => i.Target == pageTitle || edit_records.Any(j => j.Title == i.Target)) // points to the title of this page, or one of its previous titles
                        .Where(i => i.Title != species.FullName) // the title doesn't match this species' full name (the species has been renamed)
                        .ToArray();

                    foreach (RedirectRecord j in redirect_records) {

                        MediaWikiApiParseRequestResult parse_result = client.Parse(j.Title, new ParseParameters());

                        if (parse_result.IsRedirect && parse_result.Text.Contains(BotFlag)) {

                            // Only delete pages that haven't been manually edited. 

                            client.Delete(j.Title, new DeleteParameters {
                                Reason = "outdated redirect"
                            });

                        }

                    }

                }

            }

        }
        private static async Task<bool> _editPageAsync(MediaWikiClient client, EditHistory history, string pageTitle, string pageContent) {

            // Check to see if we've made this edit before.
            // If we've already made this page before, don't do anything.

            EditRecord record = await history.GetEditRecordAsync(pageTitle, pageContent);

            if (record is null) {

                // Get existing page content.
                // This allows us to make sure that no one has removed the "{{BotGenerated}}" flag.
                // If it has been removed, do not modify the page.

                MediaWikiApiParseRequestResult parse_result = client.Parse(pageTitle, new ParseParameters());

                if (parse_result.ErrorCode == ErrorCode.MissingTitle || parse_result.Text.Contains(BotFlag)) {

                    if (parse_result.ErrorCode == ErrorCode.MissingTitle)
                        Log(string.Format("creating page \"{0}\"", pageTitle));
                    else
                        Log(string.Format("editing page \"{0}\"", pageTitle));

                    try {

                        client.Edit(pageTitle, new EditParameters {
                            Action = EditAction.Text,
                            Text = pageContent
                        });

                        // Make a record of the edit.
                        await history.AddEditRecordAsync(pageTitle, pageContent);

                        // Return true to indicate that edits have occurred.
                        return true;

                    }
                    catch (Exception ex) {
                        Log(ex.ToString());
                    }

                }
                else {

                    Log(string.Format("skipping page \"{0}\" (manually edited)", pageTitle));

                }

            }
            else
                Log(string.Format("skipping page \"{0}\" (previously edited)", pageTitle));

            // Return false to indicate that no edits have occurred.
            return false;

        }

    }

}