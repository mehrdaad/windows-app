using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallabag.Models
{
    [ImplementPropertyChanged]
    public class Language
    {
        public string wallabagLanguageCode { get; set; }
        public string LanguageCode { get; set; }
        public string DisplayName { get; set; }

        public Language(string languageCode)
        {
            this.wallabagLanguageCode = languageCode;
            LanguageCode = new CultureInfo(languageCode).TwoLetterISOLanguageName;
            DisplayName = new CultureInfo(LanguageCode).DisplayName;
        }

        public override string ToString() => DisplayName;
        public override bool Equals(object obj) => LanguageCode.Equals((obj as Language).LanguageCode);
        public override int GetHashCode() => LanguageCode.GetHashCode();
    }
}
