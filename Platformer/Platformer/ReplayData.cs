using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Platformer
{
    [Serializable]
    class ReplayData
    {

        public int HighScore { get; set; }

        private List<List<PlayerGhostData>> playerHistory;
        
        [NonSerialized]
        private List<int> nextPlayerCursors = new List<int>(1);
        [NonSerialized]
        private List<PlayerGhostData> lastResults;
        [NonSerialized]
        private TimeSpan currentStreamDelta;

        public int StreamCount
        {
            get { return playerHistory.Count; }
        }

        public ReplayData() {
            playerHistory = new List<List<PlayerGhostData>>(1);
        }

        public List<PlayerGhostData> GetNextPlayerInputs(TimeSpan levelTime) {
            List<PlayerGhostData> result = new List<PlayerGhostData>(playerHistory.Count);

            for(int i=0;i<playerHistory.Count;i++)
            {
                PlayerGhostData candidate = playerHistory[i][nextPlayerCursors[i]];
                if (levelTime > candidate.TotalGametime)
                {
                    if (nextPlayerCursors[i] + 1 < playerHistory[i].Count)
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
            List<PlayerGhostData> currentInputStream = playerHistory[playerHistory.Count - 1];
            PlayerGhostData lastInput = currentInputStream.LastOrDefault();

            if(mouvement != lastInput.Mouvement || isJumping != lastInput.IsJumping)
                currentInputStream.Add(new PlayerGhostData(levelTime - currentStreamDelta, mouvement, isJumping));
        }

        public void NewPlayerInputStream(TimeSpan newLifeTimeSinceLevelStart)
        {
            // initialize the player move history with an initial capacity of 50 recorded moves
            playerHistory.Add(new List<PlayerGhostData>(50));
            nextPlayerCursors.Add(0);
            currentStreamDelta = newLifeTimeSinceLevelStart;
        }


        public static ReplayData LoadRecordedData(int levelIndex)
        {
            String fileName = levelIndex.ToString("D4") +  ".replay";
            if (File.Exists(fileName))
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    ReplayData result = (ReplayData) bf.Deserialize(fs);
                    result.nextPlayerCursors = new List<int>(result.playerHistory.Count);
                    result.lastResults = new List<PlayerGhostData>(result.playerHistory.Count);

                    for (int i = 0; i < result.playerHistory.Count; i++)
                    {
                        result.nextPlayerCursors.Add(0);
                        result.lastResults.Add(PlayerGhostData.NoMovement);
                    }
                    return result;
                }
            }
            else
            {
                return null;
            }
        }

        public void SaveRecordedData(int levelIndex)
        {
            String fileName = levelIndex.ToString("D4") + ".replay";

            using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, this);
            }
        }
    }
}
