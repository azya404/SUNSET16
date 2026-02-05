/*
this is the base class that all our managers will inherit from
most importantly from the lab we saw, how only 1 instance of each manager should exist in the game at any pot*/

using UnityEngine;

namespace SUNSET16.Core
{
    //made it abstract so class cannot be used directly, only through inheretance
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour //
    {
        //NOTE: the great thing is, we can access them from anywhere, using <ManagerName>.Instance

        private static T _instance;
        private static readonly object _lock = new object(); //the lock prevents rare timing bugs (liek when multiple things try to access any one of our singletons simul)
        private static bool _applicationIsQuitting = false; //to prevent creating new instances when the application is quitting but i think also for can help prevent errors when we try to access a manager thats already been destroyed

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

        //need to cleanup when application quits 
        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true; 
            /*
            basically OnApplicationQuit is called when the game closes
            we set the flag so other scripts arent trying to access managers
            cos if they did, we would get error mssgs
            this is because the managers are being destroyed when the application is quiting
            */
        }
    }
}
