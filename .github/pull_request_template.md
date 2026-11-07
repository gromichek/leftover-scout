## Task

T-NNN: <!-- link to docs/tasks/T-NNN-*.md -->

## Summary

<!-- What changed and why, in a few lines. -->

## Checklist

- [ ] Only the task's scope is implemented (anything else is under "Follow-ups").
- [ ] `dotnet build LeftoverScout.slnx -c Release` and `dotnet test LeftoverScout.slnx -c Release` pass locally (or explain why they could not be run).
- [ ] New/changed logic is covered by tests; tests never touch a real user profile.
- [ ] Safety rules from CLAUDE.md respected (scan read-only, deletion only via the cleanup service, no reparse points, no cloud hydration).
- [ ] No personal data, real paths, secrets or tokens in code, tests, docs or screenshots.
- [ ] New packages: none / listed below with license (MIT, Apache-2.0 or BSD only) and added to ARCHITECTURE.md.
- [ ] User guide (`docs/user-guide/`) updated if user-visible behavior changed.
- [ ] Everything is in English.

## New packages

<!-- Package — version — license — reason. Or "None". -->

## Documentation and sources checked

<!-- Framework docs or source code you relied on (CLAUDE.md, "Code conventions"). -->

## Open questions

<!-- Anything unclear in the task. Do not guess. -->

## Follow-ups

<!-- Issues noticed outside the task's scope. -->
