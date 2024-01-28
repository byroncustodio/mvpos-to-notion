using MakersManager.Models.Notion;
using MakersManager.Models.Notion.Block;
using System;
using System.Collections.Generic;

namespace MakersManager.Utilities
{
    public static class NotionUtilities
    {
        public static object CreateTitleFilter(string property, FilterCondition condition, string content)
        {
            return new
            {
                property,
                title = CreateFilterCondition(condition, content)
            };
        }

        public static object CreateRelationFilter(string property, FilterCondition condition, string id)
        {
            return new
            {
                property,
                relation = CreateFilterCondition(condition, id)
            };
        }

        public static object CreateNumberFilter(string property, FilterCondition condition, int value)
        {
            return new
            {
                property,
                number = CreateFilterCondition(condition, value)
            };
        }

        public static object CreateDateFilter(string property, FilterCondition condition, string date)
        {
            return new
            {
                property,
                date = CreateFilterCondition(condition, date)
            };
        }

        public static object CreateFilterCondition<T>(FilterCondition condition, T value)
        {
            switch (condition)
            {
                case FilterCondition.Equals:
                    return new { equals = value };
                case FilterCondition.Contains:
                    return new { contains = value };
                default: 
                    return null;
            };
        }

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

        public static object CreateDateProperty(DateTime date, string timezone = null)
        {
            if (timezone == null)
            {
                return new
                {
                    date = new { start = date.ToString("yyyy-MM-dd") }
                };
            }
            else
            {
                return new
                {
                    date = new { start = date.ToString("s"), time_zone = timezone }
                };
            }
        }

        public static object CreateRelationProperty(Page page = null)
        {
            return new
            {
                relation = page != null 
                    ? new List<object> { new { id = page.Id } } 
                    : new List<object>()
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

        public static object CreateStatusProperty(string name)
        {
            return new
            {
                status = new { name }
            };
        }

        public enum FilterCondition
        {
            Equals,
            Contains
        }
    }
}
