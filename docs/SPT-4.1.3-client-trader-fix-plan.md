# SPT 4.1.3 Client Trader Fix Plan

## Objective

Correct the `gekos_api` client regression that causes every trader inventory to
appear empty when the plugin is installed, while preserving its intended GP
currency, minimum-price, skill multiplier, and skill-point features.

The fix must also correct the confirmed `404 Not Found` responses from the
Better Progression configuration routes and prevent a failed route request from
leaving the client plugin partially initialized.

## Confirmed evidence

- Trader inventories work when `BepInEx/plugins/gekos_api` is removed from the
  gaming client.
- Trader inventories remain empty when server-side algorithmic rebalancing is
  disabled.
- New profiles with different editions, levels, and trader standings show the
  same problem.
- The server accepts trader-assort requests without reporting a trader error.
- The client log shows `GPFix` and `MinPriceFix` being enabled before the plugin
  requests `/server-config-router/skillsconfig`.
- `/server-config-router/skillsconfig` returns HTTP 404 repeatedly.
- `Plugin.Awake()` never reaches `Geko's API fully loaded!`, demonstrating that
  client initialization terminates partway through.

These observations establish a client-plugin regression and a route failure.
They do not yet prove that the route failure alone causes the empty trader
windows, so the trader-related patches must still be isolated individually.

## Suspected failure sequence

```text
gekos_api starts
  -> GPFix enabled
  -> MinPriceFix enabled
  -> skills configuration request returns 404
  -> unhandled initialization exception
  -> plugin remains partially patched
  -> trader UI fails to populate correctly
```

## Phase 1: Reproduce a controlled baseline

### Tasks

- Preserve the supplied failing `docs/LogOutput.log` as evidence.
- Record hashes of the server DLL, client DLL, `config.json5`, and
  `advancedConfig.json5` used during the failing test.
- Confirm the gaming PC and dedicated server are using matching port builds.
- Confirm only the gaming PC has `gekos_api`; do not install it on the dedicated
  server or Fika headless client for this test.
- Use one disposable profile and keep the normal mod set unchanged during the
  initial reproduction.

### Exit criteria

- The exact failing server/client build pair is known.
- Empty trader inventories can be reproduced consistently with `gekos_api`
  installed and disappear when only that plugin is removed.

## Phase 2: Repair and verify server configuration routes

### Affected files

- `user/mods/Gekos_BetterProgression/src/router.cs`
- Potentially server loader/DI registration code in `src/main.cs`.

### Tasks

- Compare the custom router declaration with a known-working SPT 4.1.3 static
  router in the installed environment.
- Reassess `TypePriority = OnLoadOrder.Routers + 1`; test the ordering required
  for the router to be discovered before the server assembles its route table.
- Prefer an explicit priority supported by a working 4.1.3 example, likely
  `OnLoadOrder.Routers - 1`, after verifying its semantics.
- Confirm the router and callback classes are registered with the appropriate DI
  lifetime.
- Retain the five-argument SPT 4.1 route-action signatures and propagate the
  `CancellationToken` if the callback becomes asynchronous.
- Add concise server startup logging that confirms both routes were registered.
- Test both endpoints directly through the same SPT request mechanism used by
  the client:
  - `/server-config-router/skillpoints`
  - `/server-config-router/skillsconfig`
- Verify each endpoint returns HTTP 200 and valid JSON matching the client
  configuration models.

### Exit criteria

- Neither route returns 404.
- Both response payloads deserialize successfully.
- The server log provides positive evidence that the router was registered.

## Phase 3: Make client initialization transactional and fault tolerant

### Affected files

- `Development/gekos_api/Plugin.cs`
- `Development/gekos_api/Helpers/ConfigHandler.cs`
- Client patches that fetch configuration from static constructors.

### Tasks

- Fetch and deserialize all required server configuration before enabling any
  Harmony patch.
- Remove network calls from static constructors such as those in
  `SkillsMultipliers` and skill-buff patch classes.
- Cache validated configuration in `ConfigHandler` and inject or explicitly
  assign it to dependent patches.
- Catch HTTP, timeout, and deserialization failures at the plugin entry point.
- If required configuration cannot be obtained, enable no Geko patches and log
  one clear error describing the failed endpoint.
- Ensure a failed initialization cannot leave only `GPFix` and `MinPriceFix`
  enabled.
- Add a final success log only after every selected patch has enabled.
- Avoid retry loops during synchronous startup. If retries are retained, bound
  their count and delay and report the final failure.

### Exit criteria

- Successful startup produces one `Geko's API fully loaded!` message.
- A simulated 404 leaves no Geko Harmony patch enabled and does not affect
  traders or other client UI.
- Configuration endpoints are requested no more often than intended.

## Phase 4: Isolate the trader-related patches

Test with repaired routes and a successful client initialization. Use separate
diagnostic builds or feature gates so only one variable changes at a time.

### Test matrix

| Build | `GPFix` | `MinPriceFix` | Expected purpose                                         |
| ----- | ------: | ------------: | -------------------------------------------------------- |
| A     |     Off |           Off | Confirm the remaining skill patches do not empty traders |
| B     |      On |           Off | Test `TradingItemView.SetPrice` independently            |
| C     |     Off |            On | Test `Trader.GetUserItemPrice` independently             |
| D     |      On |            On | Confirm the corrected combined behavior                  |

For each build:

- Restart the client completely; Harmony patches cannot be reliably unloaded by
  returning to the launcher.
- Open Prapor, Therapist, Fence, and one modded trader.
- Check both Buy and Sell modes.
- Capture `BepInEx/LogOutput.log` immediately after opening the traders.
- Confirm visible offer counts and currency/price labels.

### Exit criteria

- The exact patch or interaction that triggers empty inventories is identified.
- Trader inventories remain visible with both trader fixes disabled.

## Phase 5: Harden `GPFix`

### Current risks

- It patches `TradingItemView.SetPrice`, which runs for every rendered trader
  offer.
- It performs reflection on `_currency` for every call even though the field is
  public in the installed 4.1.3 assembly.
- It may create a waiting coroutine for every view whose sprite asset is null.
- Those coroutines wait indefinitely until a GP asset is discovered.
- The postfix has no exception boundary, so one unexpected UI state can disrupt
  trader item construction.

### Tasks

- Access `_currency` through the verified 4.1.3 member instead of repeated
  reflection.
- Return safely when the view, text component, or required Unity object is
  unavailable or destroyed.
- Only perform GP-specific work when the displayed currency is actually GP.
- Replace per-item indefinite coroutines with one bounded asset-discovery
  mechanism.
- Cache the GP sprite asset once and reuse it.
- Log one warning rather than throwing or producing one warning per offer.
- Check whether SPT 4.1.3 now renders GP correctly without this patch; remove
  the patch entirely if it is obsolete.

### Exit criteria

- Opening and switching traders produces no Geko UI exception.
- Normal rouble, dollar, and euro offers render unchanged.
- GP offers render correctly, or native 4.1.3 behavior is retained if the patch
  is no longer necessary.
- No unbounded coroutine remains per trader item.

## Phase 6: Harden `MinPriceFix`

### Tasks

- Confirm `Trader.GetUserItemPrice(Item)` remains the correct and only target
  overload in the gaming PC's exact SPT 4.1.3 client assembly.
- Confirm a null result can still mean “rounded below one” rather than another
  rejection condition.
- Do not manufacture a price when supply data, currency conversion, trader
  purchase rules, or item eligibility are unavailable.
- Prefer reproducing the original calculation and checking its rounded value
  over assuming every eligible null result represents a sub-one price.
- Remove unnecessary `ref` modifiers from Harmony parameters where not needed.
- Add a guarded diagnostic for unexpected null-result cases during testing.
- Check whether another installed pricing/trader mod patches the same method and
  whether patch priority is required.

### Exit criteria

- Items that traders reject remain unsellable.
- Legitimate prices below one are raised to exactly one in the proper currency.
- Buy and Sell trader inventories render normally with the patch enabled.

## Phase 7: Check interactions with other client mods

The supplied log shows several other trader/UI patches, including UI Fixes,
Trader Search, QuickSell, More Checkmarks, and Fika patches.

### Tasks

- First validate corrected `gekos_api` with only required SPT/Fika dependencies.
- Reintroduce the normal client mod set.
- Inspect Harmony patch ownership and ordering on:
  - `TradingItemView.SetPrice`
  - `Trader.GetUserItemPrice`
- Test specifically with UI Fixes and Trader Search enabled because both modify
  trader screens.
- Add Harmony priority or before/after annotations only when an observed
  conflict requires them.

### Exit criteria

- Traders work with the minimal client set.
- Traders also work with the user's normal client mod set.
- No patch-order fix is added without evidence of a real conflict.

## Phase 8: Build, package, and regression test

### Tasks

- Increment the local client compatibility version after the fix.
- Build Release against the actual SPT 4.1.3 client assemblies.
- Confirm zero compilation errors and review all warnings.
- Stage the client DLL under: `Build/Release/BepInEx/plugins/gekos_api/`.
- Confirm the asset bundle remains beside the plugin at the expected path.
- Update `SPT-4.1.3-port-notes.md` with the cause and correction.
- Test the following with a disposable profile:
  - All vanilla and modded trader inventories.
  - Buy and Sell modes.
  - Rouble, dollar, euro, barter, and GP offers.
  - Skill XP multipliers.
  - Skill buffs.
  - Skill-point allocation UI and persistence.
  - Client reconnect and full restart.

### Exit criteria

- No 404 appears for either Geko endpoint.
- No Geko exception appears during startup or trader use.
- `Geko's API fully loaded!` appears once.
- Every tested trader displays its expected offers.
- Removing `gekos_api` no longer changes server-provided trader inventory.

## Safety and rollback

- Test only on a disposable profile until the client and trader regression tests
  pass.
- Keep the currently staged `0.5.0` DLL for comparison, but do not reinstall it
  as the final build.
- Do not modify profile files to work around empty trader windows; the evidence
  does not indicate profile corruption.
- If a client test fails, remove `gekos_api`, restart the client, and retain the
  corresponding log before trying another build.

## Definition of done

- Both configuration routes return valid data to a remote Fika client.
- Client initialization either completes fully or performs a clean no-patch
  failure.
- The responsible trader patch has been corrected, removed, or safely gated.
- All traders render inventories with the normal client mod set.
- Client-only skill functionality remains operational.
- Release artifacts and port documentation reflect the corrected build.

## Implementation status (3 September 2026)

Implemented client build `0.5.1`:

- Configuration is loaded and validated before any Harmony patch is enabled.
- Configuration is cached instead of requested from patch static constructors.
- Failed configuration retrieval exits cleanly with no Geko patches active.
- The legacy `GPFix` hook is no longer enabled.
- The server router priority now follows the working SPT 4.1 router ordering
  used by the local reference mod.
- Server and client Release builds succeed; the server reaches a clean startup.

Gaming-PC testing with the matching server component has now confirmed:

- Trader inventories display normally with client build `0.5.1`.
- Algorithmic rebalancing works when enabled.
- Disabling algorithmic rebalancing produces visibly different trader
  inventories, confirming that its configuration gate and assort mutations are
  effective.
- `MinPriceFix` does not reproduce the empty-inventory regression when retained
  without `GPFix`.

This identifies activation of the legacy `GPFix` hook as the cause of the empty
trader windows. The remaining manual checks concern GP-offer appearance and the
client skill features; the trader-inventory regression itself is resolved.
