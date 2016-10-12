using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MigrateDataModel;
using System.Data;
using LK.Util;

namespace Etl
{
    public class Etl : MigratePlugInBase
    {
        private MigrateMission Mission = new MigrateMission();
        private MigrateTask Task = new MigrateTask();

        public override bool AfterMigrate(MigrateMission mission, MigrateTask task, DataTable sourceDt)
        {
            return true;
        }

        public override bool BeforeMigrate(MigrateMission mission, MigrateTask task, DataTable sourceDt, out DataTable processDt)
        {
            Mission = mission;
            Task = task;

            if (mission.TimeZone != 0)
            {
                foreach (DataRow dr in sourceDt.Rows)
                {
                    foreach (DataColumn dc in sourceDt.Columns)
                    {
                        MappingInfo mappingInfo = new MappingInfo();
                        if (dr[dc].GetType() == typeof(DateTime))
                        {
                            if (!string.IsNullOrEmpty(dr[dc].ToString()))
                            {
                                dr[dc] = ((DateTime)dr[dc]).AddHours(-mission.TimeZone);
                            }
                        }
                        else if (ContainCol(task.SourceTable + "." + dc.ColumnName, out mappingInfo))
                        {
                            int idValue;
                            if (GetLocalID(dr[dc].ToString(), mappingInfo, out idValue))
                            {
                                dr[dc] = idValue;
                            }
                        }
                    }
                }
            }
            processDt = sourceDt;
            return true;
        }

        private bool GetLocalID(string sourceValue, MappingInfo mappingInfo, out int idValue)
        {
            idValue = int.MinValue;
            bool result = false;
            try
            {
                if (!ConvertDt.ContainsKey(mappingInfo.TableName))
                {
                    ConvertDt[mappingInfo.TableName] = Mission.TargetDao.GetDataTable("select * from " + mappingInfo.TableName);
                }
                DataTable dt = ConvertDt[mappingInfo.TableName];
                var idCollect = dt.AsEnumerable().Where(item => item[mappingInfo.MappingField].ToString().Equals(sourceValue));
                if (idCollect.Any())
                {
                    idValue = int.Parse(idCollect.FirstOrDefault()[mappingInfo.GetField].ToString());
                    result = true;
                }
            }
            catch (Exception e)
            {
            }
            return result;
        }

        private bool ContainCol(string colKey, out MappingInfo mappingInfo)
        {
            mappingInfo = new MappingInfo();
            bool result = false;
            if (ConvertIdField.ContainsKey(Mission.MissionKey.ToUpper()))
            {
                result = ConvertIdField[Mission.MissionKey.ToUpper()].ContainsKey(colKey);
                if (result)
                {
                    mappingInfo = ConvertIdField[Mission.MissionKey.ToUpper()][colKey];
                }
            }
            return result;
        }



        private Dictionary<string, Dictionary<string, MappingInfo>> ConvertIdField = new Dictionary<string, Dictionary<string, MappingInfo>>()
        {
            //Mission Key
            {"MES_1.INI"
                ,new Dictionary<string,MappingInfo>() 
                {
                    //Source Table.Field -> Target Table.MappingField;Table.GetField
                    {"TERMINAL.TYPE_ID",new MappingInfo(){ TableName = "TERMINAL_TYPE", MappingField="CODE", GetField="ID"}}
                    ,{"TERMINAL.PROCESS_ID",new MappingInfo(){ TableName = "PROCESS", MappingField="CODE", GetField="ID"}}
                    ,{"TERMINAL.PDLINE_ID",new MappingInfo(){ TableName = "PDLINE", MappingField="CODE", GetField="ID"}}

                    ,{"ROUTE_PROCESS_MAPPING.ROUTE_ID",new MappingInfo(){ TableName = "ROUTE", MappingField="CODE", GetField="ID"}}
                    ,{"ROUTE_PROCESS_MAPPING.PROCESS_ID",new MappingInfo(){ TableName = "PROCESS", MappingField="CODE", GetField="ID"}}

                    ,{"WORK_ORDER.BOM_ID",new MappingInfo(){ TableName = "BOM", MappingField="CODE", GetField="ID"}}
                    ,{"WORK_ORDER.ROUTE_ID",new MappingInfo(){ TableName = "ROUTE", MappingField="CODE", GetField="ID"}}

                    ,{"PRODUCT_LIST.WO_ID",new MappingInfo(){ TableName = "WORK_ORDER", MappingField="CODE", GetField="ID"}}

                    ,{"PRODUCT_PASS_HISTORY.WO_ID",new MappingInfo(){ TableName = "WORK_ORDER", MappingField="CODE", GetField="ID"}}
                    ,{"PRODUCT_PASS_HISTORY.TERMINAL_ID",new MappingInfo(){ TableName = "TERMINAL", MappingField="CODE", GetField="ID"}}
                    ,{"PRODUCT_PASS_HISTORY.PRODUCT_ID",new MappingInfo(){ TableName = "PRODUCT_LIST", MappingField="PRODUCT_ID", GetField="ID"}}
                    ,{"PRODUCT_PASS_HISTORY.PROCESS_ID",new MappingInfo(){ TableName = "PROCESS", MappingField="CODE", GetField="ID"}}
                }
            },
        };

        private Dictionary<string, DataTable> ConvertDt = new Dictionary<string, DataTable>();

    }

}
