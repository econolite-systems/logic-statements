// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Econolite.Ode.Messaging;
using Econolite.Ode.Messaging.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Econolite.Ode.Router.ActionSet.Messaging.Extensions;

public static class Defined
{
    public static IServiceCollection AddActionSetRouterSink(this IServiceCollection services) => services
        .AddActionSetRouterSink(Consts.ACTION_SET_ACTION_REQUEST_DEFAULT_CHANNEL);
    
    public static IServiceCollection AddActionSetRouterSink(this IServiceCollection services, IConfiguration configuration) => services
        .AddActionSetRouterSink(configuration[Consts.ACTION_SET_ACTION_REQUEST_DEFAULT_CONFIGURATION_PATH] ?? Consts.ACTION_SET_ACTION_REQUEST_DEFAULT_CHANNEL);
    
    public static IServiceCollection AddActionSetRouterSink(this IServiceCollection services, string channel) => services
        .AddActionSetRouterSink(options => options.DefaultChannel = channel);
    
    public static IServiceCollection AddActionSetRouterSink(this IServiceCollection services, Action<SinkOptions<ActionRequest>> sinkOptions) => services
        .AddMessaging()
        .AddMessagingJsonSink<ActionRequest>(sinkOptions);
    
    public static IServiceCollection AddActionSetRouterSource(this IServiceCollection services) => services
        .AddActionSetRouterSource(Consts.ACTION_SET_ACTION_REQUEST_DEFAULT_CHANNEL);
    
    public static IServiceCollection AddActionSetRouterSource(this IServiceCollection services, IConfiguration configuration) => services
        .AddActionSetRouterSource(configuration[Consts.ACTION_SET_ACTION_REQUEST_DEFAULT_CONFIGURATION_PATH] ?? Consts.ACTION_SET_ACTION_REQUEST_DEFAULT_CHANNEL);
    
    public static IServiceCollection AddActionSetRouterSource(this IServiceCollection services, string channel) => services
        .AddActionSetRouterSource(options => options.DefaultChannel = channel);
    
    public static IServiceCollection AddActionSetRouterSource(this IServiceCollection services, Action<SourceOptions<ActionRequest>> sourceOptions) => services
        .AddMessaging()
        .AddMessagingJsonSource<ActionRequest>(sourceOptions);
}