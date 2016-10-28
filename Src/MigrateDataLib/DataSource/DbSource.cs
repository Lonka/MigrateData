using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using MigrateDataModel;
using System.Data.Common;
using LK.Util;

namespace MigrateDataLib
{
    internal class DbSource : IDataSource
    {
        public virtual DataTable GetSourceNewData(MigrateMission mission, MigrateTask task, MigrateTaskLastResult lastResult)
        {
            List<DbParameter> sourceSqlParam = new List<DbParameter>();

            string whereStr = string.Empty;
            DbParameter param = LkDaoUtil.GetParameter(mission.SourceDao.CommonType);

            if (!string.IsNullOrEmpty(task.CheckField.Trim()))
            {
                param.ParameterName = task.CheckField + "_P";

                param.Value = lastResult.LastCheckValue;
                param.DbType = LkDaoUtil.GetDbType(task.CheckType);

                sourceSqlParam.Add(param);
                whereStr += string.Format(" and {0}{1}{2} "
                        , task.CheckField
                        , (task.CheckEqual ? ">=" : ">")
                        , task.CheckField.ParseDbParam(mission.SourceDao.CommonType) + "_P");
            }

            if (!string.IsNullOrEmpty(task.Condition.Trim()))
            {
                whereStr += string.Format(" and {0} ", task.Condition.Trim());
            }

            //TODO DB一定要檔好重覆問題
            DataTable sourceData = mission.SourceDao.GetDataTable(
                string.Format(" select * from ( select {0}{3} from {1} ) as a where 1=1 {2}"
                        , task.SourceFieldStr
                        , task.SourceTable
                        , whereStr
                        , (string.IsNullOrEmpty(task.ExtraField) ? string.Empty : "," + task.ExtraField.Trim().Trim(",".ToArray())))
                , sourceSqlParam);
            return sourceData;
        }

    }
}
