using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace MigrateDataLib
{
    internal interface ITask
    {
        DataTable GetSourceNewData(MigrateTask task, MigrateTaskLastResult lastResult);
        bool DoRetry(MigrateTask task);

        bool DoMigrate(MigrateTask task, DataTable sourceNewData, bool isRetry = false);

        bool Execute(List<string> cmds, List<DbParameter> paramList);
    }
}
