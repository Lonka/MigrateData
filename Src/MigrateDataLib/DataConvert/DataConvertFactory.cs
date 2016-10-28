using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MigrateDataModel;

namespace MigrateDataLib
{
    internal class DataConvertFactory
    {
        public static IDataConvert GetFactory(SourceReturnFormat format)
        {
            IDataConvert result = null;
            switch (format)
            {
                case SourceReturnFormat.CSV:
                    break;
                case SourceReturnFormat.DataTable:
                    result = new DataTableConvert();
                    break;
                case SourceReturnFormat.JSON:
                    break;
                case SourceReturnFormat.XML:
                    break;
                default:
                    break;
            }
            return result;
        }
    }
}
