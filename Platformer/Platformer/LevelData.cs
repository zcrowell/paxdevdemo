using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Platformer
{
    class LevelData
    {
        public int Id { get; set; }
        public String Content { get; set; }
        public String Name { get; set; }

        public static List<LevelData> ParseJson(JArray arrayOfLevels)
        {
            List<LevelData> result = new List<LevelData>();
            foreach (JObject jsonLevel in arrayOfLevels)
            {
                LevelData level = new LevelData();
                level.Id = (int)jsonLevel["id"];
                level.Content = (String)jsonLevel["content"];
                level.Name = (String)jsonLevel["name"];
                result.Add(level);
            }
            return result;
        }

    }
}
