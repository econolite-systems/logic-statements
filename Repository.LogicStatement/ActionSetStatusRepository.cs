// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using System.Collections.Immutable;
using Econolite.Ode.Models.LogicStatement;
using Econolite.Ode.Persistence.Mongo.Context;
using Econolite.Ode.Persistence.Mongo.Repository;
using Econolite.Ode.Status.Common.Compare;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Econolite.Dto.Repository.LogicStatement;

public class ActionSetStatusRepository : GuidDocumentRepositoryBase<ActionSetStatus>, IActionSetStatusRepository
{
    public ActionSetStatusRepository(IMongoContext context, ILogger<ActionSetStatusRepository> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<ActionSetStatus>> GetAllByEventIdAsync(Guid eventId)
    {
        var filter = Builders<ActionSetStatus>.Filter.Eq(s => s.EventId, eventId);
        var results = await ExecuteDbSetFuncAsync(collection => collection.FindAsync(filter));
        return results?.ToList() ?? new List<ActionSetStatus>();
    }
    
    public async Task<IEnumerable<ActionSetStatus>> GetAllByIdsAsync(IEnumerable<Guid> actionSetIds)
    {
        var filter = Builders<ActionSetStatus>.Filter.In(x => x.ActionSetId, actionSetIds);
        var results = await ExecuteDbSetFuncAsync(collection => collection.FindAsync(filter));
        return results?.ToList() ?? new List<ActionSetStatus>();
    }
}