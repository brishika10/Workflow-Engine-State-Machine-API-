# Infonetica Assignment – Configurable Workflow Engine (State Machine API)

## Overview

This is my submission for the **Infonetica Software Engineer Intern Take-Home Assignment**.  
It implements a simple, configurable workflow engine using **.NET 8 (C#)** and **ASP.NET Core Minimal API**.

The API allows clients to:

- Define workflows as state machines with states and transitions (actions)
- Start new workflow instances from a defined workflow
- Execute actions to move instances between states
- Retrieve the current state and history of instances

All data is managed in-memory without a database.

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)

### Running the Application

```bash
git clone https://github.com/brishika10/Workflow-Engine-State-Machine-API-.git
cd INFONETICA_ASSIGNMENT
dotnet run

---

## API Endpoints

###  Create Workflow Definition  
**POST** `/api/workflow/definition`

Creates a workflow with the provided states and actions.

#### Sample Request:
```json
{
  "id": "exampleWorkflow",
  "states": [
    { "id": "Start", "isInitial": true, "isFinal": false, "enabled": true },
    { "id": "InProgress", "isInitial": false, "isFinal": false, "enabled": true },
    { "id": "Completed", "isInitial": false, "isFinal": true, "enabled": true }
  ],
  "actions": [
    { "id": "begin", "enabled": true, "fromStates": ["Start"], "toState": "InProgress" },
    { "id": "finish", "enabled": true, "fromStates": ["InProgress"], "toState": "Completed" }
  ]
}
```

---

###  Get Workflow Definition  
**GET** `/api/workflow/definition/{id}`

Returns the workflow definition for the given ID.

---

###  Start a Workflow Instance  
**POST** `/api/workflow/instance/{definitionId}`

Starts a new instance based on the given workflow definition. The instance begins at the workflow's initial state.

---


###  Execute Action on Instance  
**POST** `/api/workflow/instance/{instanceId}/action/{actionId}`

Executes a valid action on the specified instance and transitions it to the defined next state, only if:
- The action exists and is enabled
- The current state is listed in the action’s `fromStates`
- The instance is **not** in a final state

---

###  Get Instance Status  
**GET** `/api/workflow/instance/{instanceId}`

Returns the current state and the action history of the workflow instance.



