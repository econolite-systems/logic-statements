// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Econolite.Ode.Persistence.Common.Entities;
using Econolite.Ode.Status.Common;
using Econolite.Ode.Status.Common.Compare;

namespace Econolite.Ode.Models.LogicStatement.Dto;

public class ActionSet : IndexedEntityBase<Guid>
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public IEnumerable<Statement> Statements { get; set; } = Array.Empty<Statement>();
    public IEnumerable<Conditional> Conditionals { get; set; } = Array.Empty<Conditional>();
    public IEnumerable<StatementAction> Actions { get; set; } = Array.Empty<StatementAction>();
}

public class Conditional
{
    public string Condition { get; set; } = string.Empty;
}

public class StatementAction
{
    public string Action { get; set; } = String.Empty;
}

public static class ActionSetExtensions
{
    public static ActionSetStatus ToActionSetStatus(this ActionSet actionSet,
        IEnumerable<StatementStatus> statementsStatus, IEnumerable<StatementActionStatus> actionsStatus, Guid eventId)
    {
        return new ActionSetStatus
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ActionSetId = actionSet.Id,
            Name = actionSet.Name,
            IsEnabled = actionSet.IsEnabled,
            IsRunning = false,
            StartTime = null,
            EndTime = null,
            Statements = actionSet.Statements,
            Conditionals = actionSet.Conditionals,
            Actions = actionSet.Actions,
            StatementsStatus = statementsStatus,
            ActionsStatus = actionsStatus
        };
    }

    public static StatementActionStatus ToStatus(this StatementAction statementAction, Guid actionSetId,
        (double? Latitude, double? Longitude) location = default)
    {
        return new StatementActionStatus
        {
            ActionSetId = actionSetId,
            Action = statementAction.Action,
            StartTime = null,
            EndTime = null,
            Latitude = location.Latitude,
            Longitude = location.Longitude
        };
    }

    public static IEnumerable<StatementActionStatus> Start(this IEnumerable<StatementActionStatus> actions)
    {
        return actions.Select(a =>
        {
            a.StartTime = DateTime.UtcNow;
            a.EndTime = null;
            return a;
        });
    }

    public static IEnumerable<StatementActionStatus> End(this IEnumerable<StatementActionStatus> actions)
    {
        return actions.Select(a =>
        {
            a.EndTime = DateTime.UtcNow;
            return a;
        });
    }

    public static bool ShouldRun(this StatementStatus status)
    {
        if (!status.IsTriggered) return false;

        if (status.Schedule.Type == "immediate")
        {
            return true;
        }
        else if (status.Schedule.Type == "recurring")
        {
            if (status.Schedule.Times <= 1)
            {
                return true;
            }
            else
            {
                if (status.Triggered.Count() < status.Schedule.Times)
                {
                    return false;
                }
                else if (status.Triggered.Count() >= status.Schedule.Times)
                {
                    var triggered = status.Triggered.ToArray();
                    var firstTriggered = triggered[^status.Schedule.Times];
                    var lastTriggered = triggered.Last();

                    var nextTriggered = firstTriggered.AddMinutes(status.Schedule.In.Minutes)
                        .AddSeconds(status.Schedule.In.Seconds);
                    return nextTriggered <= lastTriggered;
                }
            }
        }

        return false;
    }

    public static ConditionalType ToConditionalType(this Conditional conditional)
    {
        return conditional.Condition.ToLower() switch
        {
            "and" => ConditionalType.And,
            "or" => ConditionalType.Or,
            "not" => ConditionalType.Not,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static bool ShouldRun(this Conditional[] conditionals, bool[] statementsValue)
    {
        if (conditionals.Length == 0) return true;
        if (conditionals.Length != statementsValue.Length - 1) throw new ArgumentOutOfRangeException();

        bool AndFunc(Func<bool> item1, Func<bool> item2) => item1() && item2();
        bool NotFunc(Func<bool> item1, Func<bool> item2) => !AndFunc(item1, item2);
        bool OrFunc(Func<bool> item1, Func<bool> item2) => item1() || item2();

        var currentFunc = () => statementsValue[0];
        for (var i = 0; i < conditionals.Length; i++)
        {
            var conditional = conditionals[i];
            var statementValue = statementsValue[i + 1];
            var funcResult = currentFunc();
            switch (conditional.ToConditionalType())
            {
                case ConditionalType.And:
                    currentFunc = () => AndFunc(() => funcResult, () => statementValue);
                    break;
                case ConditionalType.Or:
                    currentFunc = () => OrFunc(() => funcResult, () => statementValue);
                    break;
                case ConditionalType.Not:
                    currentFunc = () => NotFunc(() => funcResult, () => statementValue);
                    break;
            }
        }

        return currentFunc();
    }

    public static bool ShouldRun(this ActionSetStatus actionSetStatus)
    {
        var result = false;

        if (!actionSetStatus.Conditionals.Any())
        {
            if (actionSetStatus.StatementsStatus.Any(s => s.ShouldRun()))
            {
                result = true;
            }
        }
        else
        {
            var groups = actionSetStatus.StatementsStatus.GroupBy(status => status.Id)
                .Select(g => (g.Key, g.Any(v => v.ShouldRun()))).ToArray();
            var statementsValue = actionSetStatus.Statements
                .Select(s => groups.FirstOrDefault(g => g.Key == s.Id).Item2).ToArray();
            var conditionals = actionSetStatus.Conditionals.ToArray();

            if (conditionals.ShouldRun(statementsValue))
            {
                result = true;
            }
        }

        return result;
    }

    public static ActionSetStatus UpdateRunStatus(this ActionSetStatus status)
    {
        status.IsRunning = status.ShouldRun();
        status.StartTime = status.IsRunning ? DateTime.UtcNow : status.StartTime;
        status.EndTime = status.IsRunning ? null : DateTime.UtcNow;

        return status;
    }

    public static IEnumerable<StatementActionStatus> ActionsToSend(this IEnumerable<ActionSetStatus> statuses,
        Func<ActionSetStatus, bool> update)
    {
        return statuses.SelectMany(s => s.ActionsToSend(update));
    }

    public static IEnumerable<StatementActionStatus> ActionsToSend(this ActionSetStatus actionSetStatus,
        Func<ActionSetStatus, bool> update)
    {
        var isCurrentlyRunning = actionSetStatus.IsRunning;
        var statusResult = actionSetStatus.UpdateRunStatus();
        if (statusResult.IsRunning != isCurrentlyRunning)
        {
            if (statusResult.IsRunning)
            {
                // Run actionSet
                var actionsToRun = actionSetStatus.ActionsStatus.Start().ToArray();
                actionSetStatus.ActionsStatus = actionsToRun;
            }
            else
            {
                // Stop actionSet
                var actionsToStop = actionSetStatus.ActionsStatus.End().ToArray();
                actionSetStatus.ActionsStatus = actionsToStop;
            }

            update(actionSetStatus);
            return actionSetStatus.ActionsStatus;
        }

        return Array.Empty<StatementActionStatus>();
    }

    public static StatementStatus[] ToStatementStatus(this Statement[] currentStatements, ActionEventStatus actionEventStatus, Func<ActionEventStatus, IEnumerable<Statement>, IEnumerable<StatementStatus>> handleActionEventStatus)
    {
        var statements = currentStatements
            .Where(statement => statement.Type == actionEventStatus.ActionEventType).ToArray();
        var statementStatusResult = handleActionEventStatus(actionEventStatus, statements);
        return statementStatusResult.ToArray();
    }
    
    public static List<DateTime> AddDateTime(this IEnumerable<DateTime> dateTimes, DateTime dateTime)
    {
        var list = dateTimes.ToList();
        list.Add(dateTime);
        return list;
    }
    public static List<ActionSetStatus> ToUpdatedActionSetStatuses(
            this ActionSet[] actionSets,
            ActionEventStatus actionEventStatus,
            Func<ActionEventStatus, IEnumerable<Statement>, IEnumerable<StatementStatus>> handleActionEventStatus,
            Guid eventId,
            (double? Latitude, double? Longitude) location = default)
    {
        return actionSets.Select(actionSet =>
        {
            var statements = actionSet.Statements.ToArray();
            var statementStatus = statements.ToStatementStatus(actionEventStatus, handleActionEventStatus);
            var actionStatus = actionSet.Actions.Select(a => a.ToStatus(actionSet.Id, location)).ToArray();
            var actionSetStatus = actionSet.ToActionSetStatus(statementStatus,
                actionStatus, eventId);
            return actionSetStatus;
        }).ToList();
    }
    
    public static List<ActionSetStatus> ToUpdatedActionSetStatuses(
        this ActionSetStatus[] actionSetsStatus,
        ActionEventStatus actionEventStatus,
        Func<ActionEventStatus, IEnumerable<Statement>, IEnumerable<StatementStatus>> handleActionEventStatus)
    {
        return actionSetsStatus.Select(actionSetStatus =>
            actionEventStatus.ToUpdatedActionSetStatus(handleActionEventStatus, actionSetStatus))
            .ToList();
    }

    private static ActionSetStatus ToUpdatedActionSetStatus(this ActionEventStatus actionEventStatus,
        Func<ActionEventStatus, IEnumerable<Statement>, IEnumerable<StatementStatus>> handleActionEventStatus, ActionSetStatus actionSetStatus)
    {
        var statements = actionSetStatus.Statements.ToArray();
        var statementStatus = statements.ToStatementStatus(actionEventStatus, handleActionEventStatus);
        var statementStatusList = actionSetStatus.ToUnchangedStatementStatusList(statementStatus);

        statementStatusList.AddRange(
            statementStatus.Where(s => s is {Schedule.Type: "immediate"}));

        statementStatusList.AddRange(
            statementStatus.Where(s => s.Schedule.Type != "immediate")
                .Select(statement => statement.UpdateStatementStatus(actionSetStatus)));

        actionSetStatus.StatementsStatus = statementStatusList;
        return actionSetStatus;
    }

    private static List<StatementStatus> ToUnchangedStatementStatusList(this ActionSetStatus actionSetStatus, StatementStatus[] statementStatus)
    {
        var unchangedStatementsStatus = actionSetStatus.StatementsStatus.Where(status =>
            statementStatus.All(s => s.SourceId != status.SourceId && s.Id != status.Id)).ToArray();
        var statementStatusList = new List<StatementStatus>(unchangedStatementsStatus);
        return statementStatusList;
    }

    private static StatementStatus UpdateStatementStatus(this StatementStatus statement, ActionSetStatus actionSetStatus)
    {
        var result = statement;

        var update =
            actionSetStatus.StatementsStatus.FirstOrDefault(s =>
                s.Id == statement.Id && s.SourceId == statement.SourceId);

        if (update != null)
        {
            update.Triggered = update.Triggered.AddDateTime(DateTime.UtcNow);
            result = update;
        }

        return result;
    }

    public static IEnumerable<ActionSetStatus> ToUpdatedActionSetStatuses(
        this ActionEventStatus actionEventStatus,
        IEnumerable<ActionSet> actionSets,
        ActionSetStatus[] actionSetStatuses,
        Func<ActionEventStatus, IEnumerable<Statement>, IEnumerable<StatementStatus>> handleActionEventStatus,
        Func<ActionSetStatus, bool> addActionSetStatus,
        Func<ActionSetStatus, bool> updateActionSetStatus,
        Guid eventId,
        (double? Latitude, double? Longitude) location = default)
    {
        
        var actionSetsWithoutStatus = actionSets
            .Where(actionSet => actionSetStatuses.Length == 0 || ( actionSetStatuses.Length != 0 && actionSetStatuses.All(status => status.ActionSetId != actionSet.Id))).ToArray();
        var actionSetsUpdated = new List<ActionSetStatus>();

        var actionSetStatusToAdd =
            actionSetsWithoutStatus.ToUpdatedActionSetStatuses(actionEventStatus, handleActionEventStatus, eventId, location);
        
        actionSetStatusToAdd.ForEach(a => addActionSetStatus(a));
        actionSetsUpdated.AddRange(actionSetStatusToAdd);

        var actionSetStatusesToUpdate =
            actionSetStatuses.ToUpdatedActionSetStatuses(actionEventStatus, handleActionEventStatus);
        
        actionSetStatusesToUpdate.ToList().ForEach(a => updateActionSetStatus(a));
        actionSetsUpdated.AddRange(actionSetStatusesToUpdate);

        return actionSetsUpdated;
    }
}

public enum ConditionalType
{
    And,
    Or,
    Not
}
