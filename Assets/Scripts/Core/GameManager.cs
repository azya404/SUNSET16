using UnityEngine;
using System; //need to actions later on

namespace SUNSET16.Core
{
    public class GameManager : Singleton<GameManager>
    {

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            //empty for now
        }
    }
}
