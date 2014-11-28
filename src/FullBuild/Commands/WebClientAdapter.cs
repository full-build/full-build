using System;
using System.Net;
using NLog;

namespace FullBuild.Commands
{
    class WebClientAdapter : IWebClient
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public bool TryDownloadString(Uri uri, out string result)
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    result = webClient.DownloadString(uri);
                    return true;
                }
                catch (WebException we)
                {
                    Logger.Debug("package not found for uri : {0} with error : {1}", uri, we);
                    result = string.Empty;
                    return false;
                }
            }
        }

        public string DownloadString(Uri uri)
        {
            using (var webClient = new WebClient())
            {
                return webClient.DownloadString(uri);
            }
        }

        public void DownloadFile(Uri address, string fileName)
        {
            using (var webClient = new WebClient())
            {
                webClient.DownloadFile(address, fileName);
            }
        }
    }
}