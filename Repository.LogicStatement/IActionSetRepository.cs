// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Econolite.Ode.Models.LogicStatement.Dto;
using Econolite.Ode.Persistence.Common.Repository;

namespace Econolite.Dto.Repository.LogicStatement;

public interface IActionSetRepository : IRepository<ActionSet, Guid>
{
    Task<IEnumerable<ActionSet>> GetByEntityIdAndType(Guid id, string type);
}