# Hunt the Wumpus — AI Edition

A Unity remake of the 1973 cave-crawler *Hunt the Wumpus*, where every hazard encounter is resolved
by an LLM-generated trivia duel instead of a dice roll.

> **Status:** playable prototype. Core loop, procedural cave generation, hazards, economy,
> leaderboard and mobile touch controls all work. Known issues are listed honestly at the bottom.

---

## Demo

No hosted build yet — the project runs from the Unity Editor (see [Running locally](#running-locally)).

| | |
|---|---|
| ![Cave map screenshot](docs/screenshot-map.png) | ![Trivia encounter screenshot](docs/screenshot-trivia.png) |
| The hex cave map with proximity warnings | An AI-generated trivia encounter |

---

## Overview

You are dropped into a 30-room cave laid out as a wrapping hexagonal grid. Somewhere in it lives the
Wumpus. Two bottomless pits and two colonies of giant bats are scattered through the other rooms.

You cannot see any of it. You can only sense what is in the rooms *adjacent* to you:

- *"I smell a wumpus!"* — the Wumpus is one room away
- *"I feel a breeze..."* — a pit is one room away
- *"Bats nearby?"* — bats are one room away

From those clues you deduce where the Wumpus is and shoot it with one of your three arrows before it
finds you, before you fall into a pit, and before you run out of coins.

**The twist:** walking into a hazard doesn't kill you outright. It opens a trivia duel. A language
model generates a multiple-choice question on the spot; answer more than half correctly and you
escape, get your arrows, or buy a hint. Fail and the run ends. The Wumpus itself taunts you in
character between questions, with dialogue generated live.

---

## Tech stack

| Layer | Technology |
|---|---|
| Engine | Unity **2022.3.32f1** (LTS), built-in render pipeline, 2D |
| Language | C# (.NET Standard 2.1) |
| UI | Unity UGUI + TextMeshPro |
| Networking | `UnityWebRequest` coroutines |
| AI | OpenAI Chat Completions API (`gpt-3.5-turbo`) |
| Persistence | `JsonUtility` → JSON files in `Application.persistentDataPath` |
| Targets | macOS / Windows (keyboard) and iOS (touch, landscape) |

No third-party plugins or paid assets. Everything below is first-party code.

---

## Architecture

The game is built around **one persistent map scene plus additive encounter scenes**. This keeps the
cave state alive in memory while a fullscreen trivia round plays on top of it, and avoids
serializing and rebuilding the grid on every hazard.

```
                      ┌──────────────────────────────────────┐
   BeginningScene ───▶│            MainScene                 │
   (TextCrawl)        │                                      │
                      │  CellGenerator ──builds──▶ Cell[6,5] │
                      │       │                        ▲     │
                      │       │ owns wumpus/pits/bats  │     │
                      │       ▼                        │     │
                      │  PlayerScript ──reads/moves────┘     │
                      │       │                              │
                      │       │ LoadScene(Additive)          │
                      └───────┼──────────────────────────────┘
                              ▼
                   ┌─────────────────────────┐
                   │  Cave_01 / Cave_02 /    │
                   │  Cave_03 / wumpusRoom   │
                   │                         │
                   │     TriviaDisplay ──────┼──▶ OpenAIClient ──▶ OpenAI API
                   │          │              │           │
                   └──────────┼──────────────┘           ▼
                              │             offline fallback: newdata.json
                   SendMessage("CorrectAnswer" / "WrongAnswer")
                              │
                              ▼
                       PlayerScript ──▶ GameData ──▶ gamedata.json ──▶ Leaderboard
```

### Components

| File | Responsibility |
|---|---|
| `CellGenerator.cs` | Procedurally builds the hex grid, carves random tunnels, places the Wumpus and hazards |
| `Cell.cs` | One room. Owns its occupancy flags, its two adjacency maps, and its display tint |
| `Direction.cs` | The six hex direction constants used as adjacency keys |
| `PlayerScript.cs` | Movement, arrow shooting, hazard dispatch, economy, HUD, scoring |
| `TriviaDisplay.cs` | Runs a trivia round, grades answers, reports the outcome back to the map |
| `OpenAIClient.cs` | Single place all LLM calls, API-key resolution and response parsing live |
| `GameData.cs` | Leaderboard persistence (singleton, survives scene loads) |
| `Leaderboard.cs` | Renders saved runs on the win/lose screens |
| `TextCrawl.cs` | Star Wars style intro crawl |

### Two adjacency maps, deliberately

Each `Cell` keeps **two** dictionaries, and the distinction is the heart of the movement rules:

- `neighbors` — only the tunnels the player can actually **walk** through. Uncarved directions point
  back at the cell itself, so walking into a wall is a no-op rather than a null reference.
- `next` — the full **geometric** adjacency, ignoring walls. Arrows and the Wumpus use this, which is
  why an arrow can fly through solid rock but you cannot walk there.

---

## Core technical challenges

**1. Hex grid adjacency with wraparound.**
A flat-top hex grid has no clean row/column neighbour formula — the vertical offset of a diagonal
neighbour flips depending on whether the column index is even or odd. Every index is additionally
taken modulo the grid size, making the cave topologically a torus so the player can never reach a
dead end at the boundary. This is isolated in `CellGenerator.DiagonalNeighbor`.

**2. Keeping the cave sparse enough to be deducible.**
If every room connects to all six neighbours, proximity warnings carry almost no information and the
game stops being a deduction puzzle. The generator caps each room at three tunnels and carves exactly
one extra diagonal per column, retrying with a different source room when the chosen room's
neighbours are already saturated.

**3. Suspending a live game to run a fullscreen minigame.**
Encounters can't destroy the cave — the player has to come back to the same rooms, hazards and score.
The solution is additive scene loading: the map scene is hidden (grid, sprite, canvas and backdrop
all toggled off) while the trivia scene loads on top, then the trivia scene reports its result back
across the scene boundary via `SendMessage` and unloads itself.

**4. Six-direction input from one swipe.**
Touch movement converts the swipe vector to an angle with `Atan2` and buckets it into six 60° arcs.
The same angle-to-direction mapping serves both walking and arrow-firing; only the adjacency map it
indexes into changes. On desktop the same six directions come from Up/Down arrow keys combined with a
held Left/Right modifier.

**5. Staying playable when the API isn't.**
Every question that parses successfully is appended to an on-disk bank (`newdata.json`). If the API
call fails, the key is missing, or the response doesn't match the expected format, the game silently
draws from that bank instead. The game is fully playable offline after the first successful session.

**6. Getting structured data out of an unstructured model.**
The model is asked for a strictly labelled format (`Question:` / `A:` / `B:` / `C:` / `D:` /
`Correct:`) and the response is matched against a single regex. Any field coming back empty is
treated as a parse failure and falls through to the offline bank rather than showing a broken
question.

---

## Running locally

### Prerequisites

- **Unity 2022.3.32f1** (LTS). Other 2022.3.x patch versions work; the Hub will offer to upgrade.
- An OpenAI API key — **optional**. Without one the game runs on its offline question bank.

### Steps

```bash
git clone https://github.com/Mallard64/Wumpus.git
cd Wumpus
```

1. Open the folder in Unity Hub → **Open** → select the project root.
2. Let Unity import (first import takes a few minutes and regenerates `Library/`).
3. In the Project window open `Assets/Scenes/BeginningScene.unity`.
4. Press **Play**.

> **Build settings:** the scenes must be registered under *File → Build Settings* in this order:
> `BeginningScene`, `MainScene`, `Cave_01`, `Cave_02`, `Cave_03`, `Cave_04`, `wumpusRoom`, `Win`,
> `Lose`. Scene loading is by name, so a missing entry throws at runtime.

### Configuring the AI features

The API key is **never** stored in source. `OpenAIClient` resolves it at runtime from, in order:

1. A `TextAsset` named `openai_api_key` in any `Resources` folder, or
2. The `OPENAI_API_KEY` environment variable.

To enable live generation:

```bash
cp "Assets/Resources/openai_api_key.txt.example" "Assets/Resources/openai_api_key.txt"
# then paste your real key into that file
```

`Assets/Resources/openai_api_key.txt` is git-ignored. If no key is found the game logs a warning
once and falls back to the offline question bank — everything else works normally.

| Variable | Required | Purpose |
|---|---|---|
| `OPENAI_API_KEY` | No | Enables live trivia, hints and Wumpus dialogue. Omit to play offline. |

---

## How to play

### Goal

Find the Wumpus by deduction and shoot it with an arrow. You start with **3 arrows** in room 1.

### Controls

| Action | Desktop | Mobile |
|---|---|---|
| Move | Arrow keys — `Up`/`Down`, or `Up`/`Down` + held `Left`/`Right` for diagonals | Swipe in one of six directions |
| Enter shooting mode | `Space`, or the **Shoot** button | Tap the **Shoot** button |
| Fire an arrow | Same direction input as moving | Same swipe as moving |
| Buy arrows / buy a hint | **Arrows** / **Secret** buttons | Same buttons |
| Reveal all hazards (debug) | Hold `U`, press `W` | Double-tap with three fingers |

### Reading the cave

Colour tells you about the room you're standing in; text tells you about the rooms next to it.

| Colour of your room | Meaning |
|---|---|
| 🟢 Green | Safe |
| 🔴 Red | The Wumpus is adjacent |
| 🔵 Cyan | You are standing on a pit |
| 🟣 Magenta | You are standing on bats |
| 🟡 Yellow | An arrow is passing through |
| ⚫ Black | The Wumpus is here |

### Hazards

- **Wumpus** — opens a 5-question duel. Win and it flees to a new room; lose and the run ends.
- **Pit** — opens a trivia challenge. Win and you climb out; lose and the run ends.
- **Bats** — no challenge. They pick you up, drop you in a random room, and the hazards reshuffle.

### Economy and scoring

You earn **1 coin per move** for your first 100 moves. Each trivia attempt costs a coin. Running out
of arrows *or* going into coin debt ends the run.

```
score = 100 − turns + coins + (5 × arrows remaining) + (50 if Wumpus killed)
```

Results are written to a local leaderboard, sorted by score, shown on the win and lose screens.

### Tips

- Move along a wall of known-safe rooms rather than into unexplored space.
- A "breeze" with no "smell" is a cheap room to pass through — pits only kill you if you fail trivia.
- Save arrows. Each unspent arrow is worth 5 points, and shooting wildly may make the Wumpus roam.

---

## Project structure

```
Wumpus/
├── Assets/
│   ├── Resources/
│   │   ├── openai_api_key.txt.example   # copy to openai_api_key.txt and add your key
│   │   └── BillingMode.json
│   └── Scenes/
│       ├── BeginningScene.unity         # intro crawl
│       ├── MainScene.unity              # the cave map (persistent)
│       ├── Cave_01..04.unity            # shop / pit encounters (additive)
│       ├── wumpusRoom.unity             # Wumpus duel (additive)
│       ├── Win.unity / Lose.unity       # results + leaderboard
│       │
│       ├── Cell.cs                      # one room
│       ├── CellGenerator.cs             # procedural cave generation
│       ├── Direction.cs                 # six hex direction constants
│       ├── PlayerScript.cs              # movement, shooting, hazards, HUD
│       ├── TriviaDisplay.cs             # trivia rounds
│       ├── OpenAIClient.cs              # all LLM calls + key resolution
│       ├── GameData.cs                  # leaderboard persistence
│       ├── Leaderboard.cs               # leaderboard rendering
│       ├── TextCrawl.cs                 # intro crawl
│       └── LoadMainScene.cs             # replay button
├── Packages/
└── ProjectSettings/
```

---

## What I learned

- **Hex grids are not square grids.** The even/odd column stagger has to be handled at every diagonal
  lookup, and pushing that into one `DiagonalNeighbor` method removed a large class of off-by-one
  bugs that were otherwise scattered across the generator.
- **Additive scenes are the right tool for suspending state.** The alternative — serializing the cave
  and reloading it after each encounter — makes a hard problem out of an easy one.
- **Treat an LLM as an unreliable network dependency, not a library call.** Every generated feature
  needed a deterministic fallback path before it was safe to build gameplay on top of it.
- **Secrets do not belong in source.** This project originally had API keys pasted directly into
  `PlayerScript.cs` and `TriviaDisplay.cs`, in a public repo. Runtime key resolution plus a
  git-ignored key file is barely more work and is the only acceptable approach.
- **Unity's `[FormerlySerializedAs]` makes renaming safe.** Serialized field names are part of your
  scene files, so renaming `t1` to `scoreText` silently breaks inspector wiring unless you declare it.

---

## Known issues

Stated plainly rather than hidden — these are real and currently unfixed:

- **The Wumpus rarely relocates after a won duel.** `CellGenerator.moveWumpus` never advances its
  room counter, so relocation only takes effect when the newly drawn room number is 1. One-line fix,
  deliberately left out of the clean-code pass to keep that change behaviour-neutral.
- **Two room-numbering schemes disagree.** `Cell.GetCellIndex` numbers rooms row-major while hazard
  placement numbers them column-major, so hint text like *"Pit is at room 14"* can point at the wrong
  room.
- **Answer order is fixed.** A shuffle was written but its result was never applied, so the correct
  answer's position is not randomised.
- **The leaderboard and question bank are written to disk every frame.** Correct but wasteful; both
  should be written on change only.
- **`turns`, `coins` and `arrows` are not persisted** on the first leaderboard write for a player, so
  those columns show 0 for new entries.
- **No automated tests.** The generation and adjacency logic is pure and would be straightforward to
  unit-test; nothing has been written yet.

## Future improvements

- Fix the issues above, starting with the Wumpus relocation and the room-numbering mismatch.
- Add unit tests for `CellGenerator` adjacency and `Cell.IsNear*` — no Unity runtime required.
- Move the OpenAI call behind a small proxy so a shipped build never carries a key at all.
- Difficulty settings (grid size, hazard count, arrow count) instead of compile-time constants.
- Batch-request and cache trivia questions to remove the per-move latency spike.

---

## Credits

Built with Unity. Classic *Hunt the Wumpus* designed by Gregory Yob (1973).
