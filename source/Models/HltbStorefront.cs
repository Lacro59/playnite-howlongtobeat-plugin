using CommonPluginsShared;
using HowLongToBeat.Services;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        XboxStore
    }


    public class Storefront
    {
        public Guid SourceId { get; set; }

        public HltbStorefront HltbStorefrontId { get; set; }
        [DontSerialize]
        public string HltbStorefrontName {
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
                }

                return string.Empty;
            }
        }

        [DontSerialize]
        public List<GameSource> GameSources
        {
            get
            {
                return HowLongToBeat.PluginDatabase.PlayniteApi.Database.Sources.Distinct().OrderBy(x => x.Name).ToList();
            }
        }
    }
}
