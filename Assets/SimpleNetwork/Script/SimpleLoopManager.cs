using UnityEngine;
using System.Collections;

public class SimpleLoopManager : MonoBehaviour
{
    [SerializeField] bool m_AutoRun;

    public static SimpleLoopManager instance { get; protected set; }

    public delegate void OnUpdateStateDelegate();
    public OnUpdateStateDelegate onUpdateState;

    public const float cl_updaterate = 20;

    public bool running
    {
        get; protected set;
    }

    public void Resume()
    {
        if (!running)
        {
            running = true;
            InvokeRepeating("UpdateState", 0, (1f / cl_updaterate));
        }
    }

    public void Stop()
    {
        if (running)
        {
            running = false;
            CancelInvoke();
        }
    }

    void Awake()
    {
        instance = this;

        if (m_AutoRun)
        {
            Resume();
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
}