﻿#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityFrameworkSagaStorage.cs">
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
// <created>03/25/2015 10:00 AM</created>
// <updated>03/31/2015 12:55 PM by Ben Ramey</updated>
// --------------------------------------------------------------------------------------------------------------------

#endregion

#region Usings

using System;
using System.Linq;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;

#endregion

namespace GoodlyFere.NServiceBus.EntityFramework.SagaStorage
{
    public class EntityFrameworkSagaStorageFeature : Feature
    {
        private static readonly ILog Logger = LogManager.GetLogger<EntityFrameworkSagaStorageFeature>();

        public EntityFrameworkSagaStorageFeature()
        {
            DependsOn<Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            Logger.Debug("Setting up EntityFrameworkSagaStorageFeature");

            Logger.Debug("Configuring SagaPersister component with InstancePerCall lifecycle.");
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}