using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using LK.Util;

namespace MigrateDataLib
{
    internal class CopyTask : AbstractTask
    {
        public CopyTask(Parameter param)
        {
            m_param = param;
        }

        protected override bool DelExistRow(MigrateDataModel.MigrateTask task, System.Data.DataTable sourceNewData)
        {
            bool result = false;
            try
            {
                List<DbParameter> paramList = new List<DbParameter>();
                string where = string.Empty;
                string targetCheckField = string.Empty;
                if (!string.IsNullOrEmpty(task.CheckField))
                {

                    var targetCheckFieldCollect = task.Fields.Where(item => item.SourceField.Equals(task.CheckField));
                    if (targetCheckFieldCollect.Any())
                    {
                        targetCheckField = targetCheckFieldCollect.FirstOrDefault().TargetField;

                        DbParameter param = LkDaoUtil.GetParameter(Mission.TargetDao.CommonType);
                        param.DbType = LkDaoUtil.GetDbType(task.CheckType);
                        param.Value = task.CheckBeginValue;
                        param.ParameterName = task.CheckField;
                        paramList.Add(param);
                        where = string.Format(" where {0}{1}{2} "
                            , targetCheckField
                            , (task.CheckEqual ? " >= " : ">")
                            , task.CheckField.ParseDbParam(Mission.TargetDao.CommonType));
                    }
                }
                Mission.TargetDao.ExecuteSQL(string.Format(" delete {0} {1};", task.TargetTable, where), paramList);
                result = true;
            }
            catch (Exception e)
            {
                Mission.Log.WriteLog(LogLevel.Error, e);
            }
            return result;
        }
    }
}
