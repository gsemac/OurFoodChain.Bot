using Newtonsoft.Json.Linq;
using OurFoodChain.Debug;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OurFoodChain.Wiki {

    public enum EditAction {
        AppendText,
        PrependText,
        Text
    }

    public class EditParameters {
        public EditAction Action { get; set; } = EditAction.Text;
        public string Text { get; set; }
    }

    public class UploadParameters {

        public string FilePath {
            get {
                return _file_path;
            }
            set {

                _file_path = value;

                if (string.IsNullOrEmpty(UploadFileName) && !string.IsNullOrEmpty(FilePath))
                    UploadFileName = System.IO.Path.GetFileName(FilePath);

                if (string.IsNullOrEmpty(Comment))
                    Comment = OriginalFileName;

            }
        }
        public string UploadFileName { get; set; }
        public string Text { get; set; } = "";
        public string Comment { get; set; } = "";
        public string PageTitle {
            get {
                return string.Format("File:{0}", UploadFileName);
            }
        }

        public string OriginalFileName {
            get {

                if (string.IsNullOrEmpty(FilePath))
                    return "";

                return System.IO.Path.GetFileName(FilePath);

            }
        }

        private string _file_path;

    }

    public class DeleteParameters {

        public string Reason { get; set; } = "";

    }

    public class ParseParameters {

    }

    public enum ErrorCode {
        VerificationError = 1,
        FileExistsNoChange,
        MissingTitle
    }

    public class MediaWikiApiRequestResult {

        public MediaWikiApiRequestResult() { }
        public MediaWikiApiRequestResult(string errorCode, string errorMessage) {
            SetError(errorCode, errorMessage);
        }

        public bool Success { get; set; } = true;
        public ErrorCode ErrorCode { get; set; } = 0;
        public string ErrorMessage { get; set; }

        public void SetError(string errorCode, string errorMessage) {

            Success = false;
            ErrorMessage = errorMessage;
            ErrorCode = 0;

            switch (errorCode.ToLower()) {

                case "verification-error":
                    ErrorCode = ErrorCode.VerificationError;
                    break;

                case "fileexists-no-change":
                    ErrorCode = ErrorCode.FileExistsNoChange;
                    break;

                case "missingtitle":
                    ErrorCode = ErrorCode.MissingTitle;
                    break;

            }

        }

    }

    public class MediaWikiApiParseRequestResult :
        MediaWikiApiRequestResult {

        public string Text { get; set; } = "";

        public bool IsRedirect {
            get {
                return Text.StartsWith("#REDIRECT");
            }
        }

    }

    public class MediaWikiClient {

        public event Action<ILogMessage> Log;

        public string Protocol { get; set; } = "https";
        public string Server { get; set; }
        public string ApiPath { get; set; } = "/w";
        public string UserAgent { get; set; }

        //public string Username { get; set; }
        //public string Password { get; set; }

        public bool IsLoggedIn { get; set; } = false;

        public MediaWikiApiRequestResult Login(string username, string password) {

            if (IsLoggedIn)
                throw new Exception("Client is already logged in.");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return new MediaWikiApiRequestResult { Success = false };

            // Obtain a login token.

            LogInfo("requesting login token");

            string data = _http_get(_get_api_url() + "?action=query&meta=tokens&type=login&format=json");
            string token = JObject.Parse(data)["query"]["tokens"]["logintoken"].Value<string>();

            if (!string.IsNullOrEmpty(token)) {

                // POST login credentials.

                LogInfo(string.Format("logging in with username \"{0}\"", username));

                NameValueCollection values = new NameValueCollection {
                    ["lgname"] = username,
                    ["lgpassword"] = password,
                    ["lgtoken"] = token
                };

                data = _http_post(_get_api_url() + "?action=login&format=json", values);

                string success_string = JObject.Parse(data)["login"]["result"].Value<string>();

                IsLoggedIn = !string.IsNullOrEmpty(success_string) && success_string.ToLower() == "success";

                if (IsLoggedIn)
                    LogInfo(string.Format("logged in"));
                else
                    LogError(string.Format("login failed"));

            }
            else
                LogError("failed to obtain login token");

            return new MediaWikiApiRequestResult { Success = IsLoggedIn };

        }
        public MediaWikiApiParseRequestResult Parse(string title, ParseParameters parameters) {

            LogInfo(string.Format("parsing page \"{0}\"", title));

            string data = _http_get(_get_api_url() + string.Format("?action=parse&page={0}&prop=wikitext&format=json", title));
            JObject json = JObject.Parse(data);

            MediaWikiApiParseRequestResult result = new MediaWikiApiParseRequestResult();

            if (json.ContainsKey("parse")) {
                result.Text = json["parse"]["wikitext"]["*"].Value<string>();
            }
            else if (json.ContainsKey("error"))
                result.SetError(json["error"]["code"].Value<string>(), json["error"]["info"].Value<string>());

            return result;

        }
        public MediaWikiApiRequestResult Edit(string title, EditParameters parameters) {

            string token = _get_csrf_token();

            if (!string.IsNullOrEmpty(token)) {

                if (!IsLoggedIn)
                    LogWarn(string.Format("editing page \"{0}\" anonymously", title));
                else
                    LogInfo(string.Format("editing page \"{0}\"", title));

                // POST edit.

                NameValueCollection values = new NameValueCollection {
                    ["action"] = "edit",
                    ["format"] = "json",
                    ["title"] = title,
                    ["token"] = token
                };

                switch (parameters.Action) {

                    case EditAction.AppendText:
                        values["appendtext"] = parameters.Text;
                        break;

                    case EditAction.PrependText:
                        values["prependtext"] = parameters.Text;
                        break;

                    case EditAction.Text:
                        values["text"] = parameters.Text;
                        break;

                }

                string data = _http_post(_get_api_url(), values);

                string success_string = JObject.Parse(data)["edit"]["result"].Value<string>();
                bool success = !string.IsNullOrEmpty(success_string) && success_string.ToLower() == "success";

                if (success)
                    LogInfo(string.Format("edited page \"{0}\"", title));
                else
                    LogError(string.Format("failed to edit page \"{0}\"", title));

                return new MediaWikiApiRequestResult { Success = success };

            }

            return new MediaWikiApiRequestResult { Success = false };

        }
        public MediaWikiApiRequestResult Upload(UploadParameters parameters) {

            string token = _get_csrf_token();
            bool success = false;

            if (!string.IsNullOrEmpty(token)) {

                if (!IsLoggedIn)
                    LogWarn(string.Format("uploading file \"{0}\" anonymously", parameters.UploadFileName));
                else
                    LogInfo(string.Format("uploading file \"{0}\"", parameters.UploadFileName));

                if (Regex.IsMatch(parameters.FilePath, @"^https?://")) {

                    // Upload by URL.

                    NameValueCollection values = new NameValueCollection {
                        ["action"] = "upload",
                        ["format"] = "json",
                        ["filename"] = parameters.UploadFileName,
                        ["url"] = parameters.FilePath,
                        ["token"] = token,
                        ["ignorewarnings"] = "1",
                        ["text"] = parameters.Text,
                        ["comment"] = parameters.Comment,
                    };

                    string data = _http_post(_get_api_url(), values);

                    string success_string = string.Empty;

                    if (JObject.Parse(data).ContainsKey("upload"))
                        success_string = JObject.Parse(data)["upload"]["result"].Value<string>();
                    else if (JObject.Parse(data).ContainsKey("error")) {

                        // If an error occurred, return the error information to the caller.

                        return new MediaWikiApiRequestResult(
                            JObject.Parse(data)["error"]["code"].Value<string>(),
                            JObject.Parse(data)["error"]["info"].Value<string>());

                    }

                    success = !string.IsNullOrEmpty(success_string) && success_string.ToLower() == "success";

                }
                else
                    throw new Exception("Uploading local files is not yet supported.");

            }

            if (success)
                LogInfo(string.Format("uploaded file \"{0}\"", parameters.UploadFileName));
            else
                LogError(string.Format("failed to upload file \"{0}\"", parameters.UploadFileName));

            return new MediaWikiApiRequestResult { Success = success };

        }
        public MediaWikiApiRequestResult Delete(string title, DeleteParameters parameters) {

            string token = _get_csrf_token();

            if (!string.IsNullOrEmpty(token)) {

                if (!IsLoggedIn)
                    LogWarn(string.Format("deleting page \"{0}\" anonymously", title));
                else
                    LogInfo(string.Format("deleting page \"{0}\"", title));

                // POST edit.

                NameValueCollection values = new NameValueCollection {
                    ["action"] = "delete",
                    ["format"] = "json",
                    ["title"] = title,
                    ["token"] = token,
                    ["reason"] = parameters.Reason
                };

                string data = _http_post(_get_api_url(), values);

                bool success = !JObject.Parse(data).ContainsKey("error");

                if (success)
                    LogInfo(string.Format("deleted page \"{0}\"", title));
                else
                    LogError(string.Format("failed to deleted page \"{0}\"", title));

                return new MediaWikiApiRequestResult { Success = success };

            }

            return new MediaWikiApiRequestResult { Success = false };

        }

        private readonly CookieContainer _cookies = new CookieContainer();

        private string _get_api_url() {

            if (string.IsNullOrEmpty(Protocol) || string.IsNullOrEmpty(Server) || string.IsNullOrEmpty(ApiPath))
                throw new Exception("Protocol, Server, and ApiPath must be specified.");

            return string.Format("{0}://{1}{2}/api.php", Protocol, Server, ApiPath);

        }
        private WebClientEx _new_webclient() {

            WebClientEx client = new WebClientEx();

            client.Headers.Add(HttpRequestHeader.UserAgent, UserAgent);

            client.CookieContainer = _cookies;

            return client;

        }
        private string _get_csrf_token() {

            LogInfo("requesting CSRF token");

            string data = _http_get(_get_api_url() + "?action=query&format=json&meta=tokens");
            string token = JObject.Parse(data)["query"]["tokens"]["csrftoken"].Value<string>();

            if (string.IsNullOrEmpty(token))
                LogError("failed to obtain CSRF token");

            return token;

        }

        private string _http_get(string url) {

            using (WebClientEx client = _new_webclient())
                return client.DownloadString(url);

        }
        private string _http_post(string url, NameValueCollection data) {

            using (WebClientEx client = _new_webclient()) {

                byte[] response_bytes = client.UploadValues(url, "POST", data);
                string response_body = Encoding.UTF8.GetString(response_bytes);

                return response_body;

            }

        }

        private void OnLog(ILogMessage message) {

            if (Log is null)
                Console.WriteLine(message.ToString());
            else
                Log(message);

        }
        private void LogInfo(string message) {

            OnLog(new LogMessage(LogSeverity.Info, message, "mwclient"));

        }
        private void LogWarn(string message) {

            OnLog(new LogMessage(LogSeverity.Warning, message, "mwclient"));

        }
        private void LogError(string message) {

            OnLog(new LogMessage(LogSeverity.Error, message, "mwclient"));

        }

    }

}