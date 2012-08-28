using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Platformer
{

    [JsonObject]
    class ReplayData
    {
#if DEBUG
        private const String BEST_REPLAY_URL = "http://plat-dev.paxdevdemo.com/replays/best/{0}";
        private const String REPLAY_UPLOAD_URL = "http://plat-dev.paxdevdemo.com/replays";
#else
        private const String BEST_REPLAY_URL = "http://platformer.paxdevdemo.com/replays/best/{0}";
        private const String REPLAY_UPLOAD_URL = "http://platformer.paxdevdemo.com/replays";
#endif

        


        public int HighScore { get; set; }
        public List<List<PlayerGhostData>> PlayerHistory { get; set; }


        private List<int> nextPlayerCursors = new List<int>(1);
        private List<PlayerGhostData> lastResults;
        private TimeSpan currentStreamDelta;

        private BackgroundWorker uploader;


        [JsonIgnore]
        public int StreamCount
        {
            get { return PlayerHistory.Count; }
        }

        public ReplayData()
        {
            PlayerHistory = new List<List<PlayerGhostData>>(1);
            uploader = new BackgroundWorker();
        }



        public List<PlayerGhostData> GetNextPlayerInputs(TimeSpan levelTime)
        {
            List<PlayerGhostData> result = new List<PlayerGhostData>(PlayerHistory.Count);

            for (int i = 0; i < PlayerHistory.Count; i++)
            {
                PlayerGhostData candidate = PlayerHistory[i][nextPlayerCursors[i]];
                if (levelTime > candidate.TotalGametime)
                {
                    if (nextPlayerCursors[i] + 1 < PlayerHistory[i].Count)
                        nextPlayerCursors[i]++;

                    lastResults[i] = candidate;
                    result.Add(candidate);
                }
                else
                {
                    result.Add(lastResults[i]);
                }
            }

            return result;
        }

        public void RecordPlayerInput(TimeSpan levelTime, float mouvement, bool isJumping)
        {
            List<PlayerGhostData> currentInputStream = PlayerHistory[PlayerHistory.Count - 1];
            PlayerGhostData lastInput = currentInputStream.LastOrDefault();

            if (mouvement != lastInput.Mouvement || isJumping != lastInput.IsJumping)
                currentInputStream.Add(new PlayerGhostData(levelTime - currentStreamDelta, mouvement, isJumping));
        }

        public void NewPlayerInputStream(TimeSpan newLifeTimeSinceLevelStart)
        {
            // initialize the player move history with an initial capacity of 50 recorded moves
            PlayerHistory.Add(new List<PlayerGhostData>(50));
            nextPlayerCursors.Add(0);
            currentStreamDelta = newLifeTimeSinceLevelStart;

        }

        public static void DownloadAndLoadRecordedDataAsync(int levelId, RunWorkerCompletedEventHandler onCompletedDo)
        {
            BackgroundWorker downloader = new BackgroundWorker();
            downloader.DoWork += new DoWorkEventHandler(downloader_DoWork);
            if (onCompletedDo != null)
                downloader.RunWorkerCompleted += onCompletedDo;
            downloader.RunWorkerAsync(levelId);
        }

        public void SaveAndUploadRecordedDataAsync(int levelId, String playerName, RunWorkerCompletedEventHandler onCompletedDo)
        {
            BackgroundWorker uploader = new BackgroundWorker();
            uploader.DoWork += new DoWorkEventHandler(uploader_DoWork);
            if (onCompletedDo != null)
                uploader.RunWorkerCompleted += onCompletedDo;
            uploader.RunWorkerAsync(new { levelId = levelId, player = playerName });
        }

        void uploader_DoWork(object sender, DoWorkEventArgs e)
        {
            var obj = Cast(e.Argument, new { levelId = 0, player = "" });
            int levelId = obj.levelId;
            String playerName = obj.player;

            String fileName = levelId.ToString("D4") + ".replay";
            JsonSerializer serializer = new JsonSerializer();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                using (GZipStream compressionStream = new GZipStream(fs, CompressionMode.Compress))
                {
                    using (BsonWriter writer = new BsonWriter(compressionStream))
                    {
                        serializer.Serialize(writer, this);
                    }
                }
            }

            // now do the upload
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("replay[level_id]", levelId.ToString());
            nvc.Add("replay[player]", playerName);
            nvc.Add("replay[score]", HighScore.ToString());

            HttpUploadFile(REPLAY_UPLOAD_URL,
                 fileName, "replay[data]", "application/octet-stream", nvc);

        }

        private T Cast<T>(object obj, T type)
        {
            return (T)obj;
        }

        private static void downloader_DoWork(object sender, DoWorkEventArgs e)
        {
            int levelId = (int)e.Argument;

            SaveFileFromURL(String.Format(BEST_REPLAY_URL, levelId),
                String.Format("{0}.replay", levelId.ToString("D4")), 5);



            String fileName = levelId.ToString("D4") + ".replay";
            if (File.Exists(fileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                using (FileStream fs = new FileStream(fileName, FileMode.Open))
                {
                    using (GZipStream compressionStream = new GZipStream(fs, CompressionMode.Decompress))
                    {
                        using (BsonReader reader = new BsonReader(compressionStream))
                        {
                            ReplayData result = serializer.Deserialize<ReplayData>(reader);
                            result.nextPlayerCursors = new List<int>(result.PlayerHistory.Count);
                            result.lastResults = new List<PlayerGhostData>(result.PlayerHistory.Count);

                            for (int i = 0; i < result.PlayerHistory.Count; i++)
                            {
                                result.nextPlayerCursors.Add(0);
                                result.lastResults.Add(PlayerGhostData.NoMovement);
                            }
                            e.Result = result;

                        }
                    }
                }
            }
            else
            {
                e.Result = null;
            }
        }




        #region Helper

        private static bool SaveFileFromURL(string url, string destinationFileName, int timeoutInSeconds)
        {
            // Create a web request to the URL
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = timeoutInSeconds * 1000;

            // Get the web response
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Make sure the response is valid
                if (HttpStatusCode.OK == response.StatusCode)
                {
                    // Open the response stream
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        // Open the destination file
                        using (FileStream fileStream = new FileStream(destinationFileName, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            // Create a 4K buffer to chunk the file
                            byte[] buffer = new byte[4096];
                            int bytesRead;
                            // Read the chunk of the web response into the buffer
                            while (0 < (bytesRead = responseStream.Read(buffer, 0, buffer.Length)))
                            {
                                // Write the chunk from the buffer to the file
                                fileStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                // this is a best effort thing, don't crash if we don't succeed
                Console.WriteLine("Could not load replay data from server!");
                Console.WriteLine(ex);

                return false;
            }
            return true;
        }

        public static bool HttpUploadFile(string url, string file, string paramName, string contentType, NameValueCollection nvc)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.Accept = "application/json";
            wr.KeepAlive = false;
            wr.Credentials = new System.Net.NetworkCredential("julien", "secret");

            using (Stream rs = wr.GetRequestStream())
            {
                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                foreach (string key in nvc.Keys)
                {
                    rs.Write(boundarybytes, 0, boundarybytes.Length);
                    string formitem = string.Format(formdataTemplate, key, nvc[key]);
                    byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                    rs.Write(formitembytes, 0, formitembytes.Length);
                }
                rs.Write(boundarybytes, 0, boundarybytes.Length);

                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                string header = string.Format(headerTemplate, paramName, file, contentType);
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                rs.Write(headerbytes, 0, headerbytes.Length);

                using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        rs.Write(buffer, 0, bytesRead);
                    }
                }


                byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                rs.Write(trailer, 0, trailer.Length);
                rs.Flush();
                rs.Close();

                WebResponse wresp = null;
                try
                {
                    wresp = wr.GetResponse();

                    return true;
                }
                catch (WebException error)
                {
                    Console.WriteLine("FAILED TO UPLOAD REPLAY FILE BECAUSE: " + error);
                    return false;
                }
                finally
                {
                    if (wresp != null)
                    {
                        wresp.Close();
                    }
                }
            }
        }

        #endregion
    }
}
