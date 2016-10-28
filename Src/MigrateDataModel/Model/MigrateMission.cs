using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LK.Util;

namespace MigrateDataModel
{
    public class MigrateMission
    {
        public DataSourceType DataSourceType { get; set; }

        public string SourceConnectString { get; set; }
        public double TimeZone { get; set; }

        public string Folder { get; set; }

        public string MissionKey { get; set; }
        public LkDaoInstance SourceDao { get; set; }
        public LkDaoInstance TargetDao { get; set; }
        public LkLogInstance Log { get; set; }
        public List<MigrateTask> Tasks { get; set; }

        public List<MigrateTaskLastResult> TaskResult { get; set; }

        public DateTime SettingUpdateTime { get; set; }

        public bool CheckTask(MigrateTask task, out string errorStr)
        {
            errorStr = string.Empty;
            bool result = false;
            try
            {
                if (DataSourceType == MigrateDataModel.DataSourceType.DB)
                {
                    if (string.IsNullOrEmpty(task.SourceTable))
                    {
                        errorStr += task.TaskName + ":SourceTable not set." + Environment.NewLine;
                    }

                }
                else if (DataSourceType == MigrateDataModel.DataSourceType.WebService)
                {
                    if (string.IsNullOrEmpty(task.SourceMethod))
                    {
                        errorStr += task.TaskName + ":SourceMethod not set." + Environment.NewLine;
                    }
                    if (string.IsNullOrEmpty(task.SourceParam))
                    {
                        errorStr += task.TaskName + ":SourceParam not set." + Environment.NewLine;
                    }
                }
                else if (DataSourceType == MigrateDataModel.DataSourceType.FileServer)
                {

                }
                result = true;
            }
            catch (Exception e)
            {
                errorStr = e.Message;
            }

            return result;

        }

    }
}
