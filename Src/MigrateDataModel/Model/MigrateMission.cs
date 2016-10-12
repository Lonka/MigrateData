using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LK.Util;

namespace MigrateDataModel
{
    public class MigrateMission
    {
        public double TimeZone { get; set; }

        public string Folder { get; set; }

        public string MissionKey { get; set; }
        public LkDaoInstance SourceDao { get; set; }
        public LkDaoInstance TargetDao { get; set; }
        public LkLogInstance Log { get; set; }
        public List<MigrateTask> Tasks { get; set; }

        public List<MigrateTaskLastResult> TaskResult { get; set; }

        public DateTime SettingUpdateTime { get; set; }

    }
}
