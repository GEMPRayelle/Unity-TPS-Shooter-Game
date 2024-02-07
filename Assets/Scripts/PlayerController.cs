using UnityEngine;
using UnityEngine.AI;

//각각 독자적으로 동작하는 플레이어 컴포넌트들을 한대 묶어서 총괄하는 스크립트
public class PlayerController : MonoBehaviour
{
    private Animator animator;//자신의 애니메이터
    public AudioClip itemPickupClip;//아이템을 먹었을때 재생할 오디오클립
    public int lifeRemains = 3;//남은생명의수
    private AudioSource playerAudioPlayer;//오디오소스 컴포넌트
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private PlayerShooter playerShooter;

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<PlayerMovement>();
        playerShooter = GetComponent<PlayerShooter>();//LivingEntity를 상속하기에 OnDeath이벤트를 가지고있다
        playerAudioPlayer = GetComponent<AudioSource>();

        playerHealth.OnDeath += HandleDeath;//플레이어가 사망했을때 처리를 추가할 수 있음
        UIManager.Instance.UpdateLifeText(lifeRemains);//UI에 남은 목숨수 표시해줌
        Cursor.visible = false;//마우스커서를 비활성화
    }
    
    private void HandleDeath()//사망했을때 다른 컴포넌트들을 비활성화 해줌
    {
        playerMovement.enabled = false;
        playerShooter.enabled = false;
        if(lifeRemains > 0){//목숨이 남아있다면
            lifeRemains--;
            UIManager.Instance.UpdateLifeText(lifeRemains);//생명수 갱신
            Invoke("Respawn",3f);//3초뒤 리스폰
        }else{//남은 목숨이 없다면
            GameManager.Instance.EndGame();//게임오버창 활성화 
        }
        Cursor.visible = true;//커서 활성화
    }

    public void Respawn()
    {
        //OnEnable, OnDisable로 초기화를 시켜주기위해서 비활성화하고 다시 활성화 시켜준다
        gameObject.SetActive(false);
        transform.position = Utility.GetRandomPointOnNavMesh(transform.position,30f,NavMesh.AllAreas);
        //player 리스폰 위치를 새로운 위치로 변경

        playerMovement.enabled = true;
        playerShooter.enabled = true;
        gameObject.SetActive(true);

        playerShooter.gun.ammoRemain = 120;//총알도 다시 리셋

        Cursor.visible = false;
    }


    //아이템을 먹는 처리
    private void OnTriggerEnter(Collider other)
    {
        if(playerHealth.dead){ return; }//사망이면 아이템을 먹으면안됨
        var item = other.GetComponent<IItem>();
        //IItem으로 가져와지는 아이템이라면
        if(item != null){
            item.Use(gameObject);//아이템을 사용
            playerAudioPlayer.PlayOneShot(itemPickupClip);//아이템 먹는소리
        }
    }
}