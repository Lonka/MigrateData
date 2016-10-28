using LK.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using MigrateDataModel;

namespace MigrateDataLib
{
    internal abstract class AbstractTask
    {
        protected Parameter m_param;

        #region Field
        public MigrateMission Mission
        {
            get
            {
                return m_param.Mission;
            }
        }

        public string MissionKey
        {
            get
            {
                return m_param.MissionKey;
            }
        }

        public string LastResultDirectory
        {
            get
            {
                return m_param.LastResultDirectory;
            }
        }

        public string RetryDirectory
        {
            get
            {
                return m_param.RetryDirectory;
            }
        }

        public int ParamSize
        {
            get
            {
                return m_param.ParamSize;
            }
        }

        public int RetryMax
        {
            get
            {
                return m_param.RetryMax;
            }
        }

        public int RetryLimit
        {
            get
            {
                return m_param.RetryLimit;
            }
        }
        #endregion



        public virtual DataTable ArrangeData(MigrateTask task,DataTable dt)
        {
            return dt;
        }

        public virtual bool DoRetry(MigrateTask task)
        {
            bool result = false;
            try
            {
                string dirPath = Path.Combine(RetryDirectory, Path.Combine(MissionKey, task.TaskName));
                if (Directory.Exists(dirPath))
                {
                    FileInfo[] files = new DirectoryInfo(dirPath).GetFiles();
                    if (files.Length > 0)
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            if (files[i].Name.Split("_".ToArray(), StringSplitOptions.RemoveEmptyEntries).Length == RetryMax)
                            {
                                files[i].Delete();
                                continue;
                            }
                            if (i >= RetryLimit)
                            {
                                break;
                            }
                            DataTable dt;
                            LkDataSerializer.DataTableDeserialize(files[i].FullName, out dt);
                            if (DoMigrate(task, dt, true))
                            {
                                files[i].Delete();
                            }
                            else
                            {
                                files[i].MoveTo(files[i].FullName + "_d");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Mission.Log.WriteLog(LogLevel.Error, e);
            }
            return result;

        }

        public virtual bool Execute(MigrateTask task, string sql, List<DbParameter> paramList)
        {
            bool result = false;
            try
            {
                Mission.TargetDao.ExecuteSQL(sql, paramList, false);
                result = true;
            }
            catch (Exception e)
            {
                Mission.Log.WriteLog(LogLevel.Error, e);
            }
            return result;
        }

        public virtual bool DoMigrate(MigrateTask task, DataTable sourceNewData, bool isRetry = false)
        {
            bool result = false;
            try
            {
                DelExistRow(task, sourceNewData);

                List<string> cmds = new List<string>();
                List<DbParameter> paramList = new List<DbParameter>();
                int i = 0;
                int insertIndex = 0;
                bool isFirstRow = true;
                foreach (DataRow dr in sourceNewData.Rows)
                {
                    string sqlStr = GenSingleSqlStr(task, paramList, i, dr, isFirstRow);
                    isFirstRow = false;
                    cmds.Add(sqlStr);

                    if (paramList.Count >= ParamSize || i == sourceNewData.Rows.Count - 1)
                    {
                        string sql = GenFinalSqlStr(task, cmds);

                        if (!Execute(task, sql, paramList))
                        {
                            if (isRetry)
                            {
                                return result;
                            }
                            DataTable failureData = sourceNewData.Clone();
                            for (int rowIndex = insertIndex; rowIndex <= i; rowIndex++)
                            {
                                failureData.ImportRow(sourceNewData.Rows[rowIndex]);
                            }
                            LkDataSerializer.DataTableSerialize(
                                Path.Combine(RetryDirectory, Path.Combine(Path.Combine(MissionKey, task.TaskName), DateTime.Now.ToString("yyyyMMddHHmmssfff"))),
                                failureData);
                        }
                        cmds.Clear();
                        paramList.Clear();
                        insertIndex = i;
                        isFirstRow = true;
                    }
                    i++;
                }
                result = true;
            }
            catch (Exception e)
            {
                Mission.Log.WriteLog(LogLevel.Error, e);
            }
            return result;
        }

        protected virtual string GenFinalSqlStr(MigrateTask task, List<string> cmds)
        {
            return LkStringUtil.ConvertInsertString(Mission.TargetDao.CommonType, cmds);
        }

        protected virtual string GenSingleSqlStr(MigrateTask task, List<DbParameter> paramList, int i, DataRow dr, bool isFirstRow)
        {
            string sqlStr = string.Empty;
            string valueStr = string.Empty;
            foreach (MigrateField field in task.Fields)
            {
                valueStr += (field.SourceField + "_" + i).ParseDbParam(Mission.TargetDao.CommonType) + ",";
                DbParameter param = LkDaoUtil.GetParameter(Mission.TargetDao.CommonType);
                param.ParameterName = field.SourceField + "_" + i;
                param.DbType = LkDaoUtil.GetDbType(dr[field.SourceField].GetType());
                param.Value = (dr[field.SourceField].ToString().ToUpper().Equals("NULL") ? Convert.DBNull : dr[field.SourceField]);
                paramList.Add(param);
            }

            sqlStr = string.Format(" into {0} ({1}) values ({2}) ", task.TargetTable, task.TargetFieldStr, valueStr.TrimEnd(','));
            return sqlStr;
        }

        protected virtual bool DelExistRow(MigrateTask task, DataTable sourceNewData)
        {
            return false;
        }
    }
}
