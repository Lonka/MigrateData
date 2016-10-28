using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace MigrateDataLib
{
    internal interface IDataConvert
    {
        DataTable DataConvert(object source);
    }
}
