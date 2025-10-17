using CommonPluginsShared.Converters;
using HowLongToBeat.Models.Enumerations;
using HowLongToBeat.Services;
using Playnite.SDK.Data;
using System.Collections.Generic;
using System.Globalization;

namespace HowLongToBeat.Models
{
    public class HltbData : ObservableObject
    {
        private static HowLongToBeatDatabase PluginDatabase => HowLongToBeat.PluginDatabase;
        private PlayTimeToStringConverterWithZero Converter => new PlayTimeToStringConverterWithZero();

        public GameType GameType { get; set; } = GameType.Game;

        [DontSerialize]
        public DataType DataType
        {
            get
            {
                if (PluginDatabase.PluginSettings.Settings.UseHtltbMedian && MainStoryMedian != 0)
                {
                    return DataType.Median;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbAverage && MainStoryAverage != 0)
                {
                    return DataType.Average;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbRushed && MainStoryRushed != 0)
                {
                    return DataType.Rushed;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbLeisure && MainStoryLeisure != 0)
                {
                    return DataType.Leisure;
                }

                return DataType.Classic;
            }
        }


        [DontSerialize]
        public long MainStory
        {
            get
            {
                if (PluginDatabase.PluginSettings.Settings.UseHtltbMedian && MainStoryMedian != 0)
                {
                    return MainStoryMedian;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbAverage && MainStoryAverage != 0)
                {
                    return MainStoryAverage;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbRushed && MainStoryRushed != 0)
                {
                    return MainStoryRushed;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbLeisure && MainStoryLeisure != 0)
                {
                    return MainStoryLeisure;
                }

                return MainStoryClassic;
            }
        }
        public long MainStoryClassic { get; set; } = 0;
        public long MainStoryMedian { get; set; } = 0;
        public long MainStoryAverage { get; set; } = 0;
        public long MainStoryRushed { get; set; } = 0;
        public long MainStoryLeisure { get; set; } = 0;

        [DontSerialize]
        public string MainStoryFormat => MainStory == 0 ? "--" : (string)Converter.Convert(MainStory, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string MainStoryClassicFormat => MainStoryClassic == 0 ? "--" : (string)Converter.Convert(MainStoryClassic, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string MainStoryMedianFormat => MainStoryMedian == 0 ? "--" : (string)Converter.Convert(MainStoryMedian, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string MainStoryAverageFormat => MainStoryAverage == 0 ? "--" : (string)Converter.Convert(MainStoryAverage, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string MainStoryRushedFormat => MainStoryRushed == 0 ? "--" : (string)Converter.Convert(MainStoryRushed, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string MainStoryLeisureFormat => MainStoryLeisure == 0 ? "--" : (string)Converter.Convert(MainStoryLeisure, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);


        [DontSerialize]
        public long MainExtra
        {
            get
            {
                if (PluginDatabase.PluginSettings.Settings.UseHtltbMedian && MainExtraMedian != 0)
                {
                    return MainExtraMedian;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbAverage && MainExtraAverage != 0)
                {
                    return MainExtraAverage;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbRushed && MainExtraRushed != 0)
                {
                    return MainExtraRushed;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbLeisure && MainExtraLeisure != 0)
                {
                    return MainExtraLeisure;
                }

                return MainExtraClassic;
            }
        }
        public long MainExtraClassic { get; set; } = 0;
        public long MainExtraMedian { get; set; } = 0;
        public long MainExtraAverage { get; set; } = 0;
        public long MainExtraRushed { get; set; } = 0;
        public long MainExtraLeisure { get; set; } = 0;
        [DontSerialize]
        public string MainExtraFormat => MainExtra == 0 ? "--" : (string)Converter.Convert(MainExtra, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string MainExtraClassicFormat => MainExtraClassic == 0 ? "--" : (string)Converter.Convert(MainExtraClassic, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string MainExtraMedianFormat => MainExtraMedian == 0 ? "--" : (string)Converter.Convert(MainExtraMedian, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string MainExtraAverageFormat => MainExtraAverage == 0 ? "--" : (string)Converter.Convert(MainExtraAverage, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string MainExtraRushedFormat => MainExtraRushed == 0 ? "--" : (string)Converter.Convert(MainExtraRushed, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string MainExtraLeisureFormat => MainExtraLeisure == 0 ? "--" : (string)Converter.Convert(MainExtraLeisure, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);


        [DontSerialize]
        public long Completionist
        {
            get
            {
                if (PluginDatabase.PluginSettings.Settings.UseHtltbMedian && CompletionistMedian != 0)
                {
                    return CompletionistMedian;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbAverage && CompletionistAverage != 0)
                {
                    return CompletionistAverage;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbRushed && CompletionistRushed != 0)
                {
                    return CompletionistRushed;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbLeisure && CompletionistLeisure != 0)
                {
                    return CompletionistLeisure;
                }

                return CompletionistClassic;
            }
        }
        public long CompletionistClassic { get; set; } = 0;
        public long CompletionistMedian { get; set; } = 0;
        public long CompletionistAverage { get; set; } = 0;
        public long CompletionistRushed { get; set; } = 0;
        public long CompletionistLeisure { get; set; } = 0;
        [DontSerialize]
        public string CompletionistFormat => Completionist == 0 ? "--" : (string)Converter.Convert(Completionist, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string CompletionistClassicFormat => CompletionistClassic == 0 ? "--" : (string)Converter.Convert(CompletionistClassic, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string CompletionistMedianFormat => CompletionistMedian == 0 ? "--" : (string)Converter.Convert(CompletionistMedian, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string CompletionistAverageFormat => CompletionistAverage == 0 ? "--" : (string)Converter.Convert(CompletionistAverage, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string CompletionistRushedFormat => CompletionistRushed == 0 ? "--" : (string)Converter.Convert(CompletionistRushed, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string CompletionistLeisureFormat => CompletionistLeisure == 0 ? "--" : (string)Converter.Convert(CompletionistLeisure, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);


        [DontSerialize]
        public long Solo
        {
            get
            {
                if (PluginDatabase.PluginSettings.Settings.UseHtltbMedian && SoloMedian != 0)
                {
                    return SoloMedian;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbAverage && SoloAverage != 0)
                {
                    return SoloAverage;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbRushed && SoloRushed != 0)
                {
                    return SoloRushed;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbLeisure && SoloLeisure != 0)
                {
                    return SoloLeisure;
                }

                return SoloClassic;
            }
        }
        public long SoloClassic { get; set; } = 0;
        public long SoloMedian { get; set; } = 0;
        public long SoloAverage { get; set; } = 0;
        public long SoloRushed { get; set; } = 0;
        public long SoloLeisure { get; set; } = 0;
        [DontSerialize]
        public string SoloFormat => Solo == 0 ? "--" : (string)Converter.Convert(Solo, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string SoloClassicFormat => SoloClassic == 0 ? "--" : (string)Converter.Convert(SoloClassic, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string SoloMedianFormat => SoloMedian == 0 ? "--" : (string)Converter.Convert(SoloMedian, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string SoloAverageFormat => SoloAverage == 0 ? "--" : (string)Converter.Convert(SoloAverage, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string SoloRushedFormat => SoloRushed == 0 ? "--" : (string)Converter.Convert(SoloRushed, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string SoloLeisureFormat => SoloLeisure == 0 ? "--" : (string)Converter.Convert(SoloLeisure, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);


        [DontSerialize]
        public long CoOp
        {
            get
            {
                if (PluginDatabase.PluginSettings.Settings.UseHtltbMedian && CoOpMedian != 0)
                {
                    return CoOpMedian;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbAverage && CoOpAverage != 0)
                {
                    return CoOpAverage;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbRushed && CoOpRushed != 0)
                {
                    return CoOpRushed;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbLeisure && CoOpLeisure != 0)
                {
                    return CoOpLeisure;
                }

                return CoOpClassic;
            }
        }
        public long CoOpClassic { get; set; } = 0;
        public long CoOpMedian { get; set; } = 0;
        public long CoOpAverage { get; set; } = 0;
        public long CoOpRushed { get; set; } = 0;
        public long CoOpLeisure { get; set; } = 0;
        [DontSerialize]
        public string CoOpFormat => CoOp == 0 ? "--" : (string)Converter.Convert(CoOp, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string CoOpClassicFormat => CoOpClassic == 0 ? "--" : (string)Converter.Convert(CoOpClassic, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string CoOpMedianFormat => CoOpMedian == 0 ? "--" : (string)Converter.Convert(CoOpMedian, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string CoOpAverageFormat => CoOpAverage == 0 ? "--" : (string)Converter.Convert(CoOpAverage, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string CoOpRushedFormat => CoOpRushed == 0 ? "--" : (string)Converter.Convert(CoOpRushed, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string CoOpLeisureFormat => CoOpLeisure == 0 ? "--" : (string)Converter.Convert(CoOpLeisure, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);


        [DontSerialize]
        public long Vs
        {
            get
            {
                if (PluginDatabase.PluginSettings.Settings.UseHtltbMedian && VsMedian != 0)
                {
                    return VsMedian;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbAverage && VsAverage != 0)
                {
                    return VsAverage;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbRushed && VsRushed != 0)
                {
                    return VsRushed;
                }
                if (PluginDatabase.PluginSettings.Settings.UseHtltbLeisure && VsLeisure != 0)
                {
                    return VsLeisure;
                }

                return VsClassic;
            }
        }
        public long VsClassic { get; set; } = 0;
        public long VsMedian { get; set; } = 0;
        public long VsAverage { get; set; } = 0;
        public long VsRushed { get; set; } = 0;
        public long VsLeisure { get; set; } = 0;
        [DontSerialize]
        public string VsFormat => Vs == 0 ? "--" : (string)Converter.Convert(Vs, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string VsClassicFormat => VsClassic == 0 ? "--" : (string)Converter.Convert(VsClassic, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string VsMedianFormat => VsMedian == 0 ? "--" : (string)Converter.Convert(VsMedian, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string VsAverageFormat => VsAverage == 0 ? "--" : (string)Converter.Convert(VsAverage, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string VsRushedFormat => VsRushed == 0 ? "--" : (string)Converter.Convert(VsRushed, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);
        [DontSerialize]
        public string VsLeisureFormat => VsLeisure == 0 ? "--" : (string)Converter.Convert(VsLeisure, null, PluginDatabase.PluginSettings.Settings.IntegrationViewItemOnlyHour ? PlayTimeFormat.RoundHour : PlayTimeFormat.DefaultFormat, CultureInfo.CurrentCulture);


        [DontSerialize]
        public long TimeToBeat
        {
            get
            {
                if (GameType != GameType.Multi)
                {
                    switch (PluginDatabase.PluginSettings.Settings.PreferredForTimeToBeat)
                    {
                        case TimeType.MainStory:
                            if (MainStory != 0)
                            {
                                return MainStory;
                            }
                            break;
                        case TimeType.MainStoryExtra:
                            if (MainExtra != 0)
                            {
                                return MainExtra;
                            }
                            break;
                        case TimeType.Completionist:
                            if (Completionist != 0)
                            {
                                return Completionist;
                            }
                            break;

                        default:
                            break;
                    }

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
                    switch (PluginDatabase.PluginSettings.Settings.PreferredForTimeToBeat)
                    {
                        case TimeType.MainStory:
                            if (MainStory != 0)
                            {
                                return MainStoryFormat;
                            }
                            break;
                        case TimeType.MainStoryExtra:
                            if (MainExtra != 0)
                            {
                                return MainExtraFormat;
                            }
                            break;
                        case TimeType.Completionist:
                            if (Completionist != 0)
                            {
                                return CompletionistFormat;
                            }
                            break;

                        default:
                            break;
                    }

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
