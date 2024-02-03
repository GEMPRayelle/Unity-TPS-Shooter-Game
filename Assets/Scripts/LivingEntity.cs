using System;
using UnityEngine;

//적 AI와 플레이어를 포함해서 게임 속 생명체들이 가지게 될 공통 클래스
public class LivingEntity : MonoBehaviour, IDamageable
{
    public float startingHealth = 100f;//처음활성화될때 사용할 초기 체력
    public float health { get; protected set; }//현재 체력
    public bool dead { get; protected set; }//사망 상태 표현
    
    public event Action OnDeath;//Action타입의 이벤트
    //Entity가 사망하는순간 실행될 콜백을 외부에서 접근해서 할당할 수 있는 이벤트
    
    private const float minTimeBetDamaged = 0.1f;//공격과 공격 사이의 최소 대기 시간
    private float lastDamagedTime;//최근공격을 당한 시점

    protected bool IsInvulnerabe//현재 엔티티가 무적모드인지 반환하는 Property
    {
        get
        {
            //현재시점이 마지막으로 공격을 당한 시점에서 minTimeBetDamaged이상 지났는지 체크
            if (Time.time >= lastDamagedTime + minTimeBetDamaged) return false;

            return true;//마지막으로 공격을 당한지 0.1초도 지나지않았다면 무적모드로 만들어줌
        }
    }
    
    protected virtual void OnEnable()//자식클래스에서 접근하고 오버라이드 가능함
    {
        //생명체의 상태를 리셋
        dead = false;
        health = startingHealth;
    }

    public virtual bool ApplyDamage(DamageMessage damageMessage)
    {
        //무적상태거나, 예외사항으로 공격이 스스로에게 가했거나, 사망한 상태면 공격적용이 false
        if (IsInvulnerabe || damageMessage.damager == gameObject || dead) return false;

        lastDamagedTime = Time.time;//최근공격시점을 현재시점으로 바꾼다
        health -= damageMessage.amount;//현재체력을 데미지양만큼 감소시킨다
        
        if (health <= 0) Die();//체력이 0보다 작으면 Die를 실행

        return true;//위의 조건이 아니라면 공격적용이 true로 된걸 반환시킴
    }
    
    public virtual void RestoreHealth(float newHealth)//자기자신을 회복하는 메서드
    {
        if (dead) return;//사망상태면 회복하지않고 바로 종료
        
        health += newHealth;//체력을 입력받은 값만큼 회복시킨다
    }
    
    public virtual void Die()//사망구현
    {
        //이벤트에 최소 하나의 리스너가 등록되어있다면
        if (OnDeath != null) OnDeath();//이벤트 실행
        
        dead = true;//상태를 사망한 상태로 변경
    }
}