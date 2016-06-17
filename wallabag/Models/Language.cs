using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallabag.Common;

namespace wallabag.Models
{
    [ImplementPropertyChanged]
    public class Language
    {
        public string wallabagLanguageCode { get; set; }
        public string LanguageCode { get; set; }
        public string DisplayName { get; set; }
        public bool IsUnknown { get { return wallabagLanguageCode == null; } }

        public Language(string languageCode)
        {
            if (!string.IsNullOrEmpty(languageCode))
            {
                this.wallabagLanguageCode = languageCode;
                LanguageCode = languageCode.Substring(0, 2);
                DisplayName = new CultureInfo(LanguageCode).DisplayName;
            }
        }

        public static Language Unknown
        {
            get
            {
                return new Language("en")
                {
                    wallabagLanguageCode = null,
                    LanguageCode = null,
                    DisplayName = Helpers.LocalizedResource("UnknownLanguageDisplayName")
                };
            }
        }

        public override string ToString() => DisplayName;
        public override bool Equals(object obj)
        {
            if (obj != null && obj is Language)
                return LanguageCode == (obj as Language).LanguageCode;
            else
                return false;
        }
        public override int GetHashCode() => LanguageCode.GetHashCode();
    }
}
