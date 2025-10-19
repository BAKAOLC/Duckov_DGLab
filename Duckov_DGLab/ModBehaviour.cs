using UnityEngine;

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
            DgLabController = new();
            GameEventHandler = new(DgLabController);
            // ReSharper disable once AsyncApostle.AsyncWait
            DgLabController.InitializeAsync().Wait();
            Debug.Log("Duckov DGLab Loaded");
        }

        private void OnEnable()
        {
            if (GameEventHandler == null) return;
            GameEventHandler.Load();
            GameEventHandler.Active = true;
        }

        private void OnDisable()
        {
            if (GameEventHandler == null) return;
            GameEventHandler.Unload();
            GameEventHandler.Active = false;
        }

        private void OnDestroy()
        {
            DgLabController?.Dispose();
            DgLabController = null;
            GameEventHandler = null;
        }
    }
}