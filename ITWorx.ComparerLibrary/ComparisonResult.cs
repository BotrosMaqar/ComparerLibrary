using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ITWorx.ComparerLibrary
{
    public class ComparisonResult
    {
        public string TableName { get; set; }
        public string PropertyName { get; set; }
        public string DestinationValue { get; set; }
        public string SourceValue { get; set; }
        public string DifferenceType { get; set; }
        public bool HasCountDifference { get; set; }
        public int DestinationCount { get; set; }
        public int SourceCount { get; set; }
        public string CustomError { get; set; }

        public PropertyInfo SourceProperty { get; set; }

        public PropertyInfo DestinationProperty { get; set; }

        public object DestinationObject { get; set; }

        public object SourceObject { get; set; }

    }
}
