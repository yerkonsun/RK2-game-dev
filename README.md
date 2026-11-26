# Game dev: Midterm project

2D mobile platformer made in Unity. This repository contains the Unity project for Assignments 8–10 and the latest integrated game changes.

## Open the project

Use Unity Hub to open this folder. The project was last saved with Unity `6000.6.0f1`. Start from `Assets/Scenes/MainMenu.unity`.

## Branch guide

- `main` — latest integrated project.
- `assignment/08-09-mobile-ui` — checkpoint for the mobile UI and pause menu work.
- `assignment/10-enemy-ai` — enemy FSM/Raycast work and the later Level 3 boss and enemy updates.

The assignment branches preserve reviewable checkpoints; commits remain in the shared history. Assignment 12's AudioMixer and menu volume controls are not represented as complete here yet.

## Project contents

- `Assets` — scenes, scripts, prefabs, art, and audio clips.
- `Packages` — Unity package manifest and lock file.
- `ProjectSettings` — Unity project configuration.
- `DECISIONS.md` — decisions and changes.

Audio credits

The game uses the following music and sound effects. The project filenames are listed alongside the original filenames so each clip can be traced to its creator and license.

### Music

| Project file | Original track | Creator | Source and license |
| --- | --- | --- | --- |
| `Assets/Resources/Audio/music_menu.mp3` | `lsc_title.mp3` | Mopz | [Platformer music — OpenGameArt](https://opengameart.org/content/platformer-music), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/music_game.mp3` | `lsc_ingame.mp3` | Mopz | [Platformer music — OpenGameArt](https://opengameart.org/content/platformer-music), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/music_boss.mp3` | `lsc_boss.mp3` | Mopz | [Platformer music — OpenGameArt](https://opengameart.org/content/platformer-music), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |

### Sound effects

All effects below come from Deva's *8-Bit Sound Effect Pack (Vol. 001)*. The pack is published under CC0. The creator asks to be credited as **@Shades**; attribution is not a condition of the CC0 license, but is included here in respect of that request.

| Project file | Original clip | Use | Creator, source, and license |
| --- | --- | --- | --- |
| `Assets/Resources/Audio/click.wav` | `Select 1.wav` | Menu click | [Deva / @Shades — OpenGameArt](https://opengameart.org/content/8-bit-sound-effect-pack-vol-001), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/boss_hit.wav` | `Hit 5.wav` | Boss hit | [Deva / @Shades — OpenGameArt](https://opengameart.org/content/8-bit-sound-effect-pack-vol-001), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/enemy_die.wav` | `Explosion 1.wav` | Enemy defeat | [Deva / @Shades — OpenGameArt](https://opengameart.org/content/8-bit-sound-effect-pack-vol-001), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/fireball.wav` | `Fly 1.wav` | Fireball | [Deva / @Shades — OpenGameArt](https://opengameart.org/content/8-bit-sound-effect-pack-vol-001), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/hurt.wav` | `Hit 1.wav` | Player hurt | [Deva / @Shades — OpenGameArt](https://opengameart.org/content/8-bit-sound-effect-pack-vol-001), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/jump.wav` | `Jump 1.wav` | Jump | [Deva / @Shades — OpenGameArt](https://opengameart.org/content/8-bit-sound-effect-pack-vol-001), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/lose.wav` | `Wrong 3.wav` | Lose screen | [Deva / @Shades — OpenGameArt](https://opengameart.org/content/8-bit-sound-effect-pack-vol-001), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/pickup.wav` | `Coin 1.wav` | Pickup | [Deva / @Shades — OpenGameArt](https://opengameart.org/content/8-bit-sound-effect-pack-vol-001), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/portal.wav` | `Exit 2.wav` | Portal/exit | [Deva / @Shades — OpenGameArt](https://opengameart.org/content/8-bit-sound-effect-pack-vol-001), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/shoot.wav` | `Shoot 1.wav` | Player shot | [Deva / @Shades — OpenGameArt](https://opengameart.org/content/8-bit-sound-effect-pack-vol-001), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/warning.wav` | `Noise Select 2.wav` | Boss warning | [Deva / @Shades — OpenGameArt](https://opengameart.org/content/8-bit-sound-effect-pack-vol-001), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
| `Assets/Resources/Audio/win.wav` | `Powerup 2.wav` | Win screen | [Deva / @Shades — OpenGameArt](https://opengameart.org/content/8-bit-sound-effect-pack-vol-001), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) |
