using UnityEngine;
using UnityEngine.InputSystem;

namespace Character.Scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        public float playerSpeed = 2.0f;
        public float jumpHeight = 1.0f;

        private CharacterController _controller;
        private float _verticalVelocity;
        private float _groundedTimer;
        private float _gravityValue = 9.81f;

        void Start()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void OnMove()
        {
            var isGrounded = _controller.isGrounded;
            var horizontalInput = Input.GetAxis("Horizontal");
            var verticalInput = Input.GetAxis("Vertical");

            if (isGrounded) _groundedTimer = 0.2f;
            if (_groundedTimer > 0) _groundedTimer -= Time.deltaTime;
            if (isGrounded && _verticalVelocity < 0) _verticalVelocity = 0f;

            _verticalVelocity -= _gravityValue * Time.deltaTime;

            var direction = new Vector3(horizontalInput, 0, verticalInput);
            direction *= playerSpeed;
            
            if (direction.magnitude < 0.2f) gameObject.transform.forward = direction;

            if (Input.GetButtonDown("Jump"))
            {
                if (_groundedTimer > 0)
                {
                    _groundedTimer = 0;
                    _verticalVelocity += Mathf.Sqrt(jumpHeight * 2f * _gravityValue);
                }
            }

            direction.y = _verticalVelocity;
            
            //_controller.Move(direction * Time.deltaTime);
        }

        void Update()
        {
            OnMove();
        }
    }
}