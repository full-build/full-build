using System;

namespace FullBuild.Commands
{
    internal interface IWebClient
    {
        bool TryDownloadString(Uri uri, out string result);
        string DownloadString(Uri uri);
        void DownloadFile(Uri address, string fileName);
    }
}