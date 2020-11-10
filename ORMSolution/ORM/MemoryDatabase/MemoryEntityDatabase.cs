﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;

namespace ORM
{
    /// <summary>
    /// <para>
    /// An internal memory database which represents a set of <see cref="ORMEntity"/> objects for the unit tests.
    /// </para>
    /// <para>
    /// The <see cref="MemoryEntityDatabase"/> simulates the database through pre-defined xml table records.
    /// The <see cref="SQLBuilder"/> generates the query which can simply be tested via a simple assert:
    /// "ExpectedQuery equals SqlBuilder.GeneratedQuery.".
    /// </para>
    /// </summary>
    internal class MemoryEntityDatabase : MemoryDatabase
    {
        public IDataReader FetchEntityById(string tableName, ORMPrimaryKey primaryKey, object id)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException();
            if (primaryKey == null)
                throw new ArgumentNullException();
            if (id == null)
                throw new ArgumentNullException();

            var path = BasePath + tableName.ToUpperInvariant();
            var tableRecords = ORMUtilities.MemoryEntityDatabase.MemoryTables.DocumentElement.SelectNodes(path);

            foreach (XmlElement record in tableRecords)
            {
                if (primaryKey.Keys.Count == 1)
                {
                    var xmlAttribute = record.GetAttributeNode(primaryKey.Keys[0].ColumnName);
                    if (xmlAttribute != null && xmlAttribute.Value.Equals(id.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        var reader = new StringReader(record.OuterXml);
                        var dataSet = new DataSet();
                        dataSet.ReadXml(reader);

                        var columns = FetchTableColumns(record.Name);

                        if (dataSet.Tables[0].Columns.Count < columns.Count)
                        {
                            for (int i = 0; i < columns.Count; i++)
                            {
                                if (string.Equals(dataSet.Tables[0].Columns[i].ColumnName, columns[i], StringComparison.InvariantCultureIgnoreCase))
                                {
                                    continue;
                                }

                                // When nullable field is null in the xml we need to insert at i.
                                DataColumn missingColumn = new DataColumn(columns[i]);
                                dataSet.Tables[0].Columns.Add(missingColumn);
                                missingColumn.SetOrdinal(i);
                            }
                        }

                        return dataSet.Tables[0].CreateDataReader();
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return null;
        }

        public List<string> FetchTableColumns(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException();

            var path = BasePath + tableName.ToUpperInvariant();
            var tableRecords = ORMUtilities.MemoryEntityDatabase.MemoryTables.DocumentElement.SelectNodes(path);

            if (tableRecords.Count == 0)
            {
                // Return null when no xml records were found for the current table.
                return null;
            }

            var maxColumns = tableRecords.Cast<XmlNode>().Max(x => x.Attributes.Count);
            var columns = new List<string>(maxColumns);

            for (int i = 0; i < tableRecords.Count; i++)
            {
                if (tableRecords[i].Attributes.Count == maxColumns)
                {
                    if (tableRecords[0].Name.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (XmlAttribute column in tableRecords[0].Attributes)
                        {
                            columns.Add(column.Name);
                        }
                    }

                    break;
                }
            }

            return columns;
        }
    }
}