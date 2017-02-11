using wallabag.Data.Common.Helpers;
using Xunit;

namespace wallabag.Tests
{
    public class StringHelperTests
    {
        [Fact]
        public void FormattingWithTwoBracketsReturnsValidResult()
        {
            string stringToFormat = "This is a {{str}}";
            Assert.Equal("This is a test", stringToFormat.FormatWith(new { str = "test" }));
        }

        [Fact]
        public void FormattingWithThreeBracketsReturnsValidResultBetweenBrackets()
        {
            string stringToFormat = "This is a {{{str}}}";
            Assert.Equal("This is a {test}", stringToFormat.FormatWith(new { str = "test" }));
        }

        [Fact]
        public void FormattingWithOneBracketReturnsResultWithoutObject()
        {
            string stringToFormat = "This is a {str}";
            Assert.Equal(stringToFormat, stringToFormat.FormatWith(new { str = "test" }));
        }

        [Fact]
        public void FormattingWithoutAObjectUsesEmptyString()
        {
            string stringToFormat = "This is a {{str}}";
            Assert.Equal("This is a ", stringToFormat.FormatWith(null));
        }

        [Fact]
        public void FormattingWithMultiplePropertiesReturnsValidResult()
        {
            string stringToFormat = "This is a {{prop1}} {{prop2}}";
            Assert.Equal("This is a 123 456", stringToFormat.FormatWith(new
            {
                prop1 = "123",
                prop2 = "456"
            }));
        }

        [Fact]
        public void FormattingWithMultiplePropertiesWhereOneIsNullUsesEmptyString()
        {
            string stringToFormat = "This is a {{prop1}} {{prop2}}";
            Assert.Equal("This is a 123 ", stringToFormat.FormatWith(new { prop1 = "123" }));
        }
    }
}
