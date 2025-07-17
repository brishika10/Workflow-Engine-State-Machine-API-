using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<IWorkflowService, WorkflowService>();
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.WriteIndented = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();

// Workflow Definition endpoints
app.MapPost("/api/workflows", async (WorkflowDefinitionRequest request, IWorkflowService workflowService) =>
{
    try
    {
        var definition = await workflowService.CreateWorkflowDefinitionAsync(request);
        return Results.Created($"/api/workflows/{definition.Id}", definition);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/workflows/{id}", async (string id, IWorkflowService workflowService) =>
{
    var definition = await workflowService.GetWorkflowDefinitionAsync(id);
    return definition != null ? Results.Ok(definition) : Results.NotFound();
});

app.MapGet("/api/workflows", async (IWorkflowService workflowService) =>
{
    var definitions = await workflowService.GetAllWorkflowDefinitionsAsync();
    return Results.Ok(definitions);
});

// Workflow Instance endpoints
app.MapPost("/api/workflows/{definitionId}/instances", async (string definitionId, IWorkflowService workflowService) =>
{
    try
    {
        var instance = await workflowService.StartWorkflowInstanceAsync(definitionId);
        return Results.Created($"/api/instances/{instance.Id}", instance);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/instances/{id}", async (string id, IWorkflowService workflowService) =>
{
    var instance = await workflowService.GetWorkflowInstanceAsync(id);
    return instance != null ? Results.Ok(instance) : Results.NotFound();
});

app.MapGet("/api/instances", async (IWorkflowService workflowService) =>
{
    var instances = await workflowService.GetAllWorkflowInstancesAsync();
    return Results.Ok(instances);
});

app.MapPost("/api/instances/{instanceId}/actions/{actionId}", async (string instanceId, string actionId, IWorkflowService workflowService) =>
{
    try
    {
        var instance = await workflowService.ExecuteActionAsync(instanceId, actionId);
        return Results.Ok(instance);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

// Request DTOs
public record WorkflowDefinitionRequest(
    string Id,
    string Name,
    string? Description,
    List<StateRequest> States,
    List<ActionRequest> Actions
);

public record StateRequest(
    string Id,
    string Name,
    bool IsInitial,
    bool IsFinal,
    bool Enabled = true,
    string? Description = null
);

public record ActionRequest(
    string Id,
    string Name,
    bool Enabled,
    List<string> FromStates,
    string ToState,
    string? Description = null
);
