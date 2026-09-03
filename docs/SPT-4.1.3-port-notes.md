# SPT 4.1.3 Compatibility Port Notes

## Status

This is an unofficial compatibility port of Geko's Better Progression for SPT
4.1.3. The server mod is version `2.1.0`; the client plugin is version `0.5.1`.

Both projects compile successfully. The server component has also passed a
startup smoke test in the existing SPT 4.1.3/Fika installation: its metadata
was accepted, its configured database transformations ran, both server routes
were registered, its Harmony patches applied, and SPT reached `Server has
started, happy playing`.

## Main migration work

- Migrated server metadata to `IModMetadata` and targeted `~4.1.3`.
- Migrated load callbacks to `OnLoadAsync(CancellationToken)`.
- Replaced removed database/config aggregation services with injected 4.1
  tables, configuration, helpers, and services.
- Made the shared mod context a DI singleton and split custom-item creation into
  the pre-profile preload phase.
- Updated custom route callback signatures and load priority.
- Ported the Ref trader Harmony hooks to the current server methods and made
  `refStandingOnKill.enable` effective.
- Ported the client plugin from 4.0 obfuscated types to the named 4.1 API,
  including skills, skill buffs, trader pricing, currency, and UI refresh APIs.
- Fixed the skill-data loading coroutine so a new profile with no existing save
  does not retry forever.
- Changed client startup to fetch and validate both server configurations before
  enabling any Harmony patches. A missing or unreachable server component now
  causes a clean no-patch failure instead of leaving the plugin half loaded.
- Removed activation of the legacy `GPFix` trader-view patch. SPT 4.1.3 already
  provides a native nonstandard-currency icon path, while the old patch touched
  every trader offer and created potentially unbounded asset-wait coroutines.
- Cached client configuration so patch static constructors no longer perform
  repeated HTTP requests.
- Redirected both projects to a non-live `Build/Release` staging tree.

## Build and installation

The assembled output is under `Build/Release`. Copy its two top-level folders
into the corresponding SPT client installation:

```text
Build/Release/
  SPT_Runtime/user/mods/GekosBetterProgression/
  BepInEx/plugins/gekos_api/
```

The server component is already present in this workspace's
`SPT_Runtime/user/mods/GekosBetterProgression` directory for the smoke test.
The client component was deliberately left staged because this workspace is a
headless installation.

## Verification performed

- Server Release build: succeeded with no errors.
- Client Release build: succeeded with no warnings or errors.
- Server startup with the normal 33-mod/Fika set: succeeded.
- Geko database/config transformations: completed without a Geko error.
- Server Harmony targets: enabled without a patch error.
- Web server and WebSocket server: started on port 6969.
- Corrected client `0.5.1` Release build: zero warnings and zero errors.
- Gaming-PC test: vanilla and modded trader inventories display with client
  build `0.5.1`.
- Gaming-PC test: algorithmic rebalancing changes trader inventories when
  enabled and is bypassed when disabled.
- Retaining `MinPriceFix` without the legacy `GPFix` does not cause empty trader
  windows.

## Remaining manual tests

Compilation and server startup cannot prove client UI or gameplay behavior.
Before using an established profile, test with a disposable client profile:

1. Start a new profile and confirm custom items/trades and stash progression.
2. Open the Skills screen and test allocation/deallocation and persistence.
3. Verify XP and buff multipliers with each related option enabled and disabled.
4. Complete a PMC raid with an eligible PMC kill and verify Ref standing changes
   once, then persists after restart.
5. Check trader pricing/GP icons, hideout recipes, quest rewards, and container
   layouts.
6. Review both server and BepInEx logs for Harmony, reflection, or route errors.

## Build dependency note

The installed/local package cache supplied SPT `4.1.2` compile packages, while
the runtime is SPT `4.1.3`. This matches another working server mod in this
installation. Runtime metadata remains restricted to `~4.1.3`, and the smoke
test used the actual installed 4.1.3 assemblies and data.

## Known limitations

- The existing server source produces nullable-reference warnings inherited
  from assumptions in the original data-manipulation code. There are no build
  errors, and the exercised startup paths completed successfully.
- Client patches have been verified against the installed assemblies and by
  compilation, but require a graphical Tarkov client for behavioral testing.
- The attached client log's configuration-route 404s were captured while the
  server component was absent, so those particular 404s are expected and are
  not proof that an installed server build failed to register its routes.
- No balance values or public configuration keys were intentionally changed.
