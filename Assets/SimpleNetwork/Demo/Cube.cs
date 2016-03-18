using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TeamHoppi.Networking;

public class Cube : SimpleController
{
    GameObject m_TestItem;
    Vector3 m_PrevPos;

    public override void OnStartLocalPlayer()
    {
        if (GetComponent<SimpleTransformSync>().syncInputType == SyncInputType.TRANSFORM)
        {
            Animator animator = GetComponent<Animator>();
            animator.enabled = true;
            animator.SetTrigger(isServer ? "Right" : "Left");
        }

        m_TestItem = GameObject.Find("Ball");
    }

    public override void OnProcessInput(InputPack input)
    {
        if (GetComponent<SimpleTransformSync>().syncInputType == SyncInputType.KEYBOARD_MOUSE)
        {
            Vector3 pos = transform.position;
            float delta = 5 * Time.fixedDeltaTime;

            switch ((KeyCode)input.keyCode)
            {
                case KeyCode.LeftArrow:
                    pos.x -= delta;
                    break;
                case KeyCode.RightArrow:
                    pos.x += delta;
                    break;
                case KeyCode.DownArrow:
                    pos.z -= delta;
                    break;
                case KeyCode.UpArrow:
                    pos.z += delta;
                    break;
            }

            switch (input.mouseButton)
            {
                case 0:
                    pos.y += delta;
                    break;
                case 1:
                    pos.y -= delta;
                    break;
            }

            transform.position = pos;
        }
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