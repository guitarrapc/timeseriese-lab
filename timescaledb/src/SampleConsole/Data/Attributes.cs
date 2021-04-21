using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;

namespace SampleConsole
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class HyperTableAttribute : Attribute
    {
        public int ChunkTimeInterval { get; }

        public HyperTableAttribute(int chunkTimeInterval = 0)
        {
            ChunkTimeInterval = chunkTimeInterval;
        }
    }

    public static class AttributeHelper
    {
        public static string GetTableName<T>()
        {
            return ((TableAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute))).Name;
        }

        public static string[] GetColumns<T>()
        {
            var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
            var columns = props.Select(x => ((ColumnAttribute)Attribute.GetCustomAttribute(x, typeof(ColumnAttribute))).Name).ToArray();
            return columns;
        }
        public static HyperTableAttribute GetHypertableAttribute<T>()
        {
            return (HyperTableAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(HyperTableAttribute));
        }
    }
}