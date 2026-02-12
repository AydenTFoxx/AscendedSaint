# Ascended Saint Changelog

## v2.0.0 | Unreleased

> Started development: 2025-08-14
> Released: None

First major refactoring of the codebase; Introduction of ModLib and LogUtils as dependencies. Released `5 months, 18 days, 14 hours, 46 minutes` after the previous version, the longest delay between versions by a large margin.

* TODO

Dev Commentary:

> TODO

## v1.2.3 | 2025-08-13

> Started development: 2025-08-12
> Released: 2025-08-13

Released `20 hours, 14 minutes` after the previous version.

* Added internal caching for queried online entities.
* Added syncing of ascension effects and custom chat messages for revival and self-ascension when the Rain Meadow mod is enabled.
* Changed requirements to revive *Looks to the Moon* to allow revival in non-Saint campaigns.
* Fully fixed REMIX options de-sync by reworking how classes retrieve the client's options.
* Fixed self-ascension visual effects not being synced to other players in online modes.

Known introduced bugs:

* Creatures can sometimes be both ascended and revived at the same time in Rain Meadow's Arena mode.

Dev Commentary:

> TODO

## v1.2.2 | 2025-08-12

> Started development: 2025-08-12
> Released: 2025-08-12

Released `1 hour, 57 minutes` after the previous version.

* Fixed initialization of the client's REMIX options preventing the mod from loading entirely.

Dev Commentary:

> TODO

## v1.2.1 | 2025-08-12

> Started development: 2025-08-12
> Released: 2025-08-12

First hotfix of the mod :( Released `4 hours, 43 minutes` after the previous version.

* Fixed Meadow integration breaking the game as the client.
* Fixed REMIX options sync being overriden by the client's own settings.
  * This also addresses the issue of ghost items by removing its cause entirely.

Dev Commentary:

> TODO

## v1.2.0 | 2025-08-12

> Started development: 2025-08-10
> Released: 2025-08-12

Introduction of full Meadow integration; First release to Steam Workshop. Released `1 day, 16 hours, 21 minutes` after `v1.1.0`.

* Reworked previous Meadow-compatibility systems into a full integration with the mod's own methods and behaviors.
* Added sync of player and host REMIX options.
* Added proper sync of creature revival. Visual effects are still not synced, though.
* Added Rain Meadow as a soft dependency.
* Marked this mod as "High Impact" for Rain Meadow. This means both host and player have the mod enabled/disabled to play together.
  * If the player doesn't have the mod enabled but the host does, the player will be requested to enable it, and vice-versa.
* Added in-house logger, which logs all mod events to a separate file (see [#1]) and the game's own logs.
* Added `mod` folder with the mod's compiled files.
* Fixed iterator revival not working (since v1.1.0)
* Fixed REMIX options being rendered increasingly lower over time (since v1.1.0)
* Fixed online players not being able to be revived. (since v1.1.0)

Known introduced issues:

* Joining a Meadow lobby as a client (non-host) causes the game to freeze.
* Host-client options sync is overriden by the client right after receiving the host's own settings, invalidating the sync entirely.

Dev Commentary:

> TODO

## v1.1.1 | Unreleased[^unreleased]

> Started development: 2025-08-09
> Released: None

First partially Meadow-compatible version. Not ever released.

* Introduced Meadow-specific compatibility layer.
* Introduced system for hooking specific classes to the game, depending on whether Rain Meadow was enabled.
  * The Meadow-specific classes are either wrappers for their vanilla counterparts, or modified copies using both modded types (e.g. `OnlineCreature`) and vanilla ones (e.g. `Creature`).
* Added hacky methods to revive creatures based on their `OnlineCreature` variant. This was never actually tested in practice.
* Fixed self-ascension not working since the previous version.

Dev Commentary:

> TODO

## v1.1.0 | Unreleased[^unreleased]

> Started development: 2025-08-08
> Released: 2025-08-09

First content update. Released `21 hours, 30 minutes` after the first release.

* Added ability to revive dead creatures, including iterators.
* Revival has its own effects, and briefly debilitates both player and creature.
* Added REMIX options for toggling the mod's features.
* Initialized GitHub repository.

Known introduced bugs:

* Player cannot ascend themselves anymore.
* At some point, iterator revival is also broken.
* Rain Meadow compatibility is implicit and requires both host and player to have the mod. Related bugs are:
  * If the host does not have this mod, creatures cannot be revived at all.
  * Other players cannot be revived.
  * Option `Require Karma Flower` may cause clients to have invisible Karma Flowers, which can be eaten but not used for reviving creatures.
* REMIX options are rendered lower than before every time the player opens the mod's menu.

Dev Commentary:

> TODO

## v1.0.0 | Unreleased[^unavailable]

> Started development: 2025-08-07
> Released: 2025-08-08

Initial Ascended Saint mod release.

* Added ability for the player to ascend themselves
* Ascending oneself plays a special effect, and counts as a regular death.

Known introduced bugs:

* Self-ascension effect is not synced in online multiplayer, e.g with the Rain Meadow mod enabled.

Dev Commentary:

> TODO

[^unreleased]: This version is not available to the public as a packaged mod, but its source code can still be found within the repository's commit history.
[^unavailable]: This version is not available to the public as a packaged mod or source code.

[#1]: https://github.com/AydenTFoxx/AscendedSaint/issues/1