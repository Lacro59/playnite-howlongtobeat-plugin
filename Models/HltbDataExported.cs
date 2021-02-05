using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyCsvParser.Mapping;

namespace HowLongToBeat.Models
{
    public class HltbDataExported
    {
        public string Title { get; set; }
        public string Platform { get; set; }
        public string Category { get; set; }
        public string Progress { get; set; }
        public string MainStory { get; set; }
        public string MainExtras { get; set; }
        public string Completionist { get; set; }
        public string SpeedAny { get; set; }
        public string Speed100 { get; set; }
        public DateTime Completed { get; set; }
    }

    public class CsvHltbDataExportedMapping : CsvMapping<HltbDataExported>
    {
        public CsvHltbDataExportedMapping() : base()
        {
            MapProperty(0, x => x.Title);
            MapProperty(1, x => x.Platform);
            MapProperty(2, x => x.Category);
            MapProperty(3, x => x.Progress);
            MapProperty(4, x => x.MainStory);
            MapProperty(5, x => x.MainExtras);
            MapProperty(6, x => x.Completionist);
            MapProperty(7, x => x.SpeedAny);
            MapProperty(8, x => x.Speed100);
            MapProperty(9, x => x.Completed);
        }
    }
}
