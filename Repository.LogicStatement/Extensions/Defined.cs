// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Microsoft.Extensions.DependencyInjection;

namespace Econolite.Dto.Repository.LogicStatement.Extensions;

public static class Defined
{
    public static IServiceCollection AddActionSetStatusRepo(this IServiceCollection services)
    {
        services.AddTransient<IActionSetStatusRepository, ActionSetStatusRepository>();

        return services;
    }
    
    public static IServiceCollection AddActionSetRepo(this IServiceCollection services)
    {
        services.AddTransient<IActionSetRepository, ActionSetRepository>();

        return services;
    }
}