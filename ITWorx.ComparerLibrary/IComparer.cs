using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITWorx.ComparerLibrary
{
    public interface IComparer
    {
        List<ComparisonResult> Compare(ComparerSettingDTO comparerSettingDTO);
        List<ComparisonResult> CollectionCompare(string tableName, ICollection sourceCollection, dynamic destinationCollection, ComparerSettingDTO comparerSettingDTO);

        

    }

}
