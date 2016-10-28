using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using MigrateDataModel;

namespace MigrateDataLib
{
    internal interface IDataSource
    {
        DataTable GetSourceNewData(MigrateMission mission, MigrateTask task, MigrateTaskLastResult lastResult);
    }
}
