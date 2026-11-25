# Decision Log (Assignment 10 - Three Enemy Types)

- **2026-11-11**: Upgraded the Scorpion (`PatrolEnemy.cs`) from simple back-and-forth movement to a full 3-state FSM (Patrol -> Chase -> Attack). Added `Cooldown` state to make attacks fair.
- **2026-11-11**: Added a `Physics2D.Raycast` for vision so the Scorpion cannot see the player through walls. Assigned a `Wall` layer to all static obstacles.
- **2026-11-11**: Fixed a bug where the Scorpion would blindly run off ledges while chasing. Added ledge and wall checks inside the `ChaseLogic` so it stops at edges.
- **2026-11-11**: Added a 0.5s `windUpTime` (red flash) before the Scorpion attack so the player has time to react.


- 2026-11-12: Upgraded Boss AI. Added a 3-state machine (Patrol, WindUp, Attack). Added Raycast vision so he doesn't shoot through pillars. Added a 0.7s WindUp (red flash) before he attacks to make it fair.

- 2026-11-12: Playtested with 2 friends. They beat Level 1 in 2 tries. Level 2 was too hard (5 tries). I decreased Scorpion chase speed from 2.5 to 2.0.

- 2026-11-25: Rebalanced Level 3 and fixed Boss attack timings for fair play.
- 2026-11-25: Added more enemies to Levels 1, 2, and 3 to increase difficulty after playtesting.
- **2026-11-25 — Level 3 boss rain attack:** Asked Codex to make the overhead fire attack trigger when the boss is hit. The first suggestion only changed the alternating attack order; after clarifying the intended trigger, the boss hit handler was updated to launch or queue the rain attack, with a 3.5-second cooldown so rapid shots do not create an unbounded stream. The warning markers remain before impact.
- **2026-11-25 — Level 3 rain coordinates:** The first inspection missed that the rain X range was still 1–13 while the arena is around world X 20–80. Set the Level 3 boss override to X 22–78. I tested the hit-triggered attack in the game and observed the fire fall at the player's position.
