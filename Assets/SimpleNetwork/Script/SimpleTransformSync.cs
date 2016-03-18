using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace TeamHoppi.Networking
{
    public struct TransformPack
    {
        public float t;
        public Vector3 p;
        public Quaternion r;
    }

    public struct InputPack
    {
        public int tick;
        public int keyCode;
    }

    public class SimpleTransformSync : NetworkBehaviour
    {
        int m_Tick;
        int m_MaxPack;
        List<int> m_AllKeyCodes;
        List<TransformPack> m_Packs;
        List<InputPack> m_CurrentInputs;
        SimpleController m_Controller;

        void Start()
        {
            InitController();
            InitKeyCodeList();
            InitPackLists();

            RegisterDelegates();
        }

        void OnDestroy()
        {
            UnRegisterDelegates();
        }

        void Update()
        {
            // Update state of clients
            if (!isServer)
            {
                // Which is not me
                if (!hasAuthority)
                {
                    // And UpdateRate not 0
                    if (m_Packs != null)
                    {
                        ClientUpdateState();
                    }
                }
            }
        }

        void ClientUpdateState()
        {
            float renTime = Time.time - SimpleLoopManager.instance.interpolation;

            if (renTime > 0 && m_Packs.Count == m_MaxPack)
            {
                TransformPack pack = GetTransformByTime(renTime);
                transform.position = pack.p;
                transform.rotation = pack.r;
            }
        }

        void FixedUpdate()
        {
            // Tick counter
            m_Tick++;

            // If this is a client
            if (!isServer)
            {
                // And is me
                if (hasAuthority)
                {
                    ClientPushInput();
                }
            }

            // If this is a server
            if (isServer)
            {
                // And is not me
                if (!hasAuthority)
                {
                    ServerPopInput();
                }
                // If is me
                else
                {
                    ServerProcessInput();
                }
            }
        }

        void ProcessInput(bool pushToList)
        {
            for (int i = 0; i < m_AllKeyCodes.Count; i++)
            {
                int keyCode = m_AllKeyCodes[i];

                if (Input.GetKey((KeyCode)keyCode))
                {
                    if (pushToList)
                    {
                        InputPack pack = new InputPack();
                        pack.tick = m_Tick;
                        pack.keyCode = keyCode;

                        m_CurrentInputs.Add(pack);
                    }

                    if (m_Controller != null)
                    {
                        m_Controller.OnProcessInput((KeyCode)keyCode);
                    }
                }
            }
        }

        void ClientPushInput()
        {
            ProcessInput(true);
        }

        void ServerProcessInput()
        {
            ProcessInput(false);
        }

        void ServerPopInput()
        {
            if (m_CurrentInputs.Count > 0)
            {
                int tick = m_CurrentInputs[0].tick;

                while (m_CurrentInputs.Count > 0 && m_CurrentInputs[0].tick == tick)
                {
                    int keyCode = m_CurrentInputs[0].keyCode;
                    m_CurrentInputs.RemoveAt(0);

                    ProcessInput(keyCode);
                }
            }
        }

        void OnUpdateState()
        {
            // If this is a server
            if (isServer)
            {
                RpcState(transform.position, transform.rotation);
            }
        }

        void OnUpdateCommand()
        {
            // If this is a client
            if (!isServer)
            {
                // And is me
                if (hasAuthority)
                {
                    CmdInput(m_CurrentInputs.ToArray());
                    m_CurrentInputs.Clear();
                }
            }
        }

        [Command(channel = Channels.DefaultReliable)]
        void CmdInput(InputPack[] inputPacks)
        {
            // Not me on server
            if (!hasAuthority)
            {
                for (int i = 0; i < inputPacks.Length; i++)
                {
                    m_CurrentInputs.Add(inputPacks[i]);
                }
            }
        }

        [ClientRpc(channel = Channels.DefaultUnreliable)]
        void RpcState(Vector3 p, Quaternion r)
        {
            // Not me on client
            if (!hasAuthority)
            {
                ProcessState(p, r);
            }
            else
            {
                CheckPredictionError();
            }
        }

        void InitController()
        {
            m_Controller = GetComponent<SimpleController>();
        }

        void InitKeyCodeList()
        {
            var keyCodes = System.Enum.GetValues(typeof(KeyCode));

            m_AllKeyCodes = new List<int>(keyCodes.Length);
            m_CurrentInputs = new List<InputPack>();

            foreach (var keyCode in keyCodes)
            {
                m_AllKeyCodes.Add((int)keyCode);
            }
        }

        void InitPackLists()
        {
            float updateRate = SimpleLoopManager.instance.updateRate;
            float interpolation = SimpleLoopManager.instance.interpolation;

            if (updateRate != 0)
            {
                m_MaxPack = (int)(interpolation / (1f / updateRate) + 2);
                m_Packs = new List<TransformPack>(m_MaxPack);
            }
        }

        void RegisterDelegates()
        {
            if (SimpleLoopManager.instance != null)
            {
                SimpleLoopManager.instance.onUpdateState += OnUpdateState;
                SimpleLoopManager.instance.onUpdateCommand += OnUpdateCommand;
            }
        }

        void UnRegisterDelegates()
        {
            if (SimpleLoopManager.instance != null)
            {
                SimpleLoopManager.instance.onUpdateState -= OnUpdateState;
                SimpleLoopManager.instance.onUpdateCommand -= OnUpdateCommand;
            }
        }

        void ProcessState(Vector3 p, Quaternion r)
        {
            TransformPack pack;

            if (m_Packs.Count < m_MaxPack)
            {
                pack = NewPack();
            }
            else
            {
                int index = 0;
                if (Time.time - m_Packs[m_Packs.Count - 1].t < 0.001f)
                {
                    index = m_Packs.Count - 1;
                }
                pack = m_Packs[index];
                m_Packs.RemoveAt(index);
            }

            pack.t = Time.time;
            pack.p = p;
            pack.r = r;

            m_Packs.Add(pack);
        }

        void CheckPredictionError()
        {
        }

        void ProcessInput(int keyCode)
        {
            if (m_Controller != null)
            {
                m_Controller.OnProcessInput((KeyCode)keyCode);
            }
        }

        TransformPack GetTransformByTime(float renTime)
        {
            TransformPack p1 = NewPack();
            TransformPack p2 = NewPack();

            bool canInterpolate = false;
            bool useLastestData = false;

            for (int i = 0; i < m_Packs.Count; i++)
            {
                if (m_Packs[i].t < renTime)
                {
                    p1 = m_Packs[i];
                    useLastestData = true;
                }
                else if (m_Packs[i].t >= renTime)
                {
                    p2 = m_Packs[i];
                    canInterpolate = true;
                    break;
                }
            }

            TransformPack pack = NewPack();
            pack.t = renTime;

            if (canInterpolate)
            {
                float ratio = (renTime - p1.t) / (p2.t - p1.t);

                pack.p = Vector3.Lerp(p1.p, p2.p, ratio);
                pack.r = Quaternion.Lerp(p1.r, p2.r, ratio);
            }
            else
            {
                bool canExtrapolate = (renTime - m_Packs[m_Packs.Count - 1].t <= SimpleLoopManager.instance.extrapolation);

                if (canExtrapolate)
                {
                    p1 = m_Packs[m_Packs.Count - 2];
                    p2 = m_Packs[m_Packs.Count - 1];

                    float ratio = (renTime - p1.t) / (p2.t - p1.t);
                    pack.p = Vector3.LerpUnclamped(p1.p, p2.p, ratio);
                    pack.r = Quaternion.LerpUnclamped(p1.r, p2.r, ratio);
                }
                else
                {
                    if (useLastestData)
                    {
                        pack.p = p1.p;
                        pack.r = p1.r;
                    }
                    else
                    {
                        pack.p = transform.position;
                        pack.r = transform.rotation;
                    }
                }
            }

            return pack;
        }

        TransformPack NewPack()
        {
            TransformPack pack = new TransformPack();
            pack.t = 0;
            pack.p = Vector3.zero;
            pack.r = Quaternion.identity;

            return pack;
        }
    }
}