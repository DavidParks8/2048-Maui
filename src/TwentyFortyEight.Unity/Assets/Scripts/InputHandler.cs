using UnityEngine;
using TwentyFortyEight.Core;
using System;

namespace TwentyFortyEight.Unity
{
    /// <summary>
    /// Handles input from keyboard and touch for the game.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        public event Action<Direction> OnMove;

        [Header("Input Settings")]
        [SerializeField] private float minSwipeDistance = 50f;
        [SerializeField] private float maxSwipeTime = 0.5f;

        private Vector2 _swipeStartPos;
        private float _swipeStartTime;
        private bool _isSwiping;

        public void ProcessInput()
        {
            HandleKeyboardInput();
            HandleTouchInput();
        }

        private void HandleKeyboardInput()
        {
            // Arrow keys
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                OnMove?.Invoke(Direction.Up);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                OnMove?.Invoke(Direction.Down);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                OnMove?.Invoke(Direction.Left);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                OnMove?.Invoke(Direction.Right);
            }
        }

        private void HandleTouchInput()
        {
            // Handle mouse/touch input for swipe gestures
            if (Input.GetMouseButtonDown(0))
            {
                _swipeStartPos = Input.mousePosition;
                _swipeStartTime = Time.time;
                _isSwiping = true;
            }
            else if (Input.GetMouseButtonUp(0) && _isSwiping)
            {
                _isSwiping = false;
                float swipeTime = Time.time - _swipeStartTime;
                
                if (swipeTime <= maxSwipeTime)
                {
                    Vector2 swipeEndPos = Input.mousePosition;
                    Vector2 swipeDelta = swipeEndPos - _swipeStartPos;
                    
                    if (swipeDelta.magnitude >= minSwipeDistance)
                    {
                        ProcessSwipe(swipeDelta);
                    }
                }
            }
        }

        private void ProcessSwipe(Vector2 swipeDelta)
        {
            float absX = Mathf.Abs(swipeDelta.x);
            float absY = Mathf.Abs(swipeDelta.y);

            Direction direction;

            if (absX > absY)
            {
                // Horizontal swipe
                direction = swipeDelta.x > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                // Vertical swipe
                direction = swipeDelta.y > 0 ? Direction.Up : Direction.Down;
            }

            OnMove?.Invoke(direction);
        }
    }
}
