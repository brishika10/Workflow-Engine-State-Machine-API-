using WorkflowEngine.Models;
using WorkflowEngine.Repositories;

namespace WorkflowEngine.Services
{
    public interface IWorkflowService
    {
        Task<WorkflowDefinition> CreateWorkflowDefinitionAsync(WorkflowDefinitionRequest request);
        Task<WorkflowDefinition?> GetWorkflowDefinitionAsync(string id);
        Task<List<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync();
        Task<WorkflowInstance> StartWorkflowInstanceAsync(string definitionId);
        Task<WorkflowInstance?> GetWorkflowInstanceAsync(string id);
        Task<List<WorkflowInstance>> GetAllWorkflowInstancesAsync();
        Task<WorkflowInstance> ExecuteActionAsync(string instanceId, string actionId);
    }

    public class WorkflowService : IWorkflowService
    {
        private readonly IWorkflowRepository _repository;

        public WorkflowService(IWorkflowRepository repository)
        {
            _repository = repository ?? new InMemoryWorkflowRepository();
        }

        public async Task<WorkflowDefinition> CreateWorkflowDefinitionAsync(WorkflowDefinitionRequest request)
        {
            ValidateWorkflowDefinition(request);

            var definition = new WorkflowDefinition
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                States = request.States.Select(s => new State
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsInitial = s.IsInitial,
                    IsFinal = s.IsFinal,
                    Enabled = s.Enabled,
                    Description = s.Description
                }).ToList(),
                Actions = request.Actions.Select(a => new Action
                {
                    Id = a.Id,
                    Name = a.Name,
                    Enabled = a.Enabled,
                    FromStates = a.FromStates,
                    ToState = a.ToState,
                    Description = a.Description
                }).ToList()
            };

            return await _repository.SaveWorkflowDefinitionAsync(definition);
        }

        public async Task<WorkflowDefinition?> GetWorkflowDefinitionAsync(string id)
        {
            return await _repository.GetWorkflowDefinitionAsync(id);
        }

        public async Task<List<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync()
        {
            return await _repository.GetAllWorkflowDefinitionsAsync();
        }

        public async Task<WorkflowInstance> StartWorkflowInstanceAsync(string definitionId)
        {
            var definition = await _repository.GetWorkflowDefinitionAsync(definitionId);
            if (definition == null)
            {
                throw new ArgumentException($"Workflow definition with ID '{definitionId}' not found.");
            }

            var initialState = definition.States.FirstOrDefault(s => s.IsInitial);
            if (initialState == null)
            {
                throw new InvalidOperationException($"Workflow definition '{definitionId}' has no initial state.");
            }

            var instance = new WorkflowInstance
            {
                Id = Guid.NewGuid().ToString(),
                DefinitionId = definitionId,
                CurrentStateId = initialState.Id
            };

            return await _repository.SaveWorkflowInstanceAsync(instance);
        }

        public async Task<WorkflowInstance?> GetWorkflowInstanceAsync(string id)
        {
            return await _repository.GetWorkflowInstanceAsync(id);
        }

        public async Task<List<WorkflowInstance>> GetAllWorkflowInstancesAsync()
        {
            return await _repository.GetAllWorkflowInstancesAsync();
        }

        public async Task<WorkflowInstance> ExecuteActionAsync(string instanceId, string actionId)
        {
            var instance = await _repository.GetWorkflowInstanceAsync(instanceId);
            if (instance == null)
            {
                throw new ArgumentException($"Workflow instance with ID '{instanceId}' not found.");
            }

            var definition = await _repository.GetWorkflowDefinitionAsync(instance.DefinitionId);
            if (definition == null)
            {
                throw new InvalidOperationException($"Workflow definition '{instance.DefinitionId}' not found.");
            }

            var action = definition.Actions.FirstOrDefault(a => a.Id == actionId);
            if (action == null)
            {
                throw new ArgumentException($"Action '{actionId}' not found in workflow definition.");
            }

            if (!action.Enabled)
            {
                throw new InvalidOperationException($"Action '{actionId}' is disabled.");
            }

            var currentState = definition.States.FirstOrDefault(s => s.Id == instance.CurrentStateId);
            if (currentState == null)
            {
                throw new InvalidOperationException($"Current state '{instance.CurrentStateId}' not found in workflow definition.");
            }

            if (currentState.IsFinal)
            {
                throw new InvalidOperationException($"Cannot execute actions on final state '{currentState.Id}'.");
            }

            if (!action.FromStates.Contains(instance.CurrentStateId))
            {
                throw new InvalidOperationException($"Action '{actionId}' cannot be executed from current state '{instance.CurrentStateId}'.");
            }

            var targetState = definition.States.FirstOrDefault(s => s.Id == action.ToState);
            if (targetState == null)
            {
                throw new InvalidOperationException($"Target state '{action.ToState}' not found in workflow definition.");
            }

            // Execute the action
            var historyEntry = new HistoryEntry
            {
                ActionId = action.Id,
                ActionName = action.Name,
                FromStateId = instance.CurrentStateId,
                ToStateId = action.ToState
            };

            instance.History.Add(historyEntry);
            instance.CurrentStateId = action.ToState;
            instance.LastModified = DateTime.UtcNow;

            return await _repository.SaveWorkflowInstanceAsync(instance);
        }

        private void ValidateWorkflowDefinition(WorkflowDefinitionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                throw new ArgumentException("Workflow definition ID is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Workflow definition name is required.");
            }

            if (request.States == null || !request.States.Any())
            {
                throw new ArgumentException("Workflow definition must have at least one state.");
            }

            if (request.Actions == null)
            {
                throw new ArgumentException("Workflow definition must have actions collection (can be empty).");
            }

            // Check for duplicate state IDs
            var duplicateStates = request.States.GroupBy(s => s.Id).Where(g => g.Count() > 1).ToList();
            if (duplicateStates.Any())
            {
                throw new ArgumentException($"Duplicate state IDs found: {string.Join(", ", duplicateStates.Select(g => g.Key))}");
            }

            // Check for duplicate action IDs
            var duplicateActions = request.Actions.GroupBy(a => a.Id).Where(g => g.Count() > 1).ToList();
            if (duplicateActions.Any())
            {
                throw new ArgumentException($"Duplicate action IDs found: {string.Join(", ", duplicateActions.Select(g => g.Key))}");
            }

            // Check for exactly one initial state
            var initialStates = request.States.Where(s => s.IsInitial).ToList();
            if (initialStates.Count != 1)
            {
                throw new ArgumentException($"Workflow definition must have exactly one initial state. Found {initialStates.Count}.");
            }

            // Validate state references in actions
            var stateIds = request.States.Select(s => s.Id).ToHashSet();
            foreach (var action in request.Actions)
            {
                if (string.IsNullOrWhiteSpace(action.Id))
                {
                    throw new ArgumentException("Action ID is required.");
                }

                if (string.IsNullOrWhiteSpace(action.Name))
                {
                    throw new ArgumentException("Action name is required.");
                }

                if (!stateIds.Contains(action.ToState))
                {
                    throw new ArgumentException($"Action '{action.Id}' references unknown target state '{action.ToState}'.");
                }

                foreach (var fromState in action.FromStates)
                {
                    if (!stateIds.Contains(fromState))
                    {
                        throw new ArgumentException($"Action '{action.Id}' references unknown source state '{fromState}'.");
                    }
                }
            }
        }
    }
}
