using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MigrateDataModel;
using System.Data;
using LK.Util;

namespace MigrateDataLib
{
    internal class WebServiceSource : IDataSource
    {
        public DataTable GetSourceNewData(MigrateMission mission, MigrateTask task, MigrateTaskLastResult lastResult)
        {
            DataTable result = null;
            string[] param =  task.SourceParam.Split(",");
            object[] input = new object[param.Length];
            for (int i=0;i<param.Length;i++)
            {
                string token = param[i];
                if(token.StartsWith("$"))
                {
                    if(token.ToUpper().Equals("$CHECKFIELD"))
                    {
                        input[i] = lastResult.LastCheckValue;
                    }
                    else if (token.ToUpper().Equals("$NOWTIME"))
                    {
                        input[i] = DateTime.Now;
                    }
                }
                else
                {
                    input[i] = token;
                }
            }

            object output;
            if(LkReflector.ExecuteWebServiceMethod(mission.SourceConnectString,null,task.SourceMethod,input,out output))
            {
                result = DataConvertFactory.GetFactory(task.sourceReturnFormat).DataConvert(output);
            }
            return result;
        }
    }
}
