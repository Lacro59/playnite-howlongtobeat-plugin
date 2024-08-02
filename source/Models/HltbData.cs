using CommonPluginsShared.Converters;
using Playnite.SDK.Data;
using System.Collections.Generic;
using System.Globalization;

namespace HowLongToBeat.Models
{
    public class HltbData : ObservableObject
    {
        private PlayTimeToStringConverterWithZero Converter => new PlayTimeToStringConverterWithZero();

        public GameType GameType { get; set; } = GameType.Game;

        public long MainStory { get; set; } = 0;
        [DontSerialize]
        public string MainStoryFormat => MainStory == 0 ? "--" : (string)Converter.Convert(MainStory, null, null, CultureInfo.CurrentCulture);

        public long MainExtra { get; set; }
        [DontSerialize]
        public string MainExtraFormat => MainExtra == 0 ? "--" : (string)Converter.Convert(MainExtra, null, null, CultureInfo.CurrentCulture);

        public long Completionist { get; set; }
        [DontSerialize]
        public string CompletionistFormat => Completionist == 0 ? "--" : (string)Converter.Convert(Completionist, null, null, CultureInfo.CurrentCulture);


        public long Solo { get; set; } = 0;
        [DontSerialize]
        public string SoloFormat => Solo == 0 ? "--" : (string)Converter.Convert(Solo, null, null, CultureInfo.CurrentCulture);

        public long CoOp { get; set; } = 0;
        [DontSerialize]
        public string CoOpFormat => CoOp == 0 ? "--" : (string)Converter.Convert(CoOp, null, null, CultureInfo.CurrentCulture);

        public long Vs { get; set; } = 0;
        [DontSerialize]
        public string VsFormat => Vs == 0 ? "--" : (string)Converter.Convert(Vs, null, null, CultureInfo.CurrentCulture);


        [DontSerialize]
        public long TimeToBeat
        {
            get
            {
                if (GameType != GameType.Multi)
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
                }
                else
                {
                    if (Solo != 0)
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
                }

                return 0;
            }
        }
        [DontSerialize]
        public string TimeToBeatFormat
        {
            get
            {
                if (GameType != GameType.Multi)
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
                }
                else
                {
                    if (Solo != 0)
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
                }

                return "--";
            }
        }
    }
}
