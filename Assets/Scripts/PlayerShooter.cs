using UnityEngine;

//플레이어 입력에 따라 총을 쏘거나 재장전하고 총이 항상 왼손 손잡이에 위치하도록 갱신함
public class PlayerShooter : MonoBehaviour
{
    public enum AimState
    {
        Idle,//가만히있는상태
        HipFire//조준안한상태로 발사
    }

    public AimState aimState { get; private set; }//프로퍼티로 외부에서는 값만 읽을 수 있음

    public Gun gun;//사용할 총 오브젝트
    public LayerMask excludeTarget;//조준에서 제외할 레이어
    
    private PlayerInput playerInput;//입력을 전달할 컴포넌트
    private Animator playerAnimator;//애니메이터 컴포넌트
    private Camera playerCamera;//현재 메인카메라가 할당

    private float waitingTimeForRealeasingAim = 2.5f;//마지막 발사입력 시점에서 
    //발사간 입력이 얼마나 없으면 Idle상태로 되돌릴지 지정할 대기 시간
    private float lastFireInputTime;//마지막 발사입력시점
    
    private Vector3 aimPoint;//실제로 조준하고 있는 대상이 할당될 곳
    private bool linedUp => !(Mathf.Abs( playerCamera.transform.eulerAngles.y - 
    transform.eulerAngles.y) > 1f);
    //캐릭터가 바라보는 방향과 카메라가 바라보는 방향 사이에
    //각도가 너무 벌어졌는지 벌어지지않았는지 반환하는 Property

    private bool hasEnoughDistance => !Physics.Linecast(transform.position + Vector3.up *
     gun.fireTransform.position.y,gun.fireTransform.position, ~excludeTarget);
    //플레이어가 정면에 총을 발사할 수 있을 정도로 넉넉한 공간을 확보했는지 반환하는 Property
    
    void Awake()
    {
        //플레이어 오브젝트의 레이어가 포함되어 있지 않다면 
        if (excludeTarget != (excludeTarget | (1 << gameObject.layer))){
            //플레이어 게임 오브젝트의 레이어를 excludeTarget에 추가한다
            //플레어가 실수로 자신을 쏘는 현상을 방지하도록 예외처리한것
            excludeTarget |= 1 << gameObject.layer;
        }
    }

    private void Start()
    {
        //필요한 컴포넌트들을 할당
        playerCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        aimState = AimState.Idle;
        //초기화를 실행
        gun.gameObject.SetActive(true);
        gun.Setup(this);
    }

    private void OnDisable()
    {
        aimState = AimState.Idle;
        gun.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        //발사입력이 true라면
        if (playerInput.fire){
            lastFireInputTime = Time.time;//현재시간으로 매번 갱신
            Shoot();//발사
        }
        //재장전입력이 들어왔다면
        else if (playerInput.reload){
            Reload();//재장전
        }
    }

    private void Update()
    {
        UpdateAimTarget();//매 프레임 실행해줘야한다

        var angle = playerCamera.transform.eulerAngles.x;//x방향 회전이 고개를 숙이거나 높이는 방향
        if(angle > 270f) angle -= 360f;
        //-90도랑 +270도는 같은 각도가지게 되서 -값에 +360도가된 값이 
        //들어온 경우가 생기니까 270보다 크면 360도를 빼주는 예외처리를한다

        angle = angle/-180f + 0.5f;//-90도는 1이되고 +90도는 -1이된다
        playerAnimator.SetFloat("Angle",angle);
        
        if(!playerInput.fire && Time.time >= lastFireInputTime + waitingTimeForRealeasingAim)//발사입력 버튼을 누르지않고
        //마지막으로 발사 버튼을 누른 시점에서 2.5초 이상의 시간이 흘렀다면 
        {
            aimState = AimState.Idle;//Idle 상태로 변경
        }

        UpdateUI();//가장 마지막에 UI갱신
    }

    public void Shoot()
    {
        if(aimState == AimState.Idle){
            if(linedUp) aimState = AimState.HipFire;
        }
        else if(aimState == AimState.HipFire){//발사 준비가 됐거나 발사하고 있는 상태라면
            if(hasEnoughDistance){//충분한 공간도 확보하고있다면
                if(gun.Fire(aimPoint)){//발사를 시도한다
                    playerAnimator.SetTrigger("Shoot");//트리거를 할당시켜 애니메이션도 활성화
                }
            }
            else{//충분한 거리가 없다면
                aimState = AimState.Idle;
            }
        }
    }

    public void Reload()
    {
        if(gun.Reload()){//리로드를 시도해서 성공했다면 
            playerAnimator.SetTrigger("Reload");//애니메이션에 트리거전달
        }
    }

    private void UpdateAimTarget()
    {
        //aimPoint값을 플레이어가 조준하고 있는 곳으로 매번 갱신해야한다

        RaycastHit hit;//hit정보를 저장할 레이캐스트
        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f,0.5f,1f));
        //뷰포트상의 한 점을 찍어 해당 뷰포트 상의 한 점을 향하서 나아가는 ray를 생성해준다
        //화면상에 정중앙으로 뻗어가는 Raycast를 1차적으로 실행

        //충돌한 물체가 존재한다면
        if(Physics.Raycast(ray, out hit, gun.fireDistance,~excludeTarget)){
            aimPoint = hit.point;//해당 지점을 조준 대상으로 설정한다

            //총구에 위치에서 hit.point까지 선을 그었을때 끼어들어서 충돌한 다른 물체가 있으면
            if(Physics.Linecast(gun.fireTransform.position,hit.point, out hit, ~excludeTarget)){
                //aimPoint를 그 충돌한 물체의 지점으로 다시 지정해서 갱신한다
                aimPoint = hit.point;
            }
        }
        //처음부터 해당 포인트까지 감지된게 아무것도 없으면
        else{
            //플레이어 카메라 앞쪽 방향으로 최대 사정거리 까지 이동하는 위치가 될 것이다
            aimPoint = playerCamera.transform.position + playerCamera.transform.forward * gun.fireDistance;
        }

    }

    private void UpdateUI()
    {
        //남은 탼약 UI를 갱신하고 조준점을 갱신한다
        if (gun == null || UIManager.Instance == null) return;//총이 없거나 싱글톤이 없으면 아래 코드를 무시한다
        
        //그게 아니라면 싱글톤에 접근해서 남은 탄약을 갱신한다
        UIManager.Instance.UpdateAmmoText(gun.magAmmo, gun.ammoRemain);
        
        //정면에 충분한 공간이 있는지 검사하여 활성/비활성화 한다
        UIManager.Instance.SetActiveCrosshair(hasEnoughDistance);
        UIManager.Instance.UpdateCrossHairPosition(aimPoint);//조준점 위치 갱신
    }

    private void OnAnimatorIK(int layerIndex)
    {
        //IK를 통해 총에 손잡이가 왼손에 위치되게한다

        if(gun == null || gun.state == Gun.State.Reloading){//총이 없거나 재장전상태라면
            //IK를 갱신하지않고 바로 리턴한다
            return;
        }else{//그게 아니라면
            playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand,1.0f);
            //왼손에 포지션 정도를 100%인 1.0으로 지정한다
            playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand,1.0f);

            //총에 왼손이 항상 총의 왼손잡이로 위치하게 된다
            playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand,gun.leftHandMount.position);
            playerAnimator.SetIKRotation(AvatarIKGoal.LeftHand,gun.leftHandMount.rotation);
        }
    }
}