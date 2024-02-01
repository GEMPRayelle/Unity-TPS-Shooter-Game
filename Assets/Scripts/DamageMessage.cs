using UnityEngine;

//클래스로 만들면 ref타입이라 메세지를 전달받은 측에서 데미지 메세지를 임의로 수정하면 
//같은 메세지를 전달받은 다른 곳에서도 해당 변경사항이 반영된다

//구조체같은 value타입은 데미지 메세지를 전달받은 측에서 필요에따라 메세지 내용을 
//마음대로 수정해도 다른 곳에 영향을 미치지않는다
public struct DamageMessage
{
    //공격을 가한 오브젝트
    public GameObject damager;
    //공격 데미지양
    public float amount;

    //공격이 가해진 위치
    public Vector3 hitPoint;
    //공격을 맞은 표면이 바라보는 방향 or 공격이 가해진 방향의 반대 방향
    public Vector3 hitNormal;
}