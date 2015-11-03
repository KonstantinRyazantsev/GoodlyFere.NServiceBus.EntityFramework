#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SagaPersister.cs">
//  Copyright 2015 Benjamin S. Ramey
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// </copyright>
// <created>03/25/2015 9:51 AM</created>
// <updated>03/31/2015 12:55 PM by Ben Ramey</updated>
// --------------------------------------------------------------------------------------------------------------------

#endregion

#region Usings

using System;
using System.Collections;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using GoodlyFere.NServiceBus.EntityFramework.Exceptions;
using GoodlyFere.NServiceBus.EntityFramework.Interfaces;
using GoodlyFere.NServiceBus.EntityFramework.SharedDbContext;
using NServiceBus.Saga;

#endregion

namespace GoodlyFere.NServiceBus.EntityFramework.SagaStorage
{
    public class SagaPersister : ISagaPersister
    {
        private readonly IDbContextProvider _dbContextProvider;
        private ISagaDbContext _dbContext;

        public SagaPersister(IDbContextProvider dbContextProvider)
        {
            if (dbContextProvider == null)
            {
                throw new ArgumentNullException("dbContextProvider");
            }

            _dbContextProvider = dbContextProvider;
        }

        private ISagaDbContext DbContext
        {
            get
            {
                return _dbContext ?? (_dbContext = _dbContextProvider.GetSagaDbContext());
            }
        }

        public void Complete(IContainSagaData saga)
        {
            if (saga == null)
            {
                throw new ArgumentNullException("saga");
            }

            Type sagaType = saga.GetType();
            if (!DbContext.HasSet(sagaType))
            {
                throw new SagaDbSetMissingException(DbContext.GetType(), sagaType);
            }

            try
            {
                DbEntityEntry entry = DbContext.Entry(saga);
                DbSet set = DbContext.Set(sagaType);

                if (entry.State == EntityState.Detached)
                {
                    set.Attach(saga);
                }

                set.Remove(saga);

                DbContext.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                // don't do anything, if we couldn't delete because it doesn't exist, that's OK
            }
        }

        public TSagaData Get<TSagaData>(Guid sagaId) where TSagaData : IContainSagaData
        {
            Type sagaType = typeof(TSagaData);
            if (!DbContext.HasSet(sagaType))
            {
                throw new SagaDbSetMissingException(DbContext.GetType(), sagaType);
            }

            if (sagaId == Guid.Empty)
            {
                throw new ArgumentException("sagaId cannot be empty.", "sagaId");
            }

            object result = DbContext.Set(sagaType).Find(sagaId);
            return (TSagaData)(result ?? default(TSagaData));
        }

        public TSagaData Get<TSagaData>(string propertyName, object propertyValue) where TSagaData : IContainSagaData
        {
            Type sagaType = typeof(TSagaData);
            if (!DbContext.HasSet(sagaType))
            {
                throw new SagaDbSetMissingException(DbContext.GetType(), sagaType);
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentNullException("propertyName");
            }

            ParameterExpression param = Expression.Parameter(sagaType, "sagaData");
            Expression<Func<TSagaData, bool>> filter = Expression.Lambda<Func<TSagaData, bool>>(
                Expression.MakeBinary(
                    ExpressionType.Equal,
                    Expression.Property(param, propertyName),
                    Expression.Constant(propertyValue)),
                param);

            IQueryable setQueryable = DbContext.Set(sagaType).AsQueryable();
            IQueryable result = setQueryable
                .Provider
                .CreateQuery(
                    Expression.Call(
                        typeof(Queryable),
                        "Where",
                        new[] { sagaType },
                        setQueryable.Expression,
                        Expression.Quote(filter)));

            IEnumerator enumerator = result.GetEnumerator();
            if (enumerator.MoveNext())
            {
                return (TSagaData)enumerator.Current;
            }

            return default(TSagaData);
        }

        public void Save(IContainSagaData saga)
        {
            if (saga == null)
            {
                throw new ArgumentNullException("saga");
            }

            Type sagaType = saga.GetType();
            if (!DbContext.HasSet(sagaType))
            {
                throw new SagaDbSetMissingException(DbContext.GetType(), sagaType);
            }

            DbContext.Set(sagaType).Add(saga);
            DbContext.SaveChanges();
        }

        public void Update(IContainSagaData saga)
        {
            if (saga == null)
            {
                throw new ArgumentNullException("saga");
            }

            Type sagaType = saga.GetType();
            if (!DbContext.HasSet(sagaType))
            {
                throw new SagaDbSetMissingException(DbContext.GetType(), sagaType);
            }

            object existingEnt = DbContext.Set(sagaType).Find(saga.Id);
            if (existingEnt == null)
            {
                throw new Exception(string.Format("Could not find saga with ID {0}", saga.Id));
            }

            DbEntityEntry entry = DbContext.Entry(existingEnt);
            entry.CurrentValues.SetValues(saga);
            entry.State = EntityState.Modified;

            DbContext.SaveChanges();
        }
    }
}