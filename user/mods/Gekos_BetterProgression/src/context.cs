using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;
using SPTarkov.DI.Annotations;

namespace GekosBetterProgression;

[Injectable(InjectionType.Singleton)]
public class Context
{
    
    public GlobalTable globalTable;
    public HideoutTable hideoutTable;
    public LocaleTable localeTable;
    public TemplateTable templateTable;
    public TradersTable tradersTable;
    public ItemHelper itemHelper;
    public PresetHelper presetHelper;
    public ProfileHelper profileHelper;
    public QuestConfig questConfig;
    public HashUtil hashUtil;
    public GekoConfig config;
    public AdvancedConfig advancedConfig;
    public ILoggerWrapper logger;
    public LocaleService localeService;


    public bool IsInitialized => config != null;

    public void PreInitialize(
        ItemHelper _itemHelper,
        PresetHelper _presetHelper,
        ProfileHelper _profileHelper,
        QuestConfig _questConfig,
        HashUtil _hashUtil,
        GekoConfig _config,
        AdvancedConfig _advancedConfig,
        ILoggerWrapper _logger,
        GlobalTable _globalTable,
        HideoutTable _hideoutTable,
        LocaleTable _localeTable,
        TemplateTable _templateTable,
        TradersTable _tradersTable,
        LocaleService _localeService
    )
    {
        this.itemHelper = _itemHelper;
        this.presetHelper = _presetHelper;
        this.profileHelper = _profileHelper;
        this.questConfig = _questConfig;
        this.hashUtil = _hashUtil;
        this.config = _config;
        this.advancedConfig = _advancedConfig;
        this.logger = _logger;
        this.globalTable = _globalTable;
        this.hideoutTable = _hideoutTable;
        this.localeTable = _localeTable;
        this.templateTable = _templateTable;
        this.tradersTable = _tradersTable;
        this.localeService = _localeService;
    }

}
