﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace NugetProtocol
{
    public class FrameworkAssemblyGroup
    {
        public FrameworkAssemblyGroup()
        {

        }
        public FrameworkAssemblyGroup(string oid, string targetFramework, List<string> assembly)
        {
            OId = oid;
            TargetFramework = targetFramework;
            Assembly = assembly;
        }

        [JsonProperty("@id")]
        public string OId { get; set; }
        [JsonProperty("targetFramework", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TargetFramework { get; set; }
        [JsonProperty("assembly")]
        [JsonConverter(typeof(SingleOrArrayConverter<string>))]
        public List<string> Assembly { get; set; }
    }
}
