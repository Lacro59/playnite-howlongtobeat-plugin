﻿using Playnite.SDK;
using PluginCommon.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HowLongToBeat.Models
{
    public class GameHowLongToBeatCollection : PluginItemCollection<GameHowLongToBeat>
    {
        public GameHowLongToBeatCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown) : base(path, type)
        {
        }
    }
}