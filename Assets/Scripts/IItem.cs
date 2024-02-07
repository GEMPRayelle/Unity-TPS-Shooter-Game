using UnityEngine;

public interface IItem
{
    /// <summary>
    /// IItem 인터페이스 메서드
    /// </summary>
    /// <param name="target">아이템을 적용할 게임 오브젝트</param>
    void Use(GameObject target);
}