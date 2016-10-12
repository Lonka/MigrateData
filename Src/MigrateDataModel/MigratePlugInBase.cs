using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MigrateDataModel
{
    public abstract class MigratePlugInBase
    {
        public abstract bool BeforeMigrate(MigrateMission mission,MigrateTask task,DataTable sourceDt,out DataTable processDt);

        public abstract bool AfterMigrate(MigrateMission mission, MigrateTask task, DataTable sourceDt);
    }
}
