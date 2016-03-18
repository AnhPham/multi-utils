using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace TeamHoppi.Networking
{
    public class SimpleController : NetworkBehaviour
    {
        public virtual void OnProcessInput(KeyCode keyCode)
        {
        }
    }
}