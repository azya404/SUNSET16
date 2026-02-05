using UnityEngine;

namespace SUNSET16.Core
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                //gotta add this after planning morre
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            // ADD thiw as well
        }

        protected virtual void OnApplicationQuit()
        {
            // Also to add
        }
    }
}
