using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class MonsterController : MonoBehaviour
{
    private float sightRange = 5f; //���� �þ� ����
    private float aggroRange = 10f; //���� �ִ� �̵� �ݰ�

    private float moveSpeed; //�ӵ�

    public int monsterHP = 10; //ü��

    private Rigidbody player; //Ÿ�� �÷��̾�

    private Vector3 v3StartPosition; //���� ��ġ�� ������ ���� �ݰ� �̵��� �ٽ� ����ġ�� ���ƿ�����

    Vector3 v3MoveDirection;

    private float attackRange = 2f; // ���� �ν� ����

    //private bool targetOn = false;

    private Animator animator;

    private Rigidbody monsterRigidbody;

    public Slider Hpslider;

    public enum States
    {
        idle,
        move,
        attack,
        damaged,
        die
    }

    States currentState;
    States previousState;

    private bool isMove;

    //���信 ���׸���
    public static class TrigonometricFunctions
    {
        // 1�� = ��?180 
        const float Rad = (Mathf.PI * 2) / 360;

        // �Ķ���� : ��Ŀ ������, �������ش�, �׸� ����, ������ 
        public static Vector3[] Cartasian2DCircleLineGroup(Vector3 anchorPosition, int drawCount, float offset,
            float radius)
        {
            Vector3[] pointGroup = new Vector3[drawCount];
            for (int i = 0; i < drawCount; i++)
            {
                var dro = i * Rad * 360/drawCount + offset;
                // ��Ŀ��ġ + �������⺤�� * ��������
                pointGroup[i] = anchorPosition + new Vector3(Mathf.Cos(dro), 0, Mathf.Sin(dro)) * radius;
            }
            return pointGroup;
        }
    }

    void Cartesian_Draw2DCircle(Vector3 pos, float range, Color color)
    {
        int drawCount = 60;
        var lineGroup = TrigonometricFunctions.Cartasian2DCircleLineGroup(pos + new Vector3(0f, 1f, 0f), drawCount, 0, range);
        for (int i = 0; i < drawCount; i++)
        {
            Debug.DrawLine(pos, lineGroup[i], color);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();

        monsterRigidbody = this.gameObject.GetComponent<Rigidbody>();

        v3StartPosition = monsterRigidbody.position;

        monsterHP = 10;

        currentState = States.idle;
        previousState = States.damaged;

        isMove = false;

        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody>();
    }
    /*
    IEnumerator UpdatePlayer()
    {
        while(true)
        {
            GameObject dest = GameObject.FindGameObjectWithTag("Player");
            if(dest == null)
            {
                StopAllCoroutines();
                player = null;
                yield break;
            }

            player = dest.GetComponent<Rigidbody>();

            yield return new WaitForSeconds(5f);
        }
    }
    */

    // Update is called once per frame
    void Update()
    {
        if(monsterHP <= 0)
        {
            DamagedState();
            return;
        }
        if (player == null)
            return;
        Cartesian_Draw2DCircle(v3StartPosition, aggroRange, Color.red);
        Cartesian_Draw2DCircle(monsterRigidbody.position, sightRange, Color.blue);

        CheckState();
    }

    private void CheckState()
    {
        switch (currentState)
        {
            case States.idle:
                IdleState();
                break;
            case States.move:
                MoveState();
                break;
            case States.attack:
                AttackState();
                break;
            case States.damaged:
                DamagedState();
                break;
        }
    }

    private void IdleState()
    {
        //�ڷ�ƾ ���� ���߿��� �ٸ� ���·� ��ȯ x
        //���� ���¿� ������ ��� �����ؼ� �ߺ� ���� ����
        if (previousState == States.idle)
            return;

        //���� �ʱ�ȭ
        isMove = false;
        animator.SetBool("isMove", isMove);
        animator.speed = 1.0f;
        moveSpeed = 1f;
        previousState = States.idle;
        
        //�ڷ�ƾ ����
        StartCoroutine(Idle());
    }

    Vector2 v2RandomPoint;
    Vector3 v3TargetPoint;

    private IEnumerator Idle()
    {
        //���� �⺻ ����: �̵������� ����(aggroRange)������ ������ ��ġ�� ������ �̵�
        //������ �þ� �����ȿ� �÷��̾ ���� �� �̵� ���·� ��ȯ
        
        while (true)
        {
            //���� ���� ����
            v2RandomPoint = Random.insideUnitCircle * aggroRange * 0.5f;
            v3TargetPoint = v3StartPosition;
            v3TargetPoint.x += v2RandomPoint.x;
            v3TargetPoint.z += v2RandomPoint.y;
            v3MoveDirection = (v3TargetPoint - monsterRigidbody.position).normalized;

            // 2�ʰ�(0.5 * 4) �ȱ� �ִϸ��̼� ���� (0.5�ʸ��� Ȯ��)
            isMove = true;
            animator.SetBool("isMove", true);
            for (int i = 0; i < 6; i++)
            {
                //�þ� ���� �ȿ� �÷��̾ ������
                if (CheckwhitinRange(monsterRigidbody.position, player.position, sightRange))
                {
                    //�̵� ���·� �����ϰ� �ڷ�ƾ ����
                    currentState = States.move;
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }

            //1�ʰ� (0.5 * 2) ��� �ִϸ��̼� ���� (0.5�ʸ��� Ȯ��)
            isMove = false;
            animator.SetBool("isMove", false);
            for (int i = 0; i < 2; i++)
            {
                //�þ� ���� �ȿ� �÷��̾ ������
                if (CheckwhitinRange(monsterRigidbody.position, player.position, sightRange))
                {
                    //�̵� ���·� ��ȯ�ϰ� �ڷ�ƾ ����
                    currentState = States.move;
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    //���� ���� �ִ��� üũ�ϴ� �Լ�
    private bool CheckwhitinRange(Vector3 start, Vector3 end, float range)
    {
        Vector3 s, e;
        s = start;
        e = end;
        s.y = 0;
        e.y = 0;
        return (s - e).sqrMagnitude < range * range;
    }


    private void MoveState()
    {
        //�ڷ�ƾ ���� ���߿��� �ٸ� ���·� ��ȯ x
        //���� ���¿� ������ ��� �����ؼ� �ߺ� ���� ����
        if (previousState == States.move)
            return;

        //���� �ʱ�ȭ
        isMove = true;
        animator.SetBool("isMove", isMove);
        animator.speed = 1.5f;
        moveSpeed = 2f;
        previousState = States.move;
        
        //�ڷ�ƾ ����
        StartCoroutine(Move());
    }

    private IEnumerator Move()
    {
        //���� �̵� ����: �þ߿� ������ �÷��̾ ���� �Ѿư�
        //������ �þ� ���� Ȥ�� ��׷� ���� ������ �÷��̾ ���� �� �⺻ ���·� ��ȯ
        //������ ���� ���� �ȿ� �÷��̾ ���� �� ���� ���·� ��ȯ

        while (true)
        {
            v3MoveDirection = (player.position - monsterRigidbody.position).normalized;
            
            //���Ͱ� ��׷� �������� �����
            if (!CheckwhitinRange(v3StartPosition, monsterRigidbody.position, aggroRange))
            {
                v3MoveDirection = (v3StartPosition - monsterRigidbody.position).normalized;
                animator.speed = 3.0f;
                moveSpeed = 4f;
                //�ʱ� ��ġ���� �Ÿ��� 1m���Ϸ� �ɶ����� ����
                yield return new WaitUntil(()=>CheckwhitinRange(v3StartPosition, monsterRigidbody.position, 5.0f));
                animator.speed = 1.0f;
                isMove = false;
                animator.SetBool("isMove", isMove);
                yield return new WaitForSeconds(1f);
                currentState = States.idle;
                yield break;

            }
            //�þ� �������� �����
            else if (!CheckwhitinRange(monsterRigidbody.position, player.position, sightRange))
            {
                //0.5�� ���� �� �⺻ ���·� ��ȯ�ϰ� �ڷ�ƾ ����
                isMove = false;
                animator.SetBool("isMove", isMove);
                yield return new WaitForSeconds(0.5f);
                currentState = States.idle;
                yield break;
            }
            //���� ���� �ȿ� �÷��̾ ������
            else if (CheckwhitinRange(monsterRigidbody.position, player.position, attackRange))
            {
                //���� ���·� ��ȯ�ϰ� �ڷ�ƾ ����
                currentState = States.attack;
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void AttackState()
    {
        //���� ���� ���� �÷��̾ ���� �� ���º�ȯ
        //���� ���¿� ������ ��� �����ؼ� �ߺ� ���� ����
        if (previousState == States.attack)
            return;

        //���� �ʱ�ȭ
        isMove = false;
        animator.SetBool("isMove", isMove);
        animator.speed = 1.3f;
        previousState = States.attack;

        //�ڷ�ƾ ����
        StartCoroutine(Attack());
    }

    private void DamagedState()
    {
        StopAllCoroutines();
        animator.SetTrigger("Die");
        ChangeLayerRecursively(gameObject, 16);
        this.gameObject.tag = "Die";
    }

    IEnumerator Attack()
    {
        while (true)
        {
            //���� ���� ������ �÷��̾ �����
            if (!CheckwhitinRange(monsterRigidbody.position, player.position, attackRange))
            {
                //�̵� ���·� ��ȯ�ϰ� �ڷ�ƾ ����
                currentState = States.move;
                yield break;
            }
            animator.SetTrigger("ATK0");
            yield return new WaitForSeconds(3.0f);
        }
    }

    private void ChangeLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            ChangeLayerRecursively(child.gameObject, layer);
        }
    }


    private float GetAngle(Vector3 origin, Vector3 target)
    {
        Vector3 direction = target - origin;

        return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }



    float rotateStep = 5f;

    private void FixedUpdate()
    {
        if (player == null)
            return;
        if (isMove)
        {
            v3MoveDirection.y = 0;
            monsterRigidbody.MovePosition(monsterRigidbody.position + moveSpeed * Time.deltaTime * v3MoveDirection);
            monsterRigidbody.rotation = Quaternion.Slerp(monsterRigidbody.rotation, Quaternion.LookRotation(v3MoveDirection), rotateStep * Time.deltaTime);
        }

    }

}
