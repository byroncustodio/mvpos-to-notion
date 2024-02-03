using MakersManager.Models.Notion.Block;
using MakersManager.Models.Notion.Parent;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MakersManager.Models.Notion
{
    public class Database
    {
        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("created_time")]
        public string CreatedTime { get; set; }

        [JsonProperty("created_by")]
        public object CreatedBy { get; set; }

        [JsonProperty("last_edited_time")]
        public string LastEditedTime { get; set; }

        [JsonProperty("last_edited_by")]
        public object LastEditedBy { get; set; }

        [JsonProperty("title")]
        public List<RichText> Title { get; set; }

        [JsonProperty("description")]
        public List<RichText> description { get; set; }

        [JsonProperty("icon")]
        public object Icon { get; set; }

        [JsonProperty("cover")]
        public object Cover { get; set; }

        [JsonProperty("properties")]
        public object Properties { get; set; }

        [JsonProperty("parent")]
        public Base Parent { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("archived")]
        public bool Archived { get; set; }

        [JsonProperty("is_inline")]
        public bool IsInline { get; set; }

        [JsonProperty("public_url")]
        public string PublicUrl { get; set; }
    }
}
