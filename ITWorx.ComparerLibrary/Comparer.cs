using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace ITWorx.ComparerLibrary
{
    public class Comparer : IComparer
    {
        /// <summary>
        /// compare two objects 
        /// </summary>
        /// <param name="comparerSettingDTO"> setting DTO contains Source Object And Destination objects that we need to compare and propsToExcludeFromComparison(props to exclude like PK, creation date as those props will be different anyway) and compareCollectionsInMainObjectOnly (if main object contains collections/classes props that contain collection also this param to determine compare those nested collections or no as the json object is different that DB object in such cases) and compareClassTypeProps (some class properties contains class properties also and retrieved from DB but json object is null) and CompareCollectionUsingTheirIndexes (bool to decide to compare collection using indexes or using primary key in the next param) and UniqueKeysToCompareCollectionsWith dictionary to contain pk column names for earch table that we want to compare and we can add All key if more than one table will be compared using same column name)</param>
        /// <returns></returns>
        public List<ComparisonResult> Compare(ComparerSettingDTO comparerSettingDTO)
        {
            try
            {
                List<ComparisonResult> comparisonResults = new List<ComparisonResult>();
                if (comparerSettingDTO.SourceObject != null && comparerSettingDTO.DestinationObject != null)
                {
                    PropertyInfo[] sourceObjectProperties = comparerSettingDTO.SourceObject.GetType().GetProperties();
                    PropertyInfo[] destinationObjectProperties = comparerSettingDTO.DestinationObject.GetType().GetProperties();
                    //List<string> excludedComparisonProperties = new List<string> { "SsmaTimeStamp", "TrialId", "Trial", "IdSeq", "Pk", "ProgressiveFilterFlag" }; 

                    foreach (PropertyInfo sourceProperty in sourceObjectProperties)
                    {
                        if (!comparerSettingDTO.PropsToExcludeFromComparison.Contains(sourceProperty.Name))
                        {
                            for (int index = 0; index < destinationObjectProperties.Length; index++)
                            {
                                if (sourceProperty.Name == destinationObjectProperties[index].Name)// compare two properties 
                                {
                                    PropertyInfo destinationProperty = destinationObjectProperties[index];
                                    comparisonResults.AddRange(Compare(comparerSettingDTO, sourceProperty, destinationProperty));
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (comparerSettingDTO.SourceObject != null && comparerSettingDTO.DestinationObject == null)
                {
                    ComparisonResult comparisonResult = new ComparisonResult();
                    comparisonResult.TableName = comparerSettingDTO.SourceObject.GetType().Name;
                    string recordValue = string.Empty;

                    recordValue = GetRecordUniqueOrAllVaues(comparerSettingDTO, comparerSettingDTO.SourceObject);

                    comparisonResult.CustomError = "This recored is not exists on Destination, record values is " + recordValue;
                    comparisonResults.Add(comparisonResult);
                }

                else if (comparerSettingDTO.DestinationObject != null && comparerSettingDTO.SourceObject == null)
                {
                    ComparisonResult comparisonResult = new ComparisonResult();
                    comparisonResult.TableName = comparerSettingDTO.DestinationObject.GetType().Name;
                    string recordValue = GetRecordUniqueOrAllVaues(comparerSettingDTO, comparerSettingDTO.DestinationObject);
                    comparisonResult.CustomError = "This recored is not exists on Source, record values is " + recordValue;
                    comparisonResults.Add(comparisonResult);
                }
                else
                {
                    //nothing to do
                }
                return comparisonResults;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private string GetRecordUniqueOrAllVaues(ComparerSettingDTO comparerSettingDTO, object sourceObject)
        {
            string recordValue = string.Empty;

            string tableName = sourceObject.GetType().Name;
            List<string> propsToCompareWith = new List<string>();
            if (comparerSettingDTO.UniqueKeysToCompareCollectionsWith.ContainsKey(tableName))
            {
                propsToCompareWith = comparerSettingDTO.UniqueKeysToCompareCollectionsWith[tableName];
                recordValue = GetPropsValues(propsToCompareWith, sourceObject);
            }
            else if (comparerSettingDTO.UniqueKeysToCompareCollectionsWith.ContainsKey("All"))
            {
                propsToCompareWith = comparerSettingDTO.UniqueKeysToCompareCollectionsWith["All"];
                recordValue = GetPropsValues(propsToCompareWith, sourceObject);
            }
            else
            {
                PropertyInfo[] sourceObjectProperties = sourceObject.GetType().GetProperties();
                foreach (PropertyInfo sourceProp in sourceObjectProperties)
                {
                    bool isValueType = CheckIfPropertyIsValueType(sourceProp);
                    if (isValueType)
                    {
                        object propValue = sourceProp.GetValue(sourceObject);
                        if (propValue != null && !string.IsNullOrWhiteSpace(propValue.ToString()))
                        {
                            recordValue += " " + sourceProp.Name + "= " + propValue.ToString(); //sourceProperty.GetValue(comparerSettingDTO.SourceObject);
                        }
                    }
                }
                return recordValue;
            }
            return recordValue;
        }

        private string GetPropsValues(List<string> propsToCompareWith, object sourceObject)
        {
            string propsWithValues = string.Empty;
            foreach (string item in propsToCompareWith)
            {
                object propValue = sourceObject.GetType().GetProperty(item).GetValue(sourceObject, null);
                propsWithValues += " " + item + "= " + propValue.ToString(); //sourceProperty.GetValue(comparerSettingDTO.SourceObject);
            }
            return propsWithValues;
        }

        private bool CheckIfPropertyIsValueType(PropertyInfo sourceProperty)
        {
            bool isValueType = true;
            if (typeof(IEnumerable).IsAssignableFrom(sourceProperty.PropertyType) && !typeof(string).IsAssignableFrom(sourceProperty.PropertyType))//check if collection and not string
            {
                isValueType = false; // collection
            }

            if (sourceProperty.GetType().IsClass && !typeof(string).IsAssignableFrom(sourceProperty.PropertyType) && sourceProperty.PropertyType.GetTypeInfo().BaseType?.Name != "ValueType")
            {
                isValueType = false;
            }
            return isValueType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="comparerSettingDTO"></param>
        /// <param name="sourceProperty"></param>
        /// <param name="destinationProperty"></param>
        /// <returns></returns>
        private List<ComparisonResult> Compare(ComparerSettingDTO comparerSettingDTO, PropertyInfo sourceProperty, PropertyInfo destinationProperty)
        {
            try
            {
                List<ComparisonResult> comparisonResults = new List<ComparisonResult>();
                string tableName = comparerSettingDTO.SourceObject.GetType().Name; // get main object name
                string propertyName = sourceProperty.Name;
                if (typeof(IEnumerable).IsAssignableFrom(sourceProperty.PropertyType) && !typeof(string).IsAssignableFrom(sourceProperty.PropertyType))//check if collection and not string
                {
                    //if (comparerSettingDTO.CompareCollectionsInMainObjectOnly && tableName != comparerSettingDTO.MainObjectName) // to stop compare nested collections like the collections in setting table
                    //{
                    //    return comparisonResults;
                    //}
                    //else
                    {
                        tableName = sourceProperty.Name;//get property table name
                        dynamic sourceCollection = null;
                        try
                        {
                            sourceCollection = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(sourceProperty.PropertyType));
                            if (sourceProperty.GetValue(comparerSettingDTO.SourceObject) != null)
                            {
                                sourceCollection = sourceProperty.GetValue(comparerSettingDTO.SourceObject);
                            }
                            if (sourceCollection != null && sourceCollection.Count == 0)
                                sourceCollection = null;
                        }
                        catch (Exception ex) // no need to handle exception
                        {

                        }
                        dynamic destinationObjectCollection = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(destinationProperty.PropertyType));

                        if (typeof(IEnumerable).IsAssignableFrom(destinationProperty.PropertyType) && destinationProperty.GetValue(comparerSettingDTO.DestinationObject) != null)
                        {
                            destinationObjectCollection = destinationProperty.GetValue(comparerSettingDTO.DestinationObject);
                            if (destinationObjectCollection.Count == 0)
                                destinationObjectCollection = null;
                        }
                        else
                            destinationObjectCollection = null; // should check if not collection and set it
                                                                //compare two collections with each others
                        comparisonResults.AddRange(CollectionCompare(tableName, sourceCollection, destinationObjectCollection, comparerSettingDTO));
                    }
                }
                else if (destinationProperty.PropertyType.Name == "DataSet")
                {
                    dynamic sourceDataSet = null;
                    dynamic destinationDataSet = null;
                    if (sourceProperty.GetValue(comparerSettingDTO.SourceObject) != null)
                    {
                        sourceDataSet = sourceProperty.GetValue(comparerSettingDTO.SourceObject);
                    }
                    if (destinationProperty.GetValue(comparerSettingDTO.DestinationObject) != null)
                    {
                        destinationDataSet = destinationProperty.GetValue(comparerSettingDTO.DestinationObject);
                    }
                    #region compared result 
                    //compare columns count
                    ComparisonResult comparisonResult = null;

                    foreach (DataTable sourceDataTable in sourceDataSet.Tables)
                    {
                        foreach (DataTable destinationDataTable in destinationDataSet.Tables)
                        {
                            if (comparerSettingDTO.CompareColumnsInDataSet)
                            {
                                if (destinationDataTable.Columns.Count != sourceDataTable.Columns.Count)
                                {
                                    comparisonResult = new ComparisonResult();
                                    comparisonResult.TableName = tableName;
                                    comparisonResult.HasCountDifference = true;
                                    comparisonResult.SourceCount = sourceDataTable != null ? sourceDataTable.Columns.Count : 0;
                                    comparisonResult.DestinationCount = destinationDataTable != null ? destinationDataTable.Columns.Count : 0;
                                    comparisonResults.Add(comparisonResult);
                                }
                                #region Compare columns names
                                foreach (DataColumn sourceColumn in sourceDataTable.Columns)
                                {
                                    string columnName = sourceColumn.ColumnName;
                                    bool columnNameExists = false;
                                    foreach (DataColumn destinationColumn in destinationDataTable.Columns)
                                    {
                                        if (columnName == destinationColumn.ColumnName)
                                        {
                                            columnNameExists = true;
                                            break;
                                        }
                                    }
                                    if (!columnNameExists)
                                    {
                                        //fill comparison result object that the columnn nam is not exists
                                        comparisonResult = new ComparisonResult();
                                        comparisonResult.CustomError = "This column Name :" + columnName + " is not exist on DB ";

                                        comparisonResults.Add(comparisonResult);

                                    }

                                }
                                #endregion
                            }

                            //compare rows count
                            if (sourceDataTable.Rows.Count != destinationDataTable.Rows.Count)
                            {
                                comparisonResult = new ComparisonResult();

                                comparisonResult.CustomError = "There is a difference in count between source and destination as the source count " + sourceDataTable.Rows.Count + " and destination count is " + destinationDataTable.Rows.Count;
                                comparisonResults.Add(comparisonResult);
                            }

                            //Compare data 
                            foreach (DataRow sourceRow in sourceDataTable.Rows)
                            {
                                //get filter column name
                                //validate that row contains id column
                                StringBuilder filterValueExpression = new StringBuilder();
                                string separetor = "";

                                foreach (string key in comparerSettingDTO.UniqueKeysToCompareCollectionsWith.Keys)
                                {
                                    foreach (string value in comparerSettingDTO.UniqueKeysToCompareCollectionsWith[key])
                                    {
                                        if (sourceRow.Table.Columns.Contains(value))
                                        {
                                            string filterValue = sourceRow[value].ToString();
                                            filterValueExpression.Append(separetor);
                                            filterValueExpression.Append(value + "=");
                                            filterValueExpression.Append("'" + filterValue + "'");
                                            separetor = "  AND  ";
                                        }
                                    }
                                }

                                DataRow[] destinationRows = destinationDataTable.Select(filterValueExpression.ToString());

                                if (destinationRows.Length > 0)
                                { //will send those two object to another function to compare values  
                                    ComparerSettingDTO comparerSettingDTORow = new ComparerSettingDTO();
                                    comparerSettingDTORow.DestinationObject = destinationRows[0];
                                    comparerSettingDTORow.SourceObject = sourceRow;
                                    comparerSettingDTORow.PropsToExcludeFromComparison = comparerSettingDTO.PropsToExcludeFromComparison;
                                    comparerSettingDTORow.UniqueKeysToCompareCollectionsWith = comparerSettingDTO.UniqueKeysToCompareCollectionsWith;
                                    comparisonResults.AddRange(CompareDataRows(comparerSettingDTORow));
                                }
                                else
                                {
                                    //fill comparison result object the id not exists in the other sheet
                                    comparisonResult = new ComparisonResult();
                                    comparisonResult.CustomError = "The record with " + filterValueExpression + "not exists ";
                                    comparisonResults.Add(comparisonResult);
                                }

                            }
                        }
                    }
                    #endregion
                }
                else //normal property(not DS or List) either class or primitive prop 
                {
                    if ((sourceProperty.GetType().IsClass && !typeof(string).IsAssignableFrom(sourceProperty.PropertyType) && sourceProperty.PropertyType.GetTypeInfo().BaseType.Name != "ValueType"))
                    {
                        ComparerSettingDTO classComparerSettingDTO = new ComparerSettingDTO();
                        classComparerSettingDTO.SourceObject = sourceProperty.GetValue(comparerSettingDTO.SourceObject);
                        classComparerSettingDTO.DestinationObject = destinationProperty.GetValue(comparerSettingDTO.DestinationObject);
                        classComparerSettingDTO.PropsToExcludeFromComparison = comparerSettingDTO.PropsToExcludeFromComparison;
                        classComparerSettingDTO.MainObjectName = comparerSettingDTO.MainObjectName;
                        classComparerSettingDTO.CompareCollectionsInMainObjectOnly = comparerSettingDTO.CompareCollectionsInMainObjectOnly;
                        classComparerSettingDTO.CompareCollectionUsingTheirIndexes = comparerSettingDTO.CompareCollectionUsingTheirIndexes;
                        classComparerSettingDTO.UniqueKeysToCompareCollectionsWith = comparerSettingDTO.UniqueKeysToCompareCollectionsWith;
                        comparisonResults.AddRange(Compare(classComparerSettingDTO));
                    }
                    else
                    {
                        ComparisonResult comparisonResult = null;
                        object sourceValue = sourceProperty.GetValue(comparerSettingDTO.SourceObject);
                        object destinationValue = destinationProperty.GetValue(comparerSettingDTO.DestinationObject);

                        if ((sourceValue == null && destinationValue == null) || (sourceValue == destinationValue))
                        {
                            return comparisonResults;
                        }
                        else if ((sourceValue == null && destinationValue != null) || (sourceValue != null & destinationValue == null))
                        {
                            comparisonResult = FillComparisonResult(destinationProperty, sourceProperty, comparerSettingDTO.DestinationObject, comparerSettingDTO.SourceObject);
                        }
                        else
                        {
                            if (!sourceValue.Equals(destinationValue))
                            {
                                comparisonResult = FillComparisonResult(destinationProperty, sourceProperty, comparerSettingDTO.DestinationObject, comparerSettingDTO.SourceObject);
                            }
                        }
                        if (comparisonResult != null)
                            comparisonResults.Add(comparisonResult);
                    }

                }
                return comparisonResults;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        /// <summary>
        /// collectoin compare, compares two collections
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="sourceCollection"></param>
        /// <param name="destinationCollection"></param>
        /// <param name="comparerSettingDTO"></param>
        /// <returns></returns>
        public List<ComparisonResult> CollectionCompare(string tableName, ICollection sourceCollection, dynamic destinationCollection, ComparerSettingDTO comparerSettingDTO)
        {
            List<ComparisonResult> comparisonResults = new List<ComparisonResult>();
            ComparisonResult comparisonResult = null;

            if (sourceCollection != null || destinationCollection != null)
            {
                if ((sourceCollection == null && destinationCollection != null && destinationCollection.Count > 0) || (sourceCollection != null & destinationCollection == null && sourceCollection.Count > 0) || (((ICollection)sourceCollection).Count != destinationCollection.Count))
                {
                    comparisonResult = new ComparisonResult();
                    comparisonResult.TableName = tableName;
                    comparisonResult.HasCountDifference = true;
                    comparisonResult.SourceCount = sourceCollection != null ? sourceCollection.Count : 0;
                    comparisonResult.DestinationCount = destinationCollection != null ? destinationCollection.Count : 0;
                    comparisonResults.Add(comparisonResult);
                }
                else
                {

                    IList sourceList = null;
                    try
                    {
                        sourceList = (IList)sourceCollection;//cast ICollection to IList to be able o access index
                    }
                    catch (Exception ex2)
                    {

                    }
                    //if (sourceList.Count != destinationCollection.Count)
                    //{
                    //    comparisonResult = new ComparisonResult();

                    //    comparisonResult.CustomError = "There is a difference in count between source and destination as the source count " + sourceDataTable.Rows.Count + " and destination count is " + destinationDataTable.Rows.Count;
                    //    comparisonResults.Add(comparisonResult);
                    //}

                    for (int index = 0; index < sourceList.Count; index++)
                    {
                        object sourceRow = sourceList[index];
                        object destinationObject = null;
                        string sourceUniquePropertyValue = string.Empty;
                        if (comparerSettingDTO.CompareCollectionUsingTheirIndexes)
                        {
                            int destinationCollectionIndex = 0;
                            foreach (var destinationRow in destinationCollection)
                            {
                                if (index == destinationCollectionIndex)
                                {
                                    destinationObject = destinationRow;
                                    break;
                                }
                                destinationCollectionIndex++;
                            }
                        }
                        else
                        {
                            if (comparerSettingDTO.UniqueKeysToCompareCollectionsWith.Count > 0)
                            {
                                List<string> propsToCompareWith = new List<string>();
                                if (comparerSettingDTO.UniqueKeysToCompareCollectionsWith.ContainsKey(tableName))
                                {
                                    propsToCompareWith = comparerSettingDTO.UniqueKeysToCompareCollectionsWith[tableName];
                                }
                                else if (comparerSettingDTO.UniqueKeysToCompareCollectionsWith.ContainsKey("All"))
                                {
                                    propsToCompareWith = comparerSettingDTO.UniqueKeysToCompareCollectionsWith["All"];
                                }
                                else
                                {
                                    //compare using index
                                    int destinationCollectionIndex = 0;
                                    foreach (var destinationRow in destinationCollection)
                                    {
                                        if (index == destinationCollectionIndex)
                                        {
                                            destinationObject = destinationRow;
                                            break;
                                        }
                                        destinationCollectionIndex++;
                                    }
                                }
                                if (propsToCompareWith.Count > 0)
                                {
                                    StringBuilder filterValueExpression = new StringBuilder();
                                    string separetor = string.Empty;

                                    foreach (string item in propsToCompareWith)
                                    {
                                        sourceUniquePropertyValue = sourceRow.GetType().GetProperty(item).GetValue(sourceRow, null) != null ? sourceRow.GetType().GetProperty(item).GetValue(sourceRow, null).ToString() : string.Empty;

                                        filterValueExpression.Append(separetor);
                                        filterValueExpression.Append(item + "=");
                                        filterValueExpression.Append("'" + sourceUniquePropertyValue + "'");
                                        separetor = "  AND  ";
                                    }

                                    foreach (var destinationRow in destinationCollection)
                                    {
                                        StringBuilder destinationFilterValueExpression = new StringBuilder();
                                        separetor = string.Empty;
                                        foreach (string item in propsToCompareWith)
                                        {
                                            var destinationUniquePropertyValue = destinationRow.GetType().GetProperty(item).GetValue(destinationRow, null) != null ? destinationRow.GetType().GetProperty(item).GetValue(destinationRow, null).ToString() : string.Empty;

                                            destinationFilterValueExpression.Append(separetor);
                                            destinationFilterValueExpression.Append(item + "=");
                                            destinationFilterValueExpression.Append("'" + destinationUniquePropertyValue + "'");
                                            separetor = "  AND  ";
                                        }
                                        if (destinationFilterValueExpression.ToString() == filterValueExpression.ToString())
                                        {
                                            destinationObject = destinationRow;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (destinationObject == null) // row with this IdSeq not exists on DB
                        {
                            comparisonResult = new ComparisonResult();
                            comparisonResult.TableName = tableName;
                            comparisonResult.SourceValue = sourceUniquePropertyValue.ToString();//it shouldn't be null
                            comparisonResult.CustomError = "This id is not exist on Destination " + sourceUniquePropertyValue.ToString();
                            comparisonResults.Add(comparisonResult);
                        }
                        else // compare other props
                        {
                            ComparerSettingDTO comparerSettingDTO1 = new ComparerSettingDTO();
                            comparerSettingDTO1.SourceObject = sourceRow;
                            comparerSettingDTO1.DestinationObject = destinationObject;
                            comparerSettingDTO1.PropsToExcludeFromComparison = comparerSettingDTO.PropsToExcludeFromComparison;
                            comparerSettingDTO1.MainObjectName = comparerSettingDTO.MainObjectName;
                            comparerSettingDTO1.CompareCollectionsInMainObjectOnly = comparerSettingDTO.CompareCollectionsInMainObjectOnly;
                            comparerSettingDTO1.CompareCollectionUsingTheirIndexes = comparerSettingDTO.CompareCollectionUsingTheirIndexes;
                            comparerSettingDTO1.UniqueKeysToCompareCollectionsWith = comparerSettingDTO.UniqueKeysToCompareCollectionsWith;
                            comparisonResults.AddRange(Compare(comparerSettingDTO1)); // if equals 
                        }
                    }
                }
            }
            return comparisonResults;
        }

        public static List<ComparisonResult> CompareDataRows(ComparerSettingDTO comparerSettingDTO)
        {
            List<ComparisonResult> comparisonResults = new List<ComparisonResult>();
            DataRow sourceRow = (DataRow)comparerSettingDTO.SourceObject;
            DataRow destinationRow = (DataRow)comparerSettingDTO.DestinationObject;
            foreach (DataColumn destinationColumn in destinationRow.Table.Columns)
            {
                string destinationColumnName = destinationColumn.ColumnName;
                if (sourceRow.Table.Columns.Contains(destinationColumnName) && !comparerSettingDTO.PropsToExcludeFromComparison.Contains(destinationColumnName))
                {
                    var sourceValue = (sourceRow[destinationColumnName] != null) ? sourceRow[destinationColumnName].ToString() : String.Empty;
                    var destinationvalue = (destinationRow[destinationColumnName] != null) ? destinationRow[destinationColumnName].ToString() : String.Empty;
                    if (sourceValue != destinationvalue)
                    {
                        comparisonResults.Add(FillComparisonResult(sourceValue, destinationvalue, destinationColumnName, destinationRow.Table.TableName, destinationRow, sourceRow));
                    }
                }

            }

            return comparisonResults;
        }

        private ComparisonResult FillComparisonResult(PropertyInfo destinationProperty, PropertyInfo sourceProperty, object destinationObject, object sourceObject)
        {
            object sourceValue = sourceProperty.GetValue(sourceObject);
            object destinationValue = destinationProperty.GetValue(destinationObject);
            string propertyName = sourceProperty.Name;
            string tableName = destinationObject.GetType().Name;
            ComparisonResult comparisonResult = new ComparisonResult();
            comparisonResult.DestinationProperty = destinationProperty;
            comparisonResult.SourceProperty = sourceProperty;
            comparisonResult.DestinationObject = destinationObject;
            comparisonResult.SourceObject = sourceObject;
            comparisonResult.TableName = tableName;
            comparisonResult.PropertyName = propertyName;
            comparisonResult.SourceValue = sourceValue != null ? sourceValue.ToString() : null;
            comparisonResult.DestinationValue = destinationValue != null ? destinationValue.ToString() : null;
            return comparisonResult;
        }
        private static ComparisonResult FillComparisonResult(string sourceValue, string destinationValue, string columnName, string dataTableName, object destinationObject, object sourceObject)
        {
            ComparisonResult comparisonResult = new ComparisonResult();
            comparisonResult.DestinationObject = destinationObject;
            comparisonResult.SourceObject = sourceObject;
            comparisonResult.TableName = dataTableName;
            comparisonResult.PropertyName = columnName;
            comparisonResult.SourceValue = sourceValue != null ? sourceValue.ToString() : null;
            comparisonResult.DestinationValue = destinationValue != null ? destinationValue.ToString() : null;
            return comparisonResult;
        }

    }
}
