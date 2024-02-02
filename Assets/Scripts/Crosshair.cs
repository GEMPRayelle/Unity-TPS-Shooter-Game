using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    public Image aimPointReticle;//조준하는 위치
    public Image hitPointReticle;//실제로 맞게되는 위치

    public float smoothTime = 0.2f;//지연시간
    
    private Camera screenCamera;//씬의 메인카메라
    private RectTransform crossHairRectTransform;

    private Vector2 currentHitPointVelocity;//Smooth에 사용할 값의 변화량
    private Vector2 targetPoint;//UpdatePosition에 들어온 월드포인트를 
    //스크린 포지션 화면상의 위치로 변환하는 위치

    private void Awake(){
        screenCamera = Camera.main;
        crossHairRectTransform = hitPointReticle.GetComponent<RectTransform>();
    }

    public void SetActiveCrosshair(bool active){
        hitPointReticle.enabled = active;
        aimPointReticle.enabled = active;
    }

    public void UpdatePosition(Vector3 worldPoint){
        targetPoint = screenCamera.WorldToScreenPoint(worldPoint);
        //world point를 screen point로 변경해서 vector2값으로 넣어준다
    }

    private void Update(){
        if(!hitPointReticle.enabled) return;
        crossHairRectTransform.position = Vector2.SmoothDamp(crossHairRectTransform.position,
        targetPoint, ref currentHitPointVelocity, smoothTime); 

    }
}