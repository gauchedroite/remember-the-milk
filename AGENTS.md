# RememberTheMilk — project rules

## Testing procedure (do NOT break)
- NEVER delete or overwrite `tasks.json` — it is live user data. No `rm`, no "clean slate" resets.
- To test with a clean task list, run the exe from a different working directory
  (e.g. `run from a temp dir`); the app creates its own `tasks.json` relative to
  the current directory and the real data stays untouched.
- Before any command that touches files, check whether the target is data or code.

## Publishing
- Publish framework-dependent, not self-contained: `dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish`.
  This machine has the .NET 10 runtime, and self-contained produces a ~100 MB exe.

## General
- Do not commit/push without being asked.
