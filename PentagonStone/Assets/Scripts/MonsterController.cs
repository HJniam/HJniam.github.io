using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class MonsterController : MonoBehaviour
{
    private float sightRange = 5f; //몬스터 시야 범위
    private float aggroRange = 10f; //몬스터 최대 이동 반경

    private float moveSpeed; //속도

    public int monsterHP = 10; //체력

    private Rigidbody player; //타겟 플레이어

    private Vector3 v3StartPosition; //시작 위치를 저장해 일정 반경 이동시 다시 원위치로 돌아오게함

    Vector3 v3MoveDirection;

    private float attackRange = 2f; // 공격 인식 범위

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

    //씬뷰에 원그리기
    public static class TrigonometricFunctions
    {
        // 1° = π?180 
        const float Rad = (Mathf.PI * 2) / 360;

        // 파라미터 : 앵커 포지션, 각도분해능, 그릴 각도, 반지름 
        public static Vector3[] Cartasian2DCircleLineGroup(Vector3 anchorPosition, int drawCount, float offset,
            float radius)
        {
            Vector3[] pointGroup = new Vector3[drawCount];
            for (int i = 0; i < drawCount; i++)
            {
                var dro = i * Rad * 360/drawCount + offset;
                // 엥커위치 + 각도방향벡터 * 반지름값
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
        //코루틴 실행 도중에는 다른 상태로 변환 x
        //이전 상태와 동일할 경우 리턴해서 중복 동작 방지
        if (previousState == States.idle)
            return;

        //상태 초기화
        isMove = false;
        animator.SetBool("isMove", isMove);
        animator.speed = 1.0f;
        moveSpeed = 1f;
        previousState = States.idle;
        
        //코루틴 실행
        StartCoroutine(Idle());
    }

    Vector2 v2RandomPoint;
    Vector3 v3TargetPoint;

    private IEnumerator Idle()
    {
        //몬스터 기본 상태: 이동가능한 범위(aggroRange)내에서 랜덤한 위치를 지정해 이동
        //몬스터의 시야 범위안에 플레이어가 들어올 시 이동 상태로 전환
        
        while (true)
        {
            //랜덤 범위 지정
            v2RandomPoint = Random.insideUnitCircle * aggroRange * 0.5f;
            v3TargetPoint = v3StartPosition;
            v3TargetPoint.x += v2RandomPoint.x;
            v3TargetPoint.z += v2RandomPoint.y;
            v3MoveDirection = (v3TargetPoint - monsterRigidbody.position).normalized;

            // 2초간(0.5 * 4) 걷기 애니메이션 실행 (0.5초마다 확인)
            isMove = true;
            animator.SetBool("isMove", true);
            for (int i = 0; i < 6; i++)
            {
                //시야 범위 안에 플레이어가 들어오면
                if (CheckwhitinRange(monsterRigidbody.position, player.position, sightRange))
                {
                    //이동 상태로 변경하고 코루틴 종료
                    currentState = States.move;
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }

            //1초간 (0.5 * 2) 대기 애니메이션 실행 (0.5초마다 확인)
            isMove = false;
            animator.SetBool("isMove", false);
            for (int i = 0; i < 2; i++)
            {
                //시야 범위 안에 플레이어가 들어오면
                if (CheckwhitinRange(monsterRigidbody.position, player.position, sightRange))
                {
                    //이동 상태로 전환하고 코루틴 종료
                    currentState = States.move;
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    //범위 내에 있는지 체크하는 함수
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
        //코루틴 실행 도중에는 다른 상태로 변환 x
        //이전 상태와 동일할 경우 리턴해서 중복 동작 방지
        if (previousState == States.move)
            return;

        //상태 초기화
        isMove = true;
        animator.SetBool("isMove", isMove);
        animator.speed = 1.5f;
        moveSpeed = 2f;
        previousState = States.move;
        
        //코루틴 실행
        StartCoroutine(Move());
    }

    private IEnumerator Move()
    {
        //몬스터 이동 상태: 시야에 감지된 플레이어를 향해 쫓아감
        //몬스터의 시야 범위 혹은 어그로 범위 밖으로 플레이어가 나갈 시 기본 상태로 전환
        //몬스터의 공격 범위 안에 플레이어가 들어올 시 공격 상태로 전환

        while (true)
        {
            v3MoveDirection = (player.position - monsterRigidbody.position).normalized;
            
            //몬스터가 어그로 범위에서 벗어나면
            if (!CheckwhitinRange(v3StartPosition, monsterRigidbody.position, aggroRange))
            {
                v3MoveDirection = (v3StartPosition - monsterRigidbody.position).normalized;
                animator.speed = 3.0f;
                moveSpeed = 4f;
                //초기 위치에서 거리가 1m이하로 될때까지 실행
                yield return new WaitUntil(()=>CheckwhitinRange(v3StartPosition, monsterRigidbody.position, 5.0f));
                animator.speed = 1.0f;
                isMove = false;
                animator.SetBool("isMove", isMove);
                yield return new WaitForSeconds(1f);
                currentState = States.idle;
                yield break;

            }
            //시야 범위에서 벗어나면
            else if (!CheckwhitinRange(monsterRigidbody.position, player.position, sightRange))
            {
                //0.5초 멈춘 후 기본 상태로 전환하고 코루틴 종료
                isMove = false;
                animator.SetBool("isMove", isMove);
                yield return new WaitForSeconds(0.5f);
                currentState = States.idle;
                yield break;
            }
            //공격 범위 안에 플레이어가 들어오면
            else if (CheckwhitinRange(monsterRigidbody.position, player.position, attackRange))
            {
                //공격 상태로 전환하고 코루틴 종료
                currentState = States.attack;
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void AttackState()
    {
        //공격 범위 내에 플레이어가 접근 시 상태변환
        //이전 상태와 동일할 경우 리턴해서 중복 동작 방지
        if (previousState == States.attack)
            return;

        //상태 초기화
        isMove = false;
        animator.SetBool("isMove", isMove);
        animator.speed = 1.3f;
        previousState = States.attack;

        //코루틴 실행
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
            //공격 범위 밖으로 플레이어가 벗어나면
            if (!CheckwhitinRange(monsterRigidbody.position, player.position, attackRange))
            {
                //이동 상태로 전환하고 코루틴 종료
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
