using CommonShared.Collections;
using Newtonsoft.Json;
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

        [JsonIgnore]
        public override bool HasData
        {
            get
            {
                if (Items.Count > 0)
                {
                    return Items.First().IsEmpty;
                }

                return false;
            }
        }

        public HltbDataUser GetData()
        {
            return Items.First();
        }
    }
}
