using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Cube : NetworkBehaviour
{
    [SerializeField] Transform m_BallContainer;
    [SyncVar] bool m_Holding;
    GameObject m_Ball;

    public override void OnStartLocalPlayer()
    {
        Animator animator = GetComponent<Animator>();
        animator.enabled = true;

        if (isServer)
        {
            animator.SetTrigger("Right");
        }
        else
        {
            animator.SetTrigger("Left");
        }
    }

    void Awake()
    {
        m_Ball = GameObject.Find("Ball");
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                m_Holding = true;
                CmdHold();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_Holding = false;
                m_Ball.GetComponent<Rigidbody>().AddForce(new Vector3(0, 200, -100));
                CmdThrow();
            }
        }
    }

    void LateUpdate()
    {
        if (m_Holding)
        {
            m_Ball.transform.position = m_BallContainer.position;
            m_Ball.transform.rotation = m_BallContainer.rotation;
        }

        m_Ball.GetComponent<SimpleTransformSync>().enabled = !m_Holding;
    }

    [Command]
    void CmdHold()
    {
        m_Holding = true;

        NetworkIdentity ballId = m_Ball.GetComponent<NetworkIdentity>();

        if (ballId.clientAuthorityOwner != null)
        {
            ballId.RemoveClientAuthority(ballId.clientAuthorityOwner);
        }
        ballId.AssignClientAuthority(connectionToClient);
    }

    [Command]
    void CmdThrow()
    {
        m_Holding = false;
    }
}