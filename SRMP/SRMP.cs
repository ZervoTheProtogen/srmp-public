﻿using MonomiPark.SlimeRancher.Persist;
using MonomiPark.SlimeRancher.Regions;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Epic.OnlineServices;
using HarmonyLib;
using SRMultiplayer.EpicSDK;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace SRMultiplayer
{
    public class SRMP : SRSingleton<SRMP>
    {
        public static string ModDataPath { get { return Path.Combine(Application.dataPath, "..", "SRMP"); } }

        private float m_LastTimeSync;

        /// <summary>
        /// Acts as the initializer for the Mod
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            //attach scene manager to trigger event when in a menu or loading up a game
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            //attach log messager to log all game errors and exceptions into the SRMP Logs
            Application.logMessageReceived += Application_logMessageReceived;

            //load up mod specific resources 
            var myLoadedAssetBundle = AssetBundle.LoadFromMemory(Utils.ExtractResource("SRMultiplayer.srmultiplayer.dat"));
            if (myLoadedAssetBundle == null)
            {
                Log("Failed to load AssetBundle!");
                return;
            }
            //load up the Player moment animator for the Beatrix model
            Globals.BeatrixController = myLoadedAssetBundle.LoadAsset<RuntimeAnimatorController>("Controller");

            //unused prefab menus, these menus functions are handled in the floating gui
            //Globals.IngameMultiplayerMenuPrefab = myLoadedAssetBundle.LoadAsset<GameObject>("IngameMultiplayerMenu");
            //Globals.MainMultiplayerMenuPrefab = myLoadedAssetBundle.LoadAsset<GameObject>("MainMultiplayerMenu");
        }
        /// <summary>
        /// Subscriber to the Applicaiton log and process it on to the Mods console
        /// </summary>
        /// <param name="condition">Log condition</param>
        /// <param name="stackTrace">Stack trace of log strigger (if applicable)</param>
        /// <param name="type">Log Type</param>
        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            //if Error or Exception hand the error of to the Mods log/console to display
            if(type == LogType.Error || type == LogType.Exception)
            {
                Log(condition);
                if (!string.IsNullOrEmpty(stackTrace))
                    Log(stackTrace);
            }
        }

        /// <summary>
        /// After triggering base destroy
        /// trigger disconnect and shut down the server
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();

            NetworkClient.Instance.Shutdown();
            NetworkServer.Instance.Shutdown();
        }

        /// <summary>
        /// On Game quit trigger disconnect and shut down the server
        /// </summary>
        private void OnApplicationQuit()
        {
            if (Globals.IsClient)
                EpicApplication.Instance.Lobby.LeaveLobby();
            if (Globals.IsServer)
                EpicApplication.Instance.Lobby.DestroyLobby();
        }

        /// <summary>
        /// On Update triggered sync up game time
        /// </summary>
        private void Update()
        {
            if(Globals.GameLoaded)
            {
                if(Globals.IsServer)
                {
                    //every 30 seconds  send a time updater out to all clients 
                    if(Time.time - m_LastTimeSync > 30)
                    {
                        m_LastTimeSync = Time.time;
                        new PacketWorldTime()
                        {
                            Time = SRSingleton<SceneContext>.Instance.TimeDirector.WorldTime()
                        }.Send();
                    }
                }

                //if(Time.time - m_LastActorTime > 0.5f)
                //{
                //    foreach(var actor in Globals.Actors.Values.ToList())
                //    {
                //        if(actor.IsLocal && !actor.gameObject.activeInHierarchy)
                //        {
                //            actor.DropOwnership();
                //            //Log($"Dropping actor {actor.name} ({actor.ID}) as it's unloaded");
                //        }
                //    }
                //}
            }
        }

        /// <summary>
        /// Handle scene changed events triggered by the game
        /// </summary>
        /// <param name="from">Scene previously</param>
        /// <param name="to">New Scene</param>
        private void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            //trigger handlers for returning or going to the main menu
            if (to.buildIndex == 2) OnMainMenuLoaded();
            //trigger handlers for loading the game
            else if (to.buildIndex == 3) OnGameLoaded();
        }

        /// <summary>
        /// Handle user changing to the main menu, whether it is start up or from saving/ being kicked out of the game
        /// </summary>
        private void OnMainMenuLoaded()
        {
            //var menuObj = Instantiate(Globals.MainMultiplayerMenuPrefab, null, false);
            //menuObj.AddComponent<NetworkClientUI>();

            //innitialize all necessary global variables
            Globals.LocalID = 0;
            Globals.DisableAchievements = false;
            Globals.GameLoaded = false;
            Globals.ClientLoaded = false;
            Globals.LocalPlayer = null;
            Globals.Audios.Clear();
            Globals.Actors.Clear();
            Globals.Regions.Clear();
            Globals.LandPlots.Clear();
            Globals.SpawnResources.Clear();
            Globals.FXPrefabs.Clear();
            Globals.AccessDoors.Clear();
            Globals.Gordos.Clear();
            Globals.PuzzleSlots.Clear();
            Globals.Switches.Clear();
            Globals.GadgetSites.Clear();
            Globals.PacketSize.Clear();
            Globals.Spawners.Clear();
            Globals.TreasurePods.Clear();
            Globals.ExchangeAcceptors.Clear();
            Globals.FireColumns.Clear();
            Globals.Kookadobas.Clear();
            Globals.LemonTrees.Clear();
            Globals.Nutcrackers.Clear();
            Globals.RaceTriggers.Clear();
            NetworkAmmo.All.Clear();

            //clean up any lingering players in the global list
            foreach (var player in Globals.Players.Values.ToList())
            {
                if(player != null && player.gameObject != null)
                {
                    Destroy(player.gameObject);
                }
            }
            Globals.Players.Clear();


            //reset the chat 
            ChatUI.Instance.Clear();
        }

        /// <summary>
        /// Handle the user loading into the multiplayer game
        /// </summary>
        private void OnGameLoaded()
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            var ranchui = Resources.FindObjectsOfTypeAll<RanchHouseUI>().FirstOrDefault();
            if (ranchui != null)
            {
                Globals.BeatrixModel = Instantiate(ranchui.beatrixPrefab.transform.GetChild(1).GetChild(0).gameObject);
                Globals.BeatrixModel.transform.localScale *= 0.75f;
                Globals.BeatrixModel.SetActive(false);

                Utils.SetLayer(Globals.BeatrixModel, 0);
            }

            foreach (var audio in Resources.FindObjectsOfTypeAll<SECTR_AudioCue>())
            {
                Globals.Audios.TryAdd(audio.name, audio);
            }

            var splashOnTrigger = GameObject.FindObjectOfType<SplashOnTrigger>();
            Globals.FXPrefabs.TryAdd(splashOnTrigger.playerSplashFX.name, splashOnTrigger.playerSplashFX);
            Globals.FXPrefabs.TryAdd(splashOnTrigger.splashFX.name, splashOnTrigger.splashFX);


            if (Globals.IsClient)
            {
                Globals.DisableAchievements = true;
                Globals.LocalPlayer.transform.SetParent(SRSingleton<SceneContext>.Instance.Player.transform, false);
                Globals.LocalPlayer.HasLoaded = true;
                foreach (var player in Globals.Players.Values.ToList())
                {
                    if (player.HasLoaded)
                    {
                        player.Spawn();
                    }
                }
                new PacketPlayerLoaded().Send();
                Log("\"PacketPlayerLoaded\" Has been sent to server successfully!");
            }
            else
            {
                //var hostMenuObj = Instantiate(Globals.IngameMultiplayerMenuPrefab, SRSingleton<PauseMenu>.Instance.transform, true);
                //hostMenuObj.AddComponent<NetworkHostUI>();
                //hostMenuObj.SetActive(false);
            }

            Globals.PauseState = PauseState.Playing;
            Globals.GameLoaded = true;

            stopwatch.Stop();
            Log($"Loaded the game in {stopwatch.ElapsedMilliseconds}ms");
        }

        private static FileStream m_LogFileStream;
        private static StreamWriter m_LogWriter;
        /// <summary>
        /// Custom message logging of a given message
        /// </summary>
        /// <param name="msg">Main message to be logged</param>
        /// <param name="prefix">Prefix to be displayed before the message. Prefix will be after time marker and before the message
        /// It will also be incapsulated in []</param>
        public static void Log(string msg, string prefix = null)
        {
            if(m_LogFileStream == null)
            {
                string n = string.Format("log-{0:yyyy-MM-dd_hh-mm-ss-tt}.txt", DateTime.Now);
                Directory.CreateDirectory(Path.Combine(ModDataPath, "Logs"));
                m_LogFileStream = File.Create(Path.Combine(ModDataPath, "Logs", n));
                m_LogWriter = new StreamWriter(m_LogFileStream);
            }

            string txt = $"[{DateTime.Now.ToString("HH:mm:ss")}]{(prefix != null ? "[" + prefix + "]" : "")} {msg}";
            Debug.Log("[SRMP]" + txt);
            m_LogWriter.WriteLine(txt);
            m_LogWriter.Flush();
        }
        /// <summary>
        /// Logs the StackTrace of the current frame. 
        /// </summary>
        // Used mainly for EOS Debugging.
        public static void LogStack()
        {
            Log(new StackTrace().ToString());
        }

        public static void CompatPatch(Harmony harmony, string className, string methodName, Type patchType)
        {
            Type ogType = AccessTools.TypeByName(className);
            MethodInfo ogMethod = AccessTools.Method(ogType, methodName);


            HarmonyMethod prefix = null;
            MethodInfo prefixInfo = patchType.GetMethod("Prefix");
            
            if (prefixInfo != null)
                prefix = new HarmonyMethod(prefixInfo);
            
            
            HarmonyMethod postfix = null;
            MethodInfo postfixInfo = patchType.GetMethod("Postfix");
            
            if (postfixInfo != null)
                postfix = new HarmonyMethod(postfixInfo);
            
            harmony.Patch(ogMethod, prefix, postfix);
        }
    }
}
