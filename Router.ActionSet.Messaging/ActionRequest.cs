// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

namespace Econolite.Ode.Router.ActionSet.Messaging;

public class ActionRequest
{
    public Guid ActionSetId { get; set; } = Guid.Empty;
    public Guid Id { get; set; } = Guid.Empty;
    public bool Cancel { get; set; } = false;
    public string ActionType { get; set; } = string.Empty;
    public string Info { get; set; } = string.Empty;
    public string TransmitMode { get; set; } = "Alternating";
    public string MessageType { get; set; } = string.Empty;
    public string DurationType { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public IEnumerable<string> Parameter { get; set; } = Enumerable.Empty<string>();
}