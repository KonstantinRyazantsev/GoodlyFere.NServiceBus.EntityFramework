﻿using System;
using System.Linq;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;

namespace GoodlyFere.NServiceBus.EntityFramework.SharedDbContext
{
    public class EntityFrameworkSharedDbContextFeature : Feature
    {
        private static readonly ILog Logger = LogManager.GetLogger<EntityFrameworkSharedDbContextFeature>();

        internal EntityFrameworkSharedDbContextFeature()
        {
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            Logger.Debug("Setting up EntityFrameworkSharedDbContextFeature");

            context.Container.ConfigureComponent<CreateDbContextBehavior>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<InternalDbContextProvider>(DependencyLifecycle.SingleInstance);

            context.Pipeline.Register<CreateDbContextBehavior.Registration>();
        }
    }
}