using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMove : MonoBehaviour
{
    #region Variable
    [Serializable]
    public class Components
    {
        [HideInInspector] public Animator anim;
        [HideInInspector] public IMovement3D movement3D;
        [HideInInspector] public Rigidbody rb;
    }
    [Serializable]
    public class KeyOption
    {
        public KeyCode moveForward = KeyCode.UpArrow;
        public KeyCode moveBackward = KeyCode.DownArrow;
        public KeyCode moveLeft = KeyCode.LeftArrow;
        public KeyCode moveRight = KeyCode.RightArrow;
        public KeyCode run = KeyCode.LeftShift;

        public KeyCode attack = KeyCode.A;
    }

    [Serializable]
    public class AnimParam
    {
        public string paramisMove = "isMove";
        public string paramisRun = "isRun";
        public string paramATK0 = "ATK0";
        public string paramDamaged = "Damaged";
        public string paramDie = "Die";
    }
    [Serializable]
    public class CharacterState
    {
        public bool isMove = false;
        public bool isRun  = false;
        public bool isAttack = false;
        public bool isDamaged = false;
        public bool isDie = false;

        public bool isAction = false;
    }
    #endregion

    #region Fields, Properties
    public Components ch_component => _components;
    public KeyOption ch_key => _keyOption;
    public AnimParam ch_animParam => _animParam;
    public CharacterState ch_state => _state;


    [SerializeField]        private Components      _components     = new();
    [Space, SerializeField] private KeyOption       _keyOption      = new();
    [Space, SerializeField] private AnimParam       _animParam      = new();
    [Space, SerializeField] private CharacterState  _state          = new();

    /// <summary> Time.deltaTime �׻� ���� </summary>
    //private float _deltaTime;

    // Current Movement Variables

    /// <summary> Ű���� WASD �Է����� ��� ���� �̵� ���� </summary>
    [SerializeField]
    private Vector3 _moveDir;

    #endregion

    /***********************************************************************
        *                               Init Methods
        ***********************************************************************/
    #region .
    private void InitComponents()
    {
        ch_component.anim = GetComponent<Animator>();
        TryGetComponent(out ch_component.movement3D);
        TryGetComponent(out ch_component.rb);

        myWeaponCollider = this.gameObject.GetComponentInChildren<BoxCollider>();
        myWeaponCollider.enabled = false;

        characterHP = 10;
    }
    #endregion

    /***********************************************************************
        *                               Methods
        ***********************************************************************/
    #region .

    private bool SetStateByKeyInput()
    {
        if (Input.GetKey(ch_key.attack))
        {
            ch_state.isAttack = true;
            return true;
        }

        return false;
    }

    /// <summary> Ű���� �Է��� ���� �ʵ� �ʱ�ȭ </summary>
    private void SetMoveValuesByKeyInput()
    {
        float h = 0f, v = 0f;

        if (Input.GetKey(ch_key.moveForward)) v += 1.0f;
        if (Input.GetKey(ch_key.moveBackward)) v -= 1.0f;
        if (Input.GetKey(ch_key.moveLeft)) h -= 1.0f;
        if (Input.GetKey(ch_key.moveRight)) h += 1.0f;

        // Move, Rotate
        SendMoveInfo(h, v);
    }

    private void SetCharacterBehavior()
    {
        if (characterHP <= 0) Die();
        else if (ch_state.isDamaged) StartCoroutine(Damaged());
        else if (ch_state.isAttack) StartCoroutine(Attack());

        ch_component.anim.SetBool(ch_animParam.paramisMove, ch_state.isMove);
        ch_component.anim.SetBool(ch_animParam.paramisRun, ch_state.isRun);
    }

    private void Die()
    {
        StopMove();
        ch_state.isDie = true;
        ch_component.anim.SetTrigger(ch_animParam.paramDie);
        ChangeLayerRecursively(gameObject, 16);
        this.gameObject.tag = "Die";
    }

    IEnumerator Damaged()
    {
        StopMove();
        ch_state.isAction = true;
        characterHP--;
        ch_component.anim.SetTrigger(ch_animParam.paramDamaged);
        yield return new WaitForSeconds(1.0f);
        while (gameObject.layer == 15)
        {
            SetMoveValuesByKeyInput();
            ch_component.anim.SetBool(ch_animParam.paramisMove, ch_state.isMove);
            ch_component.anim.SetBool(ch_animParam.paramisRun, ch_state.isRun);
            yield return null;
        }
        ch_state.isDamaged = false;
        ch_state.isAction = false;
    }

    IEnumerator Attack()
    {
        StopMove();
        ch_state.isAction = true;
        //AŰ�� ������ �ִ� ���� ���� ���� ����
        while (Input.GetKey(KeyCode.A))
        {
            myWeaponCollider.enabled = true;
            //�ִϸ��̼� Ʈ���� ����
            ch_component.anim.SetTrigger(ch_animParam.paramATK0);
            yield return new WaitForSeconds(0.8f);
        }
        //AŰ�� ���� �⺻ ���·� ��ȯ
        myWeaponCollider.enabled = false;
        ch_state.isAttack = false;
        ch_state.isAction = false;
    }


    #endregion
    /***********************************************************************
    *                               Movement Methods
    ***********************************************************************/
    #region .

    private void SendMoveInfo(float horizontal, float vertical)
    {
        //���� ���� ���
        _moveDir = new Vector3(horizontal, 0f, vertical).normalized;

        ch_component.movement3D.SetMovement(_moveDir, ch_state.isRun);

        ch_state.isMove = horizontal != 0 || vertical != 0;
        ch_state.isRun = Input.GetKey(ch_key.run);
    }

    private void StopMove()
    {
        ch_state.isMove = false;
        ch_state.isRun = false;
        SendMoveInfo(0, 0);
    }
    #endregion
    /***********************************************************************
    *                               Public Methods
    ***********************************************************************/
    #region .
    public void KnockBack(in Vector3 force, float time)
    {
        ch_component.movement3D.KnockBack(force, time);
    }

    private void ChangeLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            ChangeLayerRecursively(child.gameObject, layer);
        }
    }

    #endregion


    private void Start()
    {
        InitComponents();
    }

    private void Update()
    {
        //�ڷ�ƾ ���߿� ������Ʈ x
        if (ch_state.isAction || ch_state.isDie) return;
        //������ ���̾�� ����� ������ ���� true
        if (this.gameObject.layer == 15) ch_state.isDamaged = true;
        //���� �ƴϸ� Ű �Է� �ޱ�
        else if (!SetStateByKeyInput())
            SetMoveValuesByKeyInput();

        // 2. Behaviors, Camera Actions
        SetCharacterBehavior();
    }



    public float characterHP;
    private BoxCollider myWeaponCollider;

    /*
    void Update()
    {
        if (characterHP <= 0)
        {
            currentState = States.die;
            CheckState();
            return;
        }
        if (this.gameObject.layer == 15)
            currentState = States.damaged;
        CheckState();
    }


    private void LateUpdate()
    {
        if (currentState == States.move)
            Move();
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
            case States.die:
                DieState();
                break;
        }
    }

    private bool CheckAxisInput()
    {
        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
            return true;
        else
            return false;
    }

    private void IdleState()
    {
        //Ű �Է� �� ���� ��ȯ
        if (CheckAxisInput())
            currentState = States.move;
        if (Input.GetKey(KeyCode.A))
            currentState = States.attack;
        //�ߺ� ���� ����
        if (previousState == currentState)
            return;
        //�ִϸ��̼� ���� ����
        animator.SetBool("isMove", false);
        //���� ���� ����
        previousState = States.idle;
    }

    private void MoveState()
    {
        //���� �Ʒ� ���� �켱, �Է¿� ���� ���� ��ȯ
        if (!CheckAxisInput())
            currentState = States.idle;
        if (Input.GetKey(KeyCode.A))
            currentState = States.attack;
        //�ӵ� ����
        speed = Input.GetButton("Dash") ? runSpeed : moveSpeed;
        //3�̻��̸� �޸���
        animator.SetFloat("speed", speed);
        //���� ���¿� ������ ��� �����ؼ� �ߺ� ���� ����
        if (previousState == currentState)
            return;
        //�ִϸ��̼� ���� ����
        animator.SetBool("isMove", true);
        //���� ���� ����
        previousState = States.move;
    }

    private void AttackState()
    {
        //�ڷ�ƾ ���� ���߿��� �ٸ� ���·� ��ȯ x
        //���� ���¿� ������ ��� �����ؼ� �ߺ� ���� ����
        if (previousState == currentState)
            return;
        //�ִϸ��̼� ���� ����
        animator.SetBool("isMove", false);
        //���� ���� ����
        previousState = States.attack;
        //���� �ڷ�ƾ ����
        StartCoroutine(Attack());
    }

    private IEnumerator Attack()
    {
        //AŰ�� ������ �ִ� ���� ���� ���� ����
        while (Input.GetKey(KeyCode.A))
        {
            myWeaponCollider.enabled = true;
            //�ִϸ��̼� Ʈ���� ����
            animator.SetTrigger("ATK0");
            yield return new WaitForSeconds(attackDuration * 0.9f); //���� �ִϸ��̼ǽ��� �ð� * 0.9�� ��ŭ ������
        }
        //AŰ�� ���� �⺻ ���·� ��ȯ
        myWeaponCollider.enabled = false;
        currentState = States.idle;
    }

    private void DamagedState()
    {
        if (this.gameObject.layer == 3)
        {
            currentState = States.idle;
            return;
        }
        if (previousState == States.damaged)
            return;
        animator.SetBool("isMove", false);
        StopAllCoroutines();
        previousState = States.damaged;
        StartCoroutine(Damaged());

    }

    private IEnumerator Damaged()
    {
        characterHP--;
        animator.SetTrigger("Damaged");
        for (int i = 0; i < 10; i++)
        {
            rb.AddForce(-transform.forward * 30f, ForceMode.VelocityChange);
            yield return new WaitForFixedUpdate();
        }
    }


    
    */
}
