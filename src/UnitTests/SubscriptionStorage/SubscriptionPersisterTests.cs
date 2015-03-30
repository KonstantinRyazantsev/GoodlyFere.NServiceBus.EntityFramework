﻿#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GoodlyFere.NServiceBus.EntityFramework.Interfaces;
using GoodlyFere.NServiceBus.EntityFramework.SubscriptionStorage;
using Moq;
using NServiceBus;
using NServiceBus.Unicast.Subscriptions;
using Xunit;

#endregion

namespace UnitTests.SubscriptionStorage
{
    public class SubscriptionPersisterTests : IDisposable
    {
        private readonly TestDbContext _dbContext;
        private readonly Mock<INServiceBusDbContextFactory> _mockFactory;
        private readonly SubscriptionPersister _persister;

        public SubscriptionPersisterTests()
        {
            _dbContext = new TestDbContext();

            _mockFactory = new Mock<INServiceBusDbContextFactory>();
            _mockFactory.Setup(m => m.CreateSubscriptionDbContext()).Returns(new TestDbContext());

            _persister = new SubscriptionPersister(_mockFactory.Object);

            _dbContext.Subscriptions.RemoveRange(_dbContext.Subscriptions);
            _dbContext.SaveChanges();
        }

        public void Dispose()
        {
            if (_dbContext != null)
            {
                _dbContext.Dispose();
            }
        }

        //subscribe, null address, throws
        [Fact]
        public void Subscribe_NullClient_Throws()
        {
            _persister.Invoking(p => p.Subscribe(null, new List<MessageType>()))
                .ShouldThrow<ArgumentNullException>();
        }

        //subscribe, null message types, throws
        [Fact]
        public void Subscribe_NullMessageTypes_Throws()
        {
            _persister.Invoking(p => p.Subscribe(new Address("queue", "machine"), null))
                .ShouldThrow<ArgumentNullException>();
        }

        //subscribe, empty messages, saves nothing
        [Fact]
        public void Subscribe_EmptyMessageTypes_SavesNothing()
        {
            int expectedCount = _dbContext.Subscriptions.Count();

            _persister.Subscribe(
                new Address("queue", "machine"),
                new List<MessageType>());

            int actualCount = _dbContext.Subscriptions.ToList().Count;

            actualCount.Should().Be(expectedCount);
        }

        // subscribe, duplicate message types, saves unique records
        [Fact]
        public void Subscribe_DuplicateMessageTypes_SavesUniqueRecords()
        {
            _persister.Subscribe(
                new Address("queue", "machine"),
                new List<MessageType>
                {
                    new MessageType(typeof(TestMessage)),
                    new MessageType(typeof(TestMessage)),
                    new MessageType(typeof(TestMessage)),
                    new MessageType(typeof(TestMessage2)),
                });

            var actualEntities = _dbContext.Subscriptions.ToList();

            actualEntities.Count.Should().Be(2);
            actualEntities.Select(e => e.MessageType).Distinct().Count().Should().Be(2);
        }

        //unsubscribe, null client, throws
        [Fact]
        public void Unsubscribe_NullClient_Throws()
        {
            _persister.Invoking(p => p.Unsubscribe(null, new List<MessageType>()))
                .ShouldThrow<ArgumentNullException>();
        }

        // unsubscribe, null messagetypes throws
        [Fact]
        public void Unsubscribe_NullMessageTypes_Throws()
        {
            _persister.Invoking(p => p.Unsubscribe(new Address("queue", "machine"), null))
                .ShouldThrow<ArgumentNullException>();
        }

        // unsubscribe, removes all subscriptions
        [Fact]
        public void Unsubscribe_RemovesAllSubscriptions()
        {
            AddSubscriptions();

            string messageType = new MessageType(typeof(TestMessage)).ToString();
            _dbContext.Subscriptions.Count(
                s => s.SubscriberEndpoint == "queue@machine"
                     && s.MessageType == messageType)
                .Should().BeGreaterThan(0);

            _persister.Unsubscribe(
                new Address("queue", "machine"),
                new List<MessageType>
                {
                    new MessageType(typeof(TestMessage))
                });

            _dbContext.Subscriptions.Count(
                s => s.SubscriberEndpoint == "queue@machine"
                     && s.MessageType == messageType)
                .Should().Be(0);
        }

        private void AddSubscriptions()
        {
            _dbContext.Subscriptions.AddRange(
                new List<SubscriptionEntity>
                {
                    new SubscriptionEntity
                    {
                        MessageType = new MessageType(typeof(TestMessage)).ToString(),
                        SubscriberEndpoint = "queue@machine"
                    },
                    new SubscriptionEntity
                    {
                        MessageType = new MessageType(typeof(TestMessage2)).ToString(),
                        SubscriberEndpoint = "queue@machine2"
                    },
                    new SubscriptionEntity
                    {
                        MessageType = new MessageType(typeof(TestMessage2)).ToString(),
                        SubscriberEndpoint = "queue@machine3"
                    },
                    new SubscriptionEntity
                    {
                        MessageType = new MessageType(typeof(TestMessage2)).ToString(),
                        SubscriberEndpoint = "queue@machine4"
                    },
                });

            _dbContext.SaveChanges();
        }
    }
}