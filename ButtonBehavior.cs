using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class ButtonBehavior : MonoBehaviour

{

    float m_Timer = 0.0f;
    [SerializeField] float m_timerTickTime = 5.0f;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        m_Timer += Time.deltaTime;
        if (m_Timer >= m_timerTickTime)
        {
            Action();
            m_Timer = 0.0f;
        }

    }

    void Action()
    {
        Debug.Log("Button Verification");

        if (TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            Debug.Log("Network Object Found");

        }
        if (TryGetComponent<NetworkTransform>(out NetworkTransform networkTransform))
        {
            Debug.Log("Network Transform Found");
        }

    }
}
