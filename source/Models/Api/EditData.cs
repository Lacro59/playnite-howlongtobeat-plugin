using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models.Api
{
    public class Additionals
    {
        [SerializationPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [SerializationPropertyName("video")]
        public string Video { get; set; } = string.Empty;
    }

    public class Comp100
    {
        [SerializationPropertyName("time")]
        public Time Time { get; set; } = new Time();

        [SerializationPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;
    }

    public class Date
    {
        [SerializationPropertyName("year")]
        public string Year { get; set; } = "0000";

        [SerializationPropertyName("month")]
        public string Month { get; set; } = "00";

        [SerializationPropertyName("day")]
        public string Day { get; set; } = "00";
    }

    public class CompMain
    {
        [SerializationPropertyName("time")]
        public Time Time { get; set; } = new Time();

        [SerializationPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;
    }

    public class CompPlus
    {
        [SerializationPropertyName("time")]
        public Time Time { get; set; } = new Time();

        [SerializationPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;
    }

    public class CoOp
    {
        [SerializationPropertyName("time")]
        public Time Time { get; set; } = new Time();
    }

    public class CustomLabels
    {
        [SerializationPropertyName("custom")]
        public string Custom { get; set; } = string.Empty;

        [SerializationPropertyName("custom2")]
        public string Custom2 { get; set; } = string.Empty;

        [SerializationPropertyName("custom3")]
        public string Custom3 { get; set; } = string.Empty;
    }

    public class General
    {
        [SerializationPropertyName("progress")]
        public Time Progress { get; set; } = new Time();

        [SerializationPropertyName("retirementNotes")]
        public string RetirementNotes { get; set; } = string.Empty;

        [SerializationPropertyName("completionDate")]
        public Date CompletionDate { get; set; } = new Date();

        [SerializationPropertyName("startDate")]
        public Date StartDate { get; set; } = new Date();

        [SerializationPropertyName("progressBefore")]
        public Time ProgressBefore { get; set; } = new Time();
    }

    public class Lists
    {
        [SerializationPropertyName("playing")]
        public bool Playing { get; set; } = true;

        [SerializationPropertyName("backlog")]
        public bool Backlog { get; set; }

        [SerializationPropertyName("replay")]
        public bool Replay { get; set; }

        [SerializationPropertyName("custom")]
        public bool Custom { get; set; }

        [SerializationPropertyName("custom2")]
        public bool Custom2 { get; set; }

        [SerializationPropertyName("custom3")]
        public bool Custom3 { get; set; }

        [SerializationPropertyName("completed")]
        public bool Completed { get; set; }

        [SerializationPropertyName("retired")]
        public bool Retired { get; set; }
    }

    public class ManualTimer
    {
        [SerializationPropertyName("time")]
        public Time Time { get; set; } = new Time();
    }

    public class MultiPlayer
    {
        [SerializationPropertyName("coOp")]
        public CoOp CoOp { get; set; } = new CoOp();

        [SerializationPropertyName("vs")]
        public Vs Vs { get; set; } = new Vs();
    }

    public class Perc100
    {
        [SerializationPropertyName("time")]
        public Time Time { get; set; } = new Time();

        [SerializationPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;
    }

    public class PercAny
    {
        [SerializationPropertyName("time")]
        public Time Time { get; set; } = new Time();

        [SerializationPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;
    }

    public class Review
    {
        [SerializationPropertyName("score")]
        public int Score { get; set; }

        [SerializationPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;
    }

    public class EditData
    {
        [SerializationPropertyName("submissionId")]
        public int SubmissionId { get; set; }

        [SerializationPropertyName("userIp")]
        public string UserIp { get; set; }

        [SerializationPropertyName("userId")]
        public int UserId { get; set; }

        [SerializationPropertyName("userName")]
        public string UserName { get; set; }

        [SerializationPropertyName("gameId")]
        public int GameId { get; set; }

        [SerializationPropertyName("title")]
        public string Title { get; set; }

        [SerializationPropertyName("platform")]
        public string Platform { get; set; }

        [SerializationPropertyName("storefront")]
        public string Storefront { get; set; }

        [SerializationPropertyName("lists")]
        public Lists Lists { get; set; } = new Lists();

        [SerializationPropertyName("general")]
        public General General { get; set; } = new General();

        [SerializationPropertyName("singlePlayer")]
        public SinglePlayer SinglePlayer { get; set; } = new SinglePlayer();

        [SerializationPropertyName("speedRuns")]
        public SpeedRuns SpeedRuns { get; set; } = new SpeedRuns();

        [SerializationPropertyName("multiPlayer")]
        public MultiPlayer MultiPlayer { get; set; } = new MultiPlayer();

        [SerializationPropertyName("review")]
        public Review Review { get; set; } = new Review();

        [SerializationPropertyName("additionals")]
        public Additionals Additionals { get; set; } = new Additionals();

        [SerializationPropertyName("manualTimer")]
        public ManualTimer ManualTimer { get; set; } = new ManualTimer();

        [SerializationPropertyName("adminId")]
        public object AdminId { get; set; }

        [SerializationPropertyName("customLabels")]
        public CustomLabels CustomLabels { get; set; } = new CustomLabels();
    }

    public class SinglePlayer
    {
        [SerializationPropertyName("playCount")]
        public bool PlayCount { get; set; }

        [SerializationPropertyName("includesDLC")]
        public bool IncludesDLC { get; set; }

        [SerializationPropertyName("compMain")]
        public CompMain CompMain { get; set; } = new CompMain();

        [SerializationPropertyName("compPlus")]
        public CompPlus CompPlus { get; set; } = new CompPlus();

        [SerializationPropertyName("comp100")]
        public Comp100 Comp100 { get; set; } = new Comp100();
    }

    public class SpeedRuns
    {
        [SerializationPropertyName("percAny")]
        public PercAny PercAny { get; set; } = new PercAny();

        [SerializationPropertyName("perc100")]
        public Perc100 Perc100 { get; set; } = new Perc100();
    }

    public class Time
    {
        [SerializationPropertyName("hours")]
        public int? Hours { get; set; } = null;

        [SerializationPropertyName("minutes")]
        public int? Minutes { get; set; } = null;

        [SerializationPropertyName("seconds")]
        public int? Seconds { get; set; } = null;
    }

    public class Vs
    {
        [SerializationPropertyName("time")]
        public Time Time { get; set; } = new Time();
    }
}
