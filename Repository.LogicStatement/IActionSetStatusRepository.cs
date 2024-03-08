// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Econolite.Ode.Models.LogicStatement;
using Econolite.Ode.Persistence.Common.Repository;

namespace Econolite.Dto.Repository.LogicStatement;

public interface IActionSetStatusRepository : IRepository<ActionSetStatus, Guid>
{
    Task<IEnumerable<ActionSetStatus>> GetAllByEventIdAsync(Guid eventId);
    Task<IEnumerable<ActionSetStatus>> GetAllByIdsAsync(IEnumerable<Guid> actionSetIds);
}