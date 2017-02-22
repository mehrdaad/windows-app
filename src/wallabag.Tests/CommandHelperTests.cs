using System;
using FakeItEasy;
using System.Windows.Input;
using Xunit;
using wallabag.Data.Common.Helpers;

namespace wallabag.Tests
{
    public class CommandHelperTests
    {
        [Fact]
        public void CommandExecutionWithoutAParameterUsesNull()
        {
            var fakeCommand = A.Fake<ICommand>();

            A.CallTo(() => fakeCommand.Execute(A<object>.That.IsNull())).Invokes(x => Assert.Null(x.Arguments.Get<object>(0)));

            fakeCommand.Execute();

            A.CallTo(() => fakeCommand.Execute(A<object>.That.IsNull())).MustHaveHappened();
        }
    }
}
