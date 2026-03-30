using UnityEngine;
using SunlessReach.Data;
using SunlessReach.Core;

namespace SunlessReach.Environment
{
    public class AbilityGate : MonoBehaviour
    {
        [SerializeField] private AbilityType requiredAbility;
        [SerializeField] private GameState gameState;

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            EventBus.OnAbilityUnlocked += CheckAbility;
        }

        private void OnDisable()
        {
            EventBus.OnAbilityUnlocked -= CheckAbility;
        }

        private void Start()
        {
            if (gameState == null)
                gameState = Resources.FindObjectsOfTypeAll<GameState>()[0];
            CheckAbility(requiredAbility);
        }

        private void CheckAbility(AbilityType type)
        {
            bool hasAbility = requiredAbility switch
            {
                AbilityType.Dash => gameState.hasDash,
                AbilityType.DoubleJump => gameState.hasDoubleJump,
                _ => false
            };

            if (hasAbility)
            {
                if (_collider != null) _collider.enabled = false;
                var renderer = GetComponent<Renderer>();
                if (renderer != null)
                {
                    var color = renderer.material.color;
                    color.a = 0.2f;
                    renderer.material.color = color;
                }
            }
        }
    }
}
