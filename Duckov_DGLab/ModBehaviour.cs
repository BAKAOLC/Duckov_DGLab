using Duckov_DGLab.Configs;
using Duckov_DGLab.MonoBehaviours;
using UnityEngine;

namespace Duckov_DGLab
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private DGLabConfigUI? _configUI;
        public DGLabController? DgLabController;
        public GameEventHandler? GameEventHandler;
        public DGLabConfig? Config { get; private set; }
        public static ModBehaviour? Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            ModLogger.Log("Duckov DGLab Loaded");
        }

        private void OnEnable()
        {
            CustomWaveManager.Initialize();
            Config = ConfigManager.LoadConfigFromFile<DGLabConfig>("DGLabConfig.json");

            DgLabController = new();
            // ReSharper disable once AsyncApostle.AsyncWait
            DgLabController.InitializeAsync().Wait();

            GameEventHandler = new(DgLabController);
            GameEventHandler.Load();
            GameEventHandler.Active = true;

            var configUIObj = new GameObject("DGLabConfigUI", typeof(DGLabConfigUI));
            DontDestroyOnLoad(configUIObj);
            _configUI = configUIObj.GetComponent<DGLabConfigUI>();

            ModLogger.Log("Duckov DGLab Activated");
        }

        private void OnDisable()
        {
            DgLabController?.Dispose();
            DgLabController = null;

            if (GameEventHandler != null)
            {
                GameEventHandler.Unload();
                GameEventHandler.Active = false;
            }

            if (_configUI != null)
            {
                Destroy(_configUI.gameObject);
                _configUI = null;
            }

            CustomWaveManager.Uninitialize();
            ModLogger.Log("Duckov DGLab Deactivated");
        }

        private void OnDestroy()
        {
            CustomWaveManager.Uninitialize();

            DgLabController?.Dispose();
            DgLabController = null;
            GameEventHandler = null;

            if (_configUI != null)
            {
                Destroy(_configUI.gameObject);
                _configUI = null;
            }

            ModLogger.Log("Duckov DGLab Destroyed");
        }
    }
}