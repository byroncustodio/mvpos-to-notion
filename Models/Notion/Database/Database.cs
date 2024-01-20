using Newtonsoft.Json;
using ShopMakersManager.Models.Notion.Block;
using ShopMakersManager.Models.Notion.Parent;
using System.Collections.Generic;

namespace ShopMakersManager.Models.Notion.Database
{
    public class Database
    {
        [JsonProperty("object")]
        public required string Object { get; set; }

        [JsonProperty("id")]
        public required string Id { get; set; }

        [JsonProperty("created_time")]
        public required string CreatedTime { get; set; }

        [JsonProperty("created_by")]
        public required object CreatedBy { get; set; }

        [JsonProperty("last_edited_time")]
        public required string LastEditedTime { get; set; }

        [JsonProperty("last_edited_by")]
        public required object LastEditedBy { get; set; }

        [JsonProperty("title")]
        public required List<RichText> Title { get; set; }

        [JsonProperty("description")]
        public required List<RichText> description { get; set; }

        [JsonProperty("icon")]
        public required object Icon { get; set; }

        [JsonProperty("cover")]
        public required object Cover { get; set; }

        [JsonProperty("properties")]
        public required object Properties { get; set; }

        [JsonProperty("parent")]
        public required Parent.Base Parent { get; set; }

        [JsonProperty("url")]
        public required string Url { get; set; }

        [JsonProperty("archived")]
        public required bool Archived { get; set; }

        [JsonProperty("is_inline")]
        public required bool IsInline { get; set; }

        [JsonProperty("public_url")]
        public required string PublicUrl { get; set; }
    }
}
