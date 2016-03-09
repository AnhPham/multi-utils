using UnityEngine;
using System.Collections;

public class SimpleLoopManager : MonoBehaviour
{
    public static SimpleLoopManager instance { get; protected set; }

    public delegate void OnUpdateStateDelegate();
    public OnUpdateStateDelegate onUpdateState;

    public const float cl_updaterate = 20;

    void Awake()
    {
        instance = this;
        InvokeRepeating("UpdateState", 0, (1f / cl_updaterate));
    }

    void OnDestroy()
    {
        instance = null;
    }

    void UpdateState()
    {
        if (onUpdateState != null)
        {
            onUpdateState();
        }
    }
}