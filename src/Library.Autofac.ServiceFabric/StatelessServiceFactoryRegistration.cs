﻿// This software is part of the Autofac IoC container
// Copyright © 2017 Autofac Contributors
// https://autofac.org
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

namespace Library.Autofac.ServiceFabric
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Fabric;

    using global::Autofac;

    using Microsoft.ServiceFabric.Services.Runtime;

    // ReSharper disable once ClassNeverInstantiated.Global
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Instantiated at runtime via dependency injection")]
    internal sealed class StatelessServiceFactoryRegistration : IStatelessServiceFactoryRegistration
    {
        public Action<Exception> ConstructorExceptionCallback { get; }

        // ReSharper disable once UnusedMember.Global
        public StatelessServiceFactoryRegistration(Action<Exception> constructorExceptionCallback)
        {
            this.ConstructorExceptionCallback = constructorExceptionCallback;
        }

        public void RegisterStatelessServiceFactory<TService>(
            ILifetimeScope container, string serviceTypeName, object lifetimeScopeTag = null, Action<ILifetimeScope> scopeCallback = null)
            where TService : StatelessService
        {
            ServiceRuntime.RegisterServiceAsync(serviceTypeName, context =>
            {
                var tag = lifetimeScopeTag ?? Constants.DefaultLifetimeScopeTag;

                var lifetimeScope = container.BeginLifetimeScope(tag, builder =>
                {
                    builder.RegisterInstance(context)
                        .As<StatelessServiceContext>()
                        .As<ServiceContext>();
                });

                scopeCallback?.Invoke(lifetimeScope);

                try
                {
                    var service = lifetimeScope.Resolve<TService>();
                    return service;
                }
                catch (Exception ex)
                {
                    // Proactively dispose lifetime scope as interceptor will not be called.
                    lifetimeScope.Dispose();

                    this.ConstructorExceptionCallback(ex);
                    throw;
                }
            }).GetAwaiter().GetResult();
        }
    }
}
