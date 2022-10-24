using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ObjectPing
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ObjectPingPlugin : BaseUnityPlugin
    {
        internal const string ModName = "ObjectPing";
        internal const string ModVersion = "2.0.4";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource ObjectPingLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        internal static GameObject _placementmarkercopy = null!;
        internal static GameObject _placementmarkerContainer = null!;

        public void Awake()
        {
            _keyboardShortcut = Config.Bind("1 - General", "Ping Keyboard Shortcut", KeyboardShortcut.Empty,
                new ConfigDescription("Set up your keys you'd like to use to trigger the ping.",
                    new AcceptableShortcuts()));

            _placementmarkercopy = new GameObject("PingPrefab");
            DontDestroyOnLoad(_placementmarkercopy);
            _placementmarkerContainer = new GameObject("PingPlacementMarker Container");
            DontDestroyOnLoad(_placementmarkerContainer);
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
                ObjectPingLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                ObjectPingLogger.LogError($"There was an issue loading your {ConfigFileName}");
                ObjectPingLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        // private static ConfigEntry<bool> _serverConfigLocked = null!;
        internal static ConfigEntry<KeyboardShortcut> _keyboardShortcut = null!;
        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", KeyboardShortcut.AllKeyCodes);
        }

        #endregion
    }
}