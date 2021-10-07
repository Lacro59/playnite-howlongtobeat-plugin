using Playnite.SDK;
using CommonPluginsShared.Collections;

namespace HowLongToBeat.Models
{
    public class GameHowLongToBeatCollection : PluginItemCollection<GameHowLongToBeat>
    {
        public HltbUserStats UserHltbData { get; set; }

        public GameHowLongToBeatCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown) : base(path, type)
        {
        }
    }
}
