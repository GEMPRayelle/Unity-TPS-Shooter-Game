using UnityEngine;
using UnityEngine.AI;

//플레이어 근처에 랜덤한 NavMesh위에 한점을 찍어서 그위치에 아이템을 랜덤하게 생성
public class ItemSpawner : MonoBehaviour
{
    public GameObject[] items;//생성할 아이템들의 원형 프리팹
    public Transform playerTransform;//생성할 반경의 기준점이 되는 플레이어 위치
    
    private float lastSpawnTime;//마지막에 아이템을 생성한 시간
    public float maxDistance = 5f;//아이템이 배치될 최대 간격
    
    private float timeBetSpawn;//현재 생성시간 ~ 다음 생성 시간까지 소요되는 대기시간

    public float timeBetSpawnMax = 7f;//timeBetSpawn최대 시간
    public float timeBetSpawnMin = 2f;//timeBetSpawn최소 시간

    private void Start()
    {
        timeBetSpawn = Random.Range(timeBetSpawnMin,timeBetSpawnMax);//스폰값을 랜덤하게 지정
        lastSpawnTime = 0f; 
    }

    private void Update()
    {
        //주기적으로 아이템을 생성
        if(Time.time >= lastSpawnTime + timeBetSpawn && playerTransform != null)
        //transform이 존재하고 현재시간이 마지막으로 아이템을 생성할 시간에 대기시간을 더한것보다 시간이 더 흘렀다면
        {
            //아이템을 생성한다
            Spawn();
            lastSpawnTime = Time.time;//시간갱신
            timeBetSpawn = Random.Range(timeBetSpawnMin,timeBetSpawnMax);
        }
    }

    private void Spawn()
    {
        //플레이어 근처의 navmesh위에 랜덤위치를 가져온다
        var spawnPosition = Utility.GetRandomPointOnNavMesh(playerTransform.position,maxDistance,NavMesh.AllAreas);

        spawnPosition += Vector3.up * 0.5f;//바닥에서 0.5만큼 올려준다
        var item = Instantiate(items[Random.Range(0,items.Length)],spawnPosition,Quaternion.identity);//하나를 랜덤으로 생성
        Destroy(item, 5f);//아이템이 생성되고 아무일도 없을때 5초뒤 파괴
    }
}