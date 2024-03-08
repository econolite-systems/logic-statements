// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Econolite.Ode.Models.LogicStatement.Dto;

namespace Econolite.Ode.Service.LogicStatement;


public interface IActionSetService
{
    Task<IEnumerable<ActionSet>> GetAllAsync();
    Task<IEnumerable<ActionSet>> GetByEntityIdAndTypeAsync(Guid id, string type);
    Task<ActionSet?> GetByIdAsync(Guid id);
    Task<ActionSet?> Add(ActionSet add);
    Task<ActionSet?> Update(ActionSet update);
    Task<bool> Delete(Guid id);
}