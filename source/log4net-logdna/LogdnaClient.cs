using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;

namespace log4net.logdna
{
    internal class LogdnaClient : ILogdnaClient
    {
        private readonly Config _config;
        private bool _isTokenValid = true;
        private readonly string _url;

        // exposing way how web request is created to allow integration testing
        internal static Func<Config, string, WebRequest> WebRequestFactory = CreateWebRequest;

        public LogdnaClient(Config config)
        {
            _config = config;
            _url = BuildUrl(config);
        }

        public void Send(string[] messagesBuffer, int numberOfMessages)
        {
            var ingests = new
            {
                lines = new System.Collections.Generic.List<JObject>()
            };

            //avoid number of messages more than buffer count
            if (numberOfMessages > messagesBuffer.Length) numberOfMessages = messagesBuffer.Length;

            for (int i = 0; i < numberOfMessages; i++)
            {
                if (CanBeJson(messagesBuffer[i]))
                {
                    JObject orgMessage = JObject.Parse(messagesBuffer[i]);

                    ingests.lines.Add(orgMessage);
                }
            }

            string message = JsonConvert.SerializeObject(ingests,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                });

            int currentRetry = 0;
            // setting MaxSendRetries means that we retry forever, we never throw away logs without delivering them
            while (_isTokenValid && (_config.MaxSendRetries < 0 || currentRetry <= _config.MaxSendRetries))
            {
                try
                {
                    SendToLogdna(message);
                    break;
                }
                catch (WebException e)
                {
                    var response = (HttpWebResponse)e.Response;
                    if (response != null && response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        _isTokenValid = false;
                        ErrorReporter.ReportError($"LogdnaClient: Provided Logdna customer token '{_config.CustomerToken}' is invalid. No logs will be sent to Logdna.");
                    }
                    else
                    {
                        ErrorReporter.ReportError($"LogdnaClient: Error sending logs to Logdna: {e.Message}");
                    }

                    currentRetry++;
                    if (currentRetry > _config.MaxSendRetries)
                    {
                        ErrorReporter.ReportError($"LogdnaClient: Maximal number of retries ({_config.MaxSendRetries}) reached. Discarding current batch of logs and moving on to the next one.");
                    }
                }
            }
        }

        private bool CanBeJson(string message)
        {
            // This loop is about 2x faster than message.TrimStart().StartsWith("{") and about 4x faster than Regex("^\s*\{")
            foreach (var t in message)
            {
                // skip leading whitespaces
                if (char.IsWhiteSpace(t))
                {
                    continue;
                }
                // if first character after whitespace is { then this can be a JSON, otherwise not
                if (t == '{')
                {
                    return true;
                }

                return false;
            }

            return false;
        }

        private static string BuildUrl(Config config)
        {
            //ref: https://docs.logdna.com/reference#logsingest

            StringBuilder sb = new StringBuilder(config.RootUrl);
            if (sb.Length > 0 && sb[sb.Length - 1] != '/')
            {
                sb.Append("/");
            }

            sb.Append("logs/ingest");
            sb.Append("?");
            sb.AppendFormat("hostname={0}", Environment.MachineName);
            //sb.Append("&");
            //sb.AppendFormat("now={0}", DateTime.Now.ToString(@"yyyy-MM-ddTHH:mm:ss.fff"));
            //sb.Append("&");
            //sb.AppendFormat("mac={0}", "");
            //sb.Append("&");
            //sb.AppendFormat("ip={0}", "");

            return sb.ToString();
        }

        private void SendToLogdna(string message)
        {
            var webRequest = WebRequestFactory(_config, _url);
            using (var dataStream = webRequest.GetRequestStream())
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                dataStream.Write(bytes, 0, bytes.Length);
                dataStream.Flush();
                dataStream.Close();
            }
            var webResponse = webRequest.GetResponse();
            webResponse.Close();
        }

        internal static WebRequest CreateWebRequest(Config config, string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ReadWriteTimeout = request.Timeout = config.TimeoutInSeconds * 1000;
            request.UserAgent = config.UserAgent;
            request.KeepAlive = true;
            request.ContentType = "application/json";

            #region auth

            String username = "INSERT_INGESTION_KEY";
            String password = config.CustomerToken;
            String encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
            request.Headers.Add("Authorization", "Basic " + encoded);

            #endregion auth

            return request;
        }
    }
}