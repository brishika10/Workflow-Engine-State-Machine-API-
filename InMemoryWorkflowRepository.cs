using WorkflowEngine.Models;
using System.Collections.Concurrent;

namespace WorkflowEngine.Repositories
{
    public interface IWorkflowRepository
    {
        Task<WorkflowDefinition> SaveWorkflowDefinitionAsync(WorkflowDefinition definition);
        Task<WorkflowDefinition?> GetWorkflowDefinitionAsync(string id);
        Task<List<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync();
        Task<WorkflowInstance> SaveWorkflowInstanceAsync(WorkflowInstance instance);
        Task<WorkflowInstance?> GetWorkflowInstanceAsync(string id);
        Task<List<WorkflowInstance>> GetAllWorkflowInstancesAsync();
    }

    public class InMemoryWorkflowRepository : IWorkflowRepository
    {
        private readonly ConcurrentDictionary<string, WorkflowDefinition> _definitions = new();
        private readonly ConcurrentDictionary<string, WorkflowInstance> _instances = new();

        public Task<WorkflowDefinition> SaveWorkflowDefinitionAsync(WorkflowDefinition definition)
        {
            _definitions.AddOrUpdate(definition.Id, definition, (key, old) => definition);
            return Task.FromResult(definition);
        }

        public Task<WorkflowDefinition?> GetWorkflowDefinitionAsync(string id)
        {
            _definitions.TryGetValue(id, out var definition);
            return Task.FromResult(definition);
        }

        public Task<List<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync()
        {
            return Task.FromResult(_definitions.Values.ToList());
        }

        public Task<WorkflowInstance> SaveWorkflowInstanceAsync(WorkflowInstance instance)
        {
            _instances.AddOrUpdate(instance.Id, instance, (key, old) => instance);
            return Task.FromResult(instance);
        }

        public Task<WorkflowInstance?> GetWorkflowInstanceAsync(string id)
        {
            _instances.TryGetValue(id, out var instance);
            return Task.FromResult(instance);
        }

        public Task<List<WorkflowInstance>> GetAllWorkflowInstancesAsync()
        {
            return Task.FromResult(_instances.Values.ToList());
        }
    }
}
