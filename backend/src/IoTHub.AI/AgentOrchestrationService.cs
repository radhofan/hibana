using IoTHub.Application.DTOs;
using IoTHub.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace IoTHub.AI;

/// <summary>
/// Uses two Semantic Kernel agents (Planner + Reviewer) backed by a local Ollama model
/// to produce a structured analysis of an IoT alert.
/// </summary>
public class AgentOrchestrationService : IAgentOrchestrationService
{
    private readonly ILogger<AgentOrchestrationService> _logger;
    private readonly Kernel _kernel;
    private readonly string _modelId;

    private const string PlannerSystemPrompt =
        "You are a Planner Agent monitoring IoT node health. When given an alert, assess the severity, " +
        "identify the likely root cause, and flag whether immediate action is required. Be concise (3-5 sentences).";

    private const string ReviewerSystemPrompt =
        "You are a Reviewer Agent. You receive a planner's assessment of an IoT failure and must critically " +
        "review it for accuracy, missed causes, and suggest a specific remediation step. Be concise (3-5 sentences).";

    public AgentOrchestrationService(IConfiguration config, ILogger<AgentOrchestrationService> logger)
    {
        _logger = logger;

        var endpoint = config["Ollama:Endpoint"] ?? "http://localhost:11434";
        _modelId = config["Ollama:Model"] ?? "llama3";

        _kernel = Kernel.CreateBuilder()
            .AddOllamaChatCompletion(_modelId, new Uri(endpoint))
            .Build();
    }

    public async Task<AgentAnalysisResult> AnalyzeAlertAsync(
        Guid alertId,
        string deviceName,
        string message,
        double triggerValue,
        double threshold,
        CancellationToken ct = default)
    {
        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            // ── Planner pass ──────────────────────────────────────────────────────────
            var plannerHistory = new ChatHistory();
            plannerHistory.AddSystemMessage(PlannerSystemPrompt);
            plannerHistory.AddUserMessage(
                $"Alert on device '{deviceName}': {message}. " +
                $"Trigger value: {triggerValue}, threshold: {threshold}. " +
                $"What is your assessment?");

            var plannerResponse = await chatService.GetChatMessageContentAsync(
                plannerHistory,
                cancellationToken: ct);

            var plannerText = plannerResponse.Content ?? string.Empty;
            _logger.LogInformation("[AI-Planner] Alert {AlertId}: {Assessment}", alertId, plannerText);

            // ── Reviewer pass ─────────────────────────────────────────────────────────
            var reviewerHistory = new ChatHistory();
            reviewerHistory.AddSystemMessage(ReviewerSystemPrompt);
            reviewerHistory.AddUserMessage(
                $"Planner's assessment: {plannerText}. " +
                $"Critique this assessment and provide one concrete remediation action.");

            var reviewerResponse = await chatService.GetChatMessageContentAsync(
                reviewerHistory,
                cancellationToken: ct);

            var reviewerText = reviewerResponse.Content ?? string.Empty;
            _logger.LogInformation("[AI-Reviewer] Alert {AlertId}: {Critique}", alertId, reviewerText);

            // Extract the recommended action from the reviewer's final sentence
            var recommendedAction = ExtractRecommendedAction(reviewerText);

            return new AgentAnalysisResult(plannerText, reviewerText, recommendedAction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI agent orchestration failed for alert {AlertId}. Returning fallback result.", alertId);

            const string fallback = "AI analysis unavailable — Ollama service could not be reached or returned an error.";
            return new AgentAnalysisResult(fallback, fallback, "Check Ollama service health and retry.");
        }
    }

    /// <summary>
    /// Heuristically extracts the last sentence of the reviewer text as the concrete action.
    /// Falls back to the full reviewer text if no sentence boundary is found.
    /// </summary>
    private static string ExtractRecommendedAction(string reviewerText)
    {
        if (string.IsNullOrWhiteSpace(reviewerText))
            return "No specific action provided.";

        var sentences = reviewerText
            .Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Return last non-empty sentence that looks like an action
        for (int i = sentences.Length - 1; i >= 0; i--)
        {
            var s = sentences[i].Trim();
            if (s.Length > 10)
                return s + ".";
        }

        return reviewerText.Length > 200
            ? reviewerText[..200] + "..."
            : reviewerText;
    }
}
