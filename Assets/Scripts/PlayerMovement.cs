using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController characterController;
    private PlayerInput playerInput;
    private PlayerShooter playerShooter;
    private Animator animator;
    
    private Camera followCam;
    
    public float speed = 6f;
    public float jumpVelocity = 20f;
    [Range(0.01f, 1f)] public float airControlPercent;

    public float speedSmoothTime = 0.1f;
    public float turnSmoothTime = 0.1f;
    
    private float speedSmoothVelocity;
    private float turnSmoothVelocity;
    
    private float currentVelocityY;
    
    public float currentSpeed =>
        new Vector2(characterController.velocity.x, characterController.velocity.z).magnitude;
    
    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        playerShooter = GetComponent<PlayerShooter>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        followCam = Camera.main;
    }

    //물리갱신주기에 맞춰서 실행되는 Update메서드, 이동과 회전에 관련된 기능을 넣을때 더 정확하게 작동함
    private void FixedUpdate()
    {
        //currentSpeed가 0.2보다 크다는건 조금이라도 움직이는 뜻
        //조금이라도 움직이거나 무기를 사용하면 플레이어가 카메라 방향으로 회전시킨다 
        if (currentSpeed > 0.2f || playerInput.fire || 
        playerShooter.aimState == PlayerShooter.AimState.HipFire) Rotate();
        //발사버튼을 누르지않아도 aim상태가 HipFire인 동안(1~2초)은 카메라 방향으로 정렬하게한다

        Move(playerInput.moveInput);
        
        if (playerInput.jump) Jump();
    }

    //매프레임 실행되기에 물리적으로 정확한 수치를 요구하는 코드를 넣으면 오차가 난다
    //겉모습으로 보이는 애니메이션은 사소한 오차는 상관없기에 애니메이션 관련 코드만 넣음
    private void Update()
    {
        UpdateAnimation(playerInput.moveInput);
    }

    //Input값을 받아 실제로 움직이게한다
    public void Move(Vector2 moveInput)
    { 
        var targetSpeed = speed * moveInput.magnitude;
        //앞으로 가면 y값이 커지고 x는 작아진다 -> forward를 100%를 사용하게됨
        //반대로 오른쪽또는 왼쪽을 사용하면 moveInput.x는 1이 나오게되고 right를 100%사용
        var moveDirection = Vector3.Normalize(transform.forward * moveInput.y + 
        transform.right * moveInput.x);
        //moveDirection은 방향벡터를 사용하고 현재 계산된 moveDirection은 길이가 1이 아닌 경우가 발생할 수 있기에
        //마지막으로 Normalize를 실행 해야한다

        //0.1초에 지연시간으로 현재속도에서 타켓속도만큼 부드럽게 이어지게함
        //만약 공중에 떠있다면 지연시간을 더 길게한다 
        var smoothTime = characterController.isGrounded ? speedSmoothTime : speedSmoothTime / airControlPercent;

        //curretSpeed에서 targetSpeed로 부드럽게 이어주는 값을 직전까지 값의 변화량(ref)을 기반해서 
        //SmoothTime만큼 지연시간을 적용해서 적절하게 부드럽게이어진 값이 targetSpeed로 할당된다
        targetSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, smoothTime);

        //중력에 의해서 바닥에 떨어지는 속도(Character Controller는 자동으로 떨어지지않는다)
        currentVelocityY += Time.deltaTime * Physics.gravity.y;//중력가속도

        //최종속도
        //방향 * 속도 + Up * 현재y방향속도
        //Vector3.up은 (0,1,0)을 가지는 위쪽을 향하는 방향벡터
        var velocity = moveDirection * targetSpeed + Vector3.up * currentVelocityY;

        //world space기준으로 현재위치에서 얼만큼 더 이동할지 정한다
        characterController.Move(velocity * Time.deltaTime);
        //fixedUpdate내에서 Time.fixedDeltaTime으로 동작하기에 그냥 DeltaTime도 상관없음

        //바닥에 닿아있다면 y값을 0으로 리셋
        if(characterController.isGrounded) currentVelocityY = 0f;
    }

    public void Rotate()
    {
        var targetRotation = followCam.transform.eulerAngles.y;//카메라의 y방향

        //smoothdamp처럼 동작하지만 각도의 범위를 고려해서 damping이 이루어진다
        targetRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y,targetRotation,ref turnSmoothVelocity, turnSmoothTime);

        transform.eulerAngles = Vector3.up * targetRotation;
        
    }

    public void Jump()
    {
        if(!characterController.isGrounded) return;
        //y방향의 속도를 새로 할당한다
        currentVelocityY = jumpVelocity;
    }

    private void UpdateAnimation(Vector2 moveInput)
    {   
        //현재속도가 최고 속도 대비 몇 %인지 할당
        var animationSpeedPercent = currentSpeed / speed;

        //입력으로 moveInput값을 받아서 애니메이터의 VerticalMove와 Horizontal Move 파라미터를 전달한다
        animator.SetFloat("Vertical Move",moveInput.y * animationSpeedPercent, 0.05f, Time.deltaTime);
        animator.SetFloat("Horizontal Move",moveInput.x * animationSpeedPercent,0.05f,Time.deltaTime);
        //수직 수평 방향 움직임이 moveInput값으로 즉시 변화되는게 아니라 이전 값에서 지금 설정한 값으로 연속적으로 부드럽게 변화하게된다
    }
}