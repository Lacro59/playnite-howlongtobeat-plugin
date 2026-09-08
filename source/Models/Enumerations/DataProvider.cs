using System.ComponentModel;

namespace HowLongToBeat.Models.Enumerations
{
    /// <summary>
    /// Default source used when downloading plugin data (manual select and mass import).
    /// </summary>
    public enum DataProvider
    {
        /// <summary>
        /// Search and match against HowLongToBeat.
        /// </summary>
        [Description("LOCHltbDataProviderHowLongToBeat")]
        HowLongToBeat,

        /// <summary>
        /// Search and match against VNDB.
        /// </summary>
        [Description("LOCHltbDataProviderVndb")]
        Vndb
    }
}
