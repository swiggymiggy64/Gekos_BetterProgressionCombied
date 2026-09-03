using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace GekosBetterProgression.Changes;

public static class AdditionalItemsChanges 
{
    public static bool Apply(Context context)
    {
        var buffDatabase = context.globalTable.Configuration.Health.Effects.Stimulator.Buffs;
        Dictionary<MongoId, TemplateItem> itemDatabase = context.templateTable.Items;

        foreach (var buff in context.advancedConfig.customBuffs)
        {
            buffDatabase[buff.Key] = buff.Value;
        }

        foreach (var item in context.advancedConfig.customItems)
        {
            itemDatabase[item.Key] = item.Value;
        }

        foreach (var item in context.advancedConfig.customLocales)
        {
            Utils.AddToLocale(context, item.Key, item.Value);
        }

        return true;
    }
}
