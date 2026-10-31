# remember-the-milk

A task scheduler that notifies you of single or recurring events using [ntfy.sh](https://ntfy.sh). You send what you want in plain English; an LLM turns it into a scheduled task.

## API

`POST /tasks` — create a task from a natural-language query:

```
curl -X POST http://localhost:8080/tasks \
  -H "Content-Type: application/json" \
  -d '{"query":"remind me to check out the yukon lightning tomorrow at 2"}'
```

Response (201):
```json
{
  "id": "a6e78ae3-...",
  "description": "check out the yukon lightning",
  "scheduleType": "Single",
  "nextOccurrence": "2026-10-23T14:00:00",
  "ntfyTitle": "Yukon Lightning",
  "rawQuery": "remind me to check out the yukon lightning tomorrow at 2"
}
```

`GET /tasks` — list all scheduled tasks (200, JSON array).

`GET /tasks/{id}` — one scheduled task (200, or 404 if the id is unknown).

`PUT /tasks/{id}` — update a task: send the full JSON object (like the create response) with new values for `description`, `scheduleType`, `weekdays`, `nextOccurrence`, `ntfyTitle`, and `isMuted`. Only recurring reminders can be muted; a muted single reminder is rejected with `400`. The id and `rawQuery` stay as they were (200, `400` if description or nextOccurrence missing, 404 if the id is unknown).

`DELETE /tasks/{id}` — remove a task (204, or 404 if the id is unknown).

Errors: `400` for an empty query or a parse failure (message includes the raw LLM output when the model returns invalid JSON), `422` if the request cannot be understood as a task.

## Web UI

`GET /` serves a small page (`index.html`) to view and manage the scheduled notifications: add one by typing a natural-language query and pressing "Remind me!", mute or unmute recurring reminders with a checkbox, and delete with the red X link (asks for confirmation). A muted recurring reminder stays scheduled but sends no ntfy alert or email until unmuted. Single reminders cannot be muted. Feedback comes as toasts; the list refreshes every 15 seconds. Styling is [chota](https://github.com/jenil/chota), served locally at `/chota.min.css`.

`index.html` and `chota.min.css` are read from the working directory at request time — they are not part of the publish output, so keep them next to the exe (or in the directory you run from).

## How it works

- **Parsing**: every query is sent via [OpenRouter](https://openrouter.ai) to the first model in `config.json`'s `Models` list. If it is unavailable or rate-limited, OpenRouter tries the remaining models in order. The repo uses `google/gemini-2.5-flash` first and `openai/gpt-4o-mini` as fallback. The model must return strict JSON (`description`, `schedule type`, `next occurrence`, optional ntfy title); if all models fail, the task is not added.
- **Storage**: tasks are persisted to `%LOCALAPPDATA%\GaucheDroite\RememberTheMilk\tasks.json`, loaded at startup. `TaskRepository` writes atomically (temp file + move, with a `.bak` copy of the last good version).
- **Scheduling**: a background loop (`Scheduler`) checks every 30 seconds and fires due single reminders and unmuted recurring reminders — single tasks are removed once handled; recurring tasks (`Daily`, `Weekly`, `Monthly`) are rescheduled for their next occurrence. Muted recurring tasks are rescheduled without sending a notification.
- **Notification**: after ntfy accepts a reminder, `NotificationService` also submits it through classic Outlook to `EmailTo`. If Outlook is unavailable or email fails, the failure is logged; ntfy remains the primary delivery and is not repeated just because email failed.
- **Logging**: timestamped to the console and `app.log`.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Setup

1. Clone and build:
   ```
   git clone https://github.com/gauchedroite/remember-the-milk.git
   cd remember-the-milk
   dotnet build
   ```
2. Put your OpenRouter key in a file named `openrouter.key` in the project root (or export it as the `OPENROUTER_API_KEY` environment variable).
3. Edit `config.json`: your ntfy channel (`NtfyUrl`), email recipient (`EmailTo`), an ordered `Models` list (first is primary; later models are fallbacks), and the API `Port`. Email delivery requires classic Outlook installed and configured on the same Windows account.

## Run

```
dotnet run
```

The API listens on `http://localhost:8080` (or your configured port). Send `POST /tasks` requests with natural-language queries: "remind me to ...", "ping me next week about ...", "clean the freezer: every day", "in 30 minutes".

A framework-dependent single-file build lives in `publish/` (`RememberTheMilk.exe`; it needs the .NET 10 runtime installed — copy `config.json`, `openrouter.key`, `index.html` and `chota.min.css` next to it).

## Known limits

- **Local time only** — no time zones; the LLM is given your current local time and works from there.
- **Monthly = +30 days** — no calendar-aware month arithmetic.
- **The API trusts localhost** — no authentication; don't expose the port without adding some.