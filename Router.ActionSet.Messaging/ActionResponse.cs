// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

namespace Econolite.Ode.Router.ActionSet.Messaging;

public class ActionResponse
{
    public Guid ActionSetId { get; set; } = Guid.Empty;
    public Guid Id { get; set; } = Guid.Empty;
    public ActionStatus Status { get; set; } = ActionStatus.Unknown;
    public string? Error { get; set; }
}

public enum ActionStatus
{
    Unknown,
    Pending,
    Completed,
    Failure
}