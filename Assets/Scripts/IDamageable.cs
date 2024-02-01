public interface IDamageable
{

    //공격받을수 있는 모든 오브젝트를 이 인터페이스를 사용해서 구현하도록 강제한다
    //인터페이스를 사용하여 상대방의 타입을 일일이 검사하지않고 총에 공격을 당한 상대방이
    //이 인터페이스를 가지고있는지만 감지하여 아래 메소드를 실행시킨다

    //공격을 성공했는지 실패했는지 반환해야한다
    bool ApplyDamage(DamageMessage damageMessage);
    //DamageMessage는 공격을 한 측에서 공격을 당하는 측에게 전달하는 정보가 포함된 구조체
}