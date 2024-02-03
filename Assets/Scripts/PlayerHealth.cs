using UnityEngine;

public class PlayerHealth : LivingEntity
{
    private Animator animator;
    private AudioSource playerAudioPlayer;

    public AudioClip deathClip;
    public AudioClip hitClip;


    private void Awake()
    {
        playerAudioPlayer = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();//LivingEntity OnEnable을 실행한 다음에 아래 코드 실행
        UpdateUI();
    }
    
    public override void RestoreHealth(float newHealth)
    {
        base.RestoreHealth(newHealth);
        UpdateUI();
    }

    private void UpdateUI()
    {
        //현재 체력을 갱신
        UIManager.Instance.UpdateHealthText(dead ? 0f : health);//사망상태를 검사해서
        //사망상태라면 0, 아니라면 현재체력을 입력한다
    }
    
    public override bool ApplyDamage(DamageMessage damageMessage)
    {
        //LivingEntity의 데미지적용 코드를 먼저 실행한다
        if (!base.ApplyDamage(damageMessage)) return false;
        //부모클래스에서 데미지 적용을 실패하면 자식 클래스 단계에서도 남은 내용을 실행하지않음 

        EffectManager.Instance.PlayHitEffect(damageMessage.hitPoint,
        damageMessage.hitNormal,transform,EffectManager.EffectType.Flesh);
        playerAudioPlayer.PlayOneShot(hitClip);
        UpdateUI();
        
        return true;//공격이 성공했음을 반환
    }
    
    public override void Die()
    {
        base.Die();
        playerAudioPlayer.PlayOneShot(deathClip);
        animator.SetTrigger("Die");//사망애니메이션 트리거 전달
        UpdateUI();//UI갱신
    }
}