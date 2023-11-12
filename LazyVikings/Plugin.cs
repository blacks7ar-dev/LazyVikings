using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LazyVikings.Functions;
using LazyVikings.Utils;
using ServerSync;

namespace LazyVikings
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("org.bepinex.plugins.odinssteelworks", BepInDependency.DependencyFlags.SoftDependency)]

    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "blacks7ar.LazyVikings";
        public const string modName = "LazyVikings";
        public const string modAuthor = "blacks7ar";
        public const string modVersion = "1.0.7";
        public const string modLink = "https://valheim.thunderstore.io/package/blacks7ar/LazyVikings/";
        private static string configFileName = modGUID + ".cfg";
        private static string configFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + configFileName;
        public static readonly ManualLogSource LVLogger = BepInEx.Logging.Logger.CreateLogSource(modName);
        private static readonly Harmony _harmony = new(modGUID);

        private static readonly ConfigSync _configSync = new(modGUID)
        {
            DisplayName = modName,
            CurrentVersion = modVersion,
            MinimumRequiredVersion = modVersion
        };

        private static ConfigEntry<Toggle> _serverConfigLocked;
        public static ConfigEntry<Toggle> _enableBeehive;
        public static ConfigEntry<float> _beehiveRadius;
        public static ConfigEntry<Toggle> _enableBlastFurnace;
        public static ConfigEntry<float> _blastfurnaceRadius;
        public static ConfigEntry<Automation> _blastfurnaceAutomation;
        public static ConfigEntry<Toggle> _blastfurnaceIgnorePrivateAreaCheck;
        public static ConfigEntry<Toggle> _blastfurnaceAllowAllOres;
        public static ConfigEntry<Toggle> _enableEitrRefinery;
        public static ConfigEntry<float> _eitrrefineryRadius;
        public static ConfigEntry<Automation> _eitrrefineryAutomation;
        public static ConfigEntry<Toggle> _eitrrefineryIgnorePrivateAreaCheck;
        public static ConfigEntry<Toggle> _enableKiln;
        public static ConfigEntry<float> _kilnRadius;
        public static ConfigEntry<Automation> _kilnAutomation;
        public static ConfigEntry<Toggle> _kilnIgnorePrivateAreaCheck;
        public static ConfigEntry<Toggle> _kilnProcessAllWoods;
        public static ConfigEntry<int> _kilnProductThreshold;
        public static ConfigEntry<Toggle> _enableSmelter;
        public static ConfigEntry<float> _smelterRadius;
        public static ConfigEntry<Automation> _smelterAutomation;
        public static ConfigEntry<Toggle> _smelterIgnorePrivateAreaCheck;
        public static ConfigEntry<Toggle> _enableSpinningWheel;
        public static ConfigEntry<float> _spinningwheelRadius;
        public static ConfigEntry<Automation> _spinningwheelAutomation;
        public static ConfigEntry<Toggle> _spinningwheelIgnorePrivateAreaCheck;
        public static ConfigEntry<Toggle> _enableWindmill;
        public static ConfigEntry<float> _windmillRadius;
        public static ConfigEntry<Automation> _windmillAutomation;
        public static ConfigEntry<Toggle> _windmillIgnorePrivateAreaCheck;
        public static ConfigEntry<Toggle> _enableSapCollector;
        public static ConfigEntry<float> _sapcollectorRadius;
        public static ConfigEntry<Toggle> _enableFermenter;
        public static ConfigEntry<float> _fermenterRadius;
        public static ConfigEntry<Automation> _fermenterAutomation;
        public static ConfigEntry<Toggle> _fermenterIgnorePrivateAreaCheck;
        public static ConfigEntry<Toggle> _enableSteelKiln;
        public static ConfigEntry<float> _steelKilnRadius;
        public static ConfigEntry<Automation> _steelKilnAutomation;
        public static ConfigEntry<Toggle> _steelKilnIgnorePrivateAreaCheck;
        public static ConfigEntry<Toggle> _enableSteelSlackTub;
        public static ConfigEntry<float> _steelSlackTubRadius;
        public static ConfigEntry<Automation> _steelSlackTubAutomation;
        public static ConfigEntry<Toggle> _steelSlackTubIgnorePrivateAreaCheck;
        public static bool _hasOdinSteelWorks;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedConfig = true)
        {
            var configDescription =
                new ConfigDescription(
                    description.Description +
                    (synchronizedConfig ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            var configEntry = Config.Bind(group, name, value, configDescription);
            var syncedConfigEntry = _configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedConfig;
            return configEntry;
        }

        private void ConfigWatcher()
        {
            var watcher = new FileSystemWatcher(Paths.ConfigPath, configFileName);
            watcher.Changed += OnConfigChanged;
            watcher.Created += OnConfigChanged;
            watcher.Renamed += OnConfigChanged;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(configFileFullPath)) return;
            try
            {
                Logging.LogDebug("OnConfigChanged called..");
                Config.Reload();
            }
            catch
            {
                Logging.LogError($"There was an issue loading your {configFileName}");
                Logging.LogError("Please check your config entries for spelling and format!");
            }
        }

        public void Awake()
        {
            _serverConfigLocked = config("01- ServerSync", "Lock Configuration", Toggle.On,
                new ConfigDescription("If On, the configuration is locked and can be changed by server admins only."));
            _configSync.AddLockingConfigEntry(_serverConfigLocked);
            _enableBeehive = config("02- Beehive", "Enable", Toggle.On,
                new ConfigDescription("Enable/Disables beehive automation."));
            _beehiveRadius = config("02- Beehive", "Radius", 5f,
                new ConfigDescription("Beehives container detection range.", new AcceptableValueRange<float>(1f, 50f)));
            _enableBlastFurnace = config("03- Blast Furnace", "Enable", Toggle.On,
                new ConfigDescription("Enable/Disables blast furnace automation."));
            _blastfurnaceAutomation = config("03- Blast Furnace", "Automation", Automation.Both,
                new ConfigDescription("Choose what to automate."));
            _blastfurnaceRadius = config("03- Blast Furnace", "Radius", 5f,
                new ConfigDescription("Blast furnace container detection range.",
                    new AcceptableValueRange<float>(1f, 50f)));
            _blastfurnaceIgnorePrivateAreaCheck = config("03- Blast Furnace", "Ignore Private Area Check", Toggle.On,
                new ConfigDescription("If On, ignores private area check for blast furnaces."));
            _blastfurnaceAllowAllOres = config("03- Blast Furnace", "Allow All Ores", Toggle.Off,
                new ConfigDescription("If On, all ores will be process by the blast furnace."));
            _enableEitrRefinery = config("04- Eitr Refinery", "Enable", Toggle.On,
                new ConfigDescription("Enable/Disables eitr refinery automation."));
            _eitrrefineryAutomation = config("04- Eitr Refinery", "Automation", Automation.Both,
                new ConfigDescription("Choose what to automate."));
            _eitrrefineryRadius = config("04- Eitr Refinery", "Radius", 5f,
                new ConfigDescription("Eitr refinery container detection range.",
                    new AcceptableValueRange<float>(1f, 50f)));
            _eitrrefineryIgnorePrivateAreaCheck = config("04- Eitr Refinery", "Ignore Private Area Check", Toggle.On,
                new ConfigDescription("If On, ignores private area check for eitr refinerys."));
            _enableKiln = config("05- Kiln", "Enable", Toggle.On,
                new ConfigDescription("Enable/Disables kiln automation."));
            _kilnAutomation = config("05- Kiln", "Automation", Automation.Both,
                new ConfigDescription("Choose what to automate."));
            _kilnRadius = config("05- Kiln", "Radius", 5f,
                new ConfigDescription("Kiln container detection range.", new AcceptableValueRange<float>(1f, 50f)));
            _kilnIgnorePrivateAreaCheck = config("05- Kiln", "Ignore Private Area Check", Toggle.On,
                new ConfigDescription("If On, ignores private area check for kilns."));
            _kilnProcessAllWoods = config("05- Kiln", "Process All Woods", Toggle.Off,
                new ConfigDescription("If On, finewood and corewood will also be process into coal."));
            _kilnProductThreshold = config("05- Kiln", "Product Threshold", 0,
                new ConfigDescription("Kiln's product threshold before it stops auto fueling.\nNOTE: Set to 0 to disable.",
                    new AcceptableValueRange<int>(0, 500)));
            _enableSapCollector = config("06- SapCollector", "Enable", Toggle.On,
                new ConfigDescription("Enable/Disables sap collector automation."));
            _sapcollectorRadius = config("06- SapCollector", "Radius", 5f,
                new ConfigDescription("SapCollector container detection range.",
                    new AcceptableValueRange<float>(1f, 50f)));
            _enableSmelter = config("07- Smelter", "Enable", Toggle.On,
                new ConfigDescription("Enable/Disables smelter automation."));
            _smelterAutomation = config("07- Smelter", "Automation", Automation.Both,
                new ConfigDescription("Choose what to automate."));
            _smelterRadius = config("07- Smelter", "Radius", 5f,
                new ConfigDescription("Smelter container detection range.", new AcceptableValueRange<float>(1f, 50f)));
            _smelterIgnorePrivateAreaCheck = config("07- Smelter", "Ignore Private Area Check", Toggle.On,
                new ConfigDescription("If On, ignores private area check for smelters."));
            _enableSpinningWheel = config("08- Spinning Wheel", "Enable", Toggle.On,
                new ConfigDescription("Enable/Disables spinning wheel automation."));
            _spinningwheelAutomation = config("08- Spinning Wheel", "Automation", Automation.Both,
                new ConfigDescription("Choose what to automate."));
            _spinningwheelRadius = config("08- Spinning Wheel", "Radius", 5f,
                new ConfigDescription("Spinning wheel container detection range.",
                    new AcceptableValueRange<float>(1f, 50f)));
            _spinningwheelIgnorePrivateAreaCheck = config("08- Spinning Wheel", "Ignore Private Area Check", Toggle.On,
                new ConfigDescription("If On, ignores private area check for spinning wheels."));
            _enableWindmill = config("09- Windmill", "Enable", Toggle.On,
                new ConfigDescription("Enable/Disables windmill automation."));
            _windmillAutomation = config("09- Windmill", "Automation", Automation.Both,
                new ConfigDescription("Choose what to automate."));
            _windmillRadius = config("09- Windmill", "Radius", 5f,
                new ConfigDescription("Windmill container detection range.", new AcceptableValueRange<float>(1f, 50f)));
            _windmillIgnorePrivateAreaCheck = config("09- Windmill", "Ignore Private Area Check", Toggle.On,
                new ConfigDescription("If On, ignores private area check for windmills."));
            _enableFermenter = config("10- Fermenter", "Enable", Toggle.On,
                new ConfigDescription("Enable/Disables fermenter automation."));
            _fermenterAutomation = config("10- Fermenter", "Automation", Automation.Both,
                new ConfigDescription("Choose what to automate."));
            _fermenterRadius = config("10- Fermenter", "Radius", 5f,
                new ConfigDescription("Fermenter container detection range.",
                    new AcceptableValueRange<float>(1f, 50f)));
            _fermenterIgnorePrivateAreaCheck = config("10- Fermenter", "Ignore Private Area Check", Toggle.On,
                new ConfigDescription("If On, ignores private area check for fermenters."));
            _hasOdinSteelWorks = Helper.CheckOdinSteelWorksMod();
            if (_hasOdinSteelWorks)
            {
                _enableSteelKiln = config("11- Steel Kiln", "Enable", Toggle.On,
                    new ConfigDescription("Enable/Disables steel kiln automation."));
                _steelKilnAutomation = config("11- Steel Kiln", "Automation", Automation.Both,
                    new ConfigDescription("Choose what to automate."));
                _steelKilnRadius = config("11- Steel Kiln", "Radius", 5f,
                    new ConfigDescription("Steel Kiln container detection range.",
                        new AcceptableValueRange<float>(1f, 50f)));
                _steelKilnIgnorePrivateAreaCheck = config("11- Steel Kiln", "Ignore Private Area Check", Toggle.On,
                    new ConfigDescription("If On, ignores private area check for steel kiln."));
                _enableSteelSlackTub = config("12- Steel Slack Tub", "Enable", Toggle.On,
                    new ConfigDescription("Enable/Disables steel slack tub automation."));
                _steelSlackTubAutomation = config("12- Steel Slack Tub", "Automation", Automation.Both,
                    new ConfigDescription("Choose what to automate."));
                _steelSlackTubRadius = config("12- Steel Slack Tub", "Radius", 5f,
                    new ConfigDescription("Steel slack tub container detection range.",
                        new AcceptableValueRange<float>(1f, 50f)));
                _steelSlackTubIgnorePrivateAreaCheck = config("12- Steel Slack Tub", "Ignore Private Area Check",
                    Toggle.On, new ConfigDescription("If On, ignores private area check for steel slack tub."));
            }
            var assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            ConfigWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }
    }
}