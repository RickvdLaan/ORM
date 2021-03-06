﻿using ORM.Attributes;
using System.Reflection;

namespace ORM
{
    internal class SQLJoin
    {
        internal bool IsManyToMany { get; set; }

        internal ORMTableAttribute LeftTableAttribute { get; set; }
        internal PropertyInfo LeftPropertyInfo { get; set; }

        internal ORMTableAttribute RightTableAttribute { get; set; }
        internal PropertyInfo[] RightPropertyInfo { get; set; }
    }
}