using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SimpleItemCatcher : NetworkBehaviour
{
    [SerializeField] Transform m_ItemContainer;
    [SyncVar] bool m_Holding;
    [SyncVar] uint m_ItemId;
    GameObject m_Item;

    public bool holding
    {
        get { return m_Holding; }
        set { m_Holding = value; UpdateTransformSync(); }
    }

    public void Hold(GameObject item)
    {
        if (!holding)
        {
            m_Item = item;
            m_ItemId = ItemId(m_Item);
            holding = true;
            CmdHold(m_ItemId);
        }
    }

    public void Throw(Vector3 force)
    {
        if (holding)
        {
            holding = false;
            m_Item.GetComponent<Rigidbody>().velocity = Vector3.zero;
            m_Item.GetComponent<Rigidbody>().AddForce(force);
            CmdThrow();
        }
    }

    void LateUpdate()
    {
        if (holding && m_ItemId > 0)
        {
            if (m_Item == null)
            {
                m_Item = FindItemFromId(m_ItemId);
            }
            m_Item.transform.position = m_ItemContainer.position;
            m_Item.transform.rotation = m_ItemContainer.rotation;
        }
        else
        {
            m_Item = null;
        }
    }

    void UpdateTransformSync()
    {
        m_Item.GetComponent<SimpleTransformSync>().enabled = !holding;
    }

    uint ItemId(GameObject item)
    {
        return item.GetComponent<NetworkIdentity>().netId.Value;
    }

    GameObject FindItemFromId(uint id)
    {
        foreach (var item in ClientScene.objects)
        {
            if (item.Key.Value == id)
            {
                return item.Value.gameObject;
            }
        }
        return null;
    }

    [Command]
    void CmdHold(uint id)
    {
        m_ItemId = id;
        m_Item = FindItemFromId(id);
        holding = true;

        NetworkIdentity itemId = m_Item.GetComponent<NetworkIdentity>();

        if (itemId.clientAuthorityOwner != null)
        {
            itemId.RemoveClientAuthority(itemId.clientAuthorityOwner);
        }
        itemId.AssignClientAuthority(connectionToClient);
    }

    [Command]
    void CmdThrow()
    {
        holding = false;
    }
}