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
        private const string modVersion = "0.0.7";
        
        private readonly Harmony harmony = new Harmony(modGUID);
        public ConfigEntry <float> my_explode_chance;
        public ConfigEntry <bool> my_explode_prediction;
        public ConfigEntry <bool> my_better_egg;
        public static ControllableExplosiveEgg Instance;

        // 在插件启动时会直接调用Awake()方法
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            // 使用// Debug.Log()方法来将文本输出到控制台
            my_better_egg = ((BaseUnityPlugin)this).Config.Bind<bool>("Controllable Explosive Egg Config",
                "Whether to enable better egg mode",
                true,
                "是否启用更好的彩蛋模式，即G键丢下彩蛋不爆炸，左键丢出彩蛋必爆炸 (仅做server时对自己有效)\n" +
                "Whether to enable a better Easter egg mode, that is, the G button will not explode when the Easter egg is dropped, and the left-click will explode when the Easter egg is thrown (only server & only works on yourself)");

            my_explode_chance = ((BaseUnityPlugin)this).Config.Bind<float>("Controllable Explosive Egg Config",
                "Egg Explode Chance (%)", 
                16, 
                "你可以在这里修改全局的彩蛋爆炸的概率 (仅做server时有效)\n" +
                "You can modify the probability of everyone's egg's exploding here (only server)");

            my_explode_prediction = ((BaseUnityPlugin)this).Config.Bind<bool>("Controllable Explosive Egg Config",
                "Explode Prediction",
                true,
                "当彩蛋出现在手上时游戏会计算该彩蛋是否会爆炸，是否将结果展示给你？(仅对自己有效，且如果自己不是server则可能会误报但不会漏报) 如果更好的彩蛋模式正常工作，则自动忽略本项\n" +
                "When the Easter egg appears in hand, the game accountant calculates whether the egg will explode and displays the result to you? (only works on yourself, and if you are not a server, you may have false positives but no false negatives). If Better Easter Egg Mode is working fine, this item will be automatically ignored");
            

            if (my_explode_chance.Value >= 99.99f) my_explode_chance.Value = 99.99f;
            harmony.PatchAll();
            ((ControllableExplosiveEgg)this).Logger.LogInfo((object)"ControllableExplosiveEgg 0.0.7 loaded.");
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
        private static void Prefix(StunGrenadeItem __instance)
        {

            // 仅作为服务端时，如果有人拿起了一个物品且这个物品是蛋
            NetworkManager networkManager = __instance.NetworkManager;
            if (networkManager.IsServer && __instance.dontRequirePullingPin)
            {

                // 将 爆炸概率 设为 x
                __instance.chanceToExplode = ControllableExplosiveEgg.Instance.my_explode_chance.Value;

                // Debug.Log("equiped! set Chance to explode to: " + __instance.chanceToExplode);
            }
/*            else
            {
                Debug.Log("equiped! Chance to explode: " + __instance.chanceToExplode);
            }*/
        }
    }

    [HarmonyPatch(typeof(StunGrenadeItem))]
    [HarmonyPatch("SetExplodeOnThrowClientRpc")]
    internal class StunGrenadeItem_SetExplodeOnThrowClientRpc_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(StunGrenadeItem __instance, bool explode)
        {
            // 如果没有启用 有效的 更好的蛋模式，才会进行预测
            NetworkManager networkManager = __instance.NetworkManager;
            if (!(ControllableExplosiveEgg.Instance.my_better_egg.Value && networkManager.IsServer))
            {

                // Debug.Log("into setexplode client, explode :" + explode);
                // NetworkManager networkManager = __instance.NetworkManager;
                // Debug.Log("is server, host, client" + networkManager.IsServer + networkManager.IsHost + networkManager.IsClient);

                // 如果服务器给某个蛋返回了爆炸=true且拿着这个蛋的人是你
                if (explode && __instance.playerHeldBy == StartOfRound.Instance.localPlayerController)
                {

                    // 根据不同角色提示可能会爆炸的信息
                    
                    if (networkManager.IsServer) HUDManager.Instance.DisplayTip("Warning!", "will explode on next drop!!!");
                    else HUDManager.Instance.DisplayTip("Warning!", "might explode on next drop!!!");

                }
            }
            
        }
    }

/*    [HarmonyPatch(typeof(StunGrenadeItem))]
    [HarmonyPatch("SetExplodeOnThrowServerRpc")]
    internal class StunGrenadeItem_SetExplodeOnThrowServerRpc_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(StunGrenadeItem __instance)
        {
            NetworkManager networkManager = __instance.NetworkManager;
            Debug.Log("is server, host, client" + networkManager.IsServer + networkManager.IsHost + networkManager.IsClient);

            Debug.Log("into setexplode server");
        }
    }*/


    [HarmonyPatch(typeof(PlayerControllerB))]
    [HarmonyPatch("DiscardHeldObject")]
    internal class PlayerControllerB_DiscardHeldObject_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(PlayerControllerB __instance, bool placeObject = false)
        {

            // 如果启用了更好的蛋模式并且自己是server，当有人丢下物品且这个人是你时
            NetworkManager networkManager = __instance.NetworkManager;
            if ((ControllableExplosiveEgg.Instance.my_better_egg.Value && networkManager.IsServer) && __instance == StartOfRound.Instance.localPlayerController)
            {

                // Debug.Log("discard held object");
                // Debug.Log("placeObject:" + placeObject);
                // Debug.Log("is stungrenade? is egg?"+(__instance.currentlyHeldObjectServer is StunGrenadeItem) + __instance.currentlyHeldObjectServer.itemProperties.itemName.Contains("egg"));
                
                // 如果丢下的物品是蛋
                if (__instance.currentlyHeldObjectServer is StunGrenadeItem && __instance.currentlyHeldObjectServer.itemProperties.itemName.Contains("egg"))
                {
                    // 如果丢出去，则爆炸
                    if (placeObject)
                    {
                        ((StunGrenadeItem)__instance.currentlyHeldObjectServer).chanceToExplode = 99.99f;
                    }
                    // 如果丢地上，则不爆炸
                    else
                    {
                        ((StunGrenadeItem)__instance.currentlyHeldObjectServer).chanceToExplode = 0f;
                    }
                    // 将更改后的爆炸信息广播给所有玩家
                    ((StunGrenadeItem)__instance.currentlyHeldObjectServer).SetExplodeOnThrowServerRpc();
                }
            }
            
        }
    }

/*    [HarmonyPatch(typeof(StunGrenadeItem))]
    [HarmonyPatch("SetExplodeOnThrowServerRpc")]
    internal class StunGrenadeItem_SetExplodeOnThrowServerRpc_Patch
    {
        [HarmonyPrefix]
        private static void Prefix(StunGrenadeItem __instance)
        {

            // Debug.Log("into server setexplode");
            // Debug.Log("chance to explode is :" + __instance.chanceToExplode);
        }

        [HarmonyPostfix]
        private static void Postfix(StunGrenadeItem __instance)
        {

            // Debug.Log("server setexplode ended");
            // Debug.Log("chance to explode is :" + __instance.chanceToExplode);
            FieldInfo explodeOnThrowField = typeof(StunGrenadeItem).GetField("explodeOnThrow", BindingFlags.NonPublic | BindingFlags.Instance);
            bool explodeOnThrowValue = (bool)explodeOnThrowField.GetValue(__instance);
            // Debug.Log("will explode? :" + explodeOnThrowValue);
            // Debug.Log("broadcasting...");
        }
    }
*/

    
}