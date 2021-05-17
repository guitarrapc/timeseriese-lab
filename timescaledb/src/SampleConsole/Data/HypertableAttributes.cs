using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;

namespace SampleConsole
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class HypertableAttribute : Attribute
    {
        /// <summary>
        /// Table Name for Hypertable
        /// </summary>
        public string TableName { get; }
        /// <summary>
        /// Column Name for Hypertable Key
        /// </summary>
        public string TimeColumn { get; }
        public int ChunkTimeInterval { get; }

        public HypertableAttribute(string tableName, string columName, int chunkTimeInterval = 0)
        {
            TableName = tableName;
            TimeColumn = columName;
            ChunkTimeInterval = chunkTimeInterval;
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DistributedHypertableAttribute : Attribute
    {
        public string TableName { get; }
        public string TimeColumn { get; }
        public string PartitioningColumn { get; }
        public int ChunkTimeInterval { get; }

        public DistributedHypertableAttribute(string tableName, string columNames, string partitioningColumn, int chunkTimeInterval = 0)
        {
            TableName = tableName;
            TimeColumn = columNames;
            PartitioningColumn = partitioningColumn;
            ChunkTimeInterval = chunkTimeInterval;
        }
    }

    public static class AttributeEx
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
        public static HypertableAttribute GetHypertableAttribute<T>()
        {
            return (HypertableAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(HypertableAttribute));
        }
    }
}