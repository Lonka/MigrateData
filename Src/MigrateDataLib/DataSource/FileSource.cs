using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrateDataLib
{
    internal class FileSource : IDataSource
    {
        public System.Data.DataTable GetSourceNewData(MigrateDataModel.MigrateMission mission, MigrateDataModel.MigrateTask task, MigrateDataModel.MigrateTaskLastResult lastResult)
        {
            throw new NotImplementedException();
        }
    }
}
