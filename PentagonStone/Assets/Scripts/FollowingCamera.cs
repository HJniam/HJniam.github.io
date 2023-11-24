using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowingCamera : MonoBehaviour
{
    public Transform target;  // 타겟 (플레이어 캐릭터)
    public Vector3 offset;    // 카메라의 기본 오프셋
    public Vector3 frontOffset = new Vector3(0, 2, 4); // 타겟 앞쪽으로의 오프셋
    public float frontOffsetdDistance;
    public float frontOffsetHeight;
    public float followSpeed = 10f;  // 카메라의 따라가는 속도
    public float viewChangeSpeed = 2f; // 탭 키로 뷰 변경 시 속도

    private bool isFrontView = false; // 타겟의 정면을 보고 있는지의 여부
    private Vector3 desiredPosition;
    private Quaternion desiredRotation;

    private void Update()
    {
        // Tab 키로 뷰 변경
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isFrontView = !isFrontView;
            frontOffset.x = 0;
            frontOffset.z = 0;
            frontOffset = target.forward * frontOffsetdDistance;
            frontOffset.y = frontOffsetHeight;
        }
        if (isFrontView)
        {
            
            desiredPosition = target.position + frontOffset;
            //desiredPosition = target.position - target.forward * frontOffset.z + new Vector3(0, frontOffset.y, 0);
        }
        else
        {
            desiredPosition = target.position + offset;
        }
        desiredRotation = Quaternion.LookRotation(target.position - desiredPosition);
        
    }

    private void LateUpdate()
    {
        // 부드러운 위치 이동
        Vector3 smoothPosition = Vector3.Lerp(transform.position, desiredPosition, viewChangeSpeed * Time.deltaTime);
        transform.position = smoothPosition;

        // 부드러운 회전
        Quaternion smoothRotation = Quaternion.Slerp(transform.rotation, desiredRotation, viewChangeSpeed * Time.deltaTime);
        transform.rotation = smoothRotation;
    }
}
