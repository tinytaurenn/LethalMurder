using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;



namespace LethalMurder;

[BepInDependency("LethalNetworkAPI")]
[BepInDependency("LC_API_V50")]
[BepInDependency("evaisa.lethallib")]
[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BaseUnityPlugin
{

    const string GUID = "TinyTaurenMurder";
    const string NAME = "TinyTaurenMurder";
    const string VERSION = "1.1.0";

    private Harmony m_Harmony = new Harmony(GUID);


    internal static new ManualLogSource Logger;

    public static Plugin Instance;

    

    internal ModManager modManager;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin " + GUID + "  is loaded!");
        

        m_Harmony.PatchAll(typeof(Plugin));
        m_Harmony.PatchAll(typeof(PlayerControllerBPatch));
        m_Harmony.PatchAll(typeof(MatchLeverPatch));
        m_Harmony.PatchAll(typeof(TurretPatch));
        m_Harmony.PatchAll(typeof(BlobPatch));
        m_Harmony.PatchAll(typeof(BugPatch));
        m_Harmony.PatchAll(typeof(HauntedMaskPatch));
        m_Harmony.PatchAll(typeof(HUDManagerPatch));
        m_Harmony.PatchAll(typeof(ModSync));



        Task.Delay(10).ContinueWith(t => { CreateModManager(); }); 
        
        
    }

    public void LogMessage(string message)
    {
        Logger.LogInfo(message);
    }


    internal static void CreateModManager()
    {
        UnityEngine.Debug.Log("Creating ModManager"); 
        Logger.LogInfo("Creating ModManager");
        Logger.LogInfo("Swaaaaaaaaaaag");
       
        GameObject modManagerObject = new GameObject("ModManager");
        UnityEngine.GameObject.DontDestroyOnLoad(modManagerObject);
        modManagerObject.hideFlags = (HideFlags)61;
        ModManager mod = modManagerObject.AddComponent<ModManager>();
        Plugin.Instance.modManager = mod;

    }

    internal static ulong GetMyPlayerID()
    {
        ulong[] IDs = LethalNetworkAPI.Utils.LNetworkUtils.OtherConnectedClients;
        return 6; 
    }

    



}
