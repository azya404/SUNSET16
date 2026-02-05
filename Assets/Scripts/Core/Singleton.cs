/*
this is the base class that all our managers will inherit from
most importantly from the lab we saw, how only 1 instance of each manager should exist in the game at any point in time
*/
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
        { // this will help us to access any manager from any other script, and unity will do it for us (ill add examples right after this function)
            get
            {
                if (_applicationIsQuitting) 
                {
                    Debug.LogWarning($"[SINGELTON] this one '{typeof(T)}' is already/being destroyed"); //cant access a singletong if youre quiting the game bruh
                    return null;
                }
                
                lock (_lock) // dont want duplicates of singletones cos that could be quite messy
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            Debug.LogError($"[SINGELTON] cant find '{typeof(T)}' anywhere in the (current?) scene");
                        }
                    }
                    return _instance;
                }
            }
        }

        // child classes shoudl call base.Awake() 
        //Awake() is called when the GameObject is createdbefore Start()
        // what i have in mind is Awake -> OnEnable -> Start -> Update (repeating) -> OnDestroy
        protected virtual void Awake() //virtual so child classes can override it if need be
        {
            if (_instance == null) //if no insatcne exists of this manager (that is a singleton) then we create
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject); //MAIN THING THAT ALLOWS OUR GAMEOBKECT TO PERSIST ACROSS SCENES
            }

            else if (_instance != this)//but if one alr exists then we dont need to make it again as 2 singletons shouldnt exist
            {
                Debug.LogWarning($"[SINGELTON] you alr created '{typeof(T)}' before, gonna amongus vote this out");
                Destroy(gameObject);
            }
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
