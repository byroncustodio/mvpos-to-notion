using MakersManager.Models.Notion.Block;
using System;
using System.Collections.Generic;

namespace MakersManager.Utilities
{
    public static class NotionUtilities
    {
        public static object CreateTitleProperty(string content)
        {
            return new
            {
                title = new List<RichText>()
                {
                    new() { Type = "text", Text = new Text() { Content = content ?? string.Empty } }
                }
            };
        }

        public static object CreateDateProperty(DateTime date, string timezone)
        {
            return new
            {
                date = new { start = date.ToString("s"), time_zone = timezone }
            };
        }

        public static object CreateRelationProperty(List<object> relation)
        {
            return new
            {
                relation
            };
        }

        public static object CreateSelectProperty(string name)
        {
            return new
            {
                select = new { name }
            };
        }

        public static object CreateNumberProperty(int number)
        {
            return new
            {
                number
            };
        }

        public static object CreateNumberProperty(decimal number)
        {
            return new
            {
                number
            };
        }

        public static object CreateRichTextProperty(string content)
        {
            return new
            {
                rich_text = new List<RichText>()
                {
                    new() { Type = "text", Text = new Text() { Content = content ?? string.Empty } }
                }
            };
        }
    }
}
