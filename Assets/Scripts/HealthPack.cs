using UnityEngine;

public class HealthPack : MonoBehaviour, IItem
{
    public float health = 50;

    public void Use(GameObject target)
    {
        var livingEntity = target.GetComponent<LivingEntity>();

        if(livingEntity != null){
            livingEntity.RestoreHealth(health);//체력양 증가
        }

        Destroy(gameObject);//아이템 파괴
    }
}