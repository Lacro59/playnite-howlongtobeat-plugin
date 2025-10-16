using CommonPluginsShared.Collections;
using CommonPluginsShared.Models;
using Playnite.SDK.Data;
using System.Linq;

namespace HowLongToBeat.Models
{
    public class GameHowLongToBeat : PluginDataBaseGame<HltbDataUser>
    {
        [DontSerialize]
        public override bool HasData => Items?.Count > 0 && Items?.First() != null;

        [DontSerialize]
        public bool HasDataEmpty => Items?.Count > 0 && Items?.First() != null && Items.First().IsEmpty;

        [DontSerialize]
        public SourceLink SourceLink => new SourceLink
        {
            GameName = GetData()?.Name,
            Name = "HowLongToBeat",
            Url = GetData()?.Url
        };


        public string UserGameId { get; set; }


        public HltbDataUser GetData()
        {
            return Items != null && Items?.Count == 0 ? new HltbDataUser { GameHltbData = new HltbData() } : Items.First();
        }
    }
}