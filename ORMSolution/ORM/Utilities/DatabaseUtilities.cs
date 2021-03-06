﻿using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ORM.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ORM
{
    public sealed class DatabaseUtilities
    {
        internal static string ConnectionString { get; private set; }

        public DatabaseUtilities(IConfiguration configuration)
        {
            if (configuration != null)
            {
                ConnectionString = configuration.GetConnectionString("DefaultConnection");
            }
        }
        public static void OverrideConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }

        #region Transactions

        internal static AsyncLocal<SqlTransaction> Transaction { get; private set; } = new AsyncLocal<SqlTransaction>();

        public static bool IsInTransaction()
        {
            return Transaction.Value != null;
        }

        public static void TransactionBegin()
        {
            Transaction.Value = SQLExecuter.CurrentConnection.Value.BeginTransaction();
        }

        public static void TransactionCommit(bool rollbackTransactionOnFailure = false)
        {
            if (IsInTransaction())
            {
                try
                {
                    Transaction.Value.Commit();
                }
                catch
                {
                    if (rollbackTransactionOnFailure)
                    {
                        Transaction.Value.Rollback();
                    }

                    throw;
                }
                finally
                {
                    DisposeTransaction();
                }
            }
        }

        public static void TransactionRollback()
        {
            if (IsInTransaction())
            {
                Transaction.Value.Rollback();

                DisposeTransaction();
            }
        }

        private static void DisposeTransaction()
        {
            if (IsInTransaction())
            {
                Transaction.Value.Dispose();
                Transaction.Value = null;
            }
        }

        #endregion

        #region Direct Queries

        public static CollectionType ExecuteDirectQuery<CollectionType, EntityType>(string query, bool disableChangeTracking = false, params object[] parameters)
            where CollectionType : ORMCollection<EntityType>, new()
            where EntityType : ORMEntity
        {
            if (query.Contains("INNER JOIN")
             || query.Contains("LEFT JOIN")
             || query.Contains("RIGHT JOIN")
             || query.Contains("FULL OUTER JOIN"))
            {
                throw new ORMInvalidJoinException("Joins are not supported on POCO's when using direct queries.");
            }

            var collection = ORMUtilities.ConvertTo<CollectionType, EntityType>(ExecuteDirectQuery(query, parameters), disableChangeTracking);

            collection.ExecutedQuery = query;

            return collection;
        }

        public static int ExecuteDirectNonQuery(string query, params object[] parameters)
        {
            return ExecuteQuery(ExecuteWriter, query, parameters);
        }

        public static DataTable ExecuteDirectQuery(string query, params object[] parameters)
        {
            return ExecuteQuery(ExecuteReader, query, parameters);
        }

        private static DataTable ExecuteReader(SqlCommand command)
        {
            if (UnitTestUtilities.IsUnitTesting)
            {
                throw new NotImplementedException();
            }

            using var reader = command.ExecuteReader();
            var dataTable = new DataTable();
            dataTable.Load(reader);

            return dataTable;
        }

        private static int ExecuteWriter(SqlCommand command)
        {
            if (Transaction.Value != null)
            {
                command.Transaction = Transaction.Value;
            }

            return command.ExecuteNonQuery();
        }

        private static T ExecuteQuery<T>(Func<SqlCommand, T> method, string query, params object[] parameters)
        {
            using SqlConnection connection = new SqlConnection(ConnectionString);
            SQLExecuter.CurrentConnection.Value = connection;

            using var command = new SqlCommand(query, connection);
            if (!UnitTestUtilities.IsUnitTesting)
            {
                command.Connection.Open();
            }

            var regexMatches = Regex.Matches(query, @"\@[^ |\))]\w+")
                .OfType<Match>()
                .Select(m => m.Groups[0].Value)
                .Distinct()
                .ToList();

            if (parameters.FirstOrDefault() == null && regexMatches.Count > 0
             || parameters.Length != regexMatches.Count)
            {
                throw new ArgumentException(string.Format("{0} unique parameter{1} found, but {2} parameter{3} provided.",
                    regexMatches.Count,
                    regexMatches.Count > 1 || regexMatches.Count == 0 ? "s were" : " was",
                    parameters.Length,
                    parameters.Length > 1 || parameters.Length == 0 ? "s were" : " was"));
            }

            if (Transaction.Value != null)
            {
                command.Transaction = Transaction.Value;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                command.Parameters.Add(new SqlParameter(regexMatches[i], parameters[i]));
            }

            return method.Invoke(command);
        }

        #endregion

        public static bool DoesTableHaveUC(string tableName)
        {
            return IfExists(new SQLBuilder().ColumnConstraintInformation(tableName));
        }

        public static bool IfExists(string query)
        {
            using SqlConnection connection = new SqlConnection(ConnectionString);
            using var command = new SqlCommand(new SQLBuilder().IfExists(query), connection);

            if (!UnitTestUtilities.IsUnitTesting)
            {
                command.Connection.Open();
            }

            return Convert.ToBoolean(ExecuteReader(command).Rows[0].ItemArray[0]);
        }

        public static void CreateUniqueConstraint(Type collectionType)
        {
            if (!DoesTableHaveUC(collectionType.Name))
            {
                var constraints = ORMUtilities.CollectionEntityRelations[collectionType].GetCustomAttributes(typeof(UniqueConstraint), true);

                foreach (var constraint in constraints)
                {
                    using SqlConnection connection = new SqlConnection(ConnectionString);
                    using var command = new SqlCommand(new SQLBuilder().CreateUniqueConstraint(collectionType.Name, "columnNames"), connection);

                    if (!UnitTestUtilities.IsUnitTesting)
                    {
                        command.Connection.Open();
                    }

                    ExecuteReader(command);
                }
            
            }
        }

        public static List<string> GetDatabaseList()
        {
            var dataTable = ExecuteDirectQuery(new SQLBuilder().ServerDatabaseList());
            var databases = new List<string>(dataTable.Rows.Count);
            var excludedDatabases = new List<string>(4)
            {
                "master",
                "tempdb",
                "model",
                "msdb"
            };

            foreach (DataRow row in dataTable.Rows)
            {
                var databaseName = (string)row[0];

                if (excludedDatabases.Contains(databaseName))
                    continue;

                databases.Add(databaseName);
            }

            return databases;
        }
    }
}
