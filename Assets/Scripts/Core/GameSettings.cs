using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>
    /// Every tunable gameplay number lives here, in one asset:
    /// Assets/Settings/GameSettings.asset. Change a value in the Inspector and press Play.
    /// </summary>
    [CreateAssetMenu(menuName = "Drowned Sanctum/Game Settings", fileName = "GameSettings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Explorer sub")]
        [Tooltip("Normal swim speed (metres per second).")]
        public float moveSpeed = 5.5f;
        [Tooltip("Speed multiplier while holding Shift.")]
        public float boostMultiplier = 1.7f;
        [Tooltip("How quickly the sub reaches its target speed (higher = snappier).")]
        public float acceleration = 6f;

        [Header("Oxygen")]
        public float maxOxygen = 100f;
        [Tooltip("Oxygen lost per second while swimming normally.")]
        public float oxygenDrainPerSecond = 0.85f;
        [Tooltip("Drain multiplier while boosting.")]
        public float boostDrainMultiplier = 2.5f;
        [Tooltip("Drain multiplier during the escape phase.")]
        public float escapeDrainMultiplier = 1.5f;
        [Tooltip("Oxygen refilled per second while inside an air vent.")]
        public float ventRefillPerSecond = 30f;

        [Header("Hazards")]
        [Tooltip("Oxygen lost when touching an urchin (the shark does 1.3x this).")]
        public float hazardOxygenDamage = 12f;
        [Tooltip("Seconds of safety after being hit.")]
        public float hitInvulnerability = 1.2f;
        public float knockbackForce = 7f;
        [Tooltip("Push strength of water currents.")]
        public float currentForce = 16f;
        public float sharkPatrolSpeed = 3f;
        public float sharkChaseSpeed = 5f;
        [Tooltip("How far along its lane the shark notices you.")]
        public float sharkDetectRange = 10f;

        [Header("Sonar (Space)")]
        public float sonarCooldown = 4f;
        public float sonarOxygenCost = 3f;
        [Tooltip("Seconds the objective arrow stays visible.")]
        public float sonarDuration = 3.5f;

        [Header("Escape")]
        [Tooltip("Seconds to get out after taking the Heart of Eternity.")]
        public float escapeTime = 90f;

        [Header("Scoring")]
        public int pearlScore = 10;
        public int sigilScore = 50;
        public int heartScore = 200;
        [Tooltip("Points per second left on the escape timer when you get out.")]
        public int timeBonusPerSecond = 5;
        [Tooltip("Points lost each time you are hit.")]
        public int hitPenalty = 20;

        [Header("Difficulty (Easy, Normal, Hard)")]
        [Tooltip("Multiplies oxygen drain and hazard damage. Picked in the main menu Options.")]
        public float[] difficultyMultipliers = { 0.6f, 1f, 1.45f };
    }
}
