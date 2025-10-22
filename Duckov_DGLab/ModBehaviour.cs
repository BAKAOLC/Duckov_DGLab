namespace Duckov_DGLab
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public DGLabController? DgLabController;
        public GameEventHandler? GameEventHandler;
        public static ModBehaviour? Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            ModLogger.Log("Duckov DGLab Loaded");
        }

        private void OnEnable()
        {
            CustomWaveManager.Initialize();
            ModConfig.Initialize();

            DgLabController = new();
            // ReSharper disable once AsyncApostle.AsyncWait
            DgLabController.InitializeAsync().Wait();

            GameEventHandler = new(DgLabController);
            GameEventHandler.Load();
            GameEventHandler.Active = true;

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

            CustomWaveManager.Uninitialize();
            ModConfig.Uninitialize();
            ModLogger.Log("Duckov DGLab Deactivated");
        }

        private void OnDestroy()
        {
            CustomWaveManager.Uninitialize();
            ModConfig.Uninitialize();

            DgLabController?.Dispose();
            DgLabController = null;
            GameEventHandler = null;

            ModLogger.Log("Duckov DGLab Destroyed");
        }
    }
}