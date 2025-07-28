# Cinema Seat Availability Service – Technical Test

## Introduction

Welcome to the DataArt interview tech challenge. You'll pair‑program with us for about 90 minutes to build a small HTTP service. Before you start, please read the guidelines below so you know what we're looking for 🙂

- **Open‑ended by design** – choose the architecture, libraries and patterns you'd use in production.
- **Quality over completeness** – clean code, clear naming, sensible tests and iterative refactoring matter more than "finishing".
- **Think out loud**: we're interested in your reasoning as much as the code you type.

### Traits we love to see:

- idiomatic C#/.NET 8
- thoughtful API design
- automated tests (TDD or test‑after – your choice)
- graceful failure‑handling

It's an "open book" exercise – Google/Stack Overflow are all fine. Any IDE or editor is welcome.

Have fun! Treat the interviewer like a teammate – feel free to ask questions or bounce ideas.

Good luck! 🚀

## Scenario

The cinema has one auditorium and is showing a single film at 19:00 tonight. Live seat availability is exposed via a JSON file on GitHub (our "flaky" upstream):

https://raw.githubusercontent.com/ihor-shnaider2/cinema-api/main/seatmap-example.json

The file looks like this:

```json
{
  "auditorium": "Main-Hall",
  "filmTitle":  "Interstellar",
  "startTime":  "19:00",
  "seatRows": {
    "A": "111111",
    "B": "110000",
    "C": "111110",
    "D": "111111"
  }
}
```

Each row string is read left → right.
- `0` = free
- `1` = sold

Assume the URL sometimes delays or returns 404.

## Your Task

Create a REST‑style API that lets clients query seat availability.

| Requirement | Details                                                                                                                                                                                                                                     |
|-------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Retrieve full map** | Return the entire seat layout converted to an object‑per‑seat structure (see contract below). If the upstream call fails or times out, serve the most recent copy held in an in‑memory cache with a 3–5 s TTL and include `"cached": true`. |
| **Check one seat** | Return a status of one seat by passing a row and a seat number.                                                                                                                                                                             |
| **Resilience** | Implement some resilience techniques so callers remain responsive during transient failures.                                                                                                                                                |

## Sample Response Contract

### GET Seat Plan

```json
{
  "auditorium": "Main-Hall",
  "filmTitle": "Space Odyssey",
  "startTime": "19:00",
  "cached": false,
  "seats": [
    { "row": "A", "seat": 1, "status": "sold" },
    { "row": "A", "seat": 2, "status": "sold" },
    { "row": "A", "seat": 3, "status": "sold" },
    { "row": "B", "seat": 1, "status": "sold" },
    { "row": "B", "seat": 2, "status": "sold" },
    { "row": "B", "seat": 3, "status": "free" }
  ]
}
```

### GET Check a seat (e.g. B3)

```json
{ "free": true }
```

## Extension Opportunities

If you complete the core requirements early, feel free to tackle any of these advanced tasks:
- Adjacent‑pair finder: For a given number `minSeats`, examine each row’s seats to locate at least `minSeats` adjacent free seats; if found, return `found: true` and the first matching block (e.g. “B3–B4”)
- Provide a **docker-compose.yml** so we can `docker compose up`.

## Notes & Constraints

- Target .NET 8 (or latest LTS). Minimal APIs, MVC controllers or both—your choice.
- Hard‑code the feed URL in appsettings.json; design so it can be swapped for a real endpoint later.
- No authentication, payments or multi‑screen logic required.
- Prioritise readability, separation of concerns and meaningful tests — 100% coverage is not required.
- Use any packages (like Polly) you'd normally reach for.

Happy coding!
