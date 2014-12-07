using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Pantheon.Server.Config.Xml
{
    public class XmlConfiguration : Configuration
    {
        public XmlConfiguration(ConfigurationModule root)
            : base(root)
        {
        }

        public XmlConfiguration(XDocument document)
        {
            Root = ParseDocument(document);
        }

        public XmlConfiguration(XElement element)
        {
            Root = ParseElement(element);
        }

        public override void Save(string file)
        {
            var root = CreateElement(Root);

            var document = new XDocument(root);

            document.Save(file);
        }

        private XAttribute CreateAttribute(ConfigurationModule module)
        {
            return new XAttribute(module.Name, module.RawValue);
        }

        private XElement CreateElement(ConfigurationModule module)
        {
            XElement element = new XElement(module.Name);
            if (!module.HasChildren)
            {
                element.Value = module.RawValue;
            }
            else
            {
                foreach (var child in module.Children())
                {
                    if (child.IsAttribute)
                    {
                        element.Add(CreateAttribute(child));
                    }
                    else
                    {
                        element.Add(CreateElement(child));
                    }
                }
            }

            return element;
        }

        private ConfigurationModule ParseAttribute(XAttribute attribute)
        {
            return new ConfigurationModule(attribute.Name.LocalName, attribute.Value).Attribute();
        }

        private ConfigurationModule ParseDocument(XDocument document)
        {
            return ParseElement(document.Root);
        }

        private ConfigurationModule ParseElement(XElement element)
        {
            var module = new ConfigurationModule(element.Name.LocalName, element.Value);

            if (element.HasElements)
            {
                foreach (var el in element.Elements())
                {
                    module.AddModule(ParseElement(el));
                }
            }

            if (element.HasAttributes)
            {
                foreach (var attribute in element.Attributes())
                {
                    module.AddModule(ParseAttribute(attribute));
                }
            }

            return module;
        }
    }
}