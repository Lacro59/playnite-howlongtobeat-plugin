using HowLongToBeat.Models.Enumerations;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.ComponentModel;

namespace HowLongToBeat.Models
{
    public class HltbPlatformMatch : IComparable<HltbPlatformMatch>
    {
        public Platform Platform { get; set; }
        public HltbPlatform? HltbPlatform { get; set; } = null;

        [DontSerialize]
        public string PlaynitePlatformId => Platform?.Id.ToString() ?? string.Empty;

        [DontSerialize]
        public bool HasPlayniteSpecificationId => !string.IsNullOrEmpty(Platform?.SpecificationId);

        [DontSerialize]
        public string PlayniteSpecificationId => Platform?.SpecificationId ?? string.Empty;

        [DontSerialize]
        public bool IsHltbPlatformConfigured => HltbPlatform != null;

        [DontSerialize]
        public string HltbPlatformDisplay => HltbPlatform?.GetDescription() ?? string.Empty;

        public int CompareTo(HltbPlatformMatch other)
        {
            return other == null ? 1 : Platform.Name.CompareTo(other.Platform.Name);
        }
    }
}
