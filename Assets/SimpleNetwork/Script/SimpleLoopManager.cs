using UnityEngine;
using System.Collections;

namespace TeamHoppi.Networking
{
    public class SimpleLoopManager : MonoBehaviour
    {
        public const float cl_updaterate = 20;
        public const float cl_cmdrate = 30;
        public const float cl_interp = 0.1f;
        public const float cl_extrapolate_amount = 0;

        [SerializeField] bool m_AutoRun = true;
        [SerializeField] float m_UpdateRate = cl_updaterate;
        [SerializeField] float m_CommandRate = cl_cmdrate;
        [SerializeField] float m_Interpolation = cl_interp;
        [SerializeField] float m_Extrapolation = cl_extrapolate_amount;

        public static SimpleLoopManager instance { get; protected set; }

        public delegate void OnUpdateDelegate();
        public OnUpdateDelegate onUpdateState;
        public OnUpdateDelegate onUpdateCommand;

        public float updateRate
        {
            get { return m_UpdateRate; }
        }

        public float commandRate
        {
            get { return m_CommandRate; }
        }

        public float interpolation
        {
            get { return m_Interpolation; }
        }

        public float extrapolation
        {
            get { return m_Extrapolation; }
        }

        public bool running
        {
            get; protected set;
        }

        public void Resume()
        {
            if (!running)
            {
                running = true;

                if (updateRate != 0)
                {
                    InvokeRepeating("UpdateState", 0, (1f / updateRate));
                }

                if (commandRate != 0)
                {
                    InvokeRepeating("UpdateCommand", 0, (1f / commandRate));
                }
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

        void UpdateCommand()
        {
            if (onUpdateCommand != null)
            {
                onUpdateCommand();
            }
        }
    }
}