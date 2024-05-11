﻿using BepInEx;
using Eremite;
using Eremite.Controller;
using Eremite.Model;
using Eremite.Model.Effects;
using Eremite.Model.State;
using Eremite.Services;
using HarmonyLib;
using System.Collections.Generic;

namespace MyFirstMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        private Harmony harmony;

        private void Awake()
        {
            Instance = this;
            harmony = Harmony.CreateAndPatchAll(typeof(Plugin));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");            
        }             

        [HarmonyPatch(typeof(MainController), nameof(MainController.InitReferences))]
        [HarmonyPostfix]
        private static void PostSetupMainController()
        {
            Instance.Logger.LogInfo("Initializing post Init References in MainController");
            CustomCornerstones.CreateNewCornerstones();
            Utils.SetSeasonsTime(20f, 20f, 10f, SeasonQuarter.First); // makes cornerstones appear immediately with game start
        }

        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.GenerateRewardsFor))]
        [HarmonyPrefix]
        private static bool GenerateRewardsFor_PrePatch(CornerstonesService __instance, SeasonRewardModel model, ref string viewConfiguration, ref bool isExtra)
        {
            Log.Info(string.Format("[Cor] Generate{0} cornerstones for {1} {2} {3} with model {4} {5} {6}",
            [
                __instance.GetExtraLogSufix(isExtra),
                Serviceable.CalendarService.Year,
                Serviceable.CalendarService.Season,
                Serviceable.CalendarService.Quarter,
                model.year,
                model.season,
                model.quarter
            ]), null);

            List<string> effects = [$"{PluginInfo.PLUGIN_GUID}_Honeytraps", "Resolve for Glade", $"{PluginInfo.PLUGIN_GUID}_Humble Bundles"];

            RewardPickState reward = new()
            {
                seed = 1,
                id = Serviceable.TwitchService.GetUniqueTwitchId(),
                options = effects,
                isExtra = isExtra,
                viewConfiguration = viewConfiguration,
                date = new GameDate
                {
                    year = model.year,
                    season = model.season,
                    quarter = model.quarter
                }
            };

            __instance.Picks.Add(reward);
            __instance.OnPicksChanged.OnNext();

            return false;
        }

        [HarmonyPatch(typeof(MainController), nameof(MainController.OnServicesReady))]
        [HarmonyPostfix]
        private static void HookMainControllerSetup()
        {
            // This method will run after game load (Roughly on entering the main menu)
            // At this point a lot of the game's data will be available.
            // Your main entry point to access this data will be `Serviceable.Settings` or `MainController.Instance.Settings`
            Instance.Logger.LogInfo($"Performing game initialization on behalf of {PluginInfo.PLUGIN_GUID}.");
            Instance.Logger.LogInfo($"The game has loaded {MainController.Instance.Settings.effects.Length} effects.");            
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.StartGame))]
        [HarmonyPostfix]
        private static void HookEveryGameStart()
        {
            // Too difficult to predict when GameController will exist and I can hook observers to it
            // So just use Harmony and save us all some time. This method will run after every game start
            var isNewGame = MB.GameSaveService.IsNewGame();
            Instance.Logger.LogInfo($"Entered a game. Is this a new game: {isNewGame}.");
        }

        [HarmonyPatch(typeof(HookedEffectModel), nameof(HookedEffectModel.GetAmountText))]
        [HarmonyPostfix]
        private static void HookedEffectModel_GetAmountText_Postfix(ref string __result, HookedEffectModel __instance)
        {
            // If we are not using nested amounts, then it will use amount texts instead. No further handling needed.
            if (!__instance.hasNestedAmount) return;

            // If amount text has any value, then prefix it to the result.
            if (__instance.amountText != null)
            {
                __result = __instance.amountText + __result;
            }            
        }
    }
}