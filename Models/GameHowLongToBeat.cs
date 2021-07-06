using CommonPluginsShared.Collections;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models
{
    public class GameHowLongToBeat : PluginDataBaseGame<HltbDataUser>
    {
        private List<HltbDataUser> _Items = new List<HltbDataUser>();
        public override List<HltbDataUser> Items
        {
            get
            {
                return _Items;
            }

            set
            {
                _Items = value;
                OnPropertyChanged();
            }
        }

        [DontSerialize]
        public override bool HasData
        {
            get
            {
                if (Items?.Count > 0)
                {
                    return !Items.First().IsEmpty;
                }

                return false;
            }
        }

        [DontSerialize]
        public bool HasDataEmpty
        {
            get
            {
                if (Items?.Count > 0)
                {
                    return Items.First().IsEmpty;
                }

                return false;
            }
        }

        public HltbDataUser GetData()
        {
            if (Items?.Count == 0)
            {
                HltbDataUser hltbDataUser = new HltbDataUser();
                hltbDataUser.GameHltbData = new HltbData();
                return hltbDataUser;
            }

            return Items.First();
        }
    }
}
