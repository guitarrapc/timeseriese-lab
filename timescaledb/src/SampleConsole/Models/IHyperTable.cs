namespace SampleConsole.Models
{
    /// <summary>
    /// Marker interface to represent this is Hypertable table.
    /// </summary>
    public interface IHyperTable
    {
        public bool IsHyperTable();
        (string tableName, string columnName, HyperTableAttribute attribute) GetHyperTableInfo();
    }
}
