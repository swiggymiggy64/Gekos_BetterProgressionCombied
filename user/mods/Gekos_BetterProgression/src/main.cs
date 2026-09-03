using SPTarkov.DI.Annotations;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using System.Reflection;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;
using GekosBetterProgression.Changes;
using GekosBetterProgression.AlgoRebalance;

namespace GekosBetterProgression;

/// <summary>
/// This is the replacement for the former package.json data. This is required for all mods.
///
/// This is where we define all the metadata associated with this mod.
/// You don't have to do anything with it, other than fill it out.
/// All properties must be overriden, properties you don't use may be left null.
/// It is read by the mod loader when this mod is loaded.
/// </summary>
public record ModMetadata : IModMetadata
{
    /// <summary>
    /// Any string can be used for a modId, but it should ideally be unique and not easily duplicated
    /// a 'bad' ID would be: "mymod", "mod1", "questmod"
    /// It is recommended (but not mandatory) to use the reverse domain name notation,
    /// see: https://docs.oracle.com/javase/tutorial/java/package/namingpkgs.html
    /// </summary>
    public string ModGuid { get; init; } = "com.geko.gekosbetterprogression";

    /// <summary>
    /// The name of your mod
    /// </summary>
    public string Name { get; init; } = "Geko's Better Progression";

    /// <summary>
    /// Who created the mod (you!)
    /// </summary>
    public string Author { get; init; } = "DrunkGeko";

    /// <summary>
    /// A list of people who helped you create the mod
    /// </summary>
    public List<string>? Contributors { get; init; } = ["marbL-"];

    /// <summary>
    ///  The version of the mod, follows SEMVER rules (https://semver.org/)
    /// </summary>
    public SemanticVersioning.Version Version { get; init; } = new("2.1.0");

    /// <summary>
    /// What version of SPT is your mod made for, follows SEMVER rules (https://semver.org/)
    /// </summary>
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.3");

    /// <summary>
    /// ModIds that you know cause problems with your mod
    /// </summary>
    public List<string>? Incompatibilities { get; init; }

    /// <summary>
    /// ModIds your mod REQUIRES to function
    /// </summary>
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }

    /// <summary>
    /// Where to find your mod online
    /// </summary>
    public string? Url { get; init; } = "https://forge.sp-tarkov.com/mod/2088/gekos-better-progression";

    /// <summary>
    /// What Licence does your mod use
    /// </summary>
    public string License { get; init; } = "MIT";

    public bool HasPrepatcher { get; init; }
}

// Load configuration and shared dependencies immediately before preload work.
[Injectable(TypePriority = OnLoadOrder.Preload - 1)]
public class PreSPTLoader(
        ISptLogger<PreSPTLoader> logger,
        ItemHelper itemHelper,
        PresetHelper presetHelper,
        ProfileHelper profileHelper,
        QuestConfig questConfig,
        HashUtil hashUtil,
        ModHelper modHelper,
        Context context,
        GlobalTable globalTable,
        HideoutTable hideoutTable,
        LocaleTable localeTable,
        TemplateTable templateTable,
        TradersTable tradersTable,
        LocaleService localeService
    ) // We inject a logger for use inside our class, it must have the class inside the diamond <> brackets
    : IOnLoad // Implement the IOnLoad interface so that this mod can do something on server load
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        if (context.IsInitialized)
        {
            return Task.CompletedTask;
        }

        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        var config = modHelper.GetJsonDataFromFile<GekoConfig>(pathToMod, "config.json5");
        var advancedConfig = modHelper.GetJsonDataFromFile<AdvancedConfig>(pathToMod, "advancedConfig.json5");

        if (config is null || advancedConfig is null)
        {
            throw new InvalidDataException("Geko's Better Progression could not load config.json5 or advancedConfig.json5.");
        }

        var logWrapper = new LoggerWrapper<PreSPTLoader>(logger);

        context.PreInitialize(itemHelper, presetHelper, profileHelper, questConfig, hashUtil, config, advancedConfig,
            logWrapper, globalTable, hideoutTable, localeTable, templateTable, tradersTable, localeService);

        return Task.CompletedTask;
    }
}

// Custom item identities must exist before profiles are loaded.
[Injectable(TypePriority = OnLoadOrder.Preload)]
public class CustomItemLoader(
    Context context,
    PreSPTLoader initializer,
    ISptLogger<CustomItemLoader> logger
) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        await initializer.OnLoadAsync(cancellationToken);

        if (!context.IsInitialized)
        {
            throw new Exception("Context was not initialized!");
        }

        SafelyApplyCustomItems();
    }

    private void SafelyApplyCustomItems()
    {
        try
        {
            if (context.config.misc.addCustomTrades)
            {
                AdditionalItemsChanges.Apply(context);
            }
        }
        catch (Exception ex)
        {
            logger.Error("Failed to add custom items during preload!");
            if (context.config.dev.showFullError)
            {
                logger.Error($"Error Details: {ex.Message}");
                logger.Error($"Stack Trace:\n{ex.StackTrace}");
            }

            throw;
        }
    }
}

[Injectable(TypePriority = OnLoadOrder.PostLoad)]
public class PostDBLoader(
    Context context,
    ISptLogger<PostDBLoader> logger
)
    : IOnLoad // Implement the `IOnLoad` interface so that this mod can do something
{

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        if (!context.IsInitialized)
        {
            throw new Exception("Context was not initialized!");
        }

        ApplyPostDBChanges(context);

        logger.Success("Geko's Better Progression finished loading!");

        return Task.CompletedTask;

    }

    private void ApplyPostDBChanges(Context context)
    {
        var cfg = context.config;
        var log = cfg.dev.muteProgressOnServerLoad
            ? null
            : context.logger;

        SafelyRunIf(cfg.algorithmicalRebalancing.enable, () => AlgoRebalance.Core.AlgorithmicallyRebalance(context), log,
            "Running algorithmical rebalancing...",
            "Failed to run algorithmical rebalancing!");

        // SafelyRunIf(true, () => ChangeStackSizes(context), log,
        //     "Changing stack sizes...",
        //     "Failed to apply changes to stack sizes!");

        SafelyRunIf(cfg.secureContainerProgression.enable, () => SecureContainerChanges.Apply(context), log,
            "Applying secure container changes...",
            "Failed to apply secure container changes!");

        SafelyRunIf(cfg.stashProgression.enable, () => StashChanges.Apply(context), log,
            "Applying stash progression changes...",
            "Failed to apply stash progression changes!");

        SafelyRunIf(cfg.fleaMarketChanges.disableFleaMarket, () => FleaChanges.Apply(context), log,
            "Disabling flea market...",
            "Failed to disable flea market!");

        SafelyRunIf(cfg.hideoutBuildsChanges.enable, () => BuildChanges.Apply(context), log,
            "Applying changes to hideout build costs...",
            "Failed to apply changes to hideout build costs!");

        SafelyRunIf(cfg.skillChanges.enable, () => SkillChanges.Apply(context), log,
            "Applying changes to skills...",
            "Failed to apply changes to skills!");

        SafelyRunIf(true, () => CraftingChanges.Apply(context), log,
            "Applying changes to craft times and output counts...",
            "Failed to apply changes to craft times and output counts!");

        SafelyRunIf(true, () => PriceChanges.Apply(context), log,
            "Applying changes to item prices...",
            "Failed to apply changes to item prices!");

        SafelyRunIf(cfg.siccBuffs.enable, () => SICCCaseChanges.Apply(context), log,
            "Applying changes to SICC container...",
            "Failed to apply changes to SICC container!");

        SafelyRunIf(cfg.misc.removeFirFromQuests, () => FirChanges.RemoveFirFromQuests(context), log,
            "Removing FiR requirements from quests...",
            "Failed to remove FiR requirements from quests!");
            
        SafelyRunIf(cfg.misc.removeFirFromQuests, () => FirChanges.RemoveFirFromRepeatables(context), log,
            "Removing FiR requirements from repeatable quests...",
            "Failed to remove FiR requirements from repeatable quests!");

        SafelyRunIf(cfg.misc.removeFirFromHideout, () => FirChanges.RemoveFirFromHideout(context), log,
            "Removing FiR requirements from hideout builds...",
            "Failed to remove FiR requirements from hideout builds!");

        SafelyRunIf(cfg.misc.removeFirFromFlea, () => FirChanges.RemoveFirFromFlea(context), log,
            "Removing FiR requirements from flea market listings...",
            "Failed to remove FiR requirements from flea market listings!");

        SafelyRunIf(cfg.misc.addCustomTrades, () => AdditionalTradesChanges.Apply(context), log,
            "Adding custom trades...",
            "Failed to add custom trades!");

        SafelyRunIf(cfg.bitcoinChanges.enable, () => BitcoinChanges.Apply(context), log,
            "Applying changes to bitcoin farming...",
            "Failed to apply changes to bitcoin farming!");

        SafelyRunIf(cfg.overrideInitialStanding.enable, () => TraderStartRepChanges.Apply(context), log,
            "Setting initial trader standing...",
            "Failed to set initial trader standing!");

        SafelyRunIf(cfg.refChanges.enable, () => RefChanges.Apply(context), log,
            "Applying changes to Ref item purchasing...",
            "Failed to apply changes to Ref item purchasing!");

        SafelyRunIf(cfg.misc.enableExtraQuestRewards, () => AdditionalQuestRewardChanges.Apply(context), log,
            "Adding additional quest rewards...",
            "Failed to add additional quest rewards!");

        SafelyRunIf(cfg.misc.containerSizeChanges.enable, () => ContainerChanges.Apply(context), log,
            "Applying changes to container sizes...",
            "Failed to change sizes of containers!");
    }

    //ToDo: incorporate success logging into this, depending on success return value from called function
    private void SafelyRunIf(bool condition, Func<bool> action, ILoggerWrapper? log, string progressMessage, string failMessage)
    {
        try
        {
            log?.Info(progressMessage);

            if (condition)
            {
                if (action())
                {
                    //log?.Success("Done!");
                }
            }
            
        }
        catch (Exception ex)
        {
            logger.Error(failMessage);

            if (context.config.dev.showFullError)
            {
                logger.Error($"Error Details: {ex.Message}");
                logger.Error($"Stack Trace:\n{ex.StackTrace}");
            }
        }
    }
}
