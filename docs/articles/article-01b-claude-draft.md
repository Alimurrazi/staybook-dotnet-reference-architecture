# Building Staybook with Claude Code

*Staybook, part 1b · Code: tag [`article-01`](https://github.com/Alimurrazi/staybook-dotnet-reference-architecture/tree/article-01), folder [`.claude/`](https://github.com/Alimurrazi/staybook-dotnet-reference-architecture/tree/article-01/.claude) · Full details: [article 1 report](../reports/article-01-designing-and-setting-up-staybook.md), section 7*

[Article 1](article-01-claude-draft.md) designed Staybook and set up its solution: a modular monolith, three module skeletons, a strict build and architecture tests. All of it was built with Claude Code. This article shows the harness that kept it on track: every file, what it does and why it's there.

The harness is optional. Delete `.claude/` and the code works exactly the same. But if a reference architecture is built with an AI assistant, readers should see exactly how that assistant is constrained.

## The idea in one line

**`CLAUDE.md` guides; tests, analyzers and hooks enforce.**

Instructions are suggestions to a model. It reads them, mostly follows them, and sometimes doesn't. A failing build is not a suggestion. So the harness has two layers: plain-language rules that make the right thing likely, and mechanical checks that make the wrong thing fail.

| Piece | Where | Job |
|---|---|---|
| `CLAUDE.md` files | Repo root, each module | Say what the architecture is |
| Permissions | `.claude/settings.json` | Decide what Claude may run without asking |
| Hooks | `.claude/hooks/*.cs` | Run checks Claude can't skip |
| Skills | `.claude/skills/` | Scaffold code with the rules built in |
| Subagents | `.claude/agents/` | Write tests first; review before a human does |

## `CLAUDE.md`: written before any code

The root `CLAUDE.md` was the first file in the repo after the plan. It holds what a new team member needs on day one:

- the build, test and run commands;
- the solution layout and the dependency rule;
- the three ways modules may talk to each other;
- the rules that must never break: no module reads another's tables, the server always calculates prices, a payment timeout is never a failure, and time comes from `TimeProvider`;
- the conventions: Wolverine, no AutoMapper, structured logging, central package versions.

Each module also has its own `CLAUDE.md` next to its code. Claude Code loads it only when it works in that folder, so the rules arrive exactly where they apply. Pricing's includes:

```markdown
- **The server always calculates prices.** No command or endpoint accepts a price; clients send a `QuoteId`.
- A quote is **immutable** once created: listing, stay, guest count, line items, currency, pricing version, owner and expiry. Changing the plan never changes an existing quote.
- The two pricing examples in plan section 5 are tests with their exact numbers. If a change breaks them, the change is wrong.
```

Writing these before any code is a design exercise in its own right. If you can't write a rule in plain words, you can't expect a model, or a colleague, to follow it.

## Permissions: what runs without asking

`.claude/settings.json` is checked into the repo, so everyone works under the same rules:

- **Allowed:** `dotnet build`, `test`, `format` and `restore`; read-only `git` commands; `git add`, `git commit` and `git switch -c`; starting the AppHost.
- **Denied:** reading or editing `.env` files, `secrets.json`, certificates and keys; `git push --force`; `git reset --hard`.
- **Everything else asks**, including `git push` and `dotnet add package`. Adding a library is a decision, not a keystroke.

The first version allowed `git branch *` and `git switch *`. The review subagent (below) pointed out that those also allow `git branch -D` and `git switch --discard-changes`. Permissions that look read-only aren't always.

## Hooks: rules the model can't skip

Hooks are commands Claude Code runs at fixed moments. Staybook's are written in C# as file-based apps (`dotnet run hook.cs`), so they need nothing but the .NET SDK. The first run of each compiles it, which takes about 30 to 40 seconds; after that they start in under a second.

| Hook | When | What it does |
|---|---|---|
| `guard-edits.cs` | Before any edit | Blocks edits to generated code, to DbUp scripts git already tracks, and to secrets |
| `build-after-edit.cs` | After a C# or MSBuild edit | Builds the owning project; errors go straight back to Claude |
| `test-before-stop.cs` | When Claude wants to stop | Runs the tests affected by the branch; if they fail, Claude keeps working |

The build hook earned its place while article 1 was being built. I registered three modules in `Program.cs`, and the `using` directives didn't land. Six compile errors came back before I moved on. The same hook exposed a bad formatter default, a blank line between every group of `using` directives, the first time it reformatted a file.

The Stop hook has a loop guard: if Claude is already continuing because of the hook, the hook lets it stop. A test it can't fix becomes a question for a human, not an endless cycle.

### A hook that didn't work out

For a while there were two more hooks. One reminded Claude to commit once uncommitted work grew past 12 files or 400 lines. The other made the Stop hook refuse to finish while finished work was uncommitted. They worked: the history is a clean sequence of steps.

Then the author started a draft of their own in the repo, and the Stop hook complained about that file at every single stop. A hook can't tell *whose* changes are uncommitted, or whether they're finished. Both hooks were removed, and committing each finished step became a rule in `CLAUDE.md` instead.

The lesson: **a hook should enforce only what it can judge correctly every time.** "Does it build?" and "do the tests pass?" qualify. "Is this work finished, and is it mine?" doesn't.

## Skills: procedures, not templates

Four skills, invoked as slash commands:

| Skill | Creates |
|---|---|
| `/new-value-object` | An immutable, always-valid value object, test-first |
| `/new-aggregate` | An aggregate, starting from "which rules must hold in one transaction?" |
| `/new-command` | A command or query with its Wolverine handler and validator |
| `/new-endpoint` | An endpoint following the API conventions, with its authorization policy declared |

A skill isn't a code template. It's a procedure: where the file goes, which questions to answer first, which tests to write before the code, and which subagent checks the result. The concrete code shapes come from the first real examples in articles 2 and 3.

## Subagents: a test writer and a reviewer

**`test-writer`** writes failing tests before the implementation exists. Its job ends when the tests compile and fail *for the right reason*: because the behavior is missing, not because of a typo. It's first used in article 2.

**`architecture-reviewer`** can't edit anything; it only reports, with file and line. Before the author saw article 1, it reviewed the whole branch and came back with fifteen findings. The best ones:

- **The article's own "break something" exercise broke nothing.** It read a `const` from another module, and the compiler copies a const's value, leaving no dependency for the tests to find. A new test now checks project references directly.
- **The Stop hook looked for module tests in the wrong folder**, so it would never have run article 2's tests. With no CI yet, that hook is the only gate.
- **Two template packages had arrived early**, ahead of the article that needs them.

Every finding was checked against the plan before anything changed. Twelve applied, two applied in part, and the rest went to the author as decisions. A reviewer's report is input, not instructions.

## What's not there yet: MCP servers

MCP servers connect Claude to outside tools. The plan allows them only when they earn their place, and nothing has yet: `git` and `gh` already cover GitHub, and there's no data worth querying. A read-only PostgreSQL connection becomes useful once article 3 stores data.

## The working loop

> plan → write the failing test → implement → hooks and tests verify → commit → `architecture-reviewer` → human review

The order matters. Cheap mechanical checks run on every edit and every stop. Judgment ("does this design make sense?") comes last, from the reviewer and then a human, once the mechanical problems are already gone.

## Try it

Install [Claude Code](https://claude.com/claude-code), clone the repo at `article-01` and open it. The project settings load automatically; run `/hooks` to see the three hooks. Then ask Claude to add a method to a Listings class and watch the build hook run. Or ask it to edit something in an `obj/` folder and read why it's refused.

**Next:** article 2 puts `test-writer` and the first two skills to work on the domain model.
