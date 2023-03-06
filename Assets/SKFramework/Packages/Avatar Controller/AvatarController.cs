using UnityEngine;

namespace SK.Framework.Avatar
{
    /// <summary>
    /// Avatar角色控制器
    /// </summary>
    [Package("Avatar Controller", "2.0.0")]
    [RequireComponent(typeof(CharacterController))]
    public class AvatarController : MonoBehaviour
    {
        //主相机
        private Transform mainCamera;
        //动画组件
        private Animator animator;
        //角色控制器
        private CharacterController cc;

        [Tooltip("走速度，注意与Animator BlendTree中的Threshold阈值保持一致")]
        [SerializeField] private float walkSpeed = 2f;
        [Tooltip("跑速度，注意与Animator BlendTree中的Threshold阈值保持一致")]
        [SerializeField] private float sprintSpeed = 5.35f;
        [Tooltip("加速快捷键")]
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        //移动速度
        private float speed;
        //Animator BlendTree中的速度
        private float animBlendSpeed;
        //目标旋转值
        private float targetRotation;
        //旋转速度
        private float rotationSpeed;
        [Tooltip("到达目标旋转值的近似时间")]
        [Range(0.0f, 0.3f), SerializeField]
        private float rotationSmoothTime = .12f;
        [Tooltip("插值速度")] 
        [SerializeField] private float lerpSpeed = 10f;

        [Tooltip("跳跃按键")]
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [Tooltip("跳跃的高度")] 
        [SerializeField] private float jumpHeight = 1.2f;
        [Tooltip("重力大小")] 
        [SerializeField] private float gravity = -15f;
        [Tooltip("跳跃冷却时长（落地后需经过该时长才可再次跳跃）")]
        [SerializeField] public float jumpCD = .3f;
        [Tooltip("经过该时长后进入下落状态")]
        [SerializeField] public float fallDelay = .2f;
        //跳跃计时
        private float jumpTimeCounter = .5f;
        //下落计时
        private float fallTimeCounter = .15f;

        //是否处于地面
        private bool isGrounded = true;
        [Tooltip("用于粗糙地面")]
        [SerializeField] private float groundedOffset = -0.14f;
        [Tooltip("地面检测的半径大小，需要与角色控制器的半径匹配")]
        [SerializeField] private float groundedRadius = 0.28f;
        [Tooltip("地面检测的层级")]
        [SerializeField] private LayerMask groundLayers = 1;

        //垂直方向上的速度
        private float verticalVelocity;
        private readonly float maxVelocity = 53.0f;
        //斜坡上的速度
        private Vector3 slopeVelocity;
        //缓存斜坡上的速度
        private Vector3 lastSlopeVelocity;
        //当前速度
        private Vector3 currentVelocity;
        //地面法线向量
        private Vector3 groundNormal;

        //Animator参数ID
        private class AnimatorParameters
        {
            public static readonly int Speed = Animator.StringToHash("Speed");
            public static readonly int Land = Animator.StringToHash("Land");
            public static readonly int Jump = Animator.StringToHash("Jump");
            public static readonly int Fall = Animator.StringToHash("Fall");
        }

        private void Start()
        {
            mainCamera = (Camera.main ?? FindObjectOfType<Camera>()).transform;
            cc = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();
        }
        private void Update()
        {
            JumpAndGravity();
            GroundedCheck();
            Movement();
        }

        //移动
        private void Movement()
        {
            //获取输入值
            Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            //没有任何输入时速度为0 加速键按下时速度为跑的速度 否则为走的速度
            float targetSpeed = input == Vector2.zero ? 0f : (Input.GetKey(sprintKey) ? sprintSpeed : walkSpeed);
            //当前水平方向上的速度
            float currentHorizontalSpeed = new Vector3(cc.velocity.x - lastSlopeVelocity.x, 0f, cc.velocity.z - lastSlopeVelocity.z).magnitude;
            //加速或降速 差值0.1
            if (currentHorizontalSpeed < targetSpeed - .1f || currentHorizontalSpeed > targetSpeed + .1f)
            {
                //插值运算
                speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * input.magnitude, Time.deltaTime * lerpSpeed);
                //保留三位小数
                speed = Mathf.Round(speed * 1000f) * .001f;
            }
            else speed = targetSpeed;

            //Animator BlendTree中的速度 
            animBlendSpeed = Mathf.Lerp(animBlendSpeed, targetSpeed, Time.deltaTime * lerpSpeed);
            animBlendSpeed = animBlendSpeed >= .1f ? animBlendSpeed : 0f;

            //移动的方向
            Vector3 moveDirection = new Vector3(input.x, 0f, input.y).normalized;

            //输入不为空时 处理方向
            if (input != Vector2.zero)
            {
                targetRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg + mainCamera.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0f, Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationSpeed, rotationSmoothTime), 0f);
            }

            moveDirection = Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;
            //移动
            //cc.Move(moveDirection.normalized * (speed * Time.deltaTime) + new Vector3(0.0f, verticalVelocity, 0.0f) * Time.deltaTime);
            Vector3 slopeVelocityToUse = Vector3.zero;
            if (slopeVelocity.magnitude > .001f && speed > .001f)
            {
                Vector3 verticalVel = Vector3.Project(slopeVelocity, transform.up);
                Vector3 slopeVelPlanar = slopeVelocity - verticalVel;
                Vector3 subtract = Vector3.Project(moveDirection.normalized * speed, slopeVelPlanar.normalized);
                Vector3 nextSlopeVelPlanar = slopeVelPlanar - subtract;
                float dot = Vector3.Dot(nextSlopeVelPlanar.normalized, slopeVelPlanar.normalized);
                if (dot > 0) slopeVelocityToUse = dot > 0f ? (nextSlopeVelPlanar + verticalVel) : Vector3.zero;
            }
            else slopeVelocityToUse = slopeVelocity;
            cc.Move(speed * Time.deltaTime * moveDirection.normalized + new Vector3(0.0f, verticalVelocity, 0.0f) * Time.deltaTime + slopeVelocityToUse * Time.deltaTime);
            lastSlopeVelocity = slopeVelocityToUse;

            //移动动画
            animator?.SetFloat(AnimatorParameters.Speed, animBlendSpeed);
        }
        //地面检测
        private void GroundedCheck()
        {
            Vector3 characterVelocity = cc.velocity;
            characterVelocity.y = 0;
            if (characterVelocity.magnitude > 0.01f) 
                currentVelocity = characterVelocity.normalized;

            groundNormal = Vector3.up;

            if (verticalVelocity > 0)
            {
                isGrounded = false;
                slopeVelocity = Vector3.Lerp(slopeVelocity, Vector3.zero, Time.deltaTime * 20.0f);
                animator?.SetBool(AnimatorParameters.Land, isGrounded);
                return;
            }

            groundedRadius = cc.radius * Mathf.Max(transform.localScale.x, transform.localScale.z);

            Vector3 targetSlopeVelocity = Vector3.zero;

            float positiveGroundedOffset = Mathf.Abs(groundedOffset);
            Vector3 origin = transform.position + transform.up * (groundedRadius + positiveGroundedOffset);
            float twoTimesPositiveGroundedOffset = positiveGroundedOffset * 2;
            if (Physics.SphereCast(origin, groundedRadius, -transform.up, out RaycastHit hit, twoTimesPositiveGroundedOffset, groundLayers, QueryTriggerInteraction.Ignore))
            {
                Vector3 scaledCenterOffsetVec = cc.center;
                scaledCenterOffsetVec.x *= transform.localScale.x;
                scaledCenterOffsetVec.y *= transform.localScale.y;
                scaledCenterOffsetVec.z *= transform.localScale.z;
                float scaledHeight = cc.height * transform.localScale.y;
                Vector3 p1 = transform.position + scaledCenterOffsetVec + transform.up * (scaledHeight * 0.5f - groundedRadius);
                Vector3 p2 = transform.position + scaledCenterOffsetVec - transform.up * (scaledHeight * 0.5f - groundedRadius);

                float penetrationCastRadius = groundedRadius * 0.99f;
                float additionalRayDist = groundedRadius - penetrationCastRadius;

                Vector3 rayDirDistVec = transform.position - hit.point;
                Vector3 rayDirVec = rayDirDistVec.normalized;
                Vector3 projRayDistVec = Vector3.ProjectOnPlane(rayDirDistVec, transform.up);
                float lengthX = Mathf.Max(projRayDistVec.magnitude, 0.01f);
                float lengthX2 = groundedRadius - lengthX;
                Vector3 hypothenuse2 = (lengthX2 / lengthX) * rayDirDistVec + rayDirVec * (additionalRayDist + twoTimesPositiveGroundedOffset);

                if (Physics.CapsuleCast(p1, p2, penetrationCastRadius, rayDirVec, hypothenuse2.magnitude, groundLayers, QueryTriggerInteraction.Ignore))
                {
                    isGrounded = true;
                }
                else
                {
                    groundNormal = hit.normal;
                    float angle = Vector3.Angle(hit.normal, transform.up);

                    if (angle > cc.slopeLimit)
                    {
                        Vector3 raycastOrigin = transform.position + transform.up * (positiveGroundedOffset + cc.stepOffset) + currentVelocity * groundedRadius;
                        if (Physics.Raycast(raycastOrigin, -transform.up, out RaycastHit hit2, twoTimesPositiveGroundedOffset + cc.stepOffset * 2.0f, groundLayers, QueryTriggerInteraction.Ignore))
                        {
                            if (Vector3.Angle(hit2.normal, transform.up) > cc.slopeLimit)
                            {
                                isGrounded = false;
                                Vector3 rightVec = Vector3.Cross(hit.normal, transform.up).normalized;
                                targetSlopeVelocity = Vector3.Cross(hit.normal, rightVec).normalized;
                            }
                            else
                            {
                                isGrounded = true;
                            }
                        }
                        else
                        {
                            isGrounded = false;
                            Vector3 rightVec = Vector3.Cross(hit.normal, transform.up).normalized;
                            targetSlopeVelocity = Vector3.Cross(hit.normal, rightVec).normalized;
                        }
                    }
                    else
                    {
                        isGrounded = true;
                    }
                }
            }
            else
            {
                isGrounded = false;
            }

            if (targetSlopeVelocity.magnitude > 0.001f)
            {
                slopeVelocity += Mathf.Abs(gravity) * Time.deltaTime * targetSlopeVelocity;
            }
            else
            {
                slopeVelocity = Vector3.Lerp(slopeVelocity, Vector3.zero, Time.deltaTime * 20.0f);
            }
            animator?.SetBool(AnimatorParameters.Land, isGrounded);
        }
        //跳跃和重力
        private void JumpAndGravity()
        {
            if (isGrounded)
            {
                //重置下落计时
                fallTimeCounter = fallDelay;

                //停止跳跃和下落动画
                if (animator)
                {
                    animator.SetBool(AnimatorParameters.Jump, false);
                    animator.SetBool(AnimatorParameters.Fall, false);
                }

                if (verticalVelocity < 0.0f)
                {
                    //verticalVelocity = -2f;
                    verticalVelocity = Mathf.Lerp(gravity, -2.0f, Vector3.Dot(Vector3.up, groundNormal));
                }

                //跳跃
                if (Input.GetKeyDown(jumpKey) && jumpTimeCounter <= 0.0f)
                {
                    //重置跳跃计时
                    jumpTimeCounter = jumpCD;
                    //到达指定高度所需的速度 = (高度 * -2 * 重力)的平方根
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    //跳跃动画
                    animator?.SetBool(AnimatorParameters.Jump, true);
                }

                if (jumpTimeCounter >= 0.0f)
                    jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                //下落动画
                if (fallTimeCounter >= 0.0f)
                    fallTimeCounter -= Time.deltaTime;
                else animator?.SetBool(AnimatorParameters.Fall, true);
            }

            //重力
            if (verticalVelocity < maxVelocity)
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            if (verticalVelocity > 0)
            {
                Vector3 scaledCenterOffsetVec = cc.center;
                scaledCenterOffsetVec.x *= transform.localScale.x;
                scaledCenterOffsetVec.y *= transform.localScale.y;
                scaledCenterOffsetVec.z *= transform.localScale.z;
                float scaledHeight = cc.height * transform.localScale.y;
                float scaledRadius = cc.radius * Mathf.Max(transform.localScale.x, transform.localScale.z);
                Vector3 origin = transform.position + scaledCenterOffsetVec + transform.up * (scaledHeight * 0.5f - scaledRadius);

                float positiveGroundedOffset = Mathf.Abs(groundedOffset);
                float penetrationCastRadius = scaledRadius * 0.99f;
                float rayDist = scaledRadius - penetrationCastRadius + positiveGroundedOffset;

                if (Physics.SphereCast(origin, penetrationCastRadius, transform.up, out RaycastHit hit, rayDist, groundLayers, QueryTriggerInteraction.Ignore))
                {
                    jumpTimeCounter = 0.0f;
                    verticalVelocity = Mathf.Lerp(verticalVelocity, 0.0f, Time.deltaTime * 10.0f);
                }
            }
        }
    }
}