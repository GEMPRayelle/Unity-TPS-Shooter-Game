using UnityEngine;
using UnityEngine.AI;

public static class Utility
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="center">중심의 위치</param>
    /// <param name="distance">반경거리</param>
    /// <param name="areaMask">Area Mask에 해당하는 NavMesh지점에서의 랜덤위치를 반환</param>
    /// <returns></returns>
    public static Vector3 GetRandomPointOnNavMesh(Vector3 center, float distance, int areaMask)
    {
        //중심위치를 기준으로 distance만큼 구를 그릴때 그 안에 있는 랜덤한 위치를 하나 찍은 곳이된다
        var randomPos = Random.insideUnitSphere * distance + center;
        
        NavMeshHit hit;//NavMesh Sampling의 정보를 담을 변수
        
        NavMesh.SamplePosition(randomPos, out hit, distance, areaMask);
        //어떤 위치를 할당하고, 샘플링 결과정보를 담을 hit을 out으로 할당, 반경과 mask를 할당하면
        //Mask에 해당하는 NavMesh중에서 랜덤 포지션에서 distance까지의 반경 내에서 randomPos에
        //가장 가까운 점을 하나 찾아서 hit에 담아준다
        
        return hit.position;
    }
    
    /// <summary>
    /// 정규분포로부터 랜덤 값을 가져온다
    /// </summary>
    /// <param name="mean"> 입력값 평균값 </param>
    /// <param name="standard"> 표준편차값 </param>
    /// <returns>랜덤 값 반환</returns>
    public static float GetRandomNormalDistribution(float mean, float standard)
    {
        var x1 = Random.Range(0f, 1f);
        var x2 = Random.Range(0f, 1f);
        return mean + standard * (Mathf.Sqrt(-2.0f * Mathf.Log(x1)) * Mathf.Sin(2.0f * Mathf.PI * x2));
    }
}