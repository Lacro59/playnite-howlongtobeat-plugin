using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models
{
    class HltbSearchRoot
    {
        public string color { get; set; }
        public string title { get; set; }
        public string category { get; set; }
        public int count { get; set; }
        public int pageCurrent { get; set; }
        public int? pageTotal { get; set; }
        public int pageSize { get; set; }
        public List<HltbSearchData> data { get; set; }
        public object displayModifier { get; set; }
    }
}
