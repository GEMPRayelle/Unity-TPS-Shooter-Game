using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    //씬에 존재하는 UI매니저 타입의 오브젝트를 다른 스크립트에서 접근하도록 싱글톤으로 구현
    private static UIManager instance;
    
    public static UIManager Instance{
        get{
            if (instance == null) instance = FindObjectOfType<UIManager>();

            return instance;
        }
    }

    //각각의 UI요소들을 외부에서 접근할 필요가 없기에 private으로 선언
    //인스펙터창에서는 UI들을 할당할수있도록 SerialzieField 필드속성 사용
    [SerializeField] private GameObject gameoverUI;
    [SerializeField] private Crosshair crosshair;

    [SerializeField] private Text healthText;
    [SerializeField] private Text lifeText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text ammoText;
    [SerializeField] private Text waveText;

    public void UpdateAmmoText(int magAmmo, int remainAmmo)
    {
        //남은 탄약수 갱신
        ammoText.text = magAmmo + "/" + remainAmmo;
    }

    public void UpdateScoreText(int newScore)
    {
        //점수 표시
        scoreText.text = "Score : " + newScore;
    }
    
    public void UpdateWaveText(int waves, int count)
    {
        //남은 웨이브
        waveText.text = "Wave : " + waves + "\nEnemy Left : " + count;
    }

    public void UpdateLifeText(int count)
    {
        //남은 목숨수
        lifeText.text = "Life : " + count;
    }

    public void UpdateCrossHairPosition(Vector3 worldPosition)
    {
        //조준점을 worldPosition위치를 표시하는 위치로 옮겨준다 
        crosshair.UpdatePosition(worldPosition);
    }
    
    public void UpdateHealthText(float health)
    {
        //체력표시
        healthText.text = Mathf.Floor(health).ToString();
    }
    
    public void SetActiveCrosshair(bool active)
    {
        //조준점을 표지할지 말지 결정
        crosshair.SetActiveCrosshair(active);
    }
    
    public void SetActiveGameoverUI(bool active)
    {
        //게임오버창 관리
        gameoverUI.SetActive(active);
    }
    
    public void GameRestart()
    {
        //현재 씬을 다시 로드해서 재시작
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}