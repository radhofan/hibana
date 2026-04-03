namespace IoTHub.Application.DTOs;

public record AgentAnalysisDto(
    Guid AlertId,
    string PlannerAssessment,
    string ReviewerCritique,
    string RecommendedAction,
    DateTime AnalyzedAt);

public record AgentAnalysisResult(
    string PlannerAssessment,
    string ReviewerCritique,
    string RecommendedAction);
