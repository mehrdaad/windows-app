using wallabag.Data.Models;
using Xunit;

namespace wallabag.Tests
{
    public class LanguageTests
    {
        [Fact]
        public void ToStringReturnsDisplayName()
        {
            var lang = new Language("en");
            Assert.Equal(lang.DisplayName, lang.ToString());
        }

        [Fact]
        public void LanguageWithEmptyInternalLanguageCodeIsUnknown()
        {
            var language = new Language("en") { InternalLanguageCode = string.Empty };
            Assert.True(language.IsUnknown);
        }

        [Fact]
        public void LanguageWithNullInternalLanguageCodeIsUnknown()
        {
            var language = new Language("en") { InternalLanguageCode = null };
            Assert.True(language.IsUnknown);
        }

        [Theory]
        [InlineData("de-DE", "de")]
        [InlineData("de-CH", "de")]
        [InlineData("en-US", "en")]
        [InlineData("en-GB", "en")]
        [InlineData("fr-FR", "fr")]
        [InlineData("en", "en")]
        [InlineData("de", "en")]
        [InlineData("fr", "fr")]
        public void LanguageCodeIsConvertedCorrect(string languageCode, string expectedResult)
        {
            var language = new Language(languageCode);

            Assert.Equal(languageCode, language.InternalLanguageCode);
            Assert.False(string.IsNullOrEmpty(language.DisplayName));
            Assert.False(string.IsNullOrEmpty(language.LanguageCode));

            Assert.Equal(expectedResult, language.LanguageCode);
        }

        [Theory]
        [InlineData("DE-DE", "de-DE")]
        [InlineData("DE", "de")]
        [InlineData("De", "dE")]
        [InlineData("fR", "fr")]
        public void LanguageCodeConvertingIsCaseInsensitive(string languageCode1, string languageCode2)
        {
            var language1 = new Language(languageCode1);
            var language2 = new Language(languageCode2);

            Assert.Equal(language1, language2);
            Assert.Equal(language1.LanguageCode, language2.LanguageCode);
        }
    }
}
