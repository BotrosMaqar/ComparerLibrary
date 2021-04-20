using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ITWorx.ComparerLibrary
{
    public class ComparerSettingDTO
    {
        public ComparerSettingDTO()
        {
            PropsToExcludeFromComparison = new List<string>();
            UniqueKeysToCompareCollectionsWith = new Dictionary<string, List<string>>();
        }
        public object SourceObject { get; set; }
        public object DestinationObject { get; set; }
        public List<string> PropsToExcludeFromComparison { get; set; }
        public bool CompareCollectionsInMainObjectOnly { get; set; }
        public string MainObjectName { get; set; }
        public bool CompareCollectionUsingTheirIndexes { get; set; }
        public Dictionary<string, List<string>> UniqueKeysToCompareCollectionsWith { get; set; }
        public bool CompareColumnsInDataSet { get; set; }

    }
}
