﻿using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using ORM.Attributes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Reflection;

[assembly: InternalsVisibleTo("ORMNUnit")]

namespace ORM
{
    public sealed class ORMInitialize
    {
        internal ORMInitialize(Assembly callingAssembly, string xmlEntityFilePath, string xmlCollectionFilePath)
        {
            ORMUtilities.MemoryEntityDatabase = new MemoryEntityDatabase(Assembly.GetCallingAssembly());
            ORMUtilities.MemoryEntityDatabase.LoadMemoryTables(LoadMemoryDatabase(callingAssembly, xmlEntityFilePath));

            ORMUtilities.MemoryCollectionDatabase = new MemoryCollectionDatabase(Assembly.GetCallingAssembly());
            ORMUtilities.MemoryCollectionDatabase.LoadMemoryTables(LoadMemoryDatabase(callingAssembly, xmlCollectionFilePath));

            new ORMInitialize(configuration: null, loadAllReferencedAssemblies: true);
        }

        private List<string> LoadMemoryDatabase(Assembly callingAssembly, string folder)
        {
            var files = new List<string>();

            foreach (string resource in callingAssembly.GetManifestResourceNames().Where(resource => resource.Contains(folder)))
            {
                files.Add(resource);
            }

            return files;
        }

        private void LoadAllReferencedAssemblies()
        {
            var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");

            foreach (var referencedPath in referencedPaths)
            {
                var DotNetCoreBugFixed = false;
                if (DotNetCoreBugFixed)
                {
                    // .Net Core does not support the ReflectionOnlyLoad feature of NETFX. ReflectionOnlyLoad was a
                    // feature for inspecting managed assemblies using the familiar Reflection api (Type, MethodInfo, etc.)

                    // The TypeLoader class is the .NET Core replacement for this feature.

                    // MetadataLoadContext doesn't work because this isn't a NuGet package.

                    // Links:
                    // https://github.com/dotnet/corefxlab/blob/master/docs/specs/typeloader.md
                    // https://github.com/dotnet/runtime/issues/15033
                    // https://github.com/dotnet/runtime/issues/31200
                    // Because of this bug we can't only load what we know we actually need.
                    // -Rick, 25 September 2020
                    var assemblyBytes = File.ReadAllBytes(referencedPath);

                    // .NET Core only: This member is not supported.
                    var assembly = Assembly.ReflectionOnlyLoad(assemblyBytes);

                    if (assembly.GetReferencedAssemblies().Contains(Assembly.GetAssembly(typeof(ORMEntity)).GetName()))
                    {
                        AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(referencedPath));
                    }
                }
                else
                {
                    // Currently the only way, untill we find another way to do this through meta-data.
                    // -Rick, 25 September 2020
                    AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(referencedPath));
                }
            }
        }

        public ORMInitialize(IConfiguration configuration = null, bool loadAllReferencedAssemblies = false)
        {
            new DatabaseUtilities(configuration);
            new ORMUtilities();

            if (loadAllReferencedAssemblies)
            {
                LoadAllReferencedAssemblies();
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes().Where(type => type.GetCustomAttributes(typeof(ORMTableAttribute), true).Length > 0))
                {
                    var tableAttribute = type.GetCustomAttribute(typeof(ORMTableAttribute), true) as ORMTableAttribute;

                    var constructor = tableAttribute.EntityType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (constructor == null)
                    {
                        throw new Exception($"Entity { tableAttribute.EntityType.Name } requires a (private) parameterless constructor.");
                    }

                    if (tableAttribute.CollectionTypeLeft == null
                     && tableAttribute.CollectionTypeRight == null)
                    {
                        ORMUtilities.CollectionEntityRelations.Add(tableAttribute.CollectionType, tableAttribute.EntityType);
                        ORMUtilities.CollectionEntityRelations.Add(tableAttribute.EntityType, tableAttribute.CollectionType);
                    }
                    else
                    {
                        ORMUtilities.CollectionEntityRelations.Add(tableAttribute.CollectionType, tableAttribute.EntityType);
                        ORMUtilities.CollectionEntityRelations.Add(tableAttribute.EntityType, tableAttribute.CollectionType);
                        ORMUtilities.ManyToManyRelations.Add((tableAttribute.CollectionTypeLeft, tableAttribute.CollectionTypeRight), tableAttribute);
                        ORMUtilities.ManyToManyRelations.Add((tableAttribute.CollectionTypeRight, tableAttribute.CollectionTypeLeft), tableAttribute);
                    }
                    if (!ORMUtilities.CachedColumns.ContainsKey(tableAttribute.CollectionType)
                     && !ORMUtilities.CachedColumns.ContainsKey(tableAttribute.EntityType))
                    {
                        if (!UnitTestUtilities.IsUnitTesting)
                        {
                            var sqlBuilder = new SQLBuilder();
                            sqlBuilder.BuildQuery(tableAttribute, null, null, null, null, 0);
                            var rows = DatabaseUtilities.ExecuteDirectQuery(sqlBuilder.GeneratedQuery)
                                  .CreateDataReader()
                                  .GetSchemaTable()
                                  .Rows;

                            var uniqueConstraints = DatabaseUtilities.ExecuteDirectQuery(sqlBuilder.ColumnConstraintInformation(tableAttribute.TableName));

                            var columns = new List<string>(rows.Count);

                            for (int i = 0; i < rows.Count; i++)
                            {
                                for (int j = 0; j < uniqueConstraints.Rows.Count; j++)
                                {
                                    if (uniqueConstraints.Rows[j][3].Equals(rows[i][0]))
                                    {
                                        ORMUtilities.UniqueConstraints.Add((tableAttribute.EntityType, (string)rows[i][0]));
                                        break;
                                    }
                                }

                                columns.Add((string)rows[i][0]);
                            }

                            ORMUtilities.CachedColumns.Add(tableAttribute.CollectionType, columns);
                            ORMUtilities.CachedColumns.Add(tableAttribute.EntityType, columns);
                        }
                        else
                        {
                            var columns = ORMUtilities.MemoryEntityDatabase.FetchTableColumns(tableAttribute.TableName);

                            if (columns != null)
                            {
                                ORMUtilities.CachedColumns.Add(tableAttribute.CollectionType, columns);
                                ORMUtilities.CachedColumns.Add(tableAttribute.EntityType, columns);
                            }
                        }
                    }
                }
            }
        }
    }
}
