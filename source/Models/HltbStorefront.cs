using HowLongToBeat.Models.Enumerations;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace HowLongToBeat.Models
{
    public class StoreFrontElement
    {
        public HltbStorefront HltbStorefrontId { get; set; } = HltbStorefront.None;

        [DontSerialize]
        public string HltbStorefrontName => GetEnumDescription(HltbStorefrontId);

        private static string GetEnumDescription(Enum value)
        {
            System.Reflection.FieldInfo field = value.GetType().GetField(value.ToString());
            if (field != null)
            {
                DescriptionAttribute[] attributes = (DescriptionAttribute[])field.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attributes != null && attributes.Length > 0)
                {
                    return attributes[0].Description;
                }
            }
            return value.ToString();
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
