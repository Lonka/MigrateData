using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace MigrateDataLib
{
    internal class DataTableConvert : IDataConvert
    {
        public DataTable DataConvert(object source)
        {
            return (DataTable)source;
        }
    }
}
