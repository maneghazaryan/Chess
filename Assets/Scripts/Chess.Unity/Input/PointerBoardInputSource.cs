using System;
using Chess.Core.Primitives;
using Chess.Unity.Views;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Chess.Unity.Input
{
    /// <summary>
    /// Translates mouse clicks and screen taps into board squares.
    /// </summary>
    /// <remarks>
    /// Uses the new Input System's unified <see cref="Pointer"/> device, which reports mouse, pen
    /// and single-touch through the same API, so this one component covers desktop and mobile.
    /// <para>
    /// Isolating the conversion here is what keeps <see cref="BoardView"/> from knowing anything
    /// about cameras or devices: adding drag-and-drop or keyboard navigation later means writing
    /// a sibling implementation, not editing the renderer.
    /// </para>
    /// </remarks>
    public sealed class PointerBoardInputSource : MonoBehaviour, IBoardInputSource
    {
        [Tooltip("Camera used to convert screen points to the board plane. Falls back to Camera.main.")]
        [SerializeField] private Camera _camera;

        [Tooltip("The board whose geometry defines where each square is. Required.")]
        [SerializeField] private BoardView _boardView;

        [Tooltip("Ignore clicks that land on UI, so pressing a HUD button does not also move a piece.")]
        [SerializeField] private bool _blockWhenPointerOverUi = true;

        public event Action<Square> SquarePressed;

        public bool IsEnabled { get; set; } = true;

        public void Initialise(Camera boardCamera, BoardView boardView)
        {
            _camera = boardCamera != null ? boardCamera : _camera;
            _boardView = boardView != null ? boardView : _boardView;
        }

        private void Update()
        {
            if (!IsEnabled || _boardView == null)
            {
                return;
            }

            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return;
            }

            if (_blockWhenPointerOverUi && IsPointerOverUi())
            {
                return;
            }

            Square square = ResolveSquare(pointer.position.ReadValue());
            if (square.IsValid)
            {
                SquarePressed?.Invoke(square);
            }
        }

        private Square ResolveSquare(Vector2 screenPosition)
        {
            Camera activeCamera = ResolveCamera();
            if (activeCamera == null)
            {
                return Square.None;
            }

            Transform boardTransform = _boardView.transform;

            // Intersect the pointer ray with the board's own plane rather than assuming z = 0, so
            // the board can be tilted or offset in the scene and picking still lines up.
            var boardPlane = new Plane(boardTransform.forward, boardTransform.position);
            Ray ray = activeCamera.ScreenPointToRay(screenPosition);

            if (!boardPlane.Raycast(ray, out float distance))
            {
                return Square.None;
            }

            Vector3 localPoint = boardTransform.InverseTransformPoint(ray.GetPoint(distance));
            return _boardView.Geometry.FromLocalPosition(localPoint);
        }

        private Camera ResolveCamera() => _camera != null ? _camera : _camera = Camera.main;

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
