using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    
    public static GameManager Instance
    {
        get
        {
            if (instance == null) instance = FindObjectOfType<GameManager>();
            
            return instance;
        }
    }

    private int score;
    public bool isGameover { get; private set; }//자동 프로퍼티

    private void Awake()
    {
        if (Instance != this) Destroy(gameObject);
        //Awake를 실행하는 오브젝트가 싱글톤이 아니면 파괴시킴
    }
    
    public void AddScore(int newScore)
    {
        if (!isGameover)//게임오버가 아니라면
        {
            score += newScore;//점수추가
            UIManager.Instance.UpdateScoreText(score);//UI갱신
        }
    }
    
    public void EndGame()
    {
        isGameover = true;//게임오버 true
        UIManager.Instance.SetActiveGameoverUI(true);//게임오버창 활성화
    }
}