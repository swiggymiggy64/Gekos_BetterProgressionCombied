using SPTarkov.Server.Core.Models.Common;

using SPTarkov.Server.Core.Constants;
using SPTarkov.Server.Core.Models.Enums;

namespace GekosBetterProgression.Changes;

public class SICCCaseChanges()
{
    public static bool Apply(Context context)
    {
        HashSet<MongoId> newFilter = new();

        var docsFilter = context.templateTable.Items[ItemTpl.CONTAINER_DOCUMENTS_CASE].Properties?.Grids?.First().Properties?.Filters?.First().Filter;
        var SICCFilter = context.templateTable.Items[ItemTpl.CONTAINER_SICC].Properties?.Grids?.First().Properties?.Filters?.First().Filter;

        if (SICCFilter is null)
        {
            context.logger.Error("Failed to fetch SICC container filter!");
            return false;
        }

        if (context.config.siccBuffs.canHoldWhatDocsCan)
        {
            if (docsFilter is null)
            {
                context.logger.Error("Failed to fetch docs container filter!");
            } else {
                newFilter.UnionWith(docsFilter);
            }
        }

        newFilter.UnionWith(SICCFilter);

        foreach (var item in context.config.siccBuffs.additionalWhitelistedItems)
        {
            newFilter.Add((MongoId)item);
        }

        context.templateTable.Items[ItemTpl.CONTAINER_SICC].Properties!.Grids!.First().Properties!.Filters!.First().Filter = newFilter;

        return true;
    }
}
