using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;
using LK.Util;
using System.Data.Common;
using MigrateDataModel;


namespace MigrateDataLib
{
    public class MigrateData
    {

        private static Dictionary<string, MigrateMission> m_migrateMessions = new Dictionary<string, MigrateMission>();

        private Parameter m_param = new Parameter();

        #region Field
        internal MigrateMission Mission
        {
            get
            {
                return m_param.Mission;
            }
        }

        internal string MissionKey
        {
            get
            {
                return m_param.MissionKey;
            }
        }

        internal string LastResultDirectory
        {
            get
            {
                return m_param.LastResultDirectory;
            }
        }

        internal string RetryDirectory
        {
            get
            {
                return m_param.RetryDirectory;
            }
        }

        internal int MigrateRowSize
        {
            get
            {
                return m_param.MigrateRowSize;
            }
        }
        #endregion

        public string DoTask(string strInput)
        {

            string errorStr = string.Empty;
            bool result = false;
            try
            {

                if (!InitialSetting(strInput, out errorStr))
                {
                    return ReturnResult("MigrateData.InitialSetting", errorStr, result);
                }

                foreach (MigrateTask task in Mission.Tasks)
                {
                    Mission.Log.WriteLog(LogLevel.Infomation, "MigrateData.DoTask", MissionKey + "-" + task.TaskName + " start.");

                    AbstractTask taskFactory = GetTaskFactory(task.TaskType);

                    taskFactory.DoRetry(task);

                    MigrateTaskLastResult lastResult = new MigrateTaskLastResult();
                    try
                    {
                        var taskResults = Mission.TaskResult.Where(item => item.TaskName.Equals(task.TaskName));
                        if (taskResults.Any())
                        {
                            lastResult = taskResults.FirstOrDefault();

                            IDataSource dataSource = GetDataSourceFactory(Mission.DataSourceType);
                            DataTable sourceData = dataSource.GetSourceNewData(Mission, task, lastResult);
                            if (sourceData == null)
                            {
                                Mission.Log.WriteLog(LogLevel.Warning, "MigrateData.GetSourceNewData", "sourceData is null");
                                break;
                            }
                            sourceData = taskFactory.ArrangeData(task, sourceData);
                            foreach (DataTable dt in GetLimitMigrateRows(sourceData, MigrateRowSize))
                            {
                                DataTable groupDt = dt;
                                if (groupDt == null || groupDt.Rows.Count == 0)
                                {
                                    continue;
                                }
                                var lastCheckValue = task.CheckBeginValue;
                                if (task.TaskType == TaskType.Batch || task.TaskType == TaskType.Update || task.TaskType == TaskType.BatchUpdate)
                                {
                                    lastCheckValue = groupDt.AsEnumerable().Max(item => item[task.CheckField]);
                                }

                                #region Before Plug In
                                Dictionary<string, LkReflectModel> dlls = new Dictionary<string, LkReflectModel>();
                                if (task.PlugIn.Count > 0)
                                {
                                    DataTable tempData = groupDt;
                                    foreach (string dll in task.PlugIn)
                                    {
                                        LkReflectModel dllInstance;
                                        if (LkReflector.GetDllClassInstance(Path.Combine(Mission.Folder, dll), dll + "." + dll, out dllInstance))
                                        {
                                            dlls[dll] = dllInstance;
                                            object[] inputParam = new object[] { Mission, task, tempData, null };
                                            object outputParam;
                                            if (LkReflector.ExecuteMethod("BeforeMigrate", dllInstance, inputParam, out outputParam))
                                            {
                                                bool dllResult = false;
                                                if (bool.TryParse(outputParam.ToString(), out dllResult) && dllResult && inputParam[3] is DataTable)
                                                {
                                                    tempData = inputParam[3] as DataTable;
                                                }
                                            }
                                        }
                                    }
                                    groupDt = tempData;
                                }
                                #endregion

                                if (groupDt != null && groupDt.Rows.Count > 0)
                                {
                                    taskFactory.DoMigrate(task, groupDt);
                                    if (task.TaskType == TaskType.Batch || task.TaskType == TaskType.Update || task.TaskType == TaskType.BatchUpdate)
                                    {
                                        lastResult.LastCheckValue = lastCheckValue;
                                    }
                                    #region After Plug In
                                    foreach (string dll in task.PlugIn)
                                    {
                                        if (dlls.ContainsKey(dll))
                                        {
                                            object[] inputParam = new object[] { Mission, task, groupDt };
                                            object outputParam;
                                            if (LkReflector.ExecuteMethod("AfterMigrate", dlls[dll], inputParam, out outputParam))
                                            {
                                                //bool dllResult = false;
                                                //if (bool.TryParse(outputParam.ToString(), out dllResult) && dllResult)
                                                //{
                                                //}
                                            }
                                        }
                                    }
                                    #endregion
                                }

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Mission.Log.WriteLog(LogLevel.Critical, MissionKey + ":" + task.TaskName + ":" + lastResult.LastCheckValue.ToString() + "-Failure. " + e.Message);
                    }
                    Mission.Log.WriteLog(LogLevel.Infomation, "MigrateData.DoTask", MissionKey + "-" + task.TaskName + " end.");
                }
                LkDataSerializer.ObjectSerialize(Path.Combine(LastResultDirectory, MissionKey), Mission.TaskResult);
                result = true;

            }
            catch (Exception e)
            {
                errorStr = e.Message;
            }
            return ReturnResult("MigrateData.DoTask", errorStr, result);

        }



        private IEnumerable<DataTable> GetLimitMigrateRows(DataTable sourceData, int rowCount)
        {
            DataTable result = sourceData.Clone();
            for (int i = 0; i < sourceData.Rows.Count; i++)
            {
                result.ImportRow(sourceData.Rows[i]);
                if (result.Rows.Count == rowCount || i == sourceData.Rows.Count - 1)
                {
                    yield return result;
                    result.Rows.Clear();
                }
            }
        }

        private AbstractTask GetTaskFactory(TaskType taskType)
        {
            AbstractTask result = null;
            switch (taskType)
            {
                case TaskType.Batch:
                    result = new BatchTask(m_param);
                    break;
                case TaskType.Sync:
                    result = new SyncTask(m_param);
                    break;
                case TaskType.Copy:
                    result = new CopyTask(m_param);
                    break;
                case TaskType.Update:
                    result = new UpdateTask(m_param);
                    break;
                case TaskType.BatchUpdate:
                    result = new BatchUpdateTask(m_param);
                    break;
                default:
                    break;
            }
            return result;
        }

        private IDataSource GetDataSourceFactory(DataSourceType dataSourceType)
        {
            IDataSource result = null;
            switch (dataSourceType)
            {
                case DataSourceType.DB:
                    result = new DbSource();
                    break;
                case DataSourceType.WebService:
                    result = new WebServiceSource();
                    break;
                case DataSourceType.FileServer:
                    result = new FileSource();
                    break;
                default:
                    break;
            }
            return result;
        }

        private string ReturnResult(string function, string errorStr, bool result)
        {
            if (!string.IsNullOrEmpty(errorStr))
            {
                string[] errorStrs = errorStr.Split(Environment.NewLine);
                foreach (string err in errorStrs)
                {
                    Mission.Log.WriteLog(LogLevel.Error, function, err);
                }
            }
            Hashtable resultHash = new Hashtable();
            if (result)
                resultHash["m_nResult"] = 1;
            else
                resultHash["m_nResult"] = 2;
            resultHash["m_strLastError"] = errorStr;
            return (LkCommonUtil.ConvertQueryStringFromHash(resultHash));
        }

        #region Initial
        private bool InitialSetting(string strInput, out string errorStr)
        {
            bool result = false;
            errorStr = string.Empty;
            try
            {
                Hashtable inputHash = null;
                inputHash = LkCommonUtil.ConvertQueryStringToHash(strInput);
                LkEncryptDecrypt.IVStr = "abcdefgh";
                LkEncryptDecrypt.KeyStr = "stuvwxyz";

                #region Setting INI
                if (!inputHash.ContainsKey("setting"))
                {
                    errorStr = "param:setting not set.";
                    return result;
                }
                string settingPath = (string)inputHash["setting"];
                m_param.MissionKey = settingPath;
                Mission.MissionKey = m_param.MissionKey;
                if (!m_migrateMessions.ContainsKey(MissionKey))
                {
                    m_migrateMessions[MissionKey] = Mission;
                }
                string fullSettingPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, settingPath);
                if (!File.Exists(fullSettingPath))
                {
                    errorStr = "「" + fullSettingPath + "」 cann't found.";
                    return result;
                }

                LkIni.IniPath = fullSettingPath;
                #endregion

                #region Check Setting Last Write Time
                DateTime time = File.GetLastWriteTimeUtc(fullSettingPath);
                if (time.Equals(m_migrateMessions[MissionKey].SettingUpdateTime))
                {
                    result = true;
                    m_param.Mission = m_migrateMessions[MissionKey];
                    return result;
                }
                else
                {
                    m_migrateMessions[MissionKey].SettingUpdateTime = time;
                }
                #endregion

                #region Mission Setting
                Mission.TimeZone = LkIni.GetProfileInt("Migrate Setting", "timeZone", 0);
                Mission.Folder = LkIni.GetProfileString("Migrate Setting", "folder", string.Empty);
                #endregion

                #region Log Setting
                Mission.Log = new LkLogInstance(new LkLogParams()
                {
                    LogImplType = LogImplType.FileDelta,
                    LogType = LogType.File,
                    LogRetentionDayCount = LkIni.GetProfileInt("Log Setting", "retentionDay", 30),
                    MaxLogSize = LkIni.GetProfileInt("Log Setting", "maxLogSize", 200),
                    LogLevel = LogLevel.Verbose,
                    LogPath = LkCommonUtil.GetFilePath(LkIni.GetProfileString("Log Setting", "path", @".\LogFiles"))
                });
                #endregion

                #region Source Database DAO

                string sourceConnStr = LkIni.GetProfileString("Source Database", "connectionString", string.Empty);
                Mission.SourceConnectString = sourceConnStr;
                if (LkEncryptDecrypt.IsEncrypt(sourceConnStr))
                {
                    LkEncryptDecrypt.Decrypt(sourceConnStr, out sourceConnStr);
                }
                else
                {
                    string encryptResult = string.Empty;
                    if (LkEncryptDecrypt.Encrypt(sourceConnStr, out encryptResult))
                    {
                        LkIni.WriteProfileString("Source Database", "connectionString", encryptResult);
                    }
                }

                if (string.IsNullOrEmpty(sourceConnStr))
                {
                    errorStr = "From Database:connectionString not set.";
                    return result;
                }
                object dataSourceTypeObj = LkIni.GetProfileEnum(typeof(DataSourceType), "Source Database", "type", "");
                if (dataSourceTypeObj == null)
                {
                    Mission.DataSourceType = DataSourceType.DB;
                    CommonType sourceType = (CommonType)LkIni.GetProfileEnum(typeof(CommonType), "Source Database", "type", "SqlServer");
                    Mission.SourceDao = new LkDaoInstance(sourceConnStr, sourceType, null);
                }
                else
                {
                    Mission.DataSourceType = (DataSourceType)dataSourceTypeObj;
                }
                #endregion

                #region Target Database DAO
                string targetConnStr = LkIni.GetProfileString("Target Database", "connectionString", string.Empty);
                if (LkEncryptDecrypt.IsEncrypt(targetConnStr))
                {
                    LkEncryptDecrypt.Decrypt(targetConnStr, out targetConnStr);
                }
                else
                {
                    string encryptResult = string.Empty;
                    if (LkEncryptDecrypt.Encrypt(targetConnStr, out encryptResult))
                    {
                        LkIni.WriteProfileString("Target Database", "connectionString", encryptResult);
                    }
                }



                if (string.IsNullOrEmpty(targetConnStr))
                {
                    errorStr = "To Database:connectionString not set.";
                    return result;
                }
                CommonType targetType = (CommonType)LkIni.GetProfileEnum(typeof(CommonType), "Target Database", "type", "SqlServer");
                Mission.TargetDao = new LkDaoInstance(targetConnStr, targetType, null);
                #endregion

                #region Get Task
                List<string> migrateTaskNames;
                GetMigrateTaskNames(out migrateTaskNames);
                string errorBuilder = string.Empty;
                List<MigrateTask> tasks = new List<MigrateTask>();
                foreach (string migrageTaskName in migrateTaskNames)
                {
                    MigrateTask migrateTask;
                    if (GetMigrateTask(migrageTaskName, out migrateTask, out errorStr))
                    {
                        tasks.Add(migrateTask);
                    }
                    else
                    {
                        errorBuilder += errorStr + Environment.NewLine;
                    }
                }
                Mission.Tasks = tasks;
                #endregion

                #region Get Task Result
                object taskResultObj;
                if (LkDataSerializer.ObjectDeserialize(Path.Combine(LastResultDirectory, MissionKey), out taskResultObj))
                {
                    Mission.TaskResult = (List<MigrateTaskLastResult>)taskResultObj;
                }
                else
                {
                    Mission.TaskResult = new List<MigrateTaskLastResult>();
                    foreach (MigrateTask task in Mission.Tasks)
                    {
                        Mission.TaskResult.Add(new MigrateTaskLastResult() { TaskName = task.TaskName, LastCheckValue = task.CheckBeginValue });
                    }

                }
                foreach (MigrateTask task in Mission.Tasks)
                {
                    var taskResultCollect = Mission.TaskResult.Where(item => item.TaskName.Equals(task.TaskName));
                    if (taskResultCollect.Any())
                    {
                        MigrateTaskLastResult taskResult = taskResultCollect.FirstOrDefault();
                        if (task.TaskType == TaskType.Sync)
                        {
                            taskResult.LastCheckValue = task.CheckBeginValue;
                        }
                        else if (LkCompareUtil.Compare(LkCompareType.IsGreaterThen, task.CheckType, task.CheckBeginValue, taskResult.LastCheckValue))
                        {
                            taskResult.LastCheckValue = task.CheckBeginValue;
                        }
                    }
                    else
                    {
                        Mission.TaskResult.Add(new MigrateTaskLastResult() { TaskName = task.TaskName, LastCheckValue = task.CheckBeginValue });
                    }
                }
                LkDataSerializer.ObjectSerialize(Path.Combine(LastResultDirectory, MissionKey), Mission.TaskResult);
                #endregion

                errorStr = errorBuilder;
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

        private bool GetMigrateTask(string migrateName, out MigrateTask migrateTask, out string errorStr)
        {
            migrateTask = new MigrateTask();
            errorStr = string.Empty;
            bool result = false;
            try
            {
                migrateTask.TaskName = migrateName;
                migrateTask.TaskType = (TaskType)LkIni.GetProfileEnum(typeof(TaskType), migrateName, "taskType", "Batch");
                migrateTask.SourceTable = LkIni.GetProfileString(migrateName, "sourceTable", string.Empty);
                migrateTask.SourceMethod = LkIni.GetProfileString(migrateName, "sourceMethod", string.Empty);
                migrateTask.SourceParam = LkIni.GetProfileString(migrateName, "sourceParam", "$CheckField");
                migrateTask.sourceReturnFormat = (SourceReturnFormat)LkIni.GetProfileEnum(typeof(SourceReturnFormat), migrateName, "sourceReturnFormat", "DataTable");
                migrateTask.TargetTable = LkIni.GetProfileString(migrateName, "targetTable", string.Empty);
                migrateTask.CheckField = LkIni.GetProfileString(migrateName, "checkField", string.Empty);
                migrateTask.CheckEqual = LkIni.GetProfileBool(migrateName, "checkEqual", false);
                migrateTask.Condition = LkIni.GetProfileString(migrateName, "condition", string.Empty);
                migrateTask.ExtraField = LkIni.GetProfileString(migrateName, "extraField", string.Empty);


                string syncField = LkIni.GetProfileString(migrateName, "syncField", string.Empty);
                string typeStr = LkIni.GetProfileString(migrateName, "checkType", "datetime");
                if (string.IsNullOrEmpty(typeStr))
                {
                    typeStr = "datetime";
                }
                string beginValue = LkIni.GetProfileString(migrateName, "checkBeginValue", "");


                Type checkType = typeof(DateTime);
                switch (typeStr.ToUpper())
                {
                    case "DATETIME":
                        if (string.IsNullOrEmpty(beginValue))
                        {
                            beginValue = "1970/1/1 00:00:00";
                        }
                        break;
                    case "DOUBLE":
                        checkType = typeof(double);
                        if (string.IsNullOrEmpty(beginValue))
                        {
                            beginValue = "0";
                        }
                        break;
                }
                migrateTask.CheckType = checkType;

                var converter = System.ComponentModel.TypeDescriptor.GetConverter(checkType);
                var checkBeginValue = converter.ConvertFrom(beginValue);
                migrateTask.CheckBeginValue = checkBeginValue;

                string plugIn = LkIni.GetProfileString(migrateName, "plugIn", "");
                migrateTask.PlugIn = plugIn.Split(",").ToList();


                GetMigrateFieldMapping(migrateTask);

                GetSyncFieldMapping(migrateTask, syncField);

                if (migrateTask.Check(out errorStr))
                {
                    if (Mission.CheckTask(migrateTask, out errorStr))
                    {
                        result = true;
                    }
                }
            }
            catch (Exception e)
            {
                errorStr = e.Message;
            }
            return result;

        }

        private bool GetSyncFieldMapping(MigrateTask migrateTask, string syncField)
        {
            bool result = false;
            try
            {
                List<MigrateField> fieldList = new List<MigrateField>();
                if (!string.IsNullOrEmpty(syncField))
                {
                    string[] syncFields = syncField.Split(",");

                    foreach (string sourceField in syncFields)
                    {
                        string targetField = string.Empty;
                        var targetFieldCollect = migrateTask.Fields.Where(item => item.SourceField.Equals(sourceField));
                        if (targetFieldCollect.Any())
                        {
                            targetField = targetFieldCollect.FirstOrDefault().TargetField;
                            MigrateField mField = new MigrateField();
                            mField.SourceField = sourceField;
                            mField.TargetField = targetField;
                            fieldList.Add(mField);
                        }
                    }
                    result = true;
                }
                migrateTask.SyncFields = fieldList;
            }
            catch (Exception e)
            {
                Mission.Log.WriteLog(LogLevel.Error, e);
            }
            return result;
        }

        private bool GetMigrateFieldMapping(MigrateTask task)
        {
            bool result = false;
            List<MigrateField> field = new List<MigrateField>();

            //TODO 想移到parameter
            List<string> excludeKeys = new List<string>();
            excludeKeys.Add("taskType".ToUpper());
            excludeKeys.Add("sourceTable".ToUpper());
            excludeKeys.Add("sourceMethod".ToUpper());
            excludeKeys.Add("sourceParam".ToUpper());
            excludeKeys.Add("sourceReturnFormat".ToUpper());
            excludeKeys.Add("targetTable".ToUpper());
            excludeKeys.Add("checkField".ToUpper());
            excludeKeys.Add("checkType".ToUpper());
            excludeKeys.Add("checkBeginValue".ToUpper());
            excludeKeys.Add("checkEqual".ToUpper());
            excludeKeys.Add("syncField".ToUpper());
            excludeKeys.Add("condition".ToUpper());
            excludeKeys.Add("extraField".ToUpper());
            excludeKeys.Add("plugIn".ToUpper());

            try
            {
                Dictionary<string, string> allKeys = LkIni.GetKeyValues(task.TaskName);
                foreach (KeyValuePair<string, string> item in allKeys)
                {
                    if (!excludeKeys.Contains(item.Key.ToUpper()))
                    {
                        if (string.IsNullOrEmpty(item.Value))
                        {
                            Mission.Log.WriteLog(LogLevel.Warning, task.TaskName + ":" + item.Key + " value not set.");
                        }
                        else
                        {
                            string defaultValue = string.Empty;
                            string sourceField = string.Empty;
                            Type defaultType = null;
                            if (item.Value.Contains("(") && item.Value.Contains(")"))
                            {
                                sourceField = item.Value.Substring(0, item.Value.IndexOf("("));
                                defaultValue = item.Value.Replace(sourceField, string.Empty).Replace("(", string.Empty).Replace(")", string.Empty);

                                DateTime dtTemp;
                                double dTemp;
                                if (DateTime.TryParse(defaultValue, out dtTemp))
                                {
                                    defaultType = typeof(DateTime);
                                }
                                else if (double.TryParse(defaultValue, out dTemp))
                                {
                                    defaultType = typeof(double);
                                }
                                else
                                {
                                    defaultType = typeof(string);
                                }
                            }
                            else
                            {
                                sourceField = item.Value;
                            }
                            field.Add(new MigrateField() { SourceField = sourceField, TargetField = item.Key, SourceValue = defaultValue, SourceValueType = defaultType });
                            task.TargetFieldStr += item.Key + ",";
                            if (string.IsNullOrEmpty(defaultValue))
                            {
                                task.SourceFieldStr += sourceField + ",";
                            }
                            else
                            {
                                task.SourceFieldStr += sourceField.ParseDbField(defaultValue, defaultType, Mission.SourceDao.CommonType) + ",";
                            }
                        }
                    }
                }
                task.Fields = field;
                task.TargetFieldStr = task.TargetFieldStr.TrimEnd(',');
                task.SourceFieldStr = task.SourceFieldStr.TrimEnd(',');
                result = true;
            }
            catch (Exception e)
            {
                Mission.Log.WriteLog(LogLevel.Error, e);
            }
            return result;
        }

        private bool GetMigrateTaskNames(out List<string> migrateTaskNames)
        {
            bool result = false;
            migrateTaskNames = new List<string>();
            try
            {
                List<string> allSection = LkIni.GetSections();
                foreach (string section in allSection)
                {
                    if (section.ToUpper().Contains("MIGRATE TASK") && !migrateTaskNames.Contains(section.ToUpper()))
                    {
                        migrateTaskNames.Add(section);
                    }
                }
                result = true;
            }
            catch (Exception e)
            {
                Mission.Log.WriteLog(LogLevel.Error, e);
            }
            return result;

        }
        #endregion
    }
}
