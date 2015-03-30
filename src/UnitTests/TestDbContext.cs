﻿#region Usings

using System;
using System.Data.Entity;
using System.Linq;
using GoodlyFere.NServiceBus.EntityFramework.Interfaces;
using GoodlyFere.NServiceBus.EntityFramework.SubscriptionStorage;
using NServiceBus;
using NServiceBus.Saga;

#endregion

namespace UnitTests
{
    internal class TestDbContext : DbContext, ISagaDbContext, ISubscriptionDbContext
    {
        public TestDbContext()
            : base("testdb")
        {
        }

        public DbSet<TestSagaData> TestSagas { get; set; }

        public DbSet SagaSet(Type sagaDataType)
        {
            if (sagaDataType == typeof(TestSagaData))
            {
                return TestSagas;
            }

            throw new ArgumentOutOfRangeException("No DbSets of type " + sagaDataType + " found.");
        }

        public DbSet<SubscriptionEntity> Subscriptions { get; set; }
    }

    public class TestSagaData : IContainSagaData
    {
        public string SomeProp1 { get; set; }
        public string SomeProp2 { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        public Guid Id { get; set; }
    }

    internal class TestMessage : IMessage
    {
    }

    internal class TestMessage2 : IMessage
    {
    }
}