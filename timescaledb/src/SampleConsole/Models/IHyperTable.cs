using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SampleConsole.Models
{
    /// <summary>
    /// Marker interface to represent this is Hypertable table.
    /// </summary>
    public interface IHyperTable
    {
        DateTime Time { get; init; }
        (string tableName, string columnName) GetHyperTableKey();
    }
}
