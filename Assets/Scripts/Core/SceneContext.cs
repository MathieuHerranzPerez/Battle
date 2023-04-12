using Fusion;
using UnityEngine;

namespace BaseSceneCore
{
    [System.Serializable]
    public class SceneContext
    {
        // General

        public SceneUI UI;
        public ObjectCache ObjectCache;
        public SceneInput Input;
        public SceneCamera Camera;

        // Gameplay

        [HideInInspector]
        public Gameplay Gameplay;
        [HideInInspector]
        public NetworkRunner Runner;

        // Player

        [HideInInspector]
        public PlayerRef LocalPlayerRef;
        [HideInInspector]
        public PlayerRef ObservedPlayerRef;
        [HideInInspector]
        public NewPlayerAgent ObservedAgent;
    }
}