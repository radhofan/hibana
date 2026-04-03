using System.CommandLine;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

// ── Ollama configuration from environment ─────────────────────────────────────
var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
var modelId  = Environment.GetEnvironmentVariable("OLLAMA_MODEL")    ?? "llama3";

// ── Root command ──────────────────────────────────────────────────────────────
var rootCommand = new RootCommand(
    "iotdiff — AI-powered diff and summarisation tool for IoT configuration and log files.\n\n" +
    "Examples:\n" +
    "  iotdiff diff --summarize appsettings.json\n" +
    "  iotdiff diff --compare appsettings.before.json appsettings.after.json");

// ── diff subcommand ───────────────────────────────────────────────────────────
var diffCommand = new Command("diff", "Summarize a file or compare two files using a local LLM.");

var summarizeOption = new Option<FileInfo?>(
    name: "--summarize",
    description: "Path to the file to summarize.")
{
    ArgumentHelpName = "file"
};

var compareOption = new Option<FileInfo[]?>(
    name: "--compare",
    description: "Paths to two files to compare (before and after).")
{
    ArgumentHelpName = "before after",
    Arity = ArgumentArity.ExactlyTwo
};

diffCommand.AddOption(summarizeOption);
diffCommand.AddOption(compareOption);

diffCommand.SetHandler(async (FileInfo? summarizeFile, FileInfo[]? compareFiles) =>
{
    if (summarizeFile is null && (compareFiles is null || compareFiles.Length == 0))
    {
        Console.Error.WriteLine("Error: provide --summarize <file> or --compare <before> <after>.");
        Environment.Exit(1);
        return;
    }

    var kernel = Kernel.CreateBuilder()
        .AddOllamaChatCompletion(modelId, new Uri(endpoint))
        .Build();

    var chat = kernel.GetRequiredService<IChatCompletionService>();

    if (summarizeFile is not null)
    {
        await RunSummarizeAsync(chat, summarizeFile);
    }
    else if (compareFiles is { Length: 2 })
    {
        await RunCompareAsync(chat, compareFiles[0], compareFiles[1]);
    }
}, summarizeOption, compareOption);

rootCommand.AddCommand(diffCommand);

return await rootCommand.InvokeAsync(args);

// ── Handlers ──────────────────────────────────────────────────────────────────

static async Task RunSummarizeAsync(IChatCompletionService chat, FileInfo file)
{
    if (!file.Exists)
    {
        Console.Error.WriteLine($"Error: file not found: {file.FullName}");
        Environment.Exit(1);
        return;
    }

    Console.Error.WriteLine($"Summarizing: {file.FullName}");
    Console.Error.WriteLine($"Model endpoint: {Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434"} / model: {Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3"}");
    Console.Error.WriteLine();

    var content = await File.ReadAllTextAsync(file.FullName);

    var history = new ChatHistory();
    history.AddSystemMessage(
        "You are a technical document summarizer. Given the contents of a file, " +
        "provide a clear, concise summary (5-10 sentences) covering: the file's purpose, " +
        "key configuration values or logic, and anything that stands out as important or risky.");
    history.AddUserMessage(
        $"File: {file.Name}\n\n" +
        $"Contents:\n```\n{TruncateIfLarge(content)}\n```\n\n" +
        $"Please summarize this file.");

    await StreamResponseAsync(chat, history);
}

static async Task RunCompareAsync(IChatCompletionService chat, FileInfo before, FileInfo after)
{
    foreach (var f in new[] { before, after })
    {
        if (!f.Exists)
        {
            Console.Error.WriteLine($"Error: file not found: {f.FullName}");
            Environment.Exit(1);
            return;
        }
    }

    Console.Error.WriteLine($"Comparing:");
    Console.Error.WriteLine($"  Before: {before.FullName}");
    Console.Error.WriteLine($"  After:  {after.FullName}");
    Console.Error.WriteLine();

    var beforeContent = await File.ReadAllTextAsync(before.FullName);
    var afterContent  = await File.ReadAllTextAsync(after.FullName);

    var history = new ChatHistory();
    history.AddSystemMessage(
        "You are a technical diff analyst. You will be given two versions of a file — a 'before' and an 'after'. " +
        "Your task is to describe, in plain English: (1) what changed, (2) why each change might have been made, " +
        "(3) any risks or regressions introduced, and (4) a concise summary of the overall impact. " +
        "Be specific about field names, values, or logic that changed.");
    history.AddUserMessage(
        $"--- BEFORE ({before.Name}) ---\n```\n{TruncateIfLarge(beforeContent)}\n```\n\n" +
        $"--- AFTER ({after.Name}) ---\n```\n{TruncateIfLarge(afterContent)}\n```\n\n" +
        $"Please analyze the differences between these two file versions.");

    await StreamResponseAsync(chat, history);
}

static async Task StreamResponseAsync(IChatCompletionService chat, ChatHistory history)
{
    try
    {
        // Use streaming for a better CLI experience when available
        await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(history))
        {
            if (chunk.Content is not null)
                Console.Write(chunk.Content);
        }
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        // Fall back to non-streaming if streaming is not supported
        try
        {
            var result = await chat.GetChatMessageContentAsync(history);
            Console.WriteLine(result.Content);
        }
        catch
        {
            Console.Error.WriteLine($"Error communicating with Ollama: {ex.Message}");
            Console.Error.WriteLine("Ensure Ollama is running and the model is pulled.");
            Console.Error.WriteLine($"  docker run -d -p 11434:11434 ollama/ollama");
            Console.Error.WriteLine($"  docker exec iothub-ollama ollama pull llama3");
            Environment.Exit(2);
        }
    }
}

/// <summary>
/// Truncates very large files to avoid hitting model context limits.
/// </summary>
static string TruncateIfLarge(string content, int maxChars = 12_000)
{
    if (content.Length <= maxChars)
        return content;

    return content[..maxChars] +
           $"\n\n[... truncated — original file is {content.Length:N0} characters ...]";
}
