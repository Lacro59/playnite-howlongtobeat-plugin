using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace HowLongToBeat.Models {

    public enum HltbPlatform {

        [Description("3DO")]
        PANASONIC_3DO,
        [Description("Amiga")]
        AMIGA,
        [Description("Amstrad CPC")]
        AMSTRAD_CPC,
        [Description("Android")]
        ANDROID,
        [Description("Apple II")]
        APPLE_2,
        [Description("Arcade")]
        ARCADE,
        [Description("Atari 2600")]
        ATARI_2600,
        [Description("Atari 5200")]
        ATARI_5200,
        [Description("Atari 7800")]
        ATARI_7800,
        [Description("Atari 8-bit Family")]
        ATARI_8_BIT,
        [Description("Atari Jaguar")]
        ATARI_JAGUAR,
        [Description("Atari Jaguar CD")]
        ATARI_JAGUAR_CD,
        [Description("Atari Lynx")]
        ATARI_LYNX,
        [Description("Atari ST")]
        ATARI_ST,
        [Description("BBC Micro")]
        BBC_MICRO,
        [Description("Browser")]
        BROWSER,
        [Description("ColecoVision")]
        COLECOVISION,
        [Description("Commodore 64")]
        COMMODORE_64,
        [Description("Dreamcast")]
        DREAMCAST,
        [Description("Emulated")]
        EMULATED,
        [Description("FM Towns")]
        FM_TOWNS,
        [Description("Game & Watch")]
        GAME_WATCH,
        [Description("Game Boy")]
        GAME_BOY,
        [Description("Game Boy Advance")]
        GAME_BOY_ADVANCE,
        [Description("Game Boy Color")]
        GAME_BOY_COLOR,
        [Description("Gear VR")]
        GEAR_VR,
        [Description("Google Stadia")]
        GOOGLE_STADIA,
        [Description("Intellivision")]
        INTELLIVISION,
        [Description("Interactive Movie")]
        INTERACTIVE_MOVIE,
        [Description("iOS")]
        IOS,
        [Description("Linux")]
        LINUX,
        [Description("Mac")]
        MAC,
        [Description("Mobile")]
        MOBILE,
        [Description("MSX")]
        MSX,
        [Description("N-Gage")]
        N_GAGE,
        [Description("NEC PC-8800")]
        NEC_PC_8800,
        [Description("NEC PC-9801/21")]
        NEC_PC_9801,
        [Description("NEC PC-FX")]
        NEC_PC_FX,
        [Description("Neo Geo")]
        NEO_GEO,
        [Description("Neo Geo CD")]
        NEO_GEO_CD,
        [Description("Neo Geo Pocket")]
        NEO_GEO_POCKET,
        [Description("NES")]
        NES,
        [Description("Nintendo 3DS")]
        NINTENDO_3DS,
        [Description("Nintendo 64")]
        NINTENDO_64,
        [Description("Nintendo DS")]
        NINTENDO_DS,
        [Description("Nintendo GameCube")]
        NINTENDO_GAMECUBE,
        [Description("Nintendo Switch")]
        NINTENDO_SWITCH,
        [Description("Oculus Go")]
        OCULUS_GO,
        [Description("Oculus Quest")]
        OCULUS_QUEST,
        [Description("OnLive")]
        ONLIVE,
        [Description("Ouya")]
        OUYA,
        [Description("PC")]
        PC,
        [Description("PC VR")]
        PC_VR,
        [Description("Philips CD-i")]
        PHILIPS_CDI,
        [Description("Philips Videopac G7000")]
        PHILIPS_VIDEOPAC_G7000,
        [Description("PlayStation")]
        PLAYSTATION,
        [Description("PlayStation 2")]
        PLAYSTATION_2,
        [Description("PlayStation 3")]
        PLAYSTATION_3,
        [Description("PlayStation 4")]
        PLAYSTATION_4,
        [Description("PlayStation 5")]
        PLAYSTATION_5,
        [Description("PlayStation Mobile")]
        PLAYSTATION_MOBILE,
        [Description("PlayStation Now")]
        PLAYSTATION_NOW,
        [Description("PlayStation Portable")]
        PLAYSTATION_PORTABLE,
        [Description("PlayStation Vita")]
        PLAYSTATION_VITA,
        [Description("PlayStation VR")]
        PLAYSTATION_VR,
        [Description("Plug & Play")]
        PLUG_PLAY,
        [Description("Sega 32X")]
        SEGA_32X,
        [Description("Sega CD")]
        SEGA_CD,
        [Description("Sega Game Gear")]
        SEGA_GAME_GEAR,
        [Description("Sega Master System")]
        SEGA_MASTER_SYSTEM,
        [Description("Sega Mega Drive/Genesis")]
        SEGA_MEGA_DRIVE,
        [Description("Sega Saturn")]
        SEGA_SATURN,
        [Description("SG-1000")]
        SG_1000,
        [Description("Sharp X68000")]
        SHARP_X68000,
        [Description("Super Nintendo")]
        SUPER_NINTENDO,
        [Description("Tiger Handheld")]
        TIGER_HANDHELD,
        [Description("TurboGrafx-16")]
        TURBOGRAFX_16,
        [Description("TurboGrafx-CD")]
        TURBOGRAFX_CD,
        [Description("Virtual Boy")]
        VIRTUAL_BOY,
        [Description("Wii")]
        WII,
        [Description("Wii U")]
        WII_U,
        [Description("Windows Phone")]
        WINDOWS_PHONE,
        [Description("WonderSwan")]
        WONDERSWAN,
        [Description("Xbox")]
        XBOX,
        [Description("Xbox 360")]
        XBOX_360,
        [Description("Xbox One")]
        XBOX_ONE,
        [Description("Xbox Series X/S")]
        XBOS_SERIES_XS,
        [Description("ZX Spectrum")]
        ZX_SPECTRUM

    }

    public class HltbPlatformMatch : IComparable<HltbPlatformMatch> {

        public Platform Platform { get; set; }
        public HltbPlatform? HltbPlatform { get; set; } = null;

        public int CompareTo(HltbPlatformMatch other) {
            return other == null ? 1 : Platform.Name.CompareTo(other.Platform.Name);
        }
    }

}
