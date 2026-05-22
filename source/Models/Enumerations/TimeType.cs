using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models.Enumerations
{
    public enum TimeType
    {
        [Description("LOCHowLongToBeatMainStory")]
        MainStory,
        [Description("LOCHowLongToBeatMainExtra")]
        MainStoryExtra,
        [Description("LOCHowLongToBeatCompletionist")]
        Completionist,
        solo,
        CoOp,
        Versus
    }
}
