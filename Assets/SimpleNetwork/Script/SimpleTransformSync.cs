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
    const float cl_extrapolate_amount = 0.25f;
    const float cl_updaterate = 20;

    int m_MaxPack;
    List<TransformPack> m_Packs;

    void Awake()
    {
        m_MaxPack = (int)(cl_interp / (1 / cl_updaterate) + 1);
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
                TransformPack pack = InterpolatePosition(renTime);
                transform.position = pack.p;
                transform.rotation = pack.r;
            }
        }
    }

    void OnUpdateState()
    {
        if (hasAuthority)
        {
            CmdState(transform.position, transform.rotation);
        }
    }

    [Command]
    void CmdState(Vector3 p, Quaternion r)
    {
        RpcState(p, r);
    }

    [ClientRpc]
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
            pack = m_Packs[0];
            m_Packs.RemoveAt(0);
        }

        pack.t = Time.time;
        pack.p = p;
        pack.r = r;

        m_Packs.Add(pack);
    }

    TransformPack InterpolatePosition(float renTime)
    {
        TransformPack p1 = NewPack();
        TransformPack p2 = NewPack();

        bool hasP1 = false;
        bool hasP2 = false;

        for (int i = 0; i < m_Packs.Count; i++)
        {
            if (m_Packs[i].t < renTime)
            {
                p1 = m_Packs[i];
                hasP1 = true;
            }
            else if (m_Packs[i].t >= renTime)
            {
                p2 = m_Packs[i];
                hasP2 = true;
                break;
            }
        }

        TransformPack pack = NewPack();
        pack.t = renTime;

        if (hasP2)
        {
            if (hasP1)
            {
                float ratio = (renTime - p1.t) / (p2.t - p1.t);

                pack.p = Vector3.Lerp(p1.p, p2.p, ratio);
                pack.r = Quaternion.Lerp(p1.r, p2.r, ratio);
            }
            else
            {
                pack.p = p2.p;
                pack.r = p2.r;
            }
        }
        else
        {
            if (renTime - m_Packs[m_Packs.Count - 1].t <= cl_extrapolate_amount)
            {
                if (m_Packs.Count > 1)
                {
                    p1 = m_Packs[m_Packs.Count - 2];
                    p2 = m_Packs[m_Packs.Count - 1];

                    float ratio = (renTime - p1.t) / (p2.t - p1.t);
                    pack.p = p1.p + (p2.p - p1.p) * ratio;
                    pack.r = transform.rotation;
                }
                else
                {
                    pack.p = m_Packs[0].p;
                    pack.r = m_Packs[0].r;
                }
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