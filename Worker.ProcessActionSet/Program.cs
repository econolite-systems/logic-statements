// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Econolite.Dto.Repository.LogicStatement.Extensions;
using Econolite.Ode.Authorization.Extensions;
using Econolite.Ode.Extensions.AspNet;
using Econolite.Ode.Messaging;
using Econolite.Ode.Messaging.Elements;
using Econolite.Ode.Messaging.Extensions;
using Econolite.Ode.Models.LogicStatement;
using Econolite.Ode.Monitoring.HealthChecks.Mongo.Extensions;
using Econolite.Ode.Persistence.Mongo;
using Econolite.Ode.Service.LogicStatement.Extensions;
using Econolite.Ode.Status.Common;
using Econolite.Ode.Status.Ess.Messaging.Extensions;
using Econolite.Ode.Status.PavementCondition.Messaging.Extensions;
using Econolite.Ode.Status.WrongWayDriver.Messaging.Extensions;
using Econolite.Ode.Status.SpeedEvent.Messaging.Extensions;
using System.Text;
using Econolite.Ode.Router.ActionSet.Messaging.Extensions;
using Worker.ProcessActionSet;

using System.Text;
using Common.Extensions;
using Econolite.Dto.Repository.LogicStatement.Extensions;
using Econolite.Ode.Authorization.Extensions;
using Econolite.Ode.Messaging;
using Econolite.Ode.Messaging.Elements;
using Econolite.Ode.Messaging.Extensions;
using Econolite.Ode.Models.LogicStatement;
using Econolite.Ode.Monitoring.Events.Extensions;
using Econolite.Ode.Monitoring.HealthChecks.Kafka.Extensions;
using Econolite.Ode.Monitoring.Metrics.Extensions;
using Econolite.Ode.Persistence.Mongo;
using Econolite.Ode.Service.LogicStatement.Extensions;
using Econolite.Ode.Status.Common;
using Econolite.Ode.Status.Ess.Messaging.Extensions;
using Econolite.Ode.Status.PavementCondition.Messaging.Extensions;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Worker.ProcessActionSet;

var builder = WebApplication.CreateBuilder(args);
builder.Host
    .ConfigureServices((hostContext, services) =>
    {
        services.AddMessaging();
        services.AddMongo();
        services.AddMetrics(hostContext.Configuration, "Logic Statements Action Set Service")
            .AddUserEventSupport(hostContext.Configuration, _ =>
            {
                _.DefaultSource = "Logic Statements Action Set Service";
                _.DefaultLogName = Econolite.Ode.Monitoring.Events.LogName.SystemEvent;
                _.DefaultCategory = Econolite.Ode.Monitoring.Events.Category.Server;
                _.DefaultTenantId = Guid.Empty;
            });

        services.AddActionSetStatusRepo();
        services.AddTokenHandler(options =>
        {
            options.Authority = hostContext.Configuration["Authentication:Authority"] ??
                                throw new NullReferenceException("Authentication:Authority missing in config");
            options.ClientId = hostContext.Configuration["Authentication:ClientId"] ??
                               throw new NullReferenceException("Authentication:ClientId missing in config");
            options.ClientSecret = hostContext.Configuration["Authentication:ClientSecret"] ??
                                   throw new NullReferenceException("Authentication:ClientSecret missing in config");
        });
        services.AddConfigServiceCall(options =>
        {
            options.Configuration = hostContext.Configuration["Services:Configuration"] ??
                                    throw new NullReferenceException("Services:Configuration missing in config");
        });
        services
            .AddMessaging()
            .AddEssActionEventStatusHandler()
            .AddPavementConditionStatusHandler()
            .AddWrongWayDriverStatusHandler()
            .AddSpeedStatusHandler()
            .AddActionSetRouterSink()
            .Configure<MessageFactoryOptions<StatementActionStatus>>(options =>
                options.FuncBuildPayloadElement = _ => new BaseJsonPayload<StatementActionStatus>(_))
            .AddTransient<MessageFactory<StatementActionStatus>>()
            .AddTransient<IProducer<Guid, StatementActionStatus>, Producer<Guid, StatementActionStatus>>();

        services.AddTransient<IConsumeResultFactory<Guid, ActionEventStatus>>(_ =>
            new ConsumeResultFactory<Guid, ActionEventStatus>(
                _ => Guid.Parse(Encoding.UTF8.GetString(_)), new JsonPayloadSpecialist<ActionEventStatus>()
            ));

        services.AddTransient<IConsumer<Guid, ActionEventStatus>, Consumer<Guid, ActionEventStatus>>();

        services.AddHealthChecks()
            .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 1024, name: "Process Allocated Memory",
                tags: new[] { "memory" })
            .AddKafkaHealthCheck()
            .AddMongoDbHealthCheck();

        services.AddHostedService<ProcessStatusService>();
    });

var app = builder.Build();

app
    .UseRouting()
    .UseHealthChecksPrometheusExporter("/metrics")
    .UseEndpoints(endpoints =>
    {
        endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });
    });

await app.RunAsync();