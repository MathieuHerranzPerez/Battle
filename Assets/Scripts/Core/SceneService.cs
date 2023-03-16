using UnityEngine;

namespace BaseSceneCore
{
    public class SceneService : MonoBehaviour
    {
        // PUBLIC MEMBERS

        public SceneContext Context => _context;
        public bool IsActive => _isActive;
        public bool IsInitialized => _isInitialized;

        // PRIVATE MEMBERS

        private SceneContext _context;
        private Scene _scene;
        private bool _isInitialized;
        private bool _isActive;

        // INTERNAL METHODS

        internal void Initialize(Scene scene, SceneContext context)
        {
            if (_isInitialized == true)
                return;

            _context = context;
            _scene = scene;

            OnInitialize();

            _isInitialized = true;
        }

        internal void Deinitialize()
        {
            if (_isInitialized == false)
                return;

            Deactivate();

            OnDeinitialize();

            _context = null;
            _scene = null;

            _isInitialized = false;
        }

        internal void Activate()
        {
            if (_isInitialized == false)
                return;

            if (_isActive == true)
                return;

            OnActivate();

            _isActive = true;
        }

        internal void Tick()
        {
            if (_isActive == false)
                return;

            OnTick();
        }

        internal void LateTick()
        {
            if (_isActive == false)
                return;

            OnLateTick();
        }

        internal void Deactivate()
        {
            if (_isActive == false)
                return;

            OnDeactivate();

            _isActive = false;
        }

        // GameService INTERFACE

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnDeinitialize()
        {
        }

        protected virtual void OnActivate()
        {
        }

        protected virtual void OnDeactivate()
        {
        }

        protected virtual void OnTick()
        {
        }

        protected virtual void OnLateTick()
        {
        }
    }
}