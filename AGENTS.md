# AGENTS.md

This repository is for small, issue-driven implementation work on TicketFlow.

## Codex Workflow Rules

1. Read the assigned GitHub Issue before making changes.
2. Work only on the assigned Issue.
3. Start from the Issue Suggested Files.
4. Keep changes small, reviewable, and limited to the Issue scope.
5. Do not continue to another Issue after opening a PR.

## Branch and PR Rules

- Use one branch per Issue.
- Prefer branch names such as `docs/issue-2-planning-docs`, `feature/issue-<number>-short-name`, or `fix/issue-<number>-short-name`.
- Open one PR per Issue.
- Link the PR to the Issue.
- Keep PR descriptions clear about summary, changed files, validation, risk, and out-of-scope work.
- Never force push unless the user explicitly requests it.

## Scope Control Rules

- Do not work on unrelated Issues.
- Do not modify unrelated files.
- Do not create frontend or backend application code unless the current Issue explicitly asks for it.
- Do not create `/frontend`, `/backend`, package files, lockfiles, Docker files, CI/CD files, deployment config, or extra docs unless the current Issue explicitly asks for them.
- Do not refactor, redesign, or restructure outside the Issue scope.
- Prefer simple, explicit, maintainable changes.

## Dependency Rules

- Do not add dependencies unless the Issue explicitly requires them.
- Do not modify package or lock files unless dependency or script work is explicitly in scope.
- Use the existing project tooling once project files exist.

## Secret Handling Rules

- Never commit secrets, API keys, tokens, credentials, database passwords, service keys, or local machine configuration.
- Do not commit `.env`, `.env.local`, or similar local configuration files.
- Use placeholder examples only when an Issue explicitly asks for environment documentation.

## Validation Failure Rules

- Run the validation requested by the Issue first.
- For planning-only or docs-only changes, manual inspection is acceptable when no project tooling exists.
- Do not fix unrelated validation failures.
- If the same command fails after two focused fix attempts, stop and report the failing command, exact error, changed files, suspected cause, and recommended next step.
