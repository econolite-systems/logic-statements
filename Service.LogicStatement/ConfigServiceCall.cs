// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Econolite.Ode.Authorization;
using Econolite.Ode.Models.Entities;
using Econolite.Ode.Models.LogicStatement.Dto;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using Econolite.Ode.Messaging.Elements;

namespace Econolite.Ode.Service.LogicStatement;

public interface IConfigServiceCall
{
    Task<IEnumerable<EntityNode>> GetEntities(string[] entityIds, bool resetToken = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActionSet>> GetAllActionSets(CancellationToken cancellationToken = default);
    Task<IEnumerable<ActionSet>> GetActionSetsByEntityIdAndType(Guid id, string type, CancellationToken cancellationToken = default);
    Task<(double Latitude, double Longitude)> GetLocation(Guid id, CancellationToken cancellationToken = default);
}

public class ConfigServiceCall : IConfigServiceCall
{
    private static HttpClient _client = null!;
    private readonly ConfigServiceCallOptions _options;
    private readonly ITokenHandler _tokenHandler;

    public ConfigServiceCall(ITokenHandler tokenHandler, HttpClient client, IOptions<ConfigServiceCallOptions> options)
    {
        _client = client;
        _options = options.Value;
        _tokenHandler = tokenHandler;
    }
    
    public async Task<IEnumerable<ActionSet>> GetAllActionSets(CancellationToken cancellationToken = default)
    {
        var url = $"{_options.Configuration}/action-set";
        _client.DefaultRequestHeaders.Authorization = await _tokenHandler.GetAuthHeaderAsync(cancellationToken);
        var response = await _client.GetFromJsonAsync<IEnumerable<ActionSet>>(url, cancellationToken: cancellationToken);
        return response ?? Array.Empty<ActionSet>();
    }
    
    public async Task<IEnumerable<ActionSet>> GetActionSetsByEntityIdAndType(Guid id, string type, CancellationToken cancellationToken = default)
    {
        var url = $"{_options.Configuration}/action-set/entity/{type}/{id.ToString()}";
        _client.DefaultRequestHeaders.Authorization = await _tokenHandler.GetAuthHeaderAsync(cancellationToken);
        var response = await _client.GetFromJsonAsync<IEnumerable<ActionSet>>(url, cancellationToken: cancellationToken);
        return response ?? Array.Empty<ActionSet>();
    }
    
    public async Task<IEnumerable<EntityNode>> GetEntities(string[] entityIds, bool resetToken = false, CancellationToken cancellationToken = default)
    {
        var result = Array.Empty<EntityNode>();
        
        try
        {
            var query = $"?ids={string.Join("&ids=", entityIds)}";
            var url = $"{_options.Configuration}/entities{query}";
            _client.DefaultRequestHeaders.Authorization = await _tokenHandler.GetAuthHeaderAsync(cancellationToken);
            var response = await _client.GetFromJsonAsync<IEnumerable<EntityNode>>(url, JsonPayloadSerializerOptions.Options, cancellationToken: cancellationToken);
            return response ?? Array.Empty<EntityNode>();
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await GetEntities(entityIds, true);
            }
        }

        return result;
    }
    
    public async Task<(double Latitude, double Longitude)> GetLocation(Guid id, CancellationToken cancellationToken = default)
    {
        var url = $"{_options.Configuration}/entities/{id.ToString()}";
        _client.DefaultRequestHeaders.Authorization = await _tokenHandler.GetAuthHeaderAsync(cancellationToken);
        var response = await _client.GetFromJsonAsync<EntityNode>(url, JsonPayloadSerializerOptions.Options, cancellationToken: cancellationToken);
        
        return (response?.Geometry?.Point?.Coordinates?[1] ?? 0, response?.Geometry?.Point?.Coordinates?[0] ?? 0);
    }
}
