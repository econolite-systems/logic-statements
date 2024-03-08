// SPDX-License-Identifier: MIT
// Copyright: 2023 Econolite Systems, Inc.

using Econolite.Ode.Models.LogicStatement.Dto;
using FluentAssertions;
using Xunit;

namespace Econolite.Ode.Domain.LogicStatement.Test;

public class ActionSetExtensionTests
{
    [Fact]
    public void EvaluateConditonalAllAndResultTrueTest()
    {
        var statementValues = new[] {true, true, true};
        var conditionals = new[] {new Conditional() {Condition = "And"}, new Conditional() {Condition = "And"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeTrue();
    }
    
    [Fact]
    public void EvaluateConditonalAllAndLastFalseResultFalseTest()
    {
        var statementValues = new[] {true, true, false};
        var conditionals = new[] {new Conditional() {Condition = "And"}, new Conditional() {Condition = "And"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeFalse();
    }
    
    [Fact]
    public void EvaluateConditonalAllAndLastTwoFalseResultFalseTest()
    {
        var statementValues = new[] {true, false, false};
        var conditionals = new[] {new Conditional() {Condition = "And"}, new Conditional() {Condition = "And"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeFalse();
    }
    
    [Fact]
    public void EvaluateConditonalAllAndAllFalseResultFalseTest()
    {
        var statementValues = new[] {false, false, false};
        var conditionals = new[] {new Conditional() {Condition = "And"}, new Conditional() {Condition = "And"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeFalse();
    }
    
    [Fact]
    public void EvaluateConditonalAllOrResultTrueTest()
    {
        var statementValues = new[] {true, false, false};
        var conditionals = new[] {new Conditional() {Condition = "Or"}, new Conditional() {Condition = "Or"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeTrue();
    }
    
    [Fact]
    public void EvaluateConditonalFirstAndLastOrResultTrueTest()
    {
        var statementValues = new[] {true, true, false};
        var conditionals = new[] {new Conditional() {Condition = "And"}, new Conditional() {Condition = "Or"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeTrue();
    }
    
    [Fact]
    public void EvaluateConditonalFirstOrLastAndResultTrueTest()
    {
        var statementValues = new[] {false, true, true};
        var conditionals = new[] {new Conditional() {Condition = "Or"}, new Conditional() {Condition = "And"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeTrue();
    }
    
    [Fact]
    public void EvaluateConditonalAllOrResultFalseTest()
    {
        var statementValues = new[] {false, false, false};
        var conditionals = new[] {new Conditional() {Condition = "Or"}, new Conditional() {Condition = "Or"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeFalse();
    }
    
    [Fact]
    public void EvaluateConditonalFirstAndLastOrResultFalseTest()
    {
        var statementValues = new[] {false, false, false};
        var conditionals = new[] {new Conditional() {Condition = "And"}, new Conditional() {Condition = "Or"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeFalse();
    }
    
    [Fact]
    public void EvaluateConditonalFirstOrLastAndResultFalseTest()
    {
        var statementValues = new[] {false, false, true};
        var conditionals = new[] {new Conditional() {Condition = "Or"}, new Conditional() {Condition = "And"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeFalse();
    }
    
    [Fact]
    public void EvaluateConditonalAllNotResultTrueTest()
    {
        var statementValues = new[] {false, true, false};
        var conditionals = new[] {new Conditional() {Condition = "Not"}, new Conditional() {Condition = "Not"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeTrue();
    }
    
    [Fact]
    public void EvaluateConditonalAllNotResultFalseTest()
    {
        var statementValues = new[] {false, true, true};
        var conditionals = new[] {new Conditional() {Condition = "Not"}, new Conditional() {Condition = "Not"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeFalse();
    }
    
    [Fact]
    public void EvaluateConditonalFirstAndLastNotResultTrueTest()
    {
        var statementValues = new[] {true, true, false};
        var conditionals = new[] {new Conditional() {Condition = "And"}, new Conditional() {Condition = "Not"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeTrue();
    }
    
    [Fact]
    public void EvaluateConditonalFirstOrLastNotResultTrueTest()
    {
        var statementValues = new[] {false, false, true};
        var conditionals = new[] {new Conditional() {Condition = "Or"}, new Conditional() {Condition = "Not"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeTrue();
    }
    
    [Fact]
    public void EvaluateConditonalFirstAndLastNotResultFalseTest()
    {
        var statementValues = new[] {true, true, true};
        var conditionals = new[] {new Conditional() {Condition = "And"}, new Conditional() {Condition = "Not"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeFalse();
    }
    
    [Fact]
    public void EvaluateConditonalFirstOrLastNotResultFalseTest()
    {
        var statementValues = new[] {true, false, true};
        var conditionals = new[] {new Conditional() {Condition = "Or"}, new Conditional() {Condition = "Not"}};

        var result = conditionals.ShouldRun(statementValues);
        result.Should().BeFalse();
    }
}