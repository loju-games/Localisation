using UnityEngine;
using UnityEngine.Networking;

namespace Loju.Localisation.Editor
{

    public sealed class GoogleSheetsDownloader
    {

        private const string kDownloadURL = "https://docs.google.com/spreadsheets/d/{0}/gviz/tq?tqx=out:csv&sheet={1}";
        private const string kOpenSheetURL = "https://docs.google.com/spreadsheets/d/{0}/edit#gid={1}";

        public readonly string documentID;
        public readonly string sheetNumber;
        public readonly string url;

        public bool IsDone { get { return _request != null && _request.isDone; } }

        private UnityWebRequest _request;
        private System.Action<bool, string> _onComplete;

        public GoogleSheetsDownloader(string documentID, string sheetNumber)
        {
            this.documentID = documentID;
            this.sheetNumber = sheetNumber;
            this.url = string.Format(kDownloadURL, documentID, sheetNumber);
        }

        public void SendRequest(System.Action<bool, string> onComplete)
        {
            _onComplete = onComplete;
            _request = UnityWebRequest.Get(url);

            AsyncOperation async = _request.SendWebRequest();
            async.completed += HandleCompleted;
        }

        private void HandleCompleted(AsyncOperation obj)
        {
            bool success = !_request.isHttpError && !_request.isNetworkError;
            string fileData = success ? _request.downloadHandler.text : null;
            if (_onComplete != null) _onComplete(success, fileData);

            // cleanup
            _request.Dispose();
            _request = null;
            _onComplete = null;
        }

        public static void OpenGoogleSheet(string documentID, string sheetID)
        {
            Application.OpenURL(string.Format(kOpenSheetURL, documentID, sheetID));
        }

    }

}