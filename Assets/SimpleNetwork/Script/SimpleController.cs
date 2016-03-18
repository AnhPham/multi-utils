using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace TeamHoppi.Networking
{
    public class SimpleController : NetworkBehaviour
    {
        public virtual InputPack[] CreateCustomInputPack(int tick)
        {
            return null;
        }

        public virtual void OnProcessInput(InputPack input)
        {
        }
    }
}