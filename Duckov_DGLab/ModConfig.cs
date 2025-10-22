namespace Duckov_DGLab
{
    public static class ModConfig
    {
        public const string ModConfigName = "Duckov_DGLab";

        private static int _hurtDuration = 1;

        private static string? _hurtWaveType;

        private static int _deathDuration = 3;

        private static string? _deathWaveType;

        public static int HurtDuration
        {
            get => _hurtDuration;
            set
            {
                _hurtDuration = value;
                ModConfigAPI.SafeSave(ModConfigName, nameof(HurtDuration), value);
            }
        }

        public static string? HurtWaveType
        {
            get => _hurtWaveType;
            set
            {
                _hurtWaveType = value;
                ModConfigAPI.SafeSave(ModConfigName, nameof(HurtWaveType), value);
            }
        }

        public static int DeathDuration
        {
            get => _deathDuration;
            set
            {
                _deathDuration = value;
                ModConfigAPI.SafeSave(ModConfigName, nameof(DeathDuration), value);
            }
        }

        public static string? DeathWaveType
        {
            get => _deathWaveType;
            set
            {
                _deathWaveType = value;
                ModConfigAPI.SafeSave(ModConfigName, nameof(DeathWaveType), value);
            }
        }

        public static void Initialize()
        {
            Uninitialize();
            LoadConfig();

            ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnOptionsChanged);
            ModConfigAPI.SafeAddInputWithSlider(ModConfigName, nameof(HurtDuration), "Hurt Wave Duration",
                typeof(int), 1, new(1, 5));
            ModConfigAPI.SafeAddInputWithSlider(ModConfigName, nameof(HurtWaveType), "Hurt Wave Type",
                typeof(string), "");
            ModConfigAPI.SafeAddInputWithSlider(ModConfigName, nameof(DeathDuration), "Death Wave Duration",
                typeof(int), 3, new(1, 5));
            ModConfigAPI.SafeAddInputWithSlider(ModConfigName, nameof(DeathWaveType), "Death Wave Type",
                typeof(string), "");

            ModLogger.Log("Config Initialized.");
        }

        public static void Uninitialize()
        {
            ModConfigAPI.SafeRemoveOnOptionsChangedDelegate(OnOptionsChanged);
        }

        private static void LoadConfig()
        {
            _hurtDuration = ModConfigAPI.SafeLoad(ModConfigName, nameof(HurtDuration), 1);
            _hurtWaveType = ModConfigAPI.SafeLoad<string>(ModConfigName, nameof(HurtDuration));
            _deathDuration = ModConfigAPI.SafeLoad(ModConfigName, nameof(DeathDuration), 3);
            _deathWaveType = ModConfigAPI.SafeLoad<string>(ModConfigName, nameof(DeathDuration));

            ModLogger.Log("Config Loaded.");
        }

        private static void OnOptionsChanged(string optionName)
        {
            switch (optionName)
            {
                case nameof(HurtDuration):
                    _hurtDuration = ModConfigAPI.SafeLoad(ModConfigName, nameof(HurtDuration), 1);
                    ModLogger.Log($"HurtDuration changed to {_hurtDuration}");
                    break;
                case nameof(HurtWaveType):
                    _hurtWaveType = ModConfigAPI.SafeLoad<string>(ModConfigName, nameof(HurtWaveType));
                    ModLogger.Log($"HurtWaveType changed to {_hurtWaveType}");
                    break;
                case nameof(DeathDuration):
                    _deathDuration = ModConfigAPI.SafeLoad(ModConfigName, nameof(DeathDuration), 3);
                    ModLogger.Log($"DeathDuration changed to {_deathDuration}");
                    break;
                case nameof(DeathWaveType):
                    _deathWaveType = ModConfigAPI.SafeLoad<string>(ModConfigName, nameof(DeathWaveType));
                    ModLogger.Log($"DeathWaveType changed to {_deathWaveType}");
                    break;
            }
        }
    }
}