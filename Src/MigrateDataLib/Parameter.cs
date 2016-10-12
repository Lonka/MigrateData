using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MigrateDataModel;

namespace MigrateDataLib
{
    internal class Parameter
    {
        public int MigrateRowSize = 200;
        public int ParamSize = 2000;
        public int ExecuteSqlSize = 4000;
        public int RetryLimit = int.MaxValue;
        public int RetryMax = 3;
        public string _lastResultDirectory = "TempFiles";
        public string LastResultDirectory
        {
            get
            {
                return Mission.Folder + "\\" + _lastResultDirectory;
            }
        }
        public string _retryDirectory = "RetryFiles";
        public string RetryDirectory
        {
            get
            {
                return Mission.Folder + "\\" + _retryDirectory;
            }
        }
        public string MissionKey = string.Empty;
        internal MigrateMission Mission = new MigrateMission();


    }
}
