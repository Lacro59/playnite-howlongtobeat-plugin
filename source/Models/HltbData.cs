using CommonPluginsPlaynite.Converters;
using Playnite.SDK.Data;
using System.Collections.Generic;
using System.Globalization;

namespace HowLongToBeat.Models
{
    public class HltbData : ObservableObject
    {
        private PlayTimeToStringConverter converter = new PlayTimeToStringConverter();

        public long MainStory { get; set; } = 0;
        [DontSerialize]
        public string MainStoryFormat
        {
            get
            {
                if (MainStory == 0)
                {
                    return "--";
                }
                return (string)converter.Convert((long)MainStory, null, null, CultureInfo.CurrentCulture);
            }
        }
        public long MainExtra { get; set; }
        [DontSerialize]
        public string MainExtraFormat
        {
            get
            {
                if (MainExtra == 0)
                {
                    return "--";
                }
                return (string)converter.Convert((long)MainExtra, null, null, CultureInfo.CurrentCulture);
            }
        }
        public long Completionist { get; set; }
        [DontSerialize]
        public string CompletionistFormat
        {
            get
            {
                if (Completionist == 0)
                {
                    return "--";
                }
                return (string)converter.Convert((long)Completionist, null, null, CultureInfo.CurrentCulture);
            }
        }


        public long Solo { get; set; } = 0;
        [DontSerialize]
        public string SoloFormat
        {
            get
            {
                if (Solo == 0)
                {
                    return "--";
                }
                return (string)converter.Convert((long)Solo, null, null, CultureInfo.CurrentCulture);
            }
        }
        public long CoOp { get; set; } = 0;
        [DontSerialize]
        public string CoOpFormat
        {
            get
            {
                if (CoOp == 0)
                {
                    return "--";
                }
                return (string)converter.Convert((long)CoOp, null, null, CultureInfo.CurrentCulture);
            }
        }
        public long Vs { get; set; } = 0;
        [DontSerialize]
        public string VsFormat
        {
            get
            {
                if (Vs == 0)
                {
                    return "--";
                }
                return (string)converter.Convert((long)Vs, null, null, CultureInfo.CurrentCulture);
            }
        }


        [DontSerialize]
        public long TimeToBeat {
            get
            {
                if (MainStory != 0)
                {
                    return MainStory;
                }
                else if (MainExtra != 0)
                {
                    return MainExtra;
                }
                else if (Completionist != 0)
                {
                    return Completionist;
                }
                else if (Solo != 0)
                {
                    return Solo;
                }
                else if (CoOp != 0)
                {
                    return CoOp;
                }
                else if (Vs != 0)
                {
                    return Vs;
                }

                return 0;
            }
        }
        [DontSerialize]
        public string TimeToBeatFormat
        {
            get
            {
                if (MainStory != 0)
                {
                    return MainStoryFormat;
                }
                else if (MainExtra != 0)
                {
                    return MainExtraFormat;
                }
                else if (Completionist != 0)
                {
                    return CompletionistFormat;
                }
                else if (Solo != 0)
                {
                    return SoloFormat;
                }
                else if (CoOp != 0)
                {
                    return CoOpFormat;
                }
                else if (Vs != 0)
                {
                    return VsFormat;
                }

                return "--";
            }
        }
    }
}
