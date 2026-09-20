# Hunt the Wumpus — AI Edition

A Unity remake of the 1973 cave game *Hunt the Wumpus*, with one change: hazards don't kill you
outright, they start a trivia duel. A language model writes the question on the spot, and answering
it is how you climb out of the pit or drive the monster off.

You're dropped into a 30-room hex cave holding a Wumpus, two pits and two colonies of bats. You
can't see any of it — you only sense what's in the rooms next to you ("I smell a wumpus!", "I feel a
breeze..."). From those clues you work out where the Wumpus is and shoot it before it finds you.

## Screenshots

| | |
|---|---|
| ![The cave map](docs/screenshot-map.png) | ![A trivia encounter](docs/screenshot-trivia.png) |
| The hex map. Colour tells you about the room you're in, text about the rooms next to it. | An AI-generated trivia encounter. |

## How it works

The cave lives in one scene that stays loaded for the whole run. When you hit a hazard, the
encounter loads *on top* of it as a second scene and reports its result back. That way the cave
never has to be saved and rebuilt mid-game.

```
  BeginningScene ──▶ MainScene ────────────────────────────────┐
   (intro crawl)      │                                        │
                      │  CellGenerator ──builds──▶ Cell[6,5]   │
                      │        │                       ▲       │
                      │        │ owns wumpus, pits,    │       │
                      │        │ bats                  │       │
                      │        ▼                       │       │
                      │  PlayerScript ──reads/moves────┘       │
                      └────────┬───────────────────────────────┘
                               │ LoadScene(Additive)
                               ▼
                 ┌───────────────────────────┐
                 │  Cave_01..04 / wumpusRoom │
                 │                           │
                 │  TriviaDisplay ──▶ OpenAIClient ──▶ OpenAI API
                 │         │                 │              │
                 └─────────┼─────────────────┘              ▼
                           │                        offline fallback:
           SendMessage("CorrectAnswer"/"WrongAnswer")   newdata.json
                           │
                           ▼
                     PlayerScript ──▶ GameData ──▶ leaderboard
```

Two details worth knowing:

- **Every room keeps two neighbour maps.** `neighbors` is the tunnels you can walk through;
  `next` is the raw hex adjacency. Arrows and the Wumpus use `next`, which is why an arrow can fly
  through rock but you can't walk there. The grid wraps at the edges, so there are no dead ends.
- **The API is treated as unreliable, not as a library.** Questions come back as
  schema-constrained JSON, get validated, and are retried once. Anything that still fails falls back
  to a bank of previously seen questions on disk, so the game is fully playable offline and with no
  API key at all.

## Stack

Unity 2022.3 (2D, built-in pipeline) · C# · UGUI + TextMeshPro · `UnityWebRequest` coroutines ·
OpenAI chat completions with structured outputs · `JsonUtility` for saves. No paid assets or
third-party plugins.

## Run it locally

```bash
git clone https://github.com/Mallard64/Wumpus.git
cd Wumpus
```

Open the folder in Unity Hub (2022.3.32f1), open `Assets/Scenes/BeginningScene.unity`, and press
**Play**. First import takes a few minutes.

The AI features are optional. Without a key the game runs off its offline question bank; everything
else works the same. To turn live generation on:

```bash
cp "Assets/Resources/openai_api_key.txt.example" "Assets/Resources/openai_api_key.txt"
# paste your key into that file — it is git-ignored
```

`OpenAIClient` also reads `OPENAI_API_KEY` from the environment if no key file is present.

**Tests:** *Window → General → Test Runner → EditMode → Run All*, or headless:

```bash
Unity -runTests -batchmode -projectPath . -testPlatform EditMode -testResults results.xml
```

## Limits

This is a finished student project, not a shipped product. Cave size, hazard count and arrow count
are compile-time constants rather than difficulty settings, and each trivia question costs an API
round trip, so there's a short pause at the start of an encounter.

## Credits

Built with Unity. The original *Hunt the Wumpus* was designed by Gregory Yob in 1973.
