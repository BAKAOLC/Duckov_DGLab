using System;
using DGLabCSharp.Enums;
using UnityEngine;

namespace Duckov_DGLab
{
    public class GameEventHandler(DGLabController dgLabController)
    {
        private const int DamageDebounceMs = 1000;

        private bool _active = true;

        private DateTime _lastDamageTime = DateTime.MinValue;

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                Debug.Log($"GameEventHandler active state set to: {_active}");
            }
        }

        public void Load()
        {
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

                Debug.Log($"Player took damage: {damageInfo.GenerateDescription()}");

                var (waveType, duration) = GetDamageResponse(damageInfo.damageValue);

                await dgLabController.SendWaveToAllChannelsAsync(waveType, duration).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling player damage event: {ex}");
            }
        }

        public async void OnPlayerDeath(DamageInfo damageInfo)
        {
            if (!dgLabController.IsInitialized || !Active)
                return;

            try
            {
                Debug.Log("Player has died.");

                const WaveType deathWaveType = WaveType.Type3;
                const int deathDuration = 5;

                await dgLabController.SendWaveToAllChannelsAsync(deathWaveType, deathDuration).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling player death event: {ex}");
            }
        }

        private void OnInitialize()
        {
            var mainCharacterControl = LevelManager.Instance.MainCharacter;
            if (!mainCharacterControl)
            {
                Debug.LogWarning("MainCharacterControl not found during initialization.");
                return;
            }

            mainCharacterControl.Health.OnHurtEvent.AddListener(OnPlayerHurt);
            mainCharacterControl.Health.OnDeadEvent.AddListener(OnPlayerDeath);

            Debug.Log("GameEventHandler initialized and event listeners registered.");
        }

        private static (WaveType waveType, int duration) GetDamageResponse(float damageAmount)
        {
            return damageAmount switch
            {
                _ => (WaveType.Type1, 1),
            };
        }
    }
}