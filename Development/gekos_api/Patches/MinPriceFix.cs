using EFT;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using EFT.Trading;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace gekos_api.Patches
{
    internal class MinPriceFix : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Trader), nameof(Trader.GetUserItemPrice));
        }

        [PatchPostfix]
        static void Postfix(ref Trader.ItemPrice? __result, ref Trader __instance, Item item)
        {
			//Only bother doing the Postfix if necessary
            if (__result != null) return;

            if (__instance._supplyData == null) return;
            if (!__instance.Info.CanBuyItem(item)) return;

            //If we've safely gotten to this point then the original logic must have retunred NULL because value was close to 0
            //Let us return a value of 1 instead of NULL
			MongoID currencyId = CurrencyUtil.GetCurrencyId(__instance.Settings.Currency);
			__result = new Trader.ItemPrice?(new Trader.ItemPrice(new MongoID?(currencyId), Convert.ToInt32(1)));
		}

    }
}
