using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gekos_api.Helpers
{
    class ConfigHandler
    {
        private static SkillsConfig skillsConfig;
        private static PointsConfig pointsConfig;

        public static void Initialize()
        {
            var skillsResponse = SPT.Common.Http.RequestHandler.GetJson("/server-config-router/skillsconfig");
            var pointsResponse = SPT.Common.Http.RequestHandler.GetJson("/server-config-router/skillpoints");

            var loadedSkillsConfig = JsonConvert.DeserializeObject<SkillsConfig>(skillsResponse);
            var loadedPointsConfig = JsonConvert.DeserializeObject<PointsConfig>(pointsResponse);

            if (loadedSkillsConfig == null || loadedSkillsConfig.SkillMultipliers == null || loadedSkillsConfig.BuffMultis == null)
            {
                throw new InvalidOperationException("The server returned an invalid skills configuration.");
            }

            if (loadedPointsConfig == null)
            {
                throw new InvalidOperationException("The server returned an invalid skill-points configuration.");
            }

            skillsConfig = loadedSkillsConfig;
            pointsConfig = loadedPointsConfig;
        }

        public static SkillsConfig GetSkillsConfig()
        {
            return skillsConfig ?? throw new InvalidOperationException("Geko's API configuration has not been initialized.");
        }

        public static PointsConfig GetPointsConfig()
        {
            return pointsConfig ?? throw new InvalidOperationException("Geko's API configuration has not been initialized.");
        }
    }

    public class SkillsConfig
    {

        [JsonProperty("GlobalXPMultiplier")]
        public float GlobalMultiplier { get; set; }

        [JsonProperty("SkillXPMultipliers")]
        public Dictionary<string, float> SkillMultipliers { get; set; }

        [JsonProperty("SkillBuffMultipliers")]
        public Dictionary<string, float> BuffMultis { get; set; }

    }

    public class PointsConfig
    {
        [JsonProperty("enable")]
        public bool enable { get; set; }

        [JsonProperty("skillPointsPerLevel")]
        public float skillPointsPerLevel { get; set; }

        [JsonProperty("automaticallyRefundOverflows")]
        public bool automaticallyRefundOverflows { get; set; }

        [JsonProperty("enableDeallocation")]
        public bool enableDeallocation { get; set; }
    }
}
