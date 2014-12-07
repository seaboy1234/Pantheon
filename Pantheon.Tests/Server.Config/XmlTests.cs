using System.Xml.Linq;
using Pantheon.Server.Config;
using Pantheon.Server.Config.Xml;
using Xunit;

namespace Pantheon.Tests.Server.Config
{
    public class XmlTests
    {
        const string Data =
@"<Object>
    <TestValue>Hello</TestValue>
    <TestNode Attributes=""Simple"">
      <TestItem Something=""MyValue"">Info</TestItem>
    </TestNode>
  </Object>";

        [Theory]
        [InlineData(Data, "TestNode.TestItem", "Info")]
        [InlineData(Data, "TestValue", "Hello")]
        [InlineData(Data, "TestNode.Attributes", "Simple")]
        [InlineData(Data, "TestNode", "Info")]
        public void XmlObjectModelGenerates(string data, string testPath, string expected)
        {
            XmlConfiguration config = new XmlConfiguration(XDocument.Parse(data));

            Assert.NotNull(config.Root);
            Assert.NotEmpty(config.Root.Children());
            Assert.NotNull(config.Module(testPath));
            Assert.Equal(config.Module(testPath).RawValue, expected);
        }

        [Fact]
        public void CanTraverseDom()
        {
            XmlConfiguration config = new XmlConfiguration(XDocument.Parse(Data));
            ConfigurationModule currentModule = config.Root;

            Assert.NotNull(currentModule);

            currentModule = currentModule.Child("TestNode");
            Assert.Equal("TestNode", currentModule.Name);

            currentModule = currentModule.Child("Attributes");

            Assert.NotNull(currentModule);
        }
    }
}
