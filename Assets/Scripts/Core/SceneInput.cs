using Fusion;
using UnityEngine;

namespace BaseSceneCore
{
    public class SceneInput : SceneService
    {
        // PUBLIC MEMBERS

        public bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        // PRIVATE MEMBERS

        private static int _lastSingleInputChange;
        private static int _cursorLockRequests;

        // PUBLIC METHODS

        public void RequestCursorLock()
        {
            // Static requests count is used for multi-peer setup
            _cursorLockRequests++;

            if (_cursorLockRequests == 1)
            {
                // First lock request, let's lock
                SetLockedState(true);
            }
        }

        public void RequestCursorRelease()
        {
            _cursorLockRequests--;

            Assert.Check(_cursorLockRequests >= 0, "Cursor lock requests are negative, this should not happen");

            if (_cursorLockRequests == 0)
            {
                SetLockedState(false);
            }
        }

        // SceneService INTERFACE

        protected override void OnTick()
        {
            // Only one single input change per frame is possible (important for multi-peer multi-input game)
            if (_lastSingleInputChange != Time.frameCount)
            {
                if (Input.GetKeyDown(KeyCode.Return) == true || Input.GetKeyDown(KeyCode.KeypadEnter) == true)
                {
                    SetLockedState(Cursor.lockState != CursorLockMode.Locked);
                    _lastSingleInputChange = Time.frameCount;
                }
            }
        }

        // PRIVATE METHODS

        private void SetLockedState(bool value)
        {
            Cursor.lockState = value == true ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !value;

            //Debug.Log($"Cursor lock state {Cursor.lockState}, visibility {Cursor.visible}");
        }
    }
}