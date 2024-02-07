using UnityEngine;

public class AmmoPack : MonoBehaviour, IItem
{
    public int ammo = 30;

    //아이템을 먹은 상대는 총알을 추가한다
    public void Use(GameObject target)
    {
        var playerShooter = target.GetComponent<PlayerShooter>();

        if(playerShooter != null && playerShooter.gun != null){
            //playerShooter와 플레이어의 총이 가져와졌다면
            playerShooter.gun.ammoRemain += ammo;//총알양 증가
        }

        Destroy(gameObject);//아이템 파괴
    }
}