using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMovement3D
{
    /// <summary> 현재 이동 중인지 여부 </summary>
    bool IsMove();
    /// <summary> 지면에 닿아 있는지 여부 </summary>
    bool OnGround();
    /// <summary> 지면으로부터의 거리 </summary>
    float GetDistanceFromGround();

    /// <summary> 월드 이동벡터 초기화(이동 명령) </summary>
    void SetMovement(in Vector3 worldMoveDirection, bool isRunning);
    /// <summary> 이동 중지 </summary>
    void StopMove();

    /// <summary> 밀쳐내기 </summary>
    void KnockBack(in Vector3 force, float time);
}