using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HowLongToBeat.Models
{
    public enum HltbStorefront
    {
        AmazonGameApp,
        AppleAppStore,
        Arc,
        Battlenet,
        Bethesda,
        DirectDownload,
        Discord,
        EpicGames,
        GameCenter,
        GOG,
        GooglePlay,
        GoogleStadia,
        HumbleBundle,
        IndieGala,
        itchio,
        Kartridge,
        MicrosoftStore,
        NintendoeShop,
        Oculus,
        Origin,
        ParadoxGames,
        PlayStationStore,
        RockstarGames,
        Steam,
        UbisoftConnect,
        XboxStore,
        AmazonLuma,
        GameJolt,
        JastUsa,
        LegacyGames,
        RobotCache,
        None
    }


    public class StoreFrontElement
    {
        public HltbStorefront HltbStorefrontId { get; set; } = HltbStorefront.None;

        [DontSerialize]
        public string HltbStorefrontName
        {
            get
            {
                switch (HltbStorefrontId)
                {
                    case HltbStorefront.AmazonGameApp:
                        return "Amazon Game App";
                    case HltbStorefront.AppleAppStore:
                        return "Apple App Store";
                    case HltbStorefront.Arc:
                        return "Arc";
                    case HltbStorefront.Battlenet:
                        return "Battle.net";
                    case HltbStorefront.Bethesda:
                        return "Bethesda";
                    case HltbStorefront.DirectDownload:
                        return "Direct Download";
                    case HltbStorefront.Discord:
                        return "Discord";
                    case HltbStorefront.EpicGames:
                        return "Epic Games";
                    case HltbStorefront.GameCenter:
                        return "GameCenter";
                    case HltbStorefront.GOG:
                        return "GOG";
                    case HltbStorefront.GooglePlay:
                        return "Google Play";
                    case HltbStorefront.GoogleStadia:
                        return "Google Stadia";
                    case HltbStorefront.HumbleBundle:
                        return "Humble Bundle";
                    case HltbStorefront.IndieGala:
                        return "IndieGala";
                    case HltbStorefront.itchio:
                        return "itch.io";
                    case HltbStorefront.Kartridge:
                        return "Kartridge";
                    case HltbStorefront.MicrosoftStore:
                        return "Microsoft Store";
                    case HltbStorefront.NintendoeShop:
                        return "Nintendo eShop";
                    case HltbStorefront.Oculus:
                        return "Oculus";
                    case HltbStorefront.Origin:
                        return "Origin";
                    case HltbStorefront.ParadoxGames:
                        return "Paradox Games";
                    case HltbStorefront.PlayStationStore:
                        return "PlayStation Store";
                    case HltbStorefront.RockstarGames:
                        return "Rockstar Games";
                    case HltbStorefront.Steam:
                        return "Steam";
                    case HltbStorefront.UbisoftConnect:
                        return "Ubisoft Connect";
                    case HltbStorefront.XboxStore:
                        return "Xbox Store";
                    case HltbStorefront.AmazonLuma:
                        return "Amazon Luma";
                    case HltbStorefront.GameJolt:
                        return "Game Jolt";
                    case HltbStorefront.JastUsa:
                        return "JAST USA";
                    case HltbStorefront.LegacyGames:
                        return "Legacy Games";
                    case HltbStorefront.RobotCache:
                        return "Robot Cache";
                    case HltbStorefront.None:
                    default:
                        return "----";
                }
            }
        }
    }


    public class Storefront : StoreFrontElement
    {
        public Guid SourceId { get; set; } = default;

        [DontSerialize]
        public string SourceName => API.Instance.Database.Sources?.Get(SourceId)?.Name;

        [DontSerialize]
        public List<StoreFrontElement> StoreFrontElements
        {
            get
            {
                List<StoreFrontElement> storeFronts = new List<StoreFrontElement>();
                foreach (int i in Enum.GetValues(typeof(HltbStorefront)))
                {
                    storeFronts.Add(new StoreFrontElement { HltbStorefrontId = (HltbStorefront)i });
                }
                return storeFronts.OrderBy(x => x.HltbStorefrontName).ToList();
            }
        }
    }
}
