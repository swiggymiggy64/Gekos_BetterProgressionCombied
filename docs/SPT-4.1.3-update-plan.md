# Geko's Better Progression: SPT 4.1.3 Update Plan

## Objective

Port Geko's Better Progression from SPT 4.0.x to SPT 4.1.3 while preserving its
existing gameplay behavior, configuration format, custom items, server routes,
and optional client-side skill features.

The update has two deliverables:

1. A server mod for `SPT_Runtime/user/mods/GekosBetterProgression/`.
2. A client BepInEx plugin for `BepInEx/plugins/gekos_api/`.

The server component must work independently when client-only skill features are
disabled. When the skill-points system or other client patches are enabled, the
matching client plugin must be installed on every player client that uses the
server.

## Scope and constraints

- Target SPT version: 4.1.3.
- Preserve the current mod's behavior unless an SPT API or data-model change
  makes that impossible.
- Avoid balancing changes during the compatibility port.
- Avoid changing configuration keys unless necessary.
- Keep existing profiles safe. Initial testing must use a disposable profile.
- Do not install development builds over the working installation until they
  pass isolated build and startup checks.
- Treat server and client updates as separate workstreams with separate test
  gates.

## Reference material

- [SPT 4.1 wiki](https://wiki.sp-tushonka.com/en/SPT_41)
- [SPT 4.1 modding index](https://wiki.sp-tushonka.com/en/modding/SPT_41_Modding)
- [Server migration: 4.0 to 4.1](https://wiki.sp-tushonka.com/en/modding/SPT_41_Modding/Server_40_to_41)
- [Server changes from 4.1.3 onward](https://wiki.sp-tushonka.com/en/modding/SPT_41_Modding/Server_413_Changes)
- [Client migration: 4.0 to 4.1](https://wiki.sp-tushonka.com/en/modding/SPT_41_Modding/Client_40_to_41)
- [Client class-name mappings](https://wiki.sp-tushonka.com/en/modding/SPT_41_Modding/client/Class_Name_Mappings)
- The installed SPT 4.1.3 assemblies and known-working 4.1.x mods in this
  workspace.

## Known migration requirements

### Server

- Rebuild against SPT 4.1.3 server packages.
- Confirm whether the available 4.1.3 feed uses the `SPTushonka.*` package IDs
  described by the wiki. Assemblies and namespaces remain `SPTarkov.*`.
- Replace `AbstractModMetadata` with `IModMetadata`.
- Remove metadata property overrides and remove `IsBundleMod`.
- Change lifecycle methods from `OnLoad()` to `OnLoadAsync(CancellationToken)`.
- Replace removed load-order values such as `PreSptModLoader`.
- Replace `DatabaseService`, `DatabaseServer`, `ConfigServer`, and
  `DatabaseTables` aggregation with directly injected tables and concrete
  configuration objects.
- Update moved helper and service namespaces.
- Add `CancellationToken` to route actions.
- Give the custom router an explicit priority relative to `OnLoadOrder.Routers`.
- Register custom items before profiles load, at `OnLoadOrder.Preload`.
- Revalidate server Harmony patches against SPT 4.1.3 method signatures.

### Client

- Rebuild against the SPT 4.1.3 game and SPT client assemblies.
- Replace obfuscated 4.0 type and member names with their 4.1 names.
- Revalidate every Harmony target, overload, postfix/prefix argument, and
  reflected private field.
- Correct project reference and post-build paths so they point to a configurable
  SPT installation.
- Validate the skill-button asset bundle and all assumed UI hierarchy paths
  in-game.

## Phase 0: Establish a safe baseline

### Tasks

- Record hashes or copies of the untouched source archive and current extracted
  tree.
- Identify the exact upstream revision represented by the extracted source, if
  possible.
- Record the current server mod version (`2.0.2`) and client plugin version
  (`0.4.0`).
- Inventory every configuration file and determine which copies are
  authoritative.
- Create a disposable SPT profile for testing.
- Record a clean SPT 4.1.3 startup log without the new build installed.
- Decide on a development output directory outside the live
  `SPT_Runtime/user/mods` and `BepInEx/plugins` directories.

### Exit criteria

- The original source can be recovered without relying on Git history.
- A disposable profile and clean baseline log are available.
- Builds cannot accidentally overwrite the currently installed mod/plugin.

## Phase 1: Normalize project and release structure

### Server tasks

- Determine whether `config/config.json5` or `src/config.json5` is the canonical
  basic configuration.
- Determine how the four files under `config/advanced/` map to
  `src/advancedConfig.json5`.
- Make the project copy only the canonical runtime files into its build output.
- Ensure the release output contains the server DLL, dependency metadata if
  required, and all JSON5 configuration files at the paths expected by
  `ModHelper`.
- Exclude legacy TypeScript/npm artifacts and the SPT 3.11 `package.json` from
  the SPT 4.1.3 release unless the loader specifically requires them.

### Client tasks

- Replace fragile hard-coded relative references with one configurable SPT
  game-directory property.
- Point references at the 4.1.3 copies of `Assembly-CSharp.dll`, BepInEx,
  Harmony, SPT reflection/common libraries, Unity modules, and other required
  assemblies.
- Change the post-build output to a staging directory rather than the live
  client.
- Ensure `Assets/skillsbutton.bundle` is copied beside the client plugin in the
  staged release.

### Exit criteria

- Both projects have deterministic staging locations.
- There is one authoritative copy of every runtime configuration file.
- Building cannot silently package stale configuration.

## Phase 2: Port server project metadata and lifecycle

### Affected files

- `user/mods/Gekos_BetterProgression/src/GekosBetterProgression.csproj`
- `user/mods/Gekos_BetterProgression/src/main.cs`
- Potentially the solution and build/output settings.

### Tasks

- Update the target framework to the framework required by SPT 4.1.3, verified
  against the installed server and an official/current example.
- Update all SPT package references to the exact 4.1.3 packages.
- Convert `ModMetadata` to `IModMetadata`.
- Remove `override` keywords and `IsBundleMod`.
- Set `SptVersion` to an appropriate 4.1 range. Prefer `~4.1.3` until broader
  4.1.x compatibility is tested.
- Decide whether the compatibility port warrants a new mod version such as
  `2.1.0` or an explicitly marked local build version.
- Convert each `IOnLoad` implementation to `OnLoadAsync(CancellationToken)`.
- Let `OperationCanceledException` propagate rather than treating cancellation
  as a mod failure.
- Replace `PreSptModLoader` and reassess every loader's priority.

### Exit criteria

- Metadata and lifecycle code conforms to the 4.1 API.
- The project restores against the intended package feed.
- No compatibility claim broader than the tested version is made.

## Phase 3: Replace removed server services with injected data

### Affected files

- `src/context.cs`
- `src/main.cs`
- `src/utils.cs`
- `src/Changes/SkillChanges.cs`
- Any change module that accesses `context.tables` or an old service.

### Tasks

- List the exact database tables used by the mod. The initial expected set is:
  - `GlobalTable`
  - `HideoutTable`
  - `LocaleTable`
  - `TemplateTable`
  - `TradersTable`
  - Any server/profile table revealed by compilation.
- Replace `DatabaseService.Get*()` calls with direct table access.
- Remove `DatabaseServer`, `DatabaseService`, `ConfigServer`, and
  `DatabaseTables` from `Context`.
- Inject concrete SPT configuration types instead of calling
  `ConfigServer.GetConfig<T>()`.
- Update namespaces for moved helpers and services, including:
  - `Helpers.Items.ItemHelper`
  - `Helpers.Items.PresetHelper`
  - `Helpers.Profile.ProfileHelper`
  - `Helpers.Server.ModHelper`
  - `Services.Locales.LocaleService`
  - `Services.InRaid.LocationLifecycleService`
- Decide between two designs:
  1. Keep `Context` as a compatibility aggregation object populated by DI.
  2. Inject only the required tables/configuration into each change service.
- Prefer the least invasive design for the initial port, then consider cleanup
  after behavior is verified.

### Exit criteria

- No removed server/database/config service remains in the source.
- All table accesses compile against SPT 4.1.3.
- Initialization order does not depend on manually half-populated context state.

## Phase 4: Split server work into correct load phases

### Early/pre-profile work

Create a dedicated `OnLoadOrder.Preload` loader for:

- Custom item templates.
- Custom item buffs required by those templates.
- Locales required by custom items.
- Any other new database identity that an existing profile could reference.

Review whether direct insertion into `TemplateTable.Items` remains appropriate
or whether the 4.1.3 `CustomItemService` should be used.

### General database changes

Move ordinary progression/database transformations to `OnLoadOrder.PostLoad`
unless a specific operation must occur earlier. These include:

- Secure-container progression.
- Stash progression.
- Flea settings.
- Hideout costs.
- Skill configuration.
- Crafting changes.
- Price changes.
- SICC changes.
- Found-in-raid changes.
- Bitcoin changes.
- Trader starting reputation.
- Ref changes.
- Additional quest rewards.
- Container-size changes.
- Algorithmic trader-assort rebalancing.

Trader-assort operations must be ordered after all required traders have been
registered. If `PostLoad` is too late for a specific behavior, assign an
explicit priority after `TraderRegistration` and document why.

### Exit criteria

- All custom identities exist before profile validation/loading.
- Ordinary mutations run only after their required source data exists.
- Startup order is explicit and documented.

## Phase 5: Port server routes

### Affected file

- `src/router.cs`

### Tasks

- Add `[Injectable(TypePriority = OnLoadOrder.Routers + 1)]` to the custom
  router.
- Add the fifth `CancellationToken` argument to both route-action lambdas.
- Propagate the token if callback work later becomes asynchronous.
- Remove unnecessary `async`/`await` where a callback already returns a
  completed `ValueTask`, if doing so does not obscure the route definition.
- Confirm both routes remain custom and do not collide with an SPT or another
  mod's route:
  - `/server-config-router/skillpoints`
  - `/server-config-router/skillsconfig`
- Confirm the client `ConfigHandler` expects exactly these URLs and response
  shapes.
- Confirm these routes do not require streamed responses. They are not among the
  known streamed 4.1.3 routes.

### Exit criteria

- Both routes compile and return valid JSON.
- Requests can be cancelled without producing an error log.
- Client and server configuration models deserialize without lost or renamed
  fields.

## Phase 6: Compile-driven server model migration

### Tasks

- Restore and build the server project against the exact 4.1.3 packages.
- Resolve errors in small groups:
  1. Namespace/type moves.
  2. Removed services and dependency injection.
  3. Renamed model properties.
  4. Nullability and collection type changes.
  5. Method signature changes.
- Do not suppress nullable warnings until each one is checked for a real
  missing-data path.
- Recheck assumptions around:
  - Mongo ID/string conversions.
  - Trader assort item IDs and loyalty maps.
  - Quest reward dictionaries.
  - Hideout requirements and production recipes.
  - Profile template inventory/equipment layout.
  - Locale storage and transformation APIs.
  - Ragfair blacklist/whitelist representation.
  - Handbook and price dictionaries.
- Add targeted guard clauses where SPT 4.1 data may legitimately omit a
  previously required property.

### Exit criteria

- Clean server build, ideally with zero warnings.
- No broad casts, null-forgiving operators, or exception swallowing added merely
  to force compilation.
- Each model change has been checked against actual 4.1.3 database data.

## Phase 7: Revalidate server Harmony patches

### Affected file

- `src/Changes/RefChanges.cs`

### Patch targets

1. `TraderController.GetItemPrices`
2. `LocationLifecycleService.EndLocalRaid`

### Tasks

- Inspect the exact 4.1.3 method declarations and overloads.
- Select targets with explicit parameter types if overload ambiguity exists.
- Confirm postfix argument names/types match Harmony's binding rules.
- Confirm `GetItemPricesResponse.CurrencyCourses` still has the expected type
  and GP currency semantics.
- Confirm the end-of-raid request still exposes victim role and level at the
  same path.
- Confirm profile trader standing is persisted when modified at this point in
  the raid lifecycle.
- Apply the `refStandingOnKill.enable` setting; the current loader enables the
  kill patch whenever the broader Ref module is enabled, so this nested toggle
  must be checked for correct behavior.
- Ensure patches are enabled only once per server process.

### Exit criteria

- Harmony reports both target methods found and patched.
- Ref purchasing behavior works with roubles and GP configuration variants.
- Ref reputation changes exactly once per eligible PMC kill and persists after
  restart.

## Phase 8: Port the client project to the 4.1.3 assemblies

### Affected files

- `Development/gekos_api/gekos_api.csproj`
- `Development/gekos_api/Plugin.cs`
- All files under `Development/gekos_api/Patches/`
- Helpers that communicate with the server or profile.

### Tasks

- Verify the target framework against a working SPT 4.1.3 BepInEx plugin.
- Reference the exact assemblies from the 4.1.3 client installation.
- Build once to obtain the authoritative list of missing and renamed symbols.
- Use the class-name mapping wiki and local assembly inspection to replace
  obfuscated identifiers, including:
  - `GClass3130`
  - `TraderClass.GStruct300`
  - `SkillManager.SkillBuffClass.Class1425` through `Class1428`
  - `SkillClass.method_4`
  - `SkillPanel.method_1`
  - Skill-buff `method_0` targets
- Prefer stable named public/internal members over numbered implementation
  methods wherever possible.
- Where no stable API exists, isolate reflection lookups in a small
  compatibility helper and fail with a precise error message.
- Update the plugin version and, if useful, replace the placeholder GUID with a
  stable reverse-domain GUID while considering save/config compatibility.
- Check whether `SPT.Reflection.Patching.ModulePatch` usage or namespace
  changed.

### Exit criteria

- Client plugin compiles against SPT 4.1.3 with no references to unresolved 4.0
  obfuscated types.
- All patch targets are deterministically selected.
- A missing target produces a clear diagnostic rather than a vague
  initialization failure.

## Phase 9: Validate client patches individually

Enable and test one feature group at a time so one failed patch does not hide
the others.

### 9.1 Configuration transport

- Request both custom server routes.
- Deserialize skill multipliers and skill-point settings.
- Handle server-offline or invalid-response cases without an infinite silent
  loop.

### 9.2 Skill XP multiplier

- Confirm `SkillClass.UseEffectiveness` still represents the intended XP path.
- Verify global and per-skill multipliers combine once and only once.
- Verify fresh/fatigued XP behavior remains controlled by the server
  configuration.

### 9.3 Skill buff multipliers

- Identify the four 4.1 buff implementation types and stable calculation
  methods.
- Confirm the reflected buff field and `EBuffId` still identify the intended
  buff.
- Verify values are not multiplied repeatedly during recalculation or UI
  refresh.

### 9.4 Additional skill levels and native-level compatibility

- Revalidate all patched `SkillClass` getters/methods.
- Replace numbered `method_4` with its 4.1 named equivalent.
- Verify native level exposure is restored even if a patched call throws.
  Consider a finalizer or scoped state mechanism if necessary.
- Confirm bonuses, progression bars, elite state, and XP calculations behave
  correctly at modified levels.

### 9.5 Skill allocation UI

- Revalidate `SkillsAndMasteringScreen.Show` and `SkillIcon.Show` signatures.
- Revalidate private fields `_levelPanel` and `skillClass`.
- Replace `SkillPanel.method_1` with the corresponding named 4.1 refresh method.
- Verify UI hierarchy paths such as `TopPanel`, `Progress Panel`,
  `Current Text`, `Skill Icon`, and `Level Panel/Level`.
- Confirm buttons are not duplicated after reopening the screen.
- Confirm fonts/materials are applied without leaking instantiated objects.
- Test multiple resolutions and UI scaling values.

### 9.6 GP icon and minimum-price fixes

- Confirm these fixes are still necessary in SPT 4.1.3 before retaining them.
- Revalidate `TradingItemView.SetPrice`, `_currency`, and
  `TraderClass.GetUserItemPrice`.
- Resolve the 4.1 equivalents of the old price-result struct and currency
  helper.
- Confirm forcing a price of one does not make items sellable to traders that
  should reject them.

### 9.7 Skill save data

- Confirm the save location is profile-specific and does not collide between
  profiles.
- Confirm the load coroutine terminates safely when no saved data exists or the
  server is unavailable.
- Test allocation, deallocation, overflow refund, restart persistence, profile
  switching, and profile deletion.

### Exit criteria

- Every patch can be tied to a successful startup log entry and an observed
  behavior test.
- No patch-not-found, ambiguous-match, Harmony, reflection, or coroutine
  exceptions appear in the client log.

## Phase 10: Server functional test matrix

Test with a disposable profile and capture the server log for each run.

| Feature               | Minimum verification                                                      |
| --------------------- | ------------------------------------------------------------------------- |
| Startup               | Mod metadata accepted; no DI or loader failures                           |
| Custom items          | Items exist before profile validation and survive restart                 |
| Secure containers     | Starter container and configured grid sizes apply                         |
| Stash                 | Starting level, sizes, costs, and loyalty requirements apply              |
| Flea                  | Disabled/whitelisted behavior matches configuration                       |
| Hideout               | Build requirement threshold and factor are applied once                   |
| Skills                | Freshness/fatigue values reach the client globals response                |
| Crafting              | Time and product multipliers apply once                                   |
| Prices                | Overrides apply to the intended items only                                |
| SICC                  | Filters and configured capacity changes are valid                         |
| FIR changes           | Quests, repeatables, hideout, and flea variants work independently        |
| Custom trades         | Assort trees, barter schemes, and loyalty levels are valid                |
| Bitcoin               | Value, speed, GPU boost, and capacity settings work independently         |
| Trader standing       | New-profile defaults and individual overrides apply correctly             |
| Ref                   | Currency, buy filters, GP course, and kill reputation work                |
| Quest rewards         | Rewards are added at the intended quest state and only once               |
| Container sizes       | Grid changes produce valid layouts                                        |
| Algorithmic rebalance | Ammo, weapons, attachments, crafts, barters, and quest locks remain valid |

For configuration modules with an `enable` flag, perform at least one enabled
and one disabled run. Features without a top-level enable flag must be checked
for accidental unconditional application.

## Phase 11: Compatibility and interaction testing

### Tasks

- Test without other nonessential mods first.
- Add Fika and the normal mod set after standalone validation.
- Check for conflicts with mods that modify:
  - Trader assorts or loyalty levels.
  - Flea configuration and prices.
  - Hideout recipes or requirements.
  - Skills and skill UI.
  - Secure containers or stash templates.
  - Ref reputation or end-of-raid processing.
- Verify server behavior with one local player and with a Fika client
  connection.
- Confirm all participating clients have the matching client plugin when
  client-dependent features are enabled.
- Review both server and client logs after startup, profile creation, raid
  completion, trader use, hideout use, and restart.

### Exit criteria

- No startup or profile-validation regression with the normal mod set.
- No duplicated mutations caused by load-order interaction.
- Fika clients receive consistent configuration and skill behavior.

## Phase 12: Packaging and release validation

### Expected package layout

```text
SPT_Runtime/
  user/
    mods/
      GekosBetterProgression/
        GekosBetterProgression.dll
        GekosBetterProgression.deps.json   # if generated/required
        config.json5
        advancedConfig.json5               # or the finalized advanced layout

BepInEx/
  plugins/
    gekos_api/
      gekos_api.dll
      Assets/
        skillsbutton.bundle
```

### Tasks

- Build Release configurations from a clean staging directory.
- Confirm the archive contains no `bin`, `obj`, source archive, test profile, or
  machine-specific paths.
- Install the archive into a clean copy of SPT 4.1.3.
- Start the server, create a new profile, launch the client, enter and complete
  a raid, restart both, and recheck the profile.
- Document which features require the client plugin.
- Document known mod conflicts or load-order requirements.
- Preserve upstream author attribution and licence information.
- Clearly label the build as an unofficial compatibility port unless
  permission/status changes.

### Exit criteria

- Clean-install smoke test passes.
- Package layout is correct for both server and client.
- No development artifacts or local absolute paths are shipped.

## Implementation checkpoints

Work should pause for review at these points:

1. After project/package metadata changes, before architectural server edits.
2. After the server compiles, before installing it into a live runtime.
3. After standalone server tests, before client patch work.
4. After client compilation, before enabling all patches together.
5. After disposable-profile testing, before testing an established profile.
6. Before creating the final distribution archive.

## Definition of done

- Server and client projects build reproducibly against SPT 4.1.3.
- Server metadata is accepted by the SPT 4.1.3 mod loader.
- No removed 4.0 server services, lifecycle signatures, or load-order constants
  remain.
- Custom items are registered before profile loading.
- Custom routes use the 4.1 signature and correct priority.
- Every Harmony target has been verified against the 4.1.3 assemblies.
- No unresolved obfuscated 4.0 type/member names remain in the client plugin.
- Every configurable feature has at least one focused behavior test.
- A disposable profile survives repeated server/client restarts without
  missing-template or profile-validation errors.
- The normal Fika mod set starts and runs without new errors attributable to
  this mod.
- The release archive installs cleanly and contains both correctly separated
  components.
- Upgrade/install instructions and known limitations are documented.

## Out-of-scope follow-up improvements

These may be worthwhile but should not be mixed into the initial compatibility
port:

- Rebalancing progression values for the SPT 4.1 economy.
- Renaming public configuration keys or reorganizing the configuration format.
- Rewriting all static change classes as injected services.
- Replacing the custom logger wrapper with direct structured logging throughout.
- Refactoring naming/style to standard C# conventions.
- Adding a full automated test project.
- Redesigning the skill allocation UI.
- Changing item IDs, quest rewards, trader inventories, or progression design
  beyond what 4.1.3 compatibility requires.

## Working checklist

- [ ] Baseline and disposable profile prepared.
- [ ] Runtime configuration source of truth identified.
- [ ] Build outputs redirected to staging.
- [ ] Server packages updated to 4.1.3.
- [ ] Metadata migrated to `IModMetadata`.
- [ ] Lifecycle methods migrated to async/token signatures.
- [ ] Removed services replaced with injected tables/configs.
- [ ] Helper/service namespaces updated.
- [ ] Load phases split, with custom items at `Preload`.
- [ ] Custom routes migrated.
- [ ] Server builds without unresolved errors.
- [ ] Server Harmony patches verified.
- [ ] Standalone server feature matrix passed.
- [ ] Client references updated to 4.1.3.
- [ ] Obfuscated client symbols mapped.
- [ ] Client builds without unresolved errors.
- [ ] Client patches passed individual tests.
- [ ] Fika/normal-mod-set interaction tests passed.
- [ ] Clean-install and restart tests passed.
- [ ] Release package and documentation completed.

## Implementation status (3 September 2026)

The compatibility implementation is complete through compilation, packaging,
and server startup testing. See `SPT-4.1.3-port-notes.md` for the exact changes,
test evidence, installation layout, and remaining graphical-client/gameplay
checks. Items in the checklist above remain unchecked where they require a
disposable graphical client profile or feature-by-feature gameplay validation.
