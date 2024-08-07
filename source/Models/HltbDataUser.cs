﻿using Playnite.SDK.Data;
using System.Collections.Generic;

namespace HowLongToBeat.Models
{
    public class HltbDataUser : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string UrlImg { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public GameType GameType { get; set; } = GameType.Game;

        public HltbData GameHltbData { get; set; }
        [DontSerialize]
        public HltbData GameHltbDataByType => GameType == GameType.Multi
                    ? new HltbData
                    {
                        Solo = GameHltbData.Solo,
                        CoOp = GameHltbData.CoOp,
                        Vs = GameHltbData.Vs
                    }
                    : new HltbData
                    {
                        MainStory = GameHltbData.MainStory,
                        MainExtra = GameHltbData.MainExtra,
                        Completionist = GameHltbData.Completionist
                    };

        [DontSerialize]
        public bool IsEmpty => GameHltbData == null || (GameHltbData.MainStory == 0 && GameHltbData.MainExtra == 0 && GameHltbData.Completionist == 0 && GameHltbData.Solo == 0 && GameHltbData.CoOp == 0 && GameHltbData.Vs == 0);


        public bool IsVndb { get; set; }
    }

    public enum GameType
    {
        Game, Multi, Compil
    }
}
