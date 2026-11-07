# T-018: In-app Help (English + Russian)

- **Phase:** 1 — Read-only scan (inserted before T-003)
- **Branch:** `t-018-in-app-help` (or the branch your session assigned)
- **Depends on:** T-002 (merged)

## Goal

The user can read short, practical help inside the app — in English or Russian — from a Help
section in the navigation and from a **?** button (or **F1**) on every page, which opens the help
for exactly that page. The help files in the repository and in the app are the same files, so
the help can always be kept in sync with the app (D-026).

## Context

- Read `CLAUDE.md` (sections "Language" and "Help — mandatory"), `docs/ARCHITECTURE.md`,
  decisions D-024 … D-030 in `docs/LEAD_HANDOVER.md`.
- Current help: `docs/user-guide/index.md`, `getting-started.md`, `scanning.md` (English only).
- Pages today: Scan, Results (placeholder), Cleanup (placeholder), History (placeholder), About.

## Owner decisions for this task

- **D-027** Help source = **Markdown files** in `docs/user-guide/en/` and `docs/user-guide/ru/`,
  **embedded** into the exe; parsed with **Markdig** (BSD-2-Clause), rendered by our own small
  Markdown → WPF `FlowDocument` renderer.
- **D-028** Help language at first open follows the **Windows UI language** (Russian → RU,
  anything else → EN). An **EN / RU** switch on the Help page; the choice is remembered in settings.
- **D-029** The **UI stays English**. The Russian help names buttons and pages exactly as they
  appear in the UI, in bold (e.g. «нажмите **Start scan**»).
- **D-030** Every page has a **?** button in its top-right corner, and **F1** does the same: both
  open the Help page at the topic of the current page.

## Scope

### 1. Help files (one topic per file, same file names in both languages)
- Move the existing files to `docs/user-guide/en/` and restructure into topics:
  - `index.md` — Overview: what the app does, **order of work** (Scan → Results → Cleanup →
    History), what is safe, requirements;
  - `getting-started.md` — starting, single instance, data folder, info bar, damaged settings;
  - `scan.md` — the Scan page (current behavior from T-002: every button, every number in the
    summary, cancel, what personal / cloud-only / skipped links / inaccessible mean, tips);
  - `results.md`, `cleanup.md`, `history.md` — one short paragraph each: what the page will do
    and that it is not available yet (say "planned", not a phase number);
  - `about.md` — the About page;
  - `help.md` — how to use Help: ? button, F1, language switch.
- Create `docs/user-guide/ru/` with **the same files**, a faithful Russian translation.
  UI names stay in English and bold (D-029).
- Keep the text short and practical: what to do, in which order, what to expect, tips.
  Describe **only what the app really does now** (D-026).
- Internal links between topics use relative file links (e.g. `[Scan](scan.md)`), so they work
  both on GitHub and in the app.
- Update `README.md` links to `docs/user-guide/en/index.md` and add the Russian index link.
- Allowed Markdown subset (document it in ARCHITECTURE.md): headings `#`…`###`, paragraphs,
  **bold**, *italic*, `inline code`, bullet and numbered lists (one nesting level), pipe tables,
  block quotes, links. No images, no raw HTML.

### 2. Embedding and loading
- Embed all `docs/user-guide/**/*.md` as resources of `LeftoverScout.App` (link the files from
  the project; do not copy them). Use explicit `LogicalName`s like `Help/en/scan.md`.
- Core: help model and lookup that do not depend on WPF, e.g. `HelpTopic` (id, file name) and a
  `HelpCatalog` that returns the Markdown text for (topic id, language) via an `IHelpSource`
  interface; missing Russian topic → fall back to English and log a warning.
- Topic ids: `overview`, `getting-started`, `scan`, `results`, `cleanup`, `history`, `about`, `help`.

### 3. Renderer (App)
- Markdig (latest stable, central version) → our renderer → `FlowDocument`, covering exactly the
  allowed subset. Unknown elements are rendered as plain text (never crash).
- Use the Fluent light theme resources for text/heading styles; readable line length.
- Links: to another `*.md` topic → navigate inside Help; `https://` → open in the browser via
  `IShellLauncher`; anything else → ignored.

### 4. Help page and navigation
- New navigation item **Help** (between History and About).
- Help page: topic list on the left (in the order of section 1), content on the right
  (`FlowDocumentScrollViewer` or similar, scrollable), **EN / RU** switch at the top.
- Language: `AppSettings` gets `HelpLanguage` (null = automatic). Automatic = `CurrentUICulture`
  two-letter name `ru` → RU, otherwise EN (D-028). Changing the switch saves the setting.
- `PageViewModel` gets a `HelpTopicId`; every page sets it. A **?** button in the top-right
  corner of every page (put it into a shared page header so new pages get it automatically)
  and an **F1** key binding on the main window open Help at the current page's topic (D-030).
  On the Help page itself, F1 opens the `help` topic.

### 5. Collected finding (PROGRESS #3)
- Move `MonitorWorkAreas` from `LeftoverScout.Windows.Scanning` to a `UI` namespace/folder.

### 6. Tests
- Every topic id exists in **both** languages (as embedded resources).
- Every file in `docs/user-guide/en/` has a counterpart in `ru/` and vice versa.
- Every page view model has a `HelpTopicId` that exists in the catalog.
- Every relative link in every help file points to an existing topic file.
- Every help file renders without exception (WPF objects need an STA thread — run the render on a
  dedicated STA thread inside the test).
- Language selection: automatic (ru / other), explicit setting, fallback to English.
- Markdown subset: headings, lists, table, link, bold/italic produce the expected elements.

### 7. Documentation
- ARCHITECTURE.md: help system (files, embedding, catalog, renderer, allowed Markdown subset,
  how to add a topic for a new page), Markdig in "Third-party packages".
- Do **not** edit `CLAUDE.md`; the lead updates the help paths there after the merge.

## Out of scope

- Translating the UI (D-029), search in help, images in help, printing.

## Acceptance criteria

- [ ] Help section in the navigation; topics listed; EN / RU switch works and is remembered.
- [ ] First start on a Russian Windows shows Russian help; otherwise English.
- [ ] **?** on every page and **F1** open the matching topic.
- [ ] English and Russian files exist for every topic and describe the current app accurately.
- [ ] Links between topics work in the app and on GitHub.
- [ ] Finding #3 done.
- [ ] Build and tests pass (Release); CI green. Markdig listed with license.

## Manual test steps for the owner

1. Start the app. Navigation shows **Help** between History and About.
2. Open **Help**: the language matches your Windows language; topics are listed on the left.
3. Switch **EN / RU** — the text changes; close and restart the app — the choice is kept.
4. Go to **Scan**, press **?** → Help opens at the Scan topic. Press **F1** on About → About topic.
5. Click a link inside a help topic (e.g. from Overview to Scan) → the topic opens in the app.
6. Read the Russian Scan topic and check that it matches what the Scan page really does.
7. Open `docs/user-guide/ru/index.md` on GitHub — the same text, links work.

## Notes for the executor

- Read the Markdig docs (AST: `MarkdownDocument`, `HeadingBlock`, `ParagraphBlock`, `ListBlock`,
  `Table` from `UsePipeTables`, `LinkInline`, `EmphasisInline`, `CodeInline`) and WPF
  `FlowDocument` docs (`Paragraph`, `List`, `Table`, `Hyperlink`). Cite them in the PR.
- `Hyperlink` inside a `FlowDocument` needs `RequestNavigate`/`Click` handling; route it to the
  view model (no logic in code-behind beyond forwarding).
- Embedded resource names: check how MSBuild builds `LogicalName` with `%(RecursiveDir)` (backslashes)
  and normalize, or list the files explicitly.
- Russian text: natural, short sentences; keep UI terms in English and bold.
