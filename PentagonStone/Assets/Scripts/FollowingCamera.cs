using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowingCamera : MonoBehaviour
{
    public Transform target;  // Ÿ�� (�÷��̾� ĳ����)
    public Vector3 offset;    // ī�޶��� �⺻ ������
    public Vector3 frontOffset = new Vector3(0, 2, 4); // Ÿ�� ���������� ������
    public float frontOffsetdDistance;
    public float frontOffsetHeight;
    public float followSpeed = 10f;  // ī�޶��� ���󰡴� �ӵ�
    public float viewChangeSpeed = 2f; // �� Ű�� �� ���� �� �ӵ�

    private bool isFrontView = false; // Ÿ���� ������ ���� �ִ����� ����
    private Vector3 desiredPosition;
    private Quaternion desiredRotation;

    private void Update()
    {
        // Tab Ű�� �� ����
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
        // �ε巯�� ��ġ �̵�
        Vector3 smoothPosition = Vector3.Lerp(transform.position, desiredPosition, viewChangeSpeed * Time.deltaTime);
        transform.position = smoothPosition;

        // �ε巯�� ȸ��
        Quaternion smoothRotation = Quaternion.Slerp(transform.rotation, desiredRotation, viewChangeSpeed * Time.deltaTime);
        transform.rotation = smoothRotation;
    }
}
