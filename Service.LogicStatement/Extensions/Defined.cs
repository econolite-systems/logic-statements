// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Microsoft.Extensions.DependencyInjection;

namespace Econolite.Ode.Service.LogicStatement.Extensions;

public static class Defined
{
    public static IServiceCollection AddConfigServiceCall(this IServiceCollection services,
        Action<ConfigServiceCallOptions> options)
    {
        services.Configure(options);
        services.AddHttpClient();
        services.AddTransient<IConfigServiceCall, ConfigServiceCall>();

        return services;
    }
    
    public static IServiceCollection AddActionSetService(this IServiceCollection services)
    {
        services.AddTransient<IActionSetService, ActionSetService>();

        return services;
    }
}
