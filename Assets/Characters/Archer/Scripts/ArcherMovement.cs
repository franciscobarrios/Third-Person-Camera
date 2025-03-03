using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArcherMovement : MonoBehaviour
{
    public float runSpeed = 5.0f;
    public float maxJumpHeight = 2.0f;
    public float groundedGravity = -0.05f;

    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsRunning = Animator.StringToHash("isRunning");
    private static readonly int IsJumping = Animator.StringToHash("isJumping");
    private static readonly int JumpingCount = Animator.StringToHash("jumpCount");

    private CharacterController _characterController;
    private Animator _animator;
    private Vector2 _currentMovementInput;
    private Vector3 _currentMovement;
    private Vector3 _currentRunMovement;
    private PlayerInput _playerInput;

    private bool _isMovementPressed;
    private bool _isRunPressed;
    private bool _isJumpPressed;
    private bool _isJumping;
    private bool _isJumpingAnimating;

    private float _rotationFactorPerFrame = 15.0f;
    private float _gravity = -9.81f;
    private float _initialJumpVelocity;
    private float _maxJumpTime = 0.75f;
    private int _jumpCount = 0;

    private Dictionary<int, float> _initialJumpVelocities = new();
    private Dictionary<int, float> _initialGravities = new();
    private Coroutine _resetCoroutine = null;

    private void Awake()
    {
        _playerInput = new PlayerInput();
        _playerInput.PlayerControl.Move.started += OnMovementInput;
        _playerInput.PlayerControl.Move.canceled += OnMovementInput;
        _playerInput.PlayerControl.Move.performed += OnMovementInput;
        _playerInput.PlayerControl.Run.started += OnRun;
        _playerInput.PlayerControl.Run.canceled += OnRun;
        _playerInput.PlayerControl.Jump.started += OnJump;
        _playerInput.PlayerControl.Jump.canceled += OnJump;
        SetUpJumpVariables();
    }

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleRotation();
        HandleAnimation();

        if (_isRunPressed) _characterController.Move(_currentRunMovement * Time.deltaTime);
        else _characterController.Move(_currentMovement * Time.deltaTime);

        HandleGravity();
        HandleJump();
    }

    private void OnMovementInput(InputAction.CallbackContext context)
    {
        _currentMovementInput = context.ReadValue<Vector2>();
        _currentMovement.x = _currentMovementInput.x;
        _currentMovement.z = _currentMovementInput.y;
        _isMovementPressed = _currentMovementInput.x != 0 || _currentMovementInput.y != 0;
        _currentRunMovement.x = _currentMovementInput.x * runSpeed;
        _currentRunMovement.z = _currentMovementInput.y * runSpeed;
        
        _currentMovement = transform.TransformDirection(_currentMovement);
        _currentRunMovement = transform.TransformDirection(_currentRunMovement);
    }

    private void OnRun(InputAction.CallbackContext context)
    {
        _isRunPressed = context.ReadValueAsButton();
    }

    private void SetUpJumpVariables()
    {
        var timeToApex = _maxJumpTime / 2;
        _gravity = -2 * maxJumpHeight / Mathf.Pow(timeToApex, 2);
        _initialJumpVelocity = 2 * maxJumpHeight / timeToApex;
        var secondJumpGravity = -2 * (maxJumpHeight + 1) / Mathf.Pow(timeToApex * 1.1f, 2);
        var secondJumpInitialVelocity = 2 * (maxJumpHeight + 1) / timeToApex * 1.1f;
        var thirdJumpGravity = -2 * (maxJumpHeight + 2) / Mathf.Pow(timeToApex * 1.2f, 2);
        var thirdJumpInitialVelocity = 2 * (maxJumpHeight + 2) / timeToApex * 1.2f;

        _initialJumpVelocities.Add(1, _initialJumpVelocity);
        _initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        _initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        _initialGravities.Add(0, _gravity);
        _initialGravities.Add(1, _gravity);
        _initialGravities.Add(2, secondJumpGravity);
        _initialGravities.Add(3, thirdJumpGravity);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        _isJumpPressed = context.ReadValueAsButton();
    }

    private IEnumerator JumpResetRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        _jumpCount = 0;
    }

    private void HandleJump()
    {
        if (!_isJumping && _characterController.isGrounded && _isJumpPressed)
        {
            if (_jumpCount < 3 && _resetCoroutine != null)
            {
                StopCoroutine(_resetCoroutine);
            }

            _animator.SetBool(IsJumping, true);
            _isJumpingAnimating = true;
            _isJumping = true;
            _jumpCount++;
            _animator.SetInteger(JumpingCount, _jumpCount);
            _currentMovement.y = _initialJumpVelocities[_jumpCount] * 0.5f;
            _currentRunMovement.y = _initialJumpVelocities[_jumpCount] * 0.5f;
        }
        else if (!_isJumpPressed && _characterController.isGrounded && _isJumping)
        {
            _isJumping = false;
        }
    }

    private void HandleGravity()
    {
        var isFalling = _currentMovement.y <= 0.0f || !_isJumpPressed;
        var fallMultiplier = 2.0f;

        if (_characterController.isGrounded)
        {
            if (_isJumpingAnimating)
            {
                _animator.SetBool(IsJumping, false);
                _isJumpingAnimating = false;
                _resetCoroutine = StartCoroutine(JumpResetRoutine());

                if (_jumpCount == 3)
                {
                    _jumpCount = 0;
                    _animator.SetInteger(JumpingCount, _jumpCount);
                }
            }

            _currentMovement.y = groundedGravity;
            _currentRunMovement.y = groundedGravity;
        }
        else if (isFalling)
        {
            var previousYVelocity = _currentMovement.y;
            var newYVelocity = _currentMovement.y + (_initialGravities[_jumpCount] * Time.deltaTime * fallMultiplier);
            var nextVelocity = (previousYVelocity + newYVelocity) * 0.5f;
            _currentMovement.y = nextVelocity;
            _currentRunMovement.y = nextVelocity;
        }
        else
        {
            var previousYVelocity = _currentMovement.y;
            var newYVelocity = _currentMovement.y + (_initialGravities[_jumpCount] * Time.deltaTime);
            var nextVelocity = (previousYVelocity + newYVelocity) * 0.5f;
            _currentMovement.y = nextVelocity;
            _currentRunMovement.y = nextVelocity;
        }
    }

    private void HandleRotation()
    {
        Vector3 positionToLookAt;

        positionToLookAt.x = _currentMovement.x;
        positionToLookAt.y = 0.0f;
        positionToLookAt.z = _currentMovement.z;
        var currentRotation = transform.rotation;

        if (_isMovementPressed)
        {
            var targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation =
                Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * _rotationFactorPerFrame);
        }
    }

    private void HandleAnimation()
    {
        var isWalking = _animator.GetBool(IsWalking);
        var isRunning = _animator.GetBool(IsRunning);

        if (_isMovementPressed && !isWalking)
        {
            _animator.SetBool(IsWalking, true);
        }
        else if (!_isMovementPressed && isWalking)
        {
            _animator.SetBool(IsWalking, false);
        }

        if ((_isMovementPressed && _isRunPressed) && !isRunning)
        {
            _animator.SetBool(IsRunning, true);
        }
        else if ((!_isMovementPressed || !_isRunPressed) && isRunning)
        {
            _animator.SetBool(IsRunning, false);
        }
    }

    void OnEnable()
    {
        _playerInput.PlayerControl.Enable();
    }

    void OnDisable()
    {
        _playerInput.PlayerControl.Disable();
    }
}