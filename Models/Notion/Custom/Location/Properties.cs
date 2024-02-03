using MakersManager.Models.Notion.Block;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MakersManager.Models.Notion.Custom.Location
{
    public class Properties
    {
        public Name Name { get; set; }

        public Id Id { get; set; }
    }

    public class Name : PageProperty
    {
        [JsonProperty("title")]
        public List<RichText> Title { get; set; }
    }

    public class Id : PageProperty
    {
        [JsonProperty("number")]
        public int Value { get; set; }
    }
}
