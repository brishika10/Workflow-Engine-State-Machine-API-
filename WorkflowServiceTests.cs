using WorkflowEngine.Models;
using WorkflowEngine.Services;
using WorkflowEngine.Repositories;

namespace WorkflowEngine.Tests
{
    public class WorkflowServiceTests
    {
        private readonly IWorkflowService _workflowService;

        public WorkflowServiceTests()
        {
            var repository = new InMemoryWorkflowRepository();
            _workflowService = new WorkflowService(repository);
        }

        public async Task TestCreateSimpleWorkflow()
        {
            // Arrange
            var request = new WorkflowDefinitionRequest(
                "test-workflow",
                "Test Workflow",
                "A simple test workflow",
                new List<StateRequest>
                {
                    new("start", "Start", true, false),
                    new("end", "End", false, true)
                },
                new List<ActionRequest>
                {
                    new("complete", "Complete", true, new List<string> { "start" }, "end")
                }
            );

            // Act
            var definition = await _workflowService.CreateWorkflowDefinitionAsync(request);

            // Assert
            Console.WriteLine($"✓ Created workflow: {definition.Name}");
            Console.WriteLine($"  States: {definition.States.Count}");
            Console.WriteLine($"  Actions: {definition.Actions.Count}");
        }

        public async Task TestWorkflowExecution()
        {
            // Arrange - Create workflow
            var request = new WorkflowDefinitionRequest(
                "execution-test",
                "Execution Test Workflow",
                null,
                new List<StateRequest>
                {
                    new("draft", "Draft", true, false),
                    new("review", "Under Review", false, false),
                    new("approved", "Approved", false, true)
                },
                new List<ActionRequest>
                {
                    new("submit", "Submit for Review", true, new List<string> { "draft" }, "review"),
                    new("approve", "Approve", true, new List<string> { "review" }, "approved")
                }
            );

            var definition = await _workflowService.CreateWorkflowDefinitionAsync(request);

            // Act - Start instance and execute actions
            var instance = await _workflowService.StartWorkflowInstanceAsync(definition.Id);
            Console.WriteLine($"✓ Started instance {instance.Id} in state: {instance.CurrentStateId}");

            // Execute first action
            var updatedInstance = await _workflowService.ExecuteActionAsync(instance.Id, "submit");
            Console.WriteLine($"✓ Executed 'submit' action, now in state: {updatedInstance.CurrentStateId}");

            // Execute second action
            var finalInstance = await _workflowService.ExecuteActionAsync(updatedInstance.Id, "approve");
            Console.WriteLine($"✓ Executed 'approve' action, now in state: {finalInstance.CurrentStateId}");
            Console.WriteLine($"  History entries: {finalInstance.History.Count}");
        }

        public async Task TestValidationErrors()
        {
            // Test duplicate state IDs
            var invalidRequest = new WorkflowDefinitionRequest(
                "invalid-workflow",
                "Invalid Workflow",
                null,
                new List<StateRequest>
                {
                    new("state1", "State 1", true, false),
                    new("state1", "Duplicate State", false, true) // Duplicate ID
                },
                new List<ActionRequest>()
            );

            try
            {
                await _workflowService.CreateWorkflowDefinitionAsync(invalidRequest);
                Console.WriteLine("✗ Should have failed validation");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"✓ Correctly caught validation error: {ex.Message}");
            }

            // Test no initial state
            var noInitialRequest = new WorkflowDefinitionRequest(
                "no-initial-workflow",
                "No Initial Workflow",
                null,
                new List<StateRequest>
                {
                    new("state1", "State 1", false, false), // Not initial
                    new("state2", "State 2", false, true)   // Not initial
                },
                new List<ActionRequest>()
            );

            try
            {
                await _workflowService.CreateWorkflowDefinitionAsync(noInitialRequest);
                Console.WriteLine("✗ Should have failed validation");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"✓ Correctly caught validation error: {ex.Message}");
            }
        }

        public async Task TestInvalidActionExecution()
        {
            // Arrange - Create workflow
            var request = new WorkflowDefinitionRequest(
                "invalid-action-test",
                "Invalid Action Test",
                null,
                new List<StateRequest>
                {
                    new("start", "Start", true, false),
                    new("middle", "Middle", false, false),
                    new("end", "End", false, true)
                },
                new List<ActionRequest>
                {
                    new("next", "Next Step", true, new List<string> { "start" }, "middle"),
                    new("finish", "Finish", true, new List<string> { "middle" }, "end")
                }
            );

            var definition = await _workflowService.CreateWorkflowDefinitionAsync(request);
            var instance = await _workflowService.StartWorkflowInstanceAsync(definition.Id);

            // Test invalid action from current state
            try
            {
                await _workflowService.ExecuteActionAsync(instance.Id, "finish"); // Can't finish from start
                Console.WriteLine("✗ Should have failed action execution");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✓ Correctly prevented invalid action: {ex.Message}");
            }

            // Test non-existent action
            try
            {
                await _workflowService.ExecuteActionAsync(instance.Id, "non-existent");
                Console.WriteLine("✗ Should have failed action execution");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"✓ Correctly caught non-existent action: {ex.Message}");
            }
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Running Workflow Engine Tests...\n");
            
            var tests = new WorkflowServiceTests();
            
            try
            {
                await tests.TestCreateSimpleWorkflow();
                Console.WriteLine();
                
                await tests.TestWorkflowExecution();
                Console.WriteLine();
                
                await tests.TestValidationErrors();
                Console.WriteLine();
                
                await tests.TestInvalidActionExecution();
                Console.WriteLine();
                
                Console.WriteLine("All tests completed successfully! ✓");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
