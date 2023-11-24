using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    MonsterController m_MonsterController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Monster")
        {
            this.gameObject.GetComponent<BoxCollider>().enabled = false;
            m_MonsterController = other.gameObject.GetComponentInParent<MonsterController>();
            if (m_MonsterController == null)
                return;
            m_MonsterController.monsterHP--;
            m_MonsterController.Hpslider.value--;
        }
    }

    
}
