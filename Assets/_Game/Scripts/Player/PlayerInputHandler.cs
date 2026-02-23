using UnityEngine;
using UnityEngine.InputSystem;
using SunlessReach.Core;

namespace SunlessReach.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        public Vector2 MoveInput => _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        public bool JumpPressed => !_suppressJump && (_jumpAction?.WasPressedThisFrame() ?? false);
        public bool JumpHeld => !_suppressJump && (_jumpAction?.IsPressed() ?? false);
        public bool AttackPressed => _attackAction?.WasPressedThisFrame() ?? false;
        public bool DashPressed => _dashAction?.WasPressedThisFrame() ?? false;
        public bool HealPressed => _healAction?.WasPressedThisFrame() ?? false;
        public bool HealHeld => _healAction?.IsPressed() ?? false;
        public bool InteractPressed => _interactAction?.WasPressedThisFrame() ?? false;
        public bool PausePressed => _pauseAction?.WasPressedThisFrame() ?? false;

        // Jump shares the gamepad A button with UI submit, so swallow it while paused and until it's released after.
        private bool _suppressJump;
        private bool _paused;

        private InputActionMap _playerMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _attackAction;
        private InputAction _dashAction;
        private InputAction _healAction;
        private InputAction _interactAction;
        private InputAction _pauseAction;

        private void Awake()
        {
            // Remove PlayerInput component if present to avoid conflicts
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
                Destroy(playerInput);

            // Define all actions in code for maximum reliability
            _playerMap = new InputActionMap("Player");

            _moveAction = _playerMap.AddAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _moveAction.AddBinding("<Gamepad>/leftStick");

            _jumpAction = _playerMap.AddAction("Jump", InputActionType.Button);
            _jumpAction.AddBinding("<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");

            _attackAction = _playerMap.AddAction("Attack", InputActionType.Button);
            _attackAction.AddBinding("<Keyboard>/j");
            _attackAction.AddBinding("<Gamepad>/buttonWest");

            _dashAction = _playerMap.AddAction("Dash", InputActionType.Button);
            _dashAction.AddBinding("<Keyboard>/leftShift");
            _dashAction.AddBinding("<Gamepad>/buttonEast");

            _healAction = _playerMap.AddAction("Heal", InputActionType.Button);
            _healAction.AddBinding("<Keyboard>/k");
            _healAction.AddBinding("<Gamepad>/buttonNorth");

            _interactAction = _playerMap.AddAction("Interact", InputActionType.Button);
            _interactAction.AddBinding("<Keyboard>/e");
            _interactAction.AddBinding("<Gamepad>/rightShoulder");

            _pauseAction = _playerMap.AddAction("Pause", InputActionType.Button);
            _pauseAction.AddBinding("<Keyboard>/escape");
            _pauseAction.AddBinding("<Gamepad>/start");
        }

        private void OnEnable()
        {
            _playerMap?.Enable();
            EventBus.OnGamePaused  += OnMenuOpened;
            EventBus.OnGameResumed += OnMenuClosed;
            EventBus.OnShopOpened  += OnMenuOpened;
            EventBus.OnShopClosed  += OnMenuClosed;
        }

        private void OnDisable()
        {
            _playerMap?.Disable();
            EventBus.OnGamePaused  -= OnMenuOpened;
            EventBus.OnGameResumed -= OnMenuClosed;
            EventBus.OnShopOpened  -= OnMenuOpened;
            EventBus.OnShopClosed  -= OnMenuClosed;
        }

        private void OnMenuOpened() { _paused = true;  _suppressJump = true; }
        private void OnMenuClosed() { _paused = false; _suppressJump = true; }   // re-arm; clears on release

        private void Update()
        {
            // Once un-paused, drop the jump lock as soon as the button is no longer held.
            if (!_paused && _suppressJump && !(_jumpAction?.IsPressed() ?? false))
                _suppressJump = false;
        }

        private void OnDestroy()
        {
            _playerMap?.Dispose();
        }

        public void DisableInput()
        {
            _playerMap?.Disable();
        }

        public void EnableInput()
        {
            _playerMap?.Enable();
        }
    }
}
