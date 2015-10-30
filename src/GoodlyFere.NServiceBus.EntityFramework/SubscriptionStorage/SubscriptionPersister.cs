﻿#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubscriptionPersister.cs">
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
// <created>03/25/2015 7:55 PM</created>
// <updated>03/31/2015 12:55 PM by Ben Ramey</updated>
// --------------------------------------------------------------------------------------------------------------------

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GoodlyFere.NServiceBus.EntityFramework.Interfaces;
using GoodlyFere.NServiceBus.EntityFramework.SharedDbContext;
using NServiceBus;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

#endregion

namespace GoodlyFere.NServiceBus.EntityFramework.SubscriptionStorage
{
    public class SubscriptionPersister : ISubscriptionStorage
    {
        private readonly ISubscriptionDbContext _dbContext;

        public SubscriptionPersister(IDbContextProvider dbContextProvider)
        {
            if (dbContextProvider == null)
            {
                throw new ArgumentNullException("dbContextProvider");
            }

            _dbContext = dbContextProvider.GetSubscriptionDbContext();
        }

        public IEnumerable<Address> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            if (messageTypes == null)
            {
                throw new ArgumentNullException("messageTypes");
            }

            MessageType[] mtArray = messageTypes as MessageType[] ?? messageTypes.ToArray();
            if (!mtArray.Any())
            {
                return new List<Address>();
            }
            var messageTypeStrings = mtArray.Select(mt => mt.ToString()).ToList();
            var subscriptions = _dbContext.Subscriptions
                .Where(s => messageTypeStrings.Contains(s.MessageType))
                .ToList();

            return subscriptions
                .Select(s => Address.Parse(s.SubscriberEndpoint))
                .Distinct()
                .ToList();
        }

        public void Init()
        {
        }

        public void Subscribe(Address client, IEnumerable<MessageType> messageTypes)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            if (messageTypes == null)
            {
                throw new ArgumentNullException("messageTypes");
            }

            string clientAddress = client.ToString();
            List<string> messageTypeStrings = new List<string>();
            List<SubscriptionEntity> subscriptions = new List<SubscriptionEntity>();

            foreach (MessageType mt in messageTypes.Distinct())
            {
                string messageTypeString = mt.ToString();

                messageTypeStrings.Add(messageTypeString);
                subscriptions.Add(
                    new SubscriptionEntity
                    {
                        SubscriberEndpoint = clientAddress,
                        MessageType = messageTypeString
                    });
            }

            var existing = _dbContext.Subscriptions.Where(
                s => s.SubscriberEndpoint == clientAddress
                     && messageTypeStrings.Contains(s.MessageType));

            foreach (var subscription in subscriptions)
            {
                if (existing.Any(s => s.MessageType == subscription.MessageType))
                {
                    return;
                }

                _dbContext.Subscriptions.Add(subscription);
            }

            _dbContext.SaveChanges();
        }

        public void Unsubscribe(Address client, IEnumerable<MessageType> messageTypes)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            if (messageTypes == null)
            {
                throw new ArgumentNullException("messageTypes");
            }

            string clientAddress = client.ToString();
            List<string> messageTypeStrings = messageTypes.Select(mt => mt.ToString()).ToList();

            List<SubscriptionEntity> existing = _dbContext.Subscriptions.Where(
                s => s.SubscriberEndpoint == clientAddress
                     && messageTypeStrings.Contains(s.MessageType))
                .ToList();

            _dbContext.Subscriptions.RemoveRange(existing);

            _dbContext.SaveChanges();
        }
    }
}