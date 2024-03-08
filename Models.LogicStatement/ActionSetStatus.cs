// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Econolite.Ode.Models.LogicStatement.Dto;
using Econolite.Ode.Status.Common.Compare;

namespace Econolite.Ode.Models.LogicStatement;

public class ActionSetStatus : ActionSet
{
    public Guid EventId { get; set; } = Guid.Empty;
    public Guid ActionSetId { get; set; } = Guid.Empty;
    public bool IsRunning { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public IEnumerable<StatementStatus> StatementsStatus { get; set; } = Array.Empty<StatementStatus>();
    public IEnumerable<StatementActionStatus> ActionsStatus { get; set; } = Array.Empty<StatementActionStatus>();
}

public class StatementActionStatus : StatementAction
{
    public Guid ActionSetId { get; set; } = Guid.Empty;
    public Guid Id { get; set; } = Guid.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string TransmitMode { get; set; } = "Alternating";
    public string MessageType { get; set; } = string.Empty;
    public string DurationType { get; set; } = "Minutes";
    public string Duration { get; set; } = "5";
}