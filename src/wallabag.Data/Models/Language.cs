using PropertyChanged;
using System;
using System.Globalization;

namespace wallabag.Data.Models
{
    [ImplementPropertyChanged]
    public class Language : IComparable
    {
        public string InternalLanguageCode { get; set; }
        public string LanguageCode { get; set; }
        public string DisplayName { get; set; }
        public bool IsUnknown => string.IsNullOrEmpty(InternalLanguageCode);

        public Language(string languageCode)
        {
            if (!string.IsNullOrEmpty(languageCode))
            {
                InternalLanguageCode = languageCode;
                LanguageCode = languageCode.Substring(0, 2).ToLower();
                DisplayName = new CultureInfo(LanguageCode).DisplayName;
            }
        }

        public static Language Unknown
        {
            get
            {
                return new Language("en")
                {
                    InternalLanguageCode = null,
                    LanguageCode = null
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

        public int CompareTo(object obj)
        {
            if (obj is Language &&
                obj != null &&
                !string.IsNullOrEmpty(DisplayName))
                return ((IComparable)DisplayName).CompareTo((obj as Language).DisplayName);
            else
                return 0;
        }
    }
}
