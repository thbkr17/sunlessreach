using System;
using UnityEngine;

namespace SunlessReach.Core
{
    public static class EventBus
    {
        // Health
        public static event Action<int, int> OnHealthChanged;        // current, max
        public static event Action OnPlayerDied;
        public static event Action OnPlayerRespawned;

        // Souls
        public static event Action<int, int> OnSoulsChanged;         // current, max

        // Money
        public static event Action<int> OnMoneyChanged;              // total

        // Abilities
        public static event Action<AbilityType> OnAbilityUnlocked;

        // Combat
        public static event Action<Vector3> OnEnemyDamaged;          // position
        public static event Action<Vector3> OnEnemyKilled;           // position

        // Game flow
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action OnBossDefeated;
        public static event Action OnVictory;

        // Boss UI / spectacle
        public static event Action<string, int> OnBossEncountered;   // name, maxHealth
        public static event Action<int, int> OnBossHealthChanged;    // current, max
        public static event Action<int> OnBossPhaseChanged;          // newPhase

        // Screen feedback
        public static event Action<float, float> OnScreenShake;      // magnitude, duration
        public static event Action<float> OnHitstop;                 // duration (real seconds)
        public static event Action<Color, float> OnScreenFlash;      // color (alpha = peak), duration

        // Waves
        public static event Action OnWaveCompleted;

        // Shop
        public static event Action OnShopOpened;
        public static event Action OnShopClosed;

        // Checkpoint
        public static event Action<Vector3> OnCheckpointActivated;   // position

        public static void RaiseHealthChanged(int current, int max) => OnHealthChanged?.Invoke(current, max);
        public static void RaisePlayerDied() => OnPlayerDied?.Invoke();
        public static void RaisePlayerRespawned() => OnPlayerRespawned?.Invoke();
        public static void RaiseSoulsChanged(int current, int max) => OnSoulsChanged?.Invoke(current, max);
        public static void RaiseMoneyChanged(int total) => OnMoneyChanged?.Invoke(total);
        public static void RaiseAbilityUnlocked(AbilityType type) => OnAbilityUnlocked?.Invoke(type);
        public static void RaiseEnemyDamaged(Vector3 pos) => OnEnemyDamaged?.Invoke(pos);
        public static void RaiseEnemyKilled(Vector3 pos) => OnEnemyKilled?.Invoke(pos);
        public static void RaiseGamePaused() => OnGamePaused?.Invoke();
        public static void RaiseGameResumed() => OnGameResumed?.Invoke();
        public static void RaiseBossDefeated() => OnBossDefeated?.Invoke();
        public static void RaiseVictory() => OnVictory?.Invoke();
        public static void RaiseWaveCompleted() => OnWaveCompleted?.Invoke();
        public static void RaiseShopOpened() => OnShopOpened?.Invoke();
        public static void RaiseShopClosed() => OnShopClosed?.Invoke();
        public static void RaiseCheckpointActivated(Vector3 pos) => OnCheckpointActivated?.Invoke(pos);
        public static void RaiseBossEncountered(string name, int max) => OnBossEncountered?.Invoke(name, max);
        public static void RaiseBossHealthChanged(int cur, int max) => OnBossHealthChanged?.Invoke(cur, max);
        public static void RaiseBossPhaseChanged(int phase) => OnBossPhaseChanged?.Invoke(phase);
        public static void RaiseScreenShake(float mag, float dur) => OnScreenShake?.Invoke(mag, dur);
        public static void RaiseHitstop(float dur) => OnHitstop?.Invoke(dur);
        public static void RaiseScreenFlash(Color c, float dur) => OnScreenFlash?.Invoke(c, dur);
    }

    public enum AbilityType
    {
        Dash,
        DoubleJump
    }
}
