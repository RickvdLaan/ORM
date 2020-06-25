﻿using ORM.Attributes;
using ORM.Interfaces;
using ORM.SQL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ORM
{
    [Serializable]
    public class ORMCollection<T> : IEnumerable<ORMEntity> where T : ORMEntity
    {
        public string GetQuery { get; internal set; }

        public ORMSortExpression SortExpression { get; set; }

        internal ORMEntityField[] SelectExpression { get; set; }

        internal Expression<Func<T, bool>> WhereExpression { get; set; }

        internal ORMTableAttribute TableAttribute
        { 
            get { return (ORMTableAttribute)Attribute.GetCustomAttribute(GetType(), typeof(ORMTableAttribute)); }
        }

        internal List<ORMEntity> _collection;
        internal List<ORMEntity> Collection
        {
            get { return _collection; }
            set { _collection = value; }
        }

        public ORMCollection()
        {
            Collection = new List<ORMEntity>();
            SortExpression = new ORMSortExpression();
        }

        public ORMEntity this[int index]
        {
            get { return Collection[index]; }
            set { Collection.Insert(index, value); }
        }

        public IEnumerator<ORMEntity> GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Fetch()
        {
            Fetch(-1);
        }

        public void Fetch(long maxNumberOfItemsToReturn)
        {
            using (SQLConnection connection = new SQLConnection())
            {
                var sqlBuilder = new SQLBuilder();

                if (WhereExpression == null)
                {
                    sqlBuilder.BuildQuery(TableAttribute, SelectExpression, SortExpression, maxNumberOfItemsToReturn);
                }
                else
                {
                    sqlBuilder.BuildQuery(TableAttribute, SelectExpression, WhereExpression.Body, SortExpression, maxNumberOfItemsToReturn);
                }

                GetQuery = sqlBuilder.ToString();

                connection.ExecuteCollectionQuery(ref _collection, sqlBuilder, TableAttribute);
            }
        }

        public void Select(params ORMEntityField[] fields)
        {
            SelectExpression = fields;
        }

        public void Where(Expression<Func<T, bool>> expression)
        {
            WhereExpression = expression;
        }

        public void OrderBy(params IORMSortClause[] sortClauses)
        {
            SortExpression.AddRange(sortClauses);
        }
    }
}