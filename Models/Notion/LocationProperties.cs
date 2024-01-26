using MakersManager.Models.Notion.Block;
using MakersManager.Models.Notion.Properties;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MakersManager.Models.Notion
{
    public class LocationProperties
    {
        public Name Name { get; set; }
    }

    public class Name : PageProperty
    {
        [JsonProperty("title")]
        public List<RichText> Title { get; set; }
    }
}
