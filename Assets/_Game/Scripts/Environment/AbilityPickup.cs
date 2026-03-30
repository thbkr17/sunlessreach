using UnityEngine;
using SunlessReach.Data;
using SunlessReach.Core;

namespace SunlessReach.Environment
{
    public class AbilityPickup : MonoBehaviour
    {
        [SerializeField] private AbilityType abilityType;
        [SerializeField] private GameState gameState;
        [SerializeField] private AudioClip pickupClip;

        private void Start()
        {
            if (gameState == null)
                gameState = Resources.FindObjectsOfTypeAll<GameState>()[0];
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            switch (abilityType)
            {
                case AbilityType.Dash:
                    if (gameState.hasDash) return;
                    gameState.hasDash = true;
                    break;
                case AbilityType.DoubleJump:
                    if (gameState.hasDoubleJump) return;
                    gameState.hasDoubleJump = true;
                    break;
            }

            EventBus.RaiseAbilityUnlocked(abilityType);
            UiSfx.Play(pickupClip);
            Destroy(gameObject);
        }
    }
}
