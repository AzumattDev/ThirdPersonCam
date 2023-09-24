using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Fusion;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace ThirdPersonCam
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ThirdPersonCamPlugin : BaseUnityPlugin
    {
        internal const string ModName = "ThirdPersonCam";
        internal const string ModVersion = "1.0.2";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource ThirdPersonCamLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            toggle3rdPerson = Config.Bind("1 - General", "Toggle 3rd Person", Toggle.On, "If on, third person is active.");
            autoToggle = Config.Bind("1 - General", "Auto Toggle", Toggle.On, "If on, third person will be toggled on when entering a vehicle.");
            scrollingCamera = Config.Bind("1 - General", "Can camera Scroll", Toggle.On, "If on, the camera can scroll in/out in 3rd person.");
            scrollingSensitivity = Config.Bind("1 - General", "Scroll Sensitivity", 1f, "The multiplier to use for the value on scrolling in/out in 3rd person. Higher values will scroll faster.");
            toggle3rdPersonKeys = Config.Bind("1 - Hotkeys", "Toggle Third Person", new KeyboardShortcut(KeyCode.G, KeyCode.LeftShift), new ConfigDescription("The hotkey to toggle third person mode. Best to use the configuration manager if you want to set this value quickly.", new AcceptableShortcuts()));
            toggle3rdPersonKeys.SettingChanged += (sender, args) => { ToggleThird(toggle3rdPerson.Value == Toggle.On); };
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                ThirdPersonCamLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                ThirdPersonCamLogger.LogError($"There was an issue loading your {ConfigFileName}");
                ThirdPersonCamLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        internal static void ToggleThird(bool active)
        {
            ThirdPersonCamLogger.LogInfo($"Third Person is now {(active ? "active" : "inactive")}");
            foreach (Transform transform in WorldScene.code.allPlayerDummies.items.ToList<Transform>())
            {
                PlayerDummy playerDummy;
                if (transform && transform.gameObject.activeSelf
                              && transform.TryGetComponent<PlayerDummy>(out playerDummy)
                              && playerDummy.Object && (playerDummy == Global.code.Player.playerDummy))
                {
                    foreach (Renderer componentsInChild in playerDummy.GetComponentsInChildren<Renderer>())
                    {
                        // Log the name of the renderer's game object to the console
                        ThirdPersonCamLogger.LogDebug($"Disabling {componentsInChild.gameObject.name}");
                        componentsInChild.enabled = active;
                    }
                }
            }
        }


        #region ConfigOptions

        internal static ConfigEntry<Toggle> toggle3rdPerson = null!;
        internal static ConfigEntry<Toggle> autoToggle = null!;
        internal static ConfigEntry<KeyboardShortcut> toggle3rdPersonKeys = null!;
        internal static ConfigEntry<Toggle> scrollingCamera = null!;
        internal static ConfigEntry<float> scrollingSensitivity = null!;

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order = null!;
            [UsedImplicitly] public bool? Browsable = null!;
            [UsedImplicitly] public string? Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }

        #endregion
    }
}