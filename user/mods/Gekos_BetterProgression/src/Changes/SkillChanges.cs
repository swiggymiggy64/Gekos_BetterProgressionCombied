namespace GekosBetterProgression.Changes;

public class SkillChanges()
{
    public static bool Apply(Context context)
    {
        var skillConfig = context.config.skillChanges;

        var eftConfig = context.globalTable.Configuration;
        eftConfig.SkillFreshEffectiveness = skillConfig.skillFreshEffectiveness;
        eftConfig.SkillFreshPoints = skillConfig.skillFreshPoints;
        eftConfig.SkillPointsBeforeFatigue = skillConfig.skillPointsBeforeFatigue;
        eftConfig.SkillMinEffectiveness = skillConfig.skillMinEffectiveness;
        
        return true;
    }
}
