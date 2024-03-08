// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Econolite.Dto.Repository.LogicStatement;
using Econolite.Ode.Helpers.Exceptions;
using Econolite.Ode.Models.LogicStatement.Dto;
using Microsoft.Extensions.Logging;

namespace Econolite.Ode.Service.LogicStatement;

public class ActionSetService : IActionSetService
{
    private readonly ILogger<ActionSetService> _logger;
    private readonly IActionSetRepository _actionSetRepository;

    public ActionSetService(IActionSetRepository actionSetRepository, ILogger<ActionSetService> logger)
    {
        _actionSetRepository = actionSetRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<ActionSet>> GetAllAsync()
    {
        return await _actionSetRepository.GetAllAsync();
    }

    public async Task<IEnumerable<ActionSet>> GetByEntityIdAndTypeAsync(Guid id, string type)
    {
        return await _actionSetRepository.GetByEntityIdAndType(id, type);
    }

    public async Task<ActionSet?> GetByIdAsync(Guid id)
    {
        return await _actionSetRepository.GetByIdAsync(id);
    }

    public async Task<ActionSet?> Add(ActionSet add)
    { 
        _actionSetRepository.Add(add);
        var (success, _) = await _actionSetRepository.DbContext.SaveChangesAsync();
        return add;
    }

    public async Task<ActionSet?> Update(ActionSet update)
    {
        try
        {
            var actionSet = await _actionSetRepository.GetByIdAsync(update.Id);
            if (actionSet is null)
            {
                return null;
            }
            
            _actionSetRepository.Update(update);
            var (success, errors) = await _actionSetRepository.DbContext.SaveChangesAsync();
            if (!success && !string.IsNullOrWhiteSpace(errors)) throw new UpdateException(errors);
            return update;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }

    public async Task<bool> Delete(Guid id)
    {
        try
        {
            _actionSetRepository.Remove(id);
            var (success, errors) = await _actionSetRepository.DbContext.SaveChangesAsync();
            return success;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return false;
        }
    }
}
