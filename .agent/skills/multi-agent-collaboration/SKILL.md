# Skill: Multi-Agent Parallel Collaboration

## Objective

Guidance for splitting a single body of work across two or more agents running at the same
time — each in its own isolated git worktree — instead of one agent working sequentially.
Written from a real incident on this project (see §10), not derived in the abstract.

Parallel agents buy wall-clock time. They cost coordination overhead: every file two agents
might both touch is a future merge conflict, and every assumption one agent makes about "what
the other one will handle" is a place the two can silently diverge. Use this skill to decide
*whether* to parallelize, *how* to split the work so the coordination cost stays low, and how
to reconcile the result safely.

---

## 1. When to parallelize (and when not to)

Parallelize when:

- The work has two or more genuinely independent workstreams — independent meaning "a person
  could review and merge either one without reading the other," not just "different files."
- Each workstream is large enough that sequential execution would visibly cost wall-clock time.
- The task doesn't require a single continuous train of design reasoning (design decisions
  should usually be made once, by one thread, and handed to the parallel agents as fixed
  input — not re-derived independently by each).

Don't parallelize when:

- The "workstreams" only look independent at a glance but actually share a central
  orchestration point (e.g. one handler class both would need to modify substantially, not
  just touch a line of). If splitting the work honestly requires one agent to half-implement
  something the other agent finishes, that's one workstream, not two.
- The task produces a derived/generated artifact from shared state — see §5.
- The work is small enough that the coordination overhead (writing precise scoped prompts,
  reviewing two diffs, resolving conflicts) exceeds the time saved.

## 2. Splitting work: by workstream, not by layer

Splitting "the frontend agent" vs. "the backend agent," or "the Domain agent" vs. the
"Infrastructure agent," sounds clean but is usually wrong for a single feature — a real change
typically ripples through every layer (Domain → Application → Infrastructure), so a
layer-based split guarantees both agents touch the same files at different points in the same
pipeline.

Split by **feature/workstream** instead: pick a boundary where two features happen to be
implemented across the same layers but don't semantically depend on each other. On this
project, "Dealership becomes an internal service" and "Customer becomes a Value Object" each
touched Domain/Application/Infrastructure/tests, but neither one's logic depended on the
other's — that's what made them safe to parallelize.

## 3. Pre-flight: verify the worktree's actual base branch

**This is the single most important check, and the one that was skipped and caused a real
problem (§10).** A worktree-isolated agent's `isolation: "worktree"` setup creates a new
branch — verify, don't assume, which commit it forked from:

```bash
git worktree list                                   # shows path + branch per worktree
git merge-base <new-branch> <intended-base-branch>   # do they share the expected ancestor?
git log <intended-base-branch>..<new-branch>         # anything the new branch is missing?
```

Do this **before** trusting anything the agent reports about build/test success — a worktree
built on the wrong base can build and pass its own tests perfectly well while being based on
stale code. Passing tests prove internal consistency, not that the base was correct.

If a fresh-agent tool call doesn't expose which branch it started from, ask it to report
`git rev-parse HEAD` and `git log -1` as part of its own verification, and cross-check that
against the intended base yourself once it returns.

**Update — confirmed systematic, not a one-off (§10 recurred twice more, 4/4 total).** In this
environment, `isolation: "worktree"` has been observed every single time to branch from the
repository's default branch (here, `master`) regardless of what branch is actually checked out
in the main worktree. Treat this as the expected behavior until proven otherwise, not an edge
case to shrug off after the first miss:

- Bake the base-branch check into every agent's own prompt as a **hard stop condition** — tell
  it explicitly to verify before doing anything else, and to do nothing and report immediately
  if the base is wrong, rather than proceeding and flagging it in a final report. Both agents
  in the second incident did exactly this correctly, and the wasted cost was small — a base
  check and an early exit, not a wrong diff to untangle.
- If the working branch differs from the repository's default branch, seriously consider
  **not** using `isolation: "worktree"` at all for that work — do it directly, or create the
  worktree yourself with an explicit `git worktree add <path> -b <branch> <correct-base>` and
  point the agent at that existing path instead of letting the tool choose the base.
- Re-check this each time before relying on it again — a fixed default in a later tool version
  would make this section stale, and the check itself is cheap enough to keep doing regardless.

## 4. Identifying and bounding shared files

Before launching either agent, list every file both workstreams are likely to touch. On a
typical refactor this is a short, predictable list:

- The central orchestration/handler file both features flow through.
- The DI registration file (both features probably register something).
- The main `DbContext`/schema file (both features probably touch the same `DbSet` list).
- Any single shared test file that exercises the whole flow end-to-end.

For each shared file, tell each agent explicitly: *which lines are yours, which lines belong
to the other agent, and to leave those other lines completely untouched* — not "be careful
around", the literal instruction of which lines/members are off-limits. This doesn't eliminate
conflicts (each agent's own edit still differs from the file's original state, so git still
sees two diverging versions), but it keeps each conflict to a small, predictable, easy region
instead of a tangle.

Do **not** try to force a fully conflict-free split by having one agent avoid a shared file
entirely if it genuinely needs to change something there — a forced avoidance produces a worse
outcome (incomplete/inconsistent code) than a small, expected, manually-resolved conflict.

## 5. What NOT to parallelize: generated and snapshot-based artifacts

Some artifacts are computed as a diff against a single shared baseline, not as an independent
edit — parallelizing their generation doesn't produce two mergeable pieces, it produces two
*competing* full snapshots:

- **EF Core migrations.** A migration is generated by diffing the current model against
  `*ModelSnapshot.cs`. Two agents each generating one from the same stale snapshot will each
  produce a migration that's individually valid but a snapshot-file conflict when merged — the
  snapshot isn't a line-diffable format in any way that helps here. Defer migration generation
  to a single step, after both workstreams' model changes are merged.
- **Any other whole-file regenerated artifact** (a formatted lockfile, a generated OpenAPI
  spec, a compiled schema) has the same property — check before assuming it's safe to split.

Also don't parallelize edits to the **shared task-tracking document** (this project's
`TASKS.md`). Tell every agent not to touch it, and update it yourself once you've reviewed
both results — otherwise two agents racing to update the same status table is its own
conflict, and worse, an inaccurate one if either agent writes status before knowing whether
its own work actually survived reconciliation.

## 6. Prompting a forked worktree agent

If the parallel agents should inherit the current conversation's design reasoning (they
usually should — re-deriving the same architecture decisions independently risks the two
agents drifting to different interpretations), fork rather than spawn fresh:

```
Agent({
  subagent_type: "fork",
  isolation: "worktree",
  name: "<workstream-name>",
  prompt: "<directive prompt — what to do, not what the situation is>"
})
```

A fork already has the full conversation's context, so the prompt should be a **directive**:
concrete file paths, exact type/method names, and the explicit "shared file, only touch X"
boundaries from §4 — not a re-explanation of the design, which the agent already has.

Launch every parallel agent for one unit of work in a **single message** with multiple `Agent`
calls — that is what actually makes them run concurrently, not sequential calls across
multiple turns.

## 7. Per-agent verification bar

Each agent should reach a **fully green build and test suite inside its own worktree** before
reporting done — even though its worktree is missing the other workstream's changes. This is
achievable specifically because of the independence established in §1/§2: if the two
workstreams are genuinely orthogonal at runtime, each one's tests should pass on its own,
using the untouched (old) version of whatever the other workstream would eventually change.
A worktree that *doesn't* reach green on its own is a signal the split wasn't as independent
as assumed — investigate before merging, not after.

Explicitly tell each agent: commit the work (so it's mergeable), but don't push, and don't
generate the deferred artifacts from §5.

## 8. Reconciliation / merge process

1. Create a fresh integration branch off the **real, verified** base branch (see §3) — not off
   whatever the parallel agents happened to branch from.
2. Merge each agent branch in one at a time. Expect the shared files from §4 to conflict;
   expect everything else to merge cleanly if the split was genuinely independent.
3. Resolve each conflict by combining both sides' intent, not by picking one side — usually
   this means deleting whatever each side independently marked for deletion, and keeping both
   sides' additions.
4. Generate any deferred artifact (§5) once, against the now-merged model.
5. Run the **full** build and test suite on the integration branch — this is the one point
   where the two workstreams' interaction is actually exercised together for the first time.
6. Update the shared tracking document yourself, recording what happened — including any
   mistake found and how it was fixed (see §10 for why this matters).
7. Delete the worktrees and their branches once merged.

## 9. Cleanup

```bash
git worktree remove <path>          # add --force if the agent left uncommitted scratch state
git branch -d <worktree-branch>     # -d refuses if genuinely unmerged — that's a real signal, don't -D past it
```

A `-d` refusal that surprises you means the branch has commits the integration branch doesn't
— re-check before forcing.

## 10. Real incident: the wrong base branch

On this project, two agents were launched with `isolation: "worktree"` to parallelize a
refactor, on the assumption each would branch from the session's actual working branch. Both
instead branched from `master`, which was six commits behind — missing an entire
already-shipped feature (a response-envelope wrapper). One agent's workstream didn't intersect
that missing feature and was unaffected. The other agent's workstream *did* intersect it: its
tests silently adapted around the missing feature instead of failing loudly, producing code
that was internally consistent but wrong relative to the real target codebase.

This was caught **before merging** by explicitly diffing branch ancestry (§3), not by a test
failure — the agent's own test suite was green precisely because it was self-consistent with
its own (wrong) starting point. That's the core lesson: a parallel agent's "build and tests
pass" claim only proves internal consistency with whatever it actually started from — it says
nothing about whether that starting point was the right one. Verify the starting point
independently, every time, before trusting the result.

## 11. Checklist

- [ ] Confirmed the workstreams are independent at the feature level, not just "different files"
- [ ] Identified every shared file and defined explicit per-agent line ownership in each
- [ ] Confirmed no step requires generating a snapshot/diff-based artifact independently
- [ ] Each agent prompt is directive (fork) or fully self-contained (fresh), not ambiguous
- [ ] Verified each worktree's actual base branch/commit before trusting any of its output
- [ ] Each agent reached green build+test inside its own worktree
- [ ] Reconciliation merged onto the *verified* real base, not onto whatever the agents used
- [ ] Full build+test run once on the merged integration branch
- [ ] Shared tracking document updated centrally, once, after reviewing both results
- [ ] Worktrees and branches cleaned up after a successful merge
