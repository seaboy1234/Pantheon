using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pantheon.Server.Config.Json;
using Xunit;

namespace Pantheon.Tests.Server.Config
{
    public class JsonTests
    {
        [Theory]
        [InlineData(@"{Name: 'Test', ChildObject: {MyName: 'Name?'}}")]
        public void JsonObjectModelGenerates(string data)
        {
            var config = new JsonConfiguration(data);

            Assert.NotNull(config.Root);
            Assert.True(config.Root.Children().Count() > 2);
            Assert.NotNull(config.Root.Child("Name"));
        }
    }
}