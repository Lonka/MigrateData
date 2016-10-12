using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using LK.Util;
using System.IO;
using MigrateDataModel;

namespace MigrateDataLib
{
    internal class SyncTask : AbstractTask
    {
        public SyncTask(Parameter param)
        {
            m_param = param;
        }

        protected override bool DelExistRow(MigrateTask task, DataTable sourceNewData)
        {
            if (task.SyncFields.Count == 0)
            {
                return false;
            }
            #region Get Target Data
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

            DataTable targetData = Mission.TargetDao.GetDataTable(string.Format(" select * from {0} {1}", task.TargetTable, where), paramList);
            #endregion

            #region Check Target 不存在於 Source 的 Row，將其刪除
            StringBuilder delSql = new StringBuilder();
            List<DbParameter> delParams = new List<DbParameter>();
            for (int i = 0; i < targetData.Rows.Count; i++)
            {
                DataRow targetDr = targetData.Rows[i];
                var checkRow = sourceNewData.AsEnumerable().Where(item =>
                    {
                        foreach (MigrateField field in task.SyncFields)
                        {
                            if (!item[field.SourceField].ToString().Equals(targetDr[field.TargetField].ToString()))
                            {
                                return false;
                            }
                        }
                        return true;
                    });

                if (!checkRow.Any())
                {
                    List<string> whereList = new List<string>();
                    foreach (MigrateField field in task.SyncFields)
                    {
                        DbParameter param = LkDaoUtil.GetParameter(Mission.TargetDao.CommonType);
                        param.Value = (targetDr[field.SourceField].ToString().ToUpper().Equals("NULL") ? Convert.DBNull : targetDr[field.TargetField]);
                        param.ParameterName = field.TargetField + "_" + i;
                        delParams.Add(param);
                        whereList.Add(" " + field.TargetField + " = " + field.TargetField.ParseDbParam(Mission.TargetDao.CommonType) + "_" + i + " ");
                    }
                    where = string.Join(" and ", whereList.ToArray());
                    //todo Oracle 會有問題
                    delSql.Append(string.Format(" delete {0} where {1};", task.TargetTable, where));
                }

                if (delSql.Length > 0 && (delSql.Length > m_param.ExecuteSqlSize || delParams.Count > m_param.ParamSize || i == targetData.Rows.Count - 1))
                {
                    Mission.TargetDao.ExecuteSQL(delSql.ToString(), delParams,false);
                    delSql = new StringBuilder();
                    delParams = new List<DbParameter>();
                }
            }
            #endregion

            return true;
        }

        protected override string GenSingleSqlStr(MigrateTask task, List<DbParameter> paramList, int i, DataRow dr, bool isFirstRow)
        {
            string strSql = string.Empty;

            string fieldStr = string.Empty;

            foreach (MigrateField field in task.Fields)
            {
                DbParameter param = LkDaoUtil.GetParameter(Mission.TargetDao.CommonType);
                param.DbType = LkDaoUtil.GetDbType(dr[field.SourceField].GetType());
                param.ParameterName = field.SourceField + "_" + i;
                param.Value = (dr[field.SourceField].ToString().ToUpper().Equals("NULL") ? Convert.DBNull : dr[field.SourceField]);
                paramList.Add(param);
                if (isFirstRow)
                {
                    if (dr[field.SourceField].GetType() == typeof(string))
                    {
                        fieldStr += "convert(nvarchar(50)," + field.SourceField.ParseDbParam(Mission.TargetDao.CommonType) + "_" + i + ")";
                    }
                    else
                    {
                        fieldStr += field.SourceField.ParseDbParam(Mission.TargetDao.CommonType) + "_" + i;
                    }
                    fieldStr += " as " + field.SourceField + ",";
                }
                else
                {
                    fieldStr += field.SourceField.ParseDbParam(Mission.TargetDao.CommonType) + "_" + i;
                    fieldStr += ",";
                }

            }
            if (isFirstRow)
            {
                strSql = string.Format(" select * into #temp from (select {0}) as a;", fieldStr.TrimEnd(','));
            }
            else
            {
                strSql = string.Format(" insert into #temp values ({0}) ;", fieldStr.TrimEnd(','));
            }
            return strSql;
        }

        protected override string GenFinalSqlStr(MigrateTask task, List<string> cmds)
        {
            List<string> keyField = new List<string>();
            List<string> updateField = new List<string>();
            List<string> insertFieldValue = new List<string>();
            List<string> insertField = new List<string>();

            List<string> keys = new List<string>();
            foreach (MigrateField Field in task.SyncFields)
            {
                keyField.Add(" a." + Field.TargetField + "=b." + Field.SourceField);
                keys.Add(Field.TargetField);
            }

            foreach (MigrateField field in task.Fields)
            {
                if (!keys.Contains(field.TargetField))
                {
                    updateField.Add(" a." + field.TargetField + "=b." + field.SourceField);
                }
                insertFieldValue.Add(" b." + field.SourceField);
                insertField.Add(field.TargetField);
            }

            string strSql = string.Join("", cmds.ToArray());
            strSql += string.Format(@"merge into {0} as a 
                                        using #temp as b
                                            on ({1})
                                        when matched
                                            then update set {2}
                                        when not matched by target
                                            then insert ({3}) values ({4});"
                , task.TargetTable
                , string.Join(" and ", keyField.ToArray())
                , string.Join(",", updateField.ToArray())
                , string.Join(",", insertField.ToArray())
                , string.Join(",", insertFieldValue.ToArray())
                );
            return strSql;
        }

    }
}
