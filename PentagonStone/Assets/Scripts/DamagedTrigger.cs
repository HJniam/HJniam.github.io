using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagedTrigger : MonoBehaviour
{
    private bool isDamaged = false;
    //private float delay = 1.5f;
    private int currentLayer;
    private SkinnedMeshRenderer[] skinnedMeshs;
    private MeshRenderer[] meshs;
    

    private void Awake()
    {
        skinnedMeshs = GetComponentsInChildren<SkinnedMeshRenderer>();
        meshs = GetComponentsInChildren<MeshRenderer>();

        currentLayer = this.gameObject.layer;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isDamaged)
        {
            if (collision.gameObject.layer == 6)
            {
                StartCoroutine(OnDamaged());
            }
        }
    }


    //데미지 입으면 깜박깜박거리기
    IEnumerator OnDamaged()
    {
        isDamaged = true;
        this.gameObject.layer = 15;
        for(int i = 0; i<5; i++)
        {
            foreach (SkinnedMeshRenderer mesh in skinnedMeshs)
            {
                mesh.enabled = false;
            }
            foreach (MeshRenderer mesh in meshs)
            {
                mesh.enabled = false;
            }
            yield return new WaitForSeconds(0.1f);
            foreach (SkinnedMeshRenderer mesh in skinnedMeshs)
            {
                mesh.enabled = true;
            }
            foreach (MeshRenderer mesh in meshs)
            {
                mesh.enabled = true;
            }
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.5f);
        isDamaged = false;
        this.gameObject.layer = currentLayer;
    }
}
