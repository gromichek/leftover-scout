# Tasks

Each file `T-NNN-short-name.md` describes exactly one executor session.

- Written by the lead from `_TEMPLATE.md` and merged into `main` before the session starts.
- The executor works only on the task's scope and follows `CLAUDE.md`.
- Large work is split into parts: `T-NNNa-…`, `T-NNNb-…`.
- After the PR is merged the task file stays as history; its status lives in `docs/ROADMAP.md`.

## How the owner starts a session

Start a new Claude Code session on this repository with a prompt like:

```
Execute task docs/tasks/T-NNN-short-name.md. Follow CLAUDE.md.
```
