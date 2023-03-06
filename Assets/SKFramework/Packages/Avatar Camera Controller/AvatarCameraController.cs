using UnityEngine;

namespace SK.Framework.Avatar
{
    /// <summary>
    /// Avatar相机控制
    /// </summary>
    [Package("Avatar Camera Controller", "1.1.0")]
    public class AvatarCameraController : MonoBehaviour
    {
        /// <summary>
        /// 控制模式
        /// </summary>
        public enum ControlMode
        {
            FirstPersonControl, //第一人称
            ThirdPersonControl, //第三人称 
        }
        //Avatar角色
        [SerializeField] private Transform avatar;
        //控制模式 默认第三人称
        [SerializeField] private ControlMode controlMode = ControlMode.ThirdPersonControl;
        //切换控制模式的快捷键
        [SerializeField] private KeyCode modeChangeKey = KeyCode.V;
        //视角前方是否与Avatar对齐
        [SerializeField] private bool forwardAlignWithAvatar;
        //水平方向灵敏度
        [SerializeField, Range(1f, 10f)] private float horizontalSensitivity = 6f;
        //垂直方向灵敏度
        [SerializeField, Range(1f, 10f)] private float verticalSensitivity = 3f;
        //用于记录水平方向输入值
        private float horizontal;
        //用于记录垂直方向输入值
        private float vertical;
        //旋转x值
        private float rotX;
        //旋转y值
        private float rotY;

        //旋转y值的最小值限制
        [SerializeField, Range(-80f, -10f)] private float rotYMinLimit = -40f;
        //旋转y值的最大值限制
        [SerializeField, Range(10f, 80f)] private float rotYMaxLimit = 70f;
        //插值到目标旋转值所需的时间
        [Range(0.01f, 1f), SerializeField] private float rotationLerpTime = .7f;
        //高度
        [SerializeField, Range(1f, 5f)] private float height = 2f;
        //默认距离
        [SerializeField] private float distance = 5f;
        //最小距离限制
        [SerializeField, Range(1f, 3f)] private float minDistanceLimit = 2f;
        //最大距离限制
        [SerializeField, Range(3f, 10f)] private float maxDistanceLimit = 5f;
        //第一人称模式所用的固定距离
        [SerializeField, Range(-1.5f, 0f)] private float fpmDistance = -.5f;
        //鼠标滚轮灵敏度
        [SerializeField, Range(1f, 5f)] private float scollSensitivity = 2f;
        //翻转滚动方向
        [SerializeField] private bool invertScrollDirection;
        //与Avatar在水平方向上的偏移值 仅在forwardAlignWithAvatar为true时开启使用
        [SerializeField, Range(-1f, 1f)] private float horizontalOffset;
        //目标距离
        private float targetDistance;
        //障碍物层级
        [SerializeField] private LayerMask obstacleLayer;
        //第一人称时是否控制Avatar角色的旋转
        [SerializeField] private bool ctrlAvatarRotWhenFPMode = true;

        private void Start()
        {
            targetDistance = Mathf.Clamp(distance, minDistanceLimit, maxDistanceLimit);
        }

        private void Update()
        {
            if (Input.GetKeyDown(modeChangeKey))
            {
                controlMode = controlMode == ControlMode.FirstPersonControl
                    ? ControlMode.ThirdPersonControl
                    : ControlMode.FirstPersonControl;
            }
        }

        private void LateUpdate()
        {
            //检测鼠标右键按下
            if (Input.GetMouseButton(1))
            {
                horizontal = forwardAlignWithAvatar ? 0f : Input.GetAxis("Mouse X") * Time.deltaTime * 100f * horizontalSensitivity;
                vertical = Input.GetAxis("Mouse Y") * Time.deltaTime * 100f * verticalSensitivity;

                rotX += horizontal;
                rotY -= vertical;
                //钳制旋转y值角度
                rotY = Mathf.Clamp(rotY, rotYMinLimit, rotYMaxLimit);
            }

            //鼠标滚轮滚动改变距离
            distance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * 100f * scollSensitivity * (invertScrollDirection ? -1f : 1f);
            //距离钳制
            distance = Mathf.Clamp(distance, minDistanceLimit, maxDistanceLimit);
            //插值方式计算距离
            targetDistance = controlMode == ControlMode.ThirdPersonControl
                ? Mathf.Lerp(targetDistance, distance, Time.deltaTime * scollSensitivity)
                : fpmDistance;

            //目标旋转值
            Quaternion targetRotation = Quaternion.Euler(rotY, rotX, 0f);
            //旋转值插值率
            float rotationLerpPct = 1f - Mathf.Exp(Mathf.Log(1f - .99f) / rotationLerpTime * Time.deltaTime);
            //插值方式计算旋转值
            targetRotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationLerpPct);

            //目标坐标值
            Vector3 targetPosition = targetRotation * Vector3.forward * -targetDistance + avatar.position + Vector3.up * height;
            //避障
            targetPosition = ObstacleAvoidance(targetPosition, avatar.position + Vector3.up * height, .1f, distance);

            transform.position = targetPosition + Vector3.left * horizontalOffset;
            transform.rotation = targetRotation;

            //第一人称控制模式 相机视角旋转的同时控制Avatar角色的旋转
            if (controlMode == ControlMode.FirstPersonControl && ctrlAvatarRotWhenFPMode)
            {
                Vector3 euler = Vector3.zero;
                //只取相机的RotY
                euler.y = targetRotation.eulerAngles.y;
                avatar.rotation = Quaternion.Euler(euler);
            }
        }

        //避障
        private Vector3 ObstacleAvoidance(Vector3 current, Vector3 target, float radius, float maxDistance)
        {
            Ray ray = new Ray(target, current - target);
            if (Physics.SphereCast(ray, radius, out RaycastHit hit, maxDistance, obstacleLayer))
            {
                return ray.GetPoint(hit.distance - radius * 2f);
            }
            return current;
        }
    }
}