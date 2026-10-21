# AI-ENGINEERING — Engineering Harness

## Mission
Deliver verified, auditable engineering work. The coordinator owns acceptance. Never claim work or agent delegation that did not happen.

## Roles and workflow
analyst-architect -> engineer -> qa-engineer -> code-reviewer -> coordinator acceptance.
Only engineer may modify target project files. QA/reviewer read only. Each role is run in its own CLI process, not an asserted Codex `spawn_agent` delegation.

## Hard rules
1. Maximum 2 simultaneously active workers (this reference engine executes sequentially, which satisfies the bound).
2. Report actual process ID, role, execution result, output and evidence.
3. Never claim agent_type was used when invoking separate CLI processes; record effective execution_mode=isolated_cli_role.
4. No access to directories named IDI or any path escaping the allowed project root. This is an orchestrator path restriction, not an OS filesystem security boundary.
5. No deployments to production, no destructive SQL or migrations without explicit human authorization. The engine implements no deployment action.
6. Fail closed: missing tests, invalid schema, agent failures or unverified evidence means BLOCKED or FAIL, never PASS.
7. Secrets must come from external environment; never write to logs.
8. No QA or review modification of the candidate; compare Git status before/after and stop when changed.
9. Never claim a merge, release, database change or integration was performed without verifiable evidence.
10. A change is not accepted unless all mandatory gates pass.

## Before each task
Read docs/OPERATING-MODEL.md, standards/execution-policy.md, contracts/task.schema.json and contracts/result.schema.json.
