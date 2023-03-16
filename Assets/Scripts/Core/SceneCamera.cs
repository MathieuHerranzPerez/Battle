using UnityEngine;

namespace BaseSceneCore
{
    public class SceneCamera : SceneService
    {
        public Camera Camera => _camera;


        [SerializeField] private Camera _camera;
    }
}