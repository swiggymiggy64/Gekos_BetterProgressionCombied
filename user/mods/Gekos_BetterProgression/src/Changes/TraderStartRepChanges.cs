using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace GekosBetterProgression.Changes;

public static class TraderStartRepChanges
{ 
    public static bool Apply(Context context)
    {
        double initialStanding = context.config.overrideInitialStanding.defaultOverride;

        foreach (KeyValuePair<string, ProfileSides> item in context.templateTable.Profiles)
        {
            foreach (var template in new TemplateSide[] { item.Value.Bear, item.Value.Usec })
            {
                template.Trader.InitialStanding["default"] = initialStanding;

                foreach (var traderId in context.tradersTable.Keys)
                {
                    template.Trader.InitialStanding[traderId] = initialStanding;
                }

                foreach (var traderStanding in context.config.overrideInitialStanding.individualOverrides)
                {
                    template.Trader.InitialStanding[traderStanding.Key] = traderStanding.Value;
                }
            }
        }

        return true;
    }
}
