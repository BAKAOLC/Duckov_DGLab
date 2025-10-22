using System;
using DGLabCSharp;
using DGLabCSharp.Enums;

namespace Duckov_DGLab
{
    public class GameEventHandler(DGLabController dgLabController)
    {
        private const int DamageDebounceMs = 200;

        private bool _active = true;

        private DateTime _lastDamageTime = DateTime.MinValue;

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                ModLogger.Log($"GameEventHandler active state set to: {_active}");
            }
        }

        public void Load()
        {
            Unload();
            LevelManager.OnAfterLevelInitialized += OnInitialize;
        }

        public void Unload()
        {
            LevelManager.OnAfterLevelInitialized -= OnInitialize;
        }

        public async void OnPlayerHurt(DamageInfo damageInfo)
        {
            if (!dgLabController.IsInitialized || !Active)
                return;

            try
            {
                var currentTime = DateTime.Now;
                var timeSinceLastDamage = (currentTime - _lastDamageTime).TotalMilliseconds;

                if (timeSinceLastDamage < DamageDebounceMs)
                    return;

                _lastDamageTime = currentTime;

                ModLogger.Log($"Player took damage: {damageInfo.GenerateDescription()}");

                var hurtWaveName = ModConfig.HurtWaveType;
                var hurtDuration = ModConfig.HurtDuration;
                var wave = string.IsNullOrWhiteSpace(hurtWaveName)
                    ? WaveData.GetWaveDataJson(WaveType.Type1)
                    : JsonSerializerFactory.Instance.Serialize(CustomWaveManager.GetWavesByName(hurtWaveName));

                await dgLabController.SendCustomWaveToAllChannelsAsync(wave, hurtDuration).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error handling player damage event: {ex}");
            }
        }

        public async void OnPlayerDeath(DamageInfo damageInfo)
        {
            if (!dgLabController.IsInitialized || !Active)
                return;

            try
            {
                ModLogger.Log("Player has died.");

                var deathWaveType = ModConfig.DeathWaveType;
                var deathDuration = ModConfig.DeathDuration;
                var wave = string.IsNullOrWhiteSpace(deathWaveType)
                    ? WaveData.GetWaveDataJson(WaveType.Type3)
                    : JsonSerializerFactory.Instance.Serialize(CustomWaveManager.GetWavesByName(deathWaveType));

                await dgLabController.SendCustomWaveToAllChannelsAsync(wave, deathDuration).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error handling player death event: {ex}");
            }
        }

        private void OnInitialize()
        {
            var mainCharacterControl = LevelManager.Instance.MainCharacter;
            if (!mainCharacterControl)
            {
                ModLogger.LogWarning("MainCharacterControl not found during initialization.");
                return;
            }

            // remove existing listeners to avoid duplicates
            mainCharacterControl.Health.OnHurtEvent.RemoveListener(OnPlayerHurt);
            mainCharacterControl.Health.OnDeadEvent.RemoveListener(OnPlayerDeath);

            // add listeners
            mainCharacterControl.Health.OnHurtEvent.AddListener(OnPlayerHurt);
            mainCharacterControl.Health.OnDeadEvent.AddListener(OnPlayerDeath);

            ModLogger.Log("GameEventHandler initialized and event listeners registered.");
        }
    }
}