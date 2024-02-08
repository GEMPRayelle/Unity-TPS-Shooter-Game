using System.Collections.Generic;
using UnityEngine;

// 적 게임 오브젝트를 주기적으로 생성
public class EnemySpawner : MonoBehaviour
{
    //Enemy타입을 다루는 리스트
    /*readonly로 선언하여 한번 오브젝트가 할당된 다음에는 
    이 변수에게는 다른 오브젝트를 새로 생성해서 덮어쓰기 할 수 없다*/
    private readonly List<Enemy> enemies = new List<Enemy>();

    public float damageMax = 40f;//공격력의 범위
    public float damageMin = 20f;
    public Enemy enemyPrefab;//생성할 오브젝트 원본

    public float healthMax = 200f;//최초 최대 체력범위
    public float healthMin = 100f;

    public Transform[] spawnPoints;//생성위치 Transform을 할당할 배열

    public float speedMax = 12f;//속도 최소 최대범위
    public float speedMin = 3f;

    public Color strongEnemyColor = Color.red;//강한 좀비일수록 이 색에 가깝게 함
    private int wave;//좀비 물량을 조절할 웨이브수치

    private void Update()
    {
        //게임매니저 싱글톤이 존재하는지 검사하고 아래 코드를 실행여부를 결정
        if (GameManager.Instance != null && GameManager.Instance.isGameover) return;
        
        if (enemies.Count <= 0) SpawnWave();//적이 다 죽어야지 다음 웨이브를 실행함
        
        UpdateUI();//UI갱신
    }

    private void UpdateUI()
    {
        UIManager.Instance.UpdateWaveText(wave, enemies.Count);//웨이브랑 남은 적의수
    }
    
    private void SpawnWave()
    {
        wave++;
        var spawnCount = Mathf.RoundToInt(wave * 5f);//현재 웨이브에서 * 5의 반올림 만큼의 좀비를 생성
        for(var i = 0; i<spawnCount;i++){
            var enemyIntansity = Random.Range(0f,1f);//강한 정도를 결정
            CreateEnemy(enemyIntansity);//적 생성 
        }
    }
    
    private void CreateEnemy(float intensity)//0과 1사이에서 적의 강한 정도를 받는다
    {
        var health = Mathf.Lerp(healthMin,healthMax,intensity);//체력설정 
        var damage = Mathf.Lerp(damageMin,damageMax,intensity);//데미지 설정
        var speed = Mathf.Lerp(speedMin,speedMax,intensity);//데미지 설정
        var skinColor = Color.Lerp(Color.white,strongEnemyColor,intensity);//색 설정

        var spawnPoint = spawnPoints[Random.Range(0,spawnPoints.Length)];//스폰 포인트

        var enemy = Instantiate(enemyPrefab,spawnPoint.position,spawnPoint.rotation);

        enemy.Setup(health, damage, speed, speed*0.3f, skinColor);//능력치 초기화
        enemies.Add(enemy);//리스트에 추가

        enemy.OnDeath += () => enemies.Remove(enemy);//리스트에서 제거
        enemy.OnDeath += () => Destroy(enemy.gameObject, 10f);//10초뒤 파괴
        enemy.OnDeath += () => GameManager.Instance.AddScore(100);//점수추가
    }
}