using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrateDataModel
{
    public class MigrateField
    {
        public string SourceField { get; set; }

        public string SourceValue { get; set; }

        public Type SourceValueType { get; set; }
        public string TargetField { get; set; }
    }
}
