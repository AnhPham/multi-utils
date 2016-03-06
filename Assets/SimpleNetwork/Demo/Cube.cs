using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Cube : NetworkBehaviour
{
    GameObject m_TestItem;
    Vector3 m_PrevPos;

    public override void OnStartLocalPlayer()
    {
        Animator animator = GetComponent<Animator>();
        animator.enabled = true;
        animator.SetTrigger(isServer ? "Right" : "Left");

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
                Vector3 v = (transform.position - m_PrevPos) / Time.deltaTime;

                GetComponent<SimpleItemCatcher>().Throw(v * 150);
            }
        }

        m_PrevPos = transform.position;
    }
}