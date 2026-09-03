using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using EFT;

namespace gekos_api.Patches
{
    class LevelExpFix : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Skill), nameof(Skill.LevelExp));
        }

        [PatchPrefix]
        static bool Prefix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = true;
            return true;
        }

        [PatchPostfix]
        static void Postfix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = false;
        }

    }

    class CalculateExpOnFirstLevelsFix : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Skill), nameof(Skill.CalculateExpOnFirstLevels));
        }

        [PatchPrefix]
        static bool Prefix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = true;
            return true;
        }

        [PatchPostfix]
        static void Postfix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = false;
        }
    }

    class BaseProgressFix : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Skill), nameof(Skill.BaseProgress));
        }

        [PatchPrefix]
        static bool Prefix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = true;
            return true;
        }

        [PatchPostfix]
        static void Postfix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = false;
        }
    }

    class ProgressValueFix : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Skill), nameof(Skill.ProgressValue));
        }

        [PatchPrefix]
        static bool Prefix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = true;
            return true;
        }

        [PatchPostfix]
        static void Postfix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = false;
        }
    }

    class OnTriggerFix : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Skill), nameof(Skill.OnTrigger));
        }

        [PatchPrefix]
        static bool Prefix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = true;
            return true;
        }

        [PatchPostfix]
        static void Postfix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = false;
        }
    }

    class Method4Fix : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BaseSkill), nameof(BaseSkill.GetLevelForValue));
        }

        [PatchPrefix]
        static bool Prefix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = true;
            return true;
        }

        [PatchPostfix]
        static void Postfix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = false;
        }
    }

    class LevelProgressFix : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(BaseSkill), nameof(BaseSkill.LevelProgress));
        }

        [PatchPrefix]
        static bool Prefix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = true;
            return true;
        }

        [PatchPostfix]
        static void Postfix()
        {
            AdditionalSkillLevels.ExposeNativeLevel = false;
        }
    }
}
