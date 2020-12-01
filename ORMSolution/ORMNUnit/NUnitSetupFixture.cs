﻿using NUnit.Framework;
using ORM;
using ORM.Attributes;
using System.Collections.Generic;

namespace ORMNUnit
{
    [SetUpFixture, ORMUnitTest]
    internal class NUnitSetupFixture
    {
        [OneTimeSetUp]
        public void Initialize()
        {
            var memoryEntityTables = new List<string>()
            {
                "MemoryEntityTables/USERS.xml",
                "MemoryEntityTables/ORGANISATIONS.xml"
            };

            var memoryCollectionTables = new List<string>()
            {
                "MemoryCollectionTables/BasicFetchUsers.xml",
                "MemoryCollectionTables/BasicFetchTopUsers.xml",
                "MemoryCollectionTables/BasicJoinInner.xml",
                "MemoryCollectionTables/BasicSelectUsers.xml",
                "MemoryCollectionTables/BasicJoinLeft.xml",
                "MemoryCollectionTables/BasicOrderBy.xml",
                "MemoryCollectionTables/BasicWhereAnd.xml",
                "MemoryCollectionTables/BasicWhereEqualTo.xml",
                "MemoryCollectionTables/BasicWhereNotEqualTo.xml",
                "MemoryCollectionTables/BasicWhereLessThanOrEqual.xml",
                "MemoryCollectionTables/BasicWhereGreaterThanOrEqual.xml",
                "MemoryCollectionTables/ComplexJoin.xml",
                "MemoryCollectionTables/ComplexWhereLike.xml"
            };

            _ = new ORMInitialize(memoryEntityTables, memoryCollectionTables);
        }
    }
}
