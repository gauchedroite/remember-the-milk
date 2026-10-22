# remember-the-milk

A task scheduler that notifies you of single or recurring events using [ntfy.sh](https://ntfy.sh). You type what you want in plain English; an LLM turns it into a scheduled task.

## Example

```
Task scheduler ready. Type a reminder request, or 'list' to show scheduled tasks. Ctrl+C to exit.
remind me to check out the yukon lightning tomorrow at 2
Added: "check out the yukon lightning" (Single), next: 2026-10-23 14:00
ping me next week about bob's garage
Added: "bob's garage" (Single), next: 2026-10-29 09:00
clean the freezer: every day
Added: "clean the freezer" (Daily), next: 2026-10-23 09:00
list me the scheduled tasks
```

```
Scheduled tasks (3):
  [a6e78ae3] check out the yukon lightning
      Single, next: 2026-10-23 14:00
  [21151754] bob's garage
      Single, next: 2026-10-29 09:00
  [e9d85db9] clean the freezer
      Daily, next: 2026-10-23 09:00
```

## How it works

- **Parsing**: every query is sent to `moonshotai/kimi-k2.7-code` via [OpenRouter](https://openrouter.ai), which returns strict JSON (`description`, `schedule type`, `next occurrence`, optional ntfy title). There is no fallback parser — if OpenRouter is unreachable, the task is not added.
- **Storage**: tasks are kept in memory (see `TaskRepository`). Restarting the app loses them.
- **Scheduling**: a background loop (`Scheduler`) checks every minute and fires `next: <date> <time>` — tasks are dropped once sent; recurring tasks (`Daily`, `Weekly`, `Monthly`) are rescheduled for the next occurrence.
- **Notification**: `NotificationService` posts to your ntfy channel via `curl`.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- `curl` on your PATH (used to send notifications)

## Setup

1. Clone and build:
   ```
   git clone https://github.com/gauchedroite/remember-the-milk.git
   cd remember-the-milk
   dotnet build
   ```
2. Put your OpenRouter key in a file named `openrouter.key` in the project root (or export it as the `OPENROUTER_API_KEY` environment variable).
3. Edit `config.json` to set your ntfy channel (`NtfyUrl`) and the OpenRouter `Model` to use.

## Run

```
dotnet run
```

Type a reminder and press Enter; type `list`/`show ... tasks` to see what's scheduled; `exit` or Ctrl+C to quit.

## Known limits

- **In-memory storage** — tasks are lost on exit. Swap `TaskRepository` for SQLite if you need persistence.
- **Local time only** — no time zones; the LLM is given your current local time and works from there.
- **List matching is a regex** — anything containing "list/show" plus "task/reminder" lists instead of being parsed as a reminder.
- **Monthly = +30 days** — no calendar-aware month arithmetic.
