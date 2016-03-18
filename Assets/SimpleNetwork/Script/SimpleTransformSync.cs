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
        public int[] iVals;
        public float[] fVals;

        public int keyCode
        {
            get { return iVals[0]; }
            set
            {
                if (iVals == null)
                {
                    iVals = new int[2];
                }

                iVals[0] = value;
            }
        }

        public int mouseButton
        {
            get { return iVals[1]; }
            set
            {
                if (iVals == null)
                {
                    iVals = new int[2];
                }

                iVals[1] = value;
            }
        }

        public Vector3 position
        {
            get { return new Vector3(fVals[0], fVals[1], fVals[2]); }
            set
            {
                if (fVals == null)
                {
                    fVals = new float[6];
                }

                fVals[0] = value.x;
                fVals[1] = value.y;
                fVals[2] = value.z;
            }
        }

        public Vector3 eulerAngles
        {
            get { return new Vector3(fVals[3], fVals[4], fVals[5]); }
            set
            {
                if (fVals == null)
                {
                    fVals = new float[6];
                }

                fVals[3] = value.x;
                fVals[4] = value.y;
                fVals[5] = value.z;
            }
        }
    }

    public enum SyncInputType
    {
        KEYBOARD_MOUSE,
        TRANSFORM,
        CUSTOM
    }

    public class SimpleTransformSync : NetworkBehaviour
    {
        [SerializeField] SyncInputType m_SyncInputType;

        int m_Tick;
        int m_MaxPack;
        List<int> m_AllKeyCodes;
        List<TransformPack> m_Packs;
        List<InputPack> m_CurrentInputs;
        SimpleController m_Controller;

        public SyncInputType syncInputType
        {
            get { return m_SyncInputType; }
        }

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
            InputPack[] inputs = null;

            switch (m_SyncInputType)
            {
                case SyncInputType.KEYBOARD_MOUSE:
                    inputs = CreateKeyboardMouseInputPacks(m_Tick);
                    break;

                case SyncInputType.TRANSFORM:
                    inputs = CreateTransformInputPacks(m_Tick);
                    break;

                case SyncInputType.CUSTOM:
                    inputs = CreateCustomInputPacks(m_Tick);
                    break;
            }

            for (int i = 0; i < inputs.Length; i++)
            {
                if (pushToList)
                {
                    m_CurrentInputs.Add(inputs[i]);
                }

                if (m_Controller != null)
                {
                    m_Controller.OnProcessInput(inputs[i]);
                }
            }
        }

        InputPack[] CreateKeyboardMouseInputPacks(int tick)
        {
            List<InputPack> inputs = new List<InputPack>();

            for (int i = 0; i < m_AllKeyCodes.Count; i++)
            {
                int keyCode = m_AllKeyCodes[i];
                if (Input.GetKey((KeyCode)keyCode))
                {
                    InputPack input = new InputPack();
                    input.tick = m_Tick;
                    input.keyCode = keyCode;
                    input.mouseButton = -1;

                    inputs.Add(input);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (Input.GetMouseButton(i))
                {
                    InputPack input = new InputPack();
                    input.tick = m_Tick;
                    input.keyCode = -1;
                    input.mouseButton = i;

                    inputs.Add(input);
                }
            }

            return inputs.ToArray();
        }

        InputPack[] CreateTransformInputPacks(int tick)
        {
            InputPack[] inputs = new InputPack[1];

            InputPack input = new InputPack();
            input.tick = m_Tick;
            input.position = transform.position;
            input.eulerAngles = transform.eulerAngles;

            inputs[0] = input;

            return inputs;
        }

        InputPack[] CreateCustomInputPacks(int tick)
        {
            return m_Controller.CreateCustomInputPack(tick);
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
                    InputPack input = m_CurrentInputs[0];

                    switch (m_SyncInputType)
                    {
                        case SyncInputType.KEYBOARD_MOUSE:
                            ProcessInput(input);
                            break;

                        case SyncInputType.TRANSFORM:
                            transform.position = input.position;
                            transform.eulerAngles = input.eulerAngles;
                            ProcessInput(input);
                            break;

                        case SyncInputType.CUSTOM:
                            ProcessInput(input);
                            break;
                    }

                    m_CurrentInputs.RemoveAt(0);
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

        void ProcessInput(InputPack input)
        {
            if (m_Controller != null)
            {
                m_Controller.OnProcessInput(input);
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