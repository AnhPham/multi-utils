using UnityEngine;
using System.Collections;

public class SimpleLoopManager : MonoBehaviour
{
    [SerializeField] bool m_UseFixedUpdate;

    public static SimpleLoopManager instance { get; protected set; }

    public delegate void OnUpdateStateDelegate();
    public OnUpdateStateDelegate onUpdateState;

    public const float cl_updaterate = 50;

    void Awake()
    {
        instance = this;

        if (!m_UseFixedUpdate)
        {
            InvokeRepeating("UpdateState", 0, (1f / cl_updaterate));
        }
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

    void FixedUpdate()
    {
        if (m_UseFixedUpdate)
        {
            if (onUpdateState != null)
            {
                onUpdateState();
            }
        }
    }
}