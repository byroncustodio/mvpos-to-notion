using Newtonsoft.Json;

namespace ShopMakersManager.Models.Notion.Block
{
    public class RichText
    {

        [JsonProperty("type")]
        public string Type { get; set; } // text, mention, equation

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public Text Text { get; set; }

        [JsonProperty("annotations", NullValueHandling = NullValueHandling.Ignore)]
        public Annotation Annotation { get; set; }

        [JsonProperty("plain_text", NullValueHandling = NullValueHandling.Ignore)]
        public string PlainText { get; set; }

        [JsonProperty("href", NullValueHandling = NullValueHandling.Ignore)]
        public string Href { get; set; }
    }

    public class Text
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("link", NullValueHandling = NullValueHandling.Ignore)]
        public object Link { get; set; }
    }

    public class Annotation
    {
        [JsonProperty("bold", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Bold { get; set; }

        [JsonProperty("italic", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Italic { get; set; }

        [JsonProperty("strikethrough", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Strikethrough { get; set; }

        [JsonProperty("underline", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Underline { get; set; }

        [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Code { get; set; }

        [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
        public string Color { get; set; }
    }
}
