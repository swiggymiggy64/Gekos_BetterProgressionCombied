using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GekosBetterProgression
{
    [Injectable(TypePriority = OnLoadOrder.Routers - 1)]
    public class Router(JsonUtil jsonUtil, Callbacks callbacks) : StaticRouter(jsonUtil, [
            new RouteAction<EmptyRequestData>(
                "/server-config-router/skillpoints", (_, _, _, _, _) => callbacks.HandleGetSkillPointConfig()
            ),
            new RouteAction<EmptyRequestData>(
                "/server-config-router/skillsconfig", (_, _, _, _, _) => callbacks.HandleGetSkillsConfig()
            )
        ])
    { }

    [Injectable]
    public class Callbacks(JsonUtil jsonUtil, HttpResponseUtil httpResponseUtil, Context context)
    {
        public ValueTask<string> HandleGetSkillPointConfig()
        {
            return new ValueTask<string>(jsonUtil.Serialize(context.config.skillChanges.skillPointsSystem));
        }

        public ValueTask<string> HandleGetSkillsConfig()
        {
            return new ValueTask<string>(jsonUtil.Serialize(context.config.skillChanges.customMultipliers));
        }
    }
}
