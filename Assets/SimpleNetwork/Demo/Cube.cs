using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Cube : NetworkBehaviour
{
    GameObject m_TestItem;

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

        m_TestItem = GameObject.Find("Ball");
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                GetComponent<SimpleItemCatcher>().Hold(m_TestItem);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                GetComponent<SimpleItemCatcher>().Throw(new Vector3(0, 200, -100));
            }
        }
    }
}