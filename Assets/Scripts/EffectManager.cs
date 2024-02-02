using UnityEngine;

public class EffectManager : MonoBehaviour
{
    private static EffectManager m_Instance;
    public static EffectManager Instance
    {
        get
        {
            if (m_Instance == null) m_Instance = FindObjectOfType<EffectManager>();
            return m_Instance;
        }
    }

    public enum EffectType
    {
        Common,
        Flesh
    }
    
    public ParticleSystem commonHitEffectPrefab;//대부분에 경우에 사용할 이펙트
    public ParticleSystem fleshHitEffectPrefab;//피가 나오는 생명체를 대상으로할 이펙트
    
    /// <summary>
    /// 이펙트를 재생할 함수
    /// </summary>
    /// <param name="pos">이펙트를 재생할 위치</param>
    /// <param name="normal">이펙트가 바라볼 방향</param>
    /// <param name="parent">이펙트에게 할당할 부모</param>
    /// <param name="effectType">사용할 타입</param>
    public void PlayHitEffect(Vector3 pos, Vector3 normal, Transform parent = null, 
    EffectType effectType = EffectType.Common)
    {
        var targetPrefab = commonHitEffectPrefab;//사용할 이펙트타입을 일반적인 피탄효과로 지정
        if(effectType == EffectType.Flesh){//피가 튀는 효과를 재생하려면 
            targetPrefab = fleshHitEffectPrefab;
        }

        var effect = Instantiate(targetPrefab, pos, Quaternion.LookRotation(normal));

        if(parent != null){//이펙트의 부모가될 트랜스폼이 존재한다면
            effect.transform.SetParent(parent);//effect의 부모 트랜스폼으로 설정해준다
        }
        effect.Play();//이펙트 재생
    }
}