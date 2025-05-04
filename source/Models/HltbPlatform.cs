using HowLongToBeat.Models.Enumerations;
using Playnite.SDK.Models;
using System;
using System.ComponentModel;

namespace HowLongToBeat.Models
{
    public class HltbPlatformMatch : IComparable<HltbPlatformMatch> {

        public Platform Platform { get; set; }
        public HltbPlatform? HltbPlatform { get; set; } = null;

        public int CompareTo(HltbPlatformMatch other) {
            return other == null ? 1 : Platform.Name.CompareTo(other.Platform.Name);
        }
    }
}
