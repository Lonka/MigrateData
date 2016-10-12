using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrateDataModel
{
    public class MigrateTask
    {
        public TaskType TaskType { get; set; }
        public string TaskName { get; set; }
        public string SourceTable { get; set; }
        public string TargetTable { get; set; }
        public string CheckField { get; set; }

        public Type CheckType { get; set; }

        public object CheckBeginValue { get; set; }

        public bool CheckEqual { get; set; }

        public List<MigrateField> SyncFields { get; set; }

        public string Condition { get; set; }

        public string ExtraField { get; set; }

        public List<MigrateField> Fields { get; set; }

        public bool Check(out string errorStr)
        {
            errorStr = String.Empty;

            bool result = false;
            try
            {
                if (string.IsNullOrEmpty(SourceTable))
                {
                    errorStr += TaskName + ":FromTable not set." + Environment.NewLine;
                }
                if (string.IsNullOrEmpty(TargetTable))
                {
                    errorStr += TaskName + ":ToTable not set." + Environment.NewLine;
                }
                if (TaskType == TaskType.Batch || TaskType == MigrateDataModel.TaskType.Update)
                {
                    if (string.IsNullOrEmpty(CheckField))
                    {
                        errorStr += TaskName + ":CheckField not set." + Environment.NewLine;
                    }
                    if (CheckType == null)
                    {
                        errorStr += TaskName + ":CheckType not set." + Environment.NewLine;
                    }
                }

                if (Fields == null)
                {
                    errorStr += TaskName + ":Fields not set." + Environment.NewLine;
                }
                else if (Fields != null && Fields.Count == 0)
                {
                    errorStr += TaskName + ":Fields not set." + Environment.NewLine;
                }
                if (string.IsNullOrEmpty(errorStr))
                {
                    result = true;
                }
            }
            catch (Exception e)
            {
                errorStr = e.Message;
            }
            return result;
        }

        public string SourceFieldStr { get; set; }
        public string TargetFieldStr { get; set; }

        public List<string> PlugIn { get; set; }
    }
}
