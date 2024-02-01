using UnityEngine;

//매 프레임마다 특정 Transform의 위치와 회전으로 자기 자신의 위치화 회전을 덮어쓰기하는 스크립트
public class LateUpdateFollow : MonoBehaviour
{
    //따라갈 대상 Transform
    public Transform targetToFollow;

    //Update가 종료되는 타이밍에 실행되는 메서드이다
    private void LateUpdate()
    {
        //현재 transform의 위치와 회전을 타켓 transform의 위치와 회전값으로 덮어쓰게한다
        transform.position = targetToFollow.position;
        transform.rotation = targetToFollow.rotation;
    }
}