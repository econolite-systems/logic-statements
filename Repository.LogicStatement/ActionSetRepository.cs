// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Econolite.Ode.Models.LogicStatement.Dto;
using Econolite.Ode.Persistence.Mongo.Context;
using Econolite.Ode.Persistence.Mongo.Repository;
using Econolite.Ode.Status.Common.Compare;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Econolite.Dto.Repository.LogicStatement;

public class ActionSetRepository : GuidDocumentRepositoryBase<ActionSet>, IActionSetRepository
{
    public ActionSetRepository(IMongoContext context, ILogger<ActionSetRepository> logger) : base(context, logger)
    {
    }
    
    public async Task<IEnumerable<ActionSet>> GetByEntityIdAndType(Guid id, string type)
    {
        var entityId = id.ToString();
        var entityIdFilter = Builders<Statement>.Filter.AnyIn<string>(s => s.Entities, new[] {entityId});
        var emptyEntitiesFilter = Builders<Statement>.Filter.Size("entities", 0);
        var statementTypeFilter = Builders<Statement>.Filter.Eq(s => s.Type, type);
        var filter =
            Builders<ActionSet>.Filter
                .ElemMatch(x => x.Statements,
                    Builders<Statement>.Filter.Or(
                    entityIdFilter,
                    Builders<Statement>.Filter.And(statementTypeFilter, emptyEntitiesFilter)
                    ));

        var results = await ExecuteDbSetFuncAsync(collection => collection.FindAsync(filter));
        return results?.ToList() ?? new List<ActionSet>();
    }
}