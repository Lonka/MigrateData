using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MigrateDataModel;
using LK.Util;
using System.Data;
using System.Data.Common;

namespace MigrateDataLib
{
    internal class UpdateTask : AbstractTask
    {
        public UpdateTask(Parameter param)
        {
            m_param = param;
        }

        protected override string GenSingleSqlStr(MigrateTask task, List<DbParameter> paramList, int i, DataRow dr, bool isFirstRow)
        {
            string strSql =string.Format(" update {0} set ",task.TargetTable);
            foreach (MigrateField field in task.Fields)
            {
                if (!task.SyncFields.Where(item => item.SourceField.Equals(field.SourceField)).Any())
                {
                    DbParameter param = LkDaoUtil.GetParameter(Mission.TargetDao.CommonType);
                    param.DbType = LkDaoUtil.GetDbType(dr[field.SourceField].GetType());
                    param.ParameterName = field.SourceField + "_" + i;
                    param.Value = (dr[field.SourceField].ToString().ToUpper().Equals("NULL") ? Convert.DBNull : dr[field.SourceField]);
                    paramList.Add(param);

                    strSql += string.Format(" {0}={1}, ", field.TargetField, field.SourceField.ParseDbParam(Mission.TargetDao.CommonType) + "_" + i);
                }
            }
            strSql = strSql.TrimEnd().TrimEnd(',') + " where ";
            List<string> where = new List<string>();
            foreach (MigrateField field in task.SyncFields)
            {
                if (!paramList.Where(item => item.ParameterName.Equals(field.SourceField + "_" + i)).Any())
                {
                    DbParameter param = LkDaoUtil.GetParameter(Mission.TargetDao.CommonType);
                    param.DbType = LkDaoUtil.GetDbType(dr[field.SourceField].GetType());
                    param.ParameterName = field.SourceField + "_" + i;
                    param.Value = (dr[field.SourceField].ToString().ToUpper().Equals("NULL") ? Convert.DBNull : dr[field.SourceField]);
                    paramList.Add(param);
                }
                where.Add (string.Format(" {0}={1} ", field.TargetField, field.SourceField.ParseDbParam(Mission.TargetDao.CommonType) + "_" + i));
            }
            strSql += string.Join(" and ",where.ToArray())+";";
            return strSql;
        }

        protected override string GenFinalSqlStr(MigrateTask task, List<string> cmds)
        {
            return string.Join("", cmds.ToArray());
        }
    }
}
