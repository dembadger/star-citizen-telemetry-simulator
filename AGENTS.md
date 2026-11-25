Use a separate branch and pull request for each feature or fix. Run the stream test before merging. Publish Windows releases on demand from a tested revision of main.

## Live telemetry findings

When in-game captures establish a new field, state transition, or ship-specific behaviour, update the affected synthetic frames and controls so a user can reproduce it locally. Record the game build, ship and observed transition in the PR and link the related MFD change. Model unavailable readings as unavailable; keep speculative values out of observed scenarios.
