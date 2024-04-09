using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace ControllableExplosiveEgg
{
    [BepInPlugin(modGUID, modName, modVersion)]
    // [BepInPlugin("nexor.MyFirstBepInExMod", "这是我的第2个BepIn插件", "1.0.0.0")]
    public class ControllableExplosiveEgg : BaseUnityPlugin
    {
        private const string modGUID = "nexor.ControllableExplosiveEgg";
        private const string modName = "ControllableExplosiveEgg";
        private const string modVersion = "0.0.1";
        
        private readonly Harmony harmony = new Harmony(modGUID);
        public ConfigEntry <float> my_explode_chance;
        public static ControllableExplosiveEgg Instance;

        // 在插件启动时会直接调用Awake()方法
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            // 使用Debug.Log()方法来将文本输出到控制台
            // Debug.Log("Hello, world!");
            my_explode_chance = ((BaseUnityPlugin)this).Config.Bind<float>("Controllable Explosive Egg Config",
                "Egg Explode Chance (%)", 
                16, 
                "你可以修改彩蛋爆炸的概率，它必须是一个 [0,100) 以内的数，大于等于100恒不爆炸\n" +
                "You can modify the probability of an egg exploding, which must be a number within [0,100)." +
                "And a number greater than or equal to 100 will never explode");
            harmony.PatchAll();
            ((ControllableExplosiveEgg)this).Logger.LogInfo((object)"ControllableExplosiveEgg 0.0.1 loaded.");
        }
    }
}

namespace ControllableExplosiveEgg.Patches.Items
{
    [HarmonyPatch(typeof(StunGrenadeItem))] // 目标类 StunGrenadeItem
    [HarmonyPatch("EquipItem")] // 目标方法 EquipItem
    internal class StunGrenadeItem_EquipItem_Patch
    {
        [HarmonyPrefix] // 前置补丁
        private static void Prefix(StunGrenadeItem __instance)
        {
            // 将 chanceToExplode 的值设为 x
            __instance.chanceToExplode = ControllableExplosiveEgg.Instance.my_explode_chance.Value;
        }
/*
        [HarmonyPostfix] // 后置补丁
        private static void Postfix(StunGrenadeItem __instance)
        {
            // 获取共有变量 chanceToExplode 并在debug中输出变量的值
            float chanceToExplode = __instance.chanceToExplode;
            Debug.Log("Chance to explode: " + chanceToExplode);
        }*/
    }

    /*[HarmonyPatch(typeof(StunGrenadeItem))] // 目标类 StunGrenadeItem
    [HarmonyPatch("Update")] // 目标方法 Update
    internal class StunGrenadeItem_Update_Patch
    {
        [HarmonyPrefix] // 前置补丁
        private static void Prefix(StunGrenadeItem __instance)
        {
            // 使用反射获取私有字段 explodeOnThrow
            FieldInfo field = typeof(StunGrenadeItem).GetField("explodeOnThrow", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                // 将私有字段 explodeOnThrow 的值设为 true
                field.SetValue(__instance, true);
            }
        }
    }*/

    /*[HarmonyPatch(typeof(StunGrenadeItem))] // 目标类 StunGrenadeItem
    [HarmonyPatch("SetExplodeOnThrowServerRpc")] // 目标方法 SetExplodeOnThrowServerRpc
    internal class StunGrenadeItem_Update_Patch
    {
        [HarmonyPrefix] // 前置补丁
        private static void Prefix(StunGrenadeItem __instance)
        {
            // 获取私有字段 hasCollided

            FieldInfo hasCollidedField = typeof(StunGrenadeItem).GetField("hasCollided", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo gotExplodeOnThrowRPCField = typeof(StunGrenadeItem).GetField("gotExplodeOnThrowRPC", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo explodeOnThrowField = typeof(StunGrenadeItem).GetField("explodeOnThrow", BindingFlags.NonPublic | BindingFlags.Instance);

            // 使用反射获取私有字段 hasCollided 的值
            bool hasCollidedValue = (bool)hasCollidedField.GetValue(__instance);
            bool gotExplodeOnThrowRPCValue = (bool)gotExplodeOnThrowRPCField.GetValue(__instance);
            bool explodeOnThrowValue = (bool)explodeOnThrowField.GetValue(__instance);
            float chanceToExplodeValue = __instance.chanceToExplode;

            // 进行判断
            if (hasCollidedValue && gotExplodeOnThrowRPCValue)
            {
                // 输出变量值信息到控制台
                Debug.Log("hasCollided: " + hasCollidedValue);
                Debug.Log("gotExplodeOnThrowRPC: " + gotExplodeOnThrowRPCValue);
                Debug.Log("explodeOnThrow: " + explodeOnThrowValue);
                Debug.Log("chanceToExplode: " + chanceToExplodeValue);
            }
        }
    }*/


    /*[HarmonyPatch(typeof(StunGrenadeItem))] // 替换成你的类名
    [HarmonyPatch("SetExplodeOnThrowServerRpc")] // 替换成你的方法名
    internal class SetExplodeOnThrowServerRpc_Postfix
    {
        [HarmonyPostfix] // 后置补丁
        private static void Postfix(StunGrenadeItem __instance)
        {
            NetworkManager networkManager = __instance.NetworkManager;
            bool isServer = networkManager.IsServer;
            bool isHost = networkManager.IsHost;
            bool isClient = networkManager.IsClient;

            Debug.Log($"IsServer: {isServer}, IsHost: {isHost}, IsClient: {isClient}");
        }
    }

    [HarmonyPatch(typeof(StunGrenadeItem))] // 替换成你的类名
    [HarmonyPatch("SetExplodeOnThrowClientRpc")] // 替换成你的方法名
    internal class SetExplodeOnThrowClientRpc_Postfix
    {
        [HarmonyPostfix] // 后置补丁
        private static void Postfix(StunGrenadeItem __instance)
        {
            NetworkManager networkManager = __instance.NetworkManager;
            bool isServer = networkManager.IsServer;
            bool isHost = networkManager.IsHost;
            bool isClient = networkManager.IsClient;

            Debug.Log($"On clientrpc, IsServer: {isServer}, IsHost: {isHost}, IsClient: {isClient}");
        }
    }*/
}