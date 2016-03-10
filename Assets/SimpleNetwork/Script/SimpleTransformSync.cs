using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public struct TransformPack
{
    public float t;
    public Vector3 p;
    public Quaternion r;
}

public class SimpleTransformSync : NetworkBehaviour
{
    const float cl_interp = 0.1f;
    const float cl_extrapolate_amount = 0;

    int m_MaxPack;
    List<TransformPack> m_Packs;

    void Awake()
    {
        m_MaxPack = (int)(cl_interp / (1f / SimpleLoopManager.cl_updaterate) + 2);
        m_Packs = new List<TransformPack>(m_MaxPack);
    }

    void Start()
    {
        if (SimpleLoopManager.instance != null)
        {
            SimpleLoopManager.instance.onUpdateState += OnUpdateState;
        }
    }

    void OnDestroy()
    {
        if (SimpleLoopManager.instance != null)
        {
            SimpleLoopManager.instance.onUpdateState -= OnUpdateState;
        }
    }

    void Update()
    {
        if (!hasAuthority)
        {
            float renTime = Time.time - cl_interp;

            if (renTime > 0 && m_Packs.Count == m_MaxPack)
            {
                TransformPack pack = GetTransformByTime(renTime);
                transform.position = pack.p;
                transform.rotation = pack.r;
            }
        }
    }

    void OnUpdateState()
    {
        if (hasAuthority)
        {
            if (isServer)
            {
                RpcState(transform.position, transform.rotation);
            }
            else
            {
                CmdState(transform.position, transform.rotation);
            }
        }
    }

    [Command(channel = Channels.DefaultUnreliable)]
    void CmdState(Vector3 p, Quaternion r)
    {
        RpcState(p, r);
    }

    [ClientRpc(channel = Channels.DefaultUnreliable)]
    void RpcState(Vector3 p, Quaternion r)
    {
        if (!hasAuthority)
        {
            ProcessState(p, r);
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

    TransformPack GetTransformByTime(float renTime)
    {
        TransformPack p1 = NewPack();
        TransformPack p2 = NewPack();

        bool canInterpolate = false;

        for (int i = 0; i < m_Packs.Count; i++)
        {
            if (m_Packs[i].t < renTime)
            {
                p1 = m_Packs[i];
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
            bool canExtrapolate = (renTime - m_Packs[m_Packs.Count - 1].t <= cl_extrapolate_amount);

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
                pack.p = transform.position;
                pack.r = transform.rotation;
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