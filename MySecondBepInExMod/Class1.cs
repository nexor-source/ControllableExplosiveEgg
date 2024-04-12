using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;

namespace ControllableExplosiveEgg
{
    [BepInPlugin(modGUID, modName, modVersion)]
    // [BepInPlugin("nexor.MyFirstBepInExMod", "这是我的第2个BepIn插件", "1.0.0.0")]
    public class ControllableExplosiveEgg : BaseUnityPlugin
    {
        private const string modGUID = "nexor.ControllableExplosiveEgg";
        private const string modName = "ControllableExplosiveEgg";
        private const string modVersion = "0.0.5";
        
        private readonly Harmony harmony = new Harmony(modGUID);
        public ConfigEntry <float> my_explode_chance;
        public ConfigEntry <bool> my_explode_prediction;
        public ConfigEntry <bool> my_better_egg;
        public bool debug = true;
        public static ControllableExplosiveEgg Instance;

        // 在插件启动时会直接调用Awake()方法
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            // 使用Debug.Log()方法来将文本输出到控制台
            my_better_egg = ((BaseUnityPlugin)this).Config.Bind<bool>("Controllable Explosive Egg Config",
                "Whether to enable better egg mode",
                true,
                "是否启用更好的彩蛋模式，即G键丢下彩蛋不爆炸，左键丢出彩蛋必爆炸 (仅对自己有效)\n" +
                "Whether to enable a better Easter egg mode, that is, the G button will not explode when the Easter egg is dropped, and the left-click will explode when the Easter egg is thrown (only works on yourself)");

            my_explode_chance = ((BaseUnityPlugin)this).Config.Bind<float>("Controllable Explosive Egg Config",
                "Egg Explode Chance (%)", 
                16, 
                "你可以在这里修改全局的彩蛋爆炸的概率 (仅做server时有效)\n" +
                "You can modify the probability of every egg's exploding here (only works on server)");

            my_explode_prediction = ((BaseUnityPlugin)this).Config.Bind<bool>("Controllable Explosive Egg Config",
                "Explode Prediction",
                true,
                "当彩蛋出现在手上时游戏会计算该彩蛋是否会爆炸，是否将结果展示给你？(仅对自己有效) 如果开启了更好的彩蛋模式，则自动忽略本项\n" +
                "When the Easter egg appears in hand, the game accountant calculates whether the egg will explode and displays the result to you? (only works on yourself). If Better Easter Egg Mode is enabled, this item will be automatically ignored");
            

            if (my_explode_chance.Value >= 99.99f) my_explode_chance.Value = 99.99f;
            harmony.PatchAll();
            ((ControllableExplosiveEgg)this).Logger.LogInfo((object)"ControllableExplosiveEgg 0.0.5 loaded.");
        }
    }
}

namespace ControllableExplosiveEgg.Patches.Items
{
    // Server 处理
    [HarmonyPatch(typeof(StunGrenadeItem))] // 目标类 StunGrenadeItem
    [HarmonyPatch("EquipItem")] // 目标方法 EquipItem
    internal class StunGrenadeItem_EquipItem_Patch
    {
        [HarmonyPrefix] // 前置补丁
        // 如果是彩蛋，就设置概率为xxx
        private static void Prefix(StunGrenadeItem __instance)
        {
            if (__instance.dontRequirePullingPin)
            {
                // 将 chanceToExplode 的值设为 x
                __instance.chanceToExplode = ControllableExplosiveEgg.Instance.my_explode_chance.Value;
                
                // Debug.Log("set Chance to explode to: " + __instance.chanceToExplode);
            }
            /*else
            {
                Debug.Log("Chance to explode: " + __instance.chanceToExplode);
            }*/
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB))]
    [HarmonyPatch("GrabObject")]
    internal class PlayerControllerB_GrabObject_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(PlayerControllerB __instance)
        {
            if (!ControllableExplosiveEgg.Instance.my_better_egg.Value && ControllableExplosiveEgg.Instance.my_explode_prediction.Value && __instance == StartOfRound.Instance.localPlayerController)
            {
                // Debug.Log("item grabbed, check if it is stungrenade");
                FieldInfo currentlyGrabbingObjectField = typeof(PlayerControllerB).GetField("currentlyGrabbingObject", BindingFlags.NonPublic | BindingFlags.Instance);
                GrabbableObject currentlyGrabbingObject = (GrabbableObject)currentlyGrabbingObjectField.GetValue(__instance);

                // 检查当前抓取的对象是否是 StunGrenadeItem 类型的实例
                if (currentlyGrabbingObject is StunGrenadeItem)
                {
                    // Debug.Log("yes, we got: " + currentlyGrabbingObject.itemProperties.itemName);
                    // 使用反射获取 explodeOnThrow 字段的值
                    FieldInfo explodeOnThrowField = typeof(StunGrenadeItem).GetField("explodeOnThrow", BindingFlags.NonPublic | BindingFlags.Instance);
                    bool explodeOnThrowValue = (bool)explodeOnThrowField.GetValue(currentlyGrabbingObject);

                    // 检查 itemName 是否包含 "egg" 并且 explodeOnThrow 字段为 true
                    if (currentlyGrabbingObject.itemProperties.itemName.Contains("egg") && explodeOnThrowValue)
                    {
                        HUDManager.Instance.DisplayTip("Warning!", "Will explode on next drop!!!");
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB))]
    [HarmonyPatch("SwitchToItemSlot")]
    internal class PlayerControllerB_SwitchToItemSlot_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(PlayerControllerB __instance, int slot)
        {
            if (!ControllableExplosiveEgg.Instance.my_better_egg.Value && ControllableExplosiveEgg.Instance.my_explode_prediction.Value && __instance == StartOfRound.Instance.localPlayerController)
            {
                // Debug.Log("item switched, check if it is stungrenade");
                FieldInfo currentlyGrabbingObjectField = typeof(PlayerControllerB).GetField("currentlyGrabbingObject", BindingFlags.NonPublic | BindingFlags.Instance);
                // GrabbableObject currentlyGrabbingObject = (GrabbableObject)currentlyGrabbingObjectField.GetValue(__instance);

                GrabbableObject switched_item = __instance.ItemSlots[slot];
                // 检查当前抓取的对象是否是 StunGrenadeItem 类型的实例
                if (switched_item is StunGrenadeItem)
                {
                    // Debug.Log("yes, we got: " + switched_item.itemProperties.itemName);
                    // 使用反射获取 explodeOnThrow 字段的值
                    FieldInfo explodeOnThrowField = typeof(StunGrenadeItem).GetField("explodeOnThrow", BindingFlags.NonPublic | BindingFlags.Instance);
                    bool explodeOnThrowValue = (bool)explodeOnThrowField.GetValue(switched_item);

                    // 检查 itemName 是否包含 "egg" 并且 explodeOnThrow 字段为 true
                    if (switched_item.itemProperties.itemName.Contains("egg") && explodeOnThrowValue)
                    {
                        HUDManager.Instance.DisplayTip("Warning!", "Will explode on next drop!!!");
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB))]
    [HarmonyPatch("DiscardHeldObject")]
    internal class PlayerControllerB_DiscardHeldObject_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(PlayerControllerB __instance, bool placeObject = false)
        {
            if (ControllableExplosiveEgg.Instance.my_better_egg.Value && __instance == StartOfRound.Instance.localPlayerController)
            {
                // Debug.Log("discard held object");
                // Debug.Log("placeObject:" + placeObject);
                // Debug.Log("is stungrenade? is egg?"+(__instance.currentlyHeldObjectServer is StunGrenadeItem) + __instance.currentlyHeldObjectServer.itemProperties.itemName.Contains("egg"));
                if (__instance.currentlyHeldObjectServer is StunGrenadeItem && __instance.currentlyHeldObjectServer.itemProperties.itemName.Contains("egg") && __instance == StartOfRound.Instance.localPlayerController)
                {
                    FieldInfo explodeOnThrowField = typeof(StunGrenadeItem).GetField("explodeOnThrow", BindingFlags.NonPublic | BindingFlags.Instance);
                    // 如果丢出去，则爆炸
                    if (placeObject)
                    {
                        // Debug.Log("EXPLODE!");
                        explodeOnThrowField.SetValue(__instance.currentlyHeldObjectServer, true);
                    }
                    // 如果丢地上，则不爆炸
                    else
                    {
                        // Debug.Log("WONT EXPLODE!");
                        explodeOnThrowField.SetValue(__instance.currentlyHeldObjectServer, false);
                    }
                }
            }
            
        }
    }

}