using LK.Util;
using MigrateDataModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace MigrateDataLib
{
    internal class BatchUpdateTask : AbstractTask
    {
        public BatchUpdateTask(Parameter param)
        {
            m_param = param;
        }

        public override DataTable ArrangeData(MigrateTask task,DataTable dt)
        {
            dt = dt.AsEnumerable().OrderByDescending(item => item[task.CheckField]).CopyToDataTable();
            DataTable result = dt.Clone();
            foreach (DataRow dr in dt.Rows)
            {
                var containRows = result.AsEnumerable().Where(item =>
                {
                    foreach (MigrateField field in task.SyncFields)
                    {
                        if (!item[field.SourceField].ToString().Equals(dr[field.SourceField].ToString()))
                        {
                            return false;
                        }
                    }
                    return true;
                });
                if(!containRows.Any())
                {
                    result.Rows.Add(dr.ItemArray);
                }
            }
            return result;
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
