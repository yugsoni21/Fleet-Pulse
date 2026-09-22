using FleetPulse.Core.Models;

namespace FleetPulse.Core.Rules;

/// <summary>
/// A pluggable rule evaluated against every telemetry reading as it flows through the
/// ingestion pipeline. Implementations may hold their own per-device state (e.g. idle
/// duration tracking) since they are registered as singletons.
/// </summary>
public interface IAlertRule
{
    IEnumerable<Alert> Evaluate(RuleEvaluationContext context);
}
