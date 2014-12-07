using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Pantheon.Server.Config.Json
{
    public class JsonConfiguration : Configuration
    {
        public JsonConfiguration(string json)
        {
            Root = ParseJson(JObject.Parse(json));
        }

        private ConfigurationModule ParseJson(JToken obj)
        {
            ConfigurationModule module = new ConfigurationModule(obj.Path);
            if (obj.HasValues)
            {
                foreach (var child in obj)
                {
                    module.Add(ParseJson(child));
                }
            }
            else
            {
                module.RawValue = (string)obj;
            }
            return module;
        }
    }
}