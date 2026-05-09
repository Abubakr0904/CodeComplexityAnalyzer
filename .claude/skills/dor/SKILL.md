---
name: dor
description: Definition of Ready check. Use to refine a task (issue, ticket, or feature request) into an agent-ready specification before planning or implementation. Asks targeted questions about objective, scope, constraints, relevant code areas, and acceptance criteria, then outputs a clean spec.
---

# Definition of Ready (DoR)

A task is "Ready" only when an engineer (human or agent) could pick it up
and start implementing without ambiguity. This skill walks through a
checklist of questions and produces a concrete specification you can
hand to `/plan`.

## When to use

- User invokes `/dor` (or asks for a DoR check)
- User pastes a GitHub issue / Jira ticket / Linear task / informal description
- About to enter Plan Mode but the task is fuzzy or one-line

## Inputs

The user typically provides one of:
- A GitHub issue URL or number (use `gh issue view <n>` to fetch)
- A Jira/Linear ticket reference (fetch via MCP if available)
- A free-text description

If only a number is given, fetch the full issue body before starting —
do not invent details from the title alone.

## Process

Work through these six dimensions. For each one, if the answer is
already clear from the issue, **state it and move on** — do not pester
the user with questions they have already answered. Only ask when
genuinely missing or ambiguous.

### 1. Objective
What outcome does this deliver, in one sentence?
- Why does this matter? (user pain, business need, dependency for X)
- What does success look like to a non-engineer?

### 2. Scope and non-scope
- What is explicitly **in** scope?
- What is explicitly **out** of scope? Name the temptations to creep
  (related cleanups, "while we're here" refactors, adjacent features).

### 3. Constraints
- Technical: language/runtime versions, frameworks, performance targets
- Compatibility: APIs that cannot break, on-disk formats, public surface
- Process: client tool/MCP allow-list, regulatory (PII, billing, etc.)
- Time: any deadline that changes the bar for "good enough"

### 4. Relevant code / context
- Which files, modules, or layers will likely be touched?
- Which existing patterns should the implementation follow?
- Are there tests or fixtures to reuse vs. write from scratch?
- Any prior PRs, ADRs, or docs the implementer should read first?

If the codebase has `docs/CODEBASE_MAP.md`, reference the relevant
sections rather than re-deriving structure from scratch.

### 5. Acceptance criteria
Concrete, checkable statements. Prefer the form:
- *"Given X, when Y, then Z"*, or
- *"`<command>` produces `<observable output>`"*

Each criterion should be falsifiable by a test, a manual check, or a
clear review judgment. Vague criteria ("should be fast", "should be
maintainable") need to be sharpened or dropped.

### 6. Risks and unknowns
- What could go wrong that the implementer should think about?
- What is uncertain enough to warrant a spike or proof-of-concept?
- Is there a high-risk area (security, data migration, perf-critical
  path) where extra review or non-AI implementation is preferred?

## Anti-patterns to flag

- "Just do it like before" without naming the prior example
- Acceptance criteria that are restatements of the title
- Scope written as "and anything else needed" — force a boundary
- Hidden assumptions that only the requester holds
- Tasks that are actually two tasks — split them

## Output

Produce a specification in this format. Place it directly in the chat
so the user can copy-paste it into `/plan` or paste it into the issue
as a refinement comment.

```markdown
# Spec: <short title>

**Source:** <issue link or "informal request">

## Objective
<one-sentence outcome + one-sentence why>

## In scope
- ...

## Out of scope
- ...

## Constraints
- ...

## Relevant code / context
- <file/module>: <why it matters>
- See: docs/CODEBASE_MAP.md § <section> (if applicable)

## Acceptance criteria
- [ ] ...
- [ ] ...

## Risks / unknowns
- ...

## Suggested validation
- <tests to add, manual checks to run, perf measurements, etc.>
```

## After producing the spec

- Ask the user to confirm or adjust before proceeding
- Once confirmed, suggest the next step (typically `/plan` with the
  spec as input, or entering Plan Mode)
- Do NOT start implementing — DoR is read-only and ends at the spec
