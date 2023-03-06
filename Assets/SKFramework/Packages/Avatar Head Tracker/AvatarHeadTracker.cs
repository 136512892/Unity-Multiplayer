using UnityEngine;

namespace SK.Framework.Avatar
{
    [Package("Avatar Head Tracker", "1.0.0")]
    public class AvatarHeadTracker : MonoBehaviour
    {
        [Tooltip("动画组件"), SerializeField] private Animator animator;
        [Tooltip("水平方向上的角度限制"), SerializeField] private Vector2 horizontalAngleLimit = new Vector2(-70f, 70f);
        [Tooltip("垂直方向上的角度限制"), SerializeField] private Vector2 verticalAngleLimit = new Vector2(-60f, 60f);
        [Tooltip("超出限制范围时自动回正"), SerializeField] private bool autoTurnback = true;
        [Tooltip("插值速度"), SerializeField] private float lerpSpeed = 5f;

        private Camera mainCamera; //主相机
        private Transform head; //头部
        private float headHeight; //头部的高度
        private float angleX;
        private float angleY;

        private void Start()
        {
            mainCamera = Camera.main ?? FindObjectOfType<Camera>();
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            headHeight = Vector3.Distance(transform.position, head.position);
        }

        /// <summary>
        /// 看向某点
        /// </summary>
        /// <param name="position"></param>
        public void LookAtPosition(Vector3 position)
        {
            //头部位置
            Vector3 headPosition = transform.position + transform.up * headHeight;
            //朝向
            Quaternion lookRotation = Quaternion.LookRotation(position - headPosition);
            Vector3 eulerAngles = lookRotation.eulerAngles - transform.rotation.eulerAngles;
            float x = NormalizeAngle(eulerAngles.x);
            float y = NormalizeAngle(eulerAngles.y);
            angleX = Mathf.Clamp(Mathf.Lerp(angleX, x, Time.deltaTime * lerpSpeed), verticalAngleLimit.x, verticalAngleLimit.y);
            angleY = Mathf.Clamp(Mathf.Lerp(angleY, y, Time.deltaTime * lerpSpeed), horizontalAngleLimit.x, horizontalAngleLimit.y);
            Quaternion rotY = Quaternion.AngleAxis(angleY, head.InverseTransformDirection(transform.up));
            head.rotation *= rotY;
            Quaternion rotX = Quaternion.AngleAxis(angleX, head.InverseTransformDirection(transform.TransformDirection(Vector3.right)));
            head.rotation *= rotX;
        }

        //角度标准化
        private float NormalizeAngle(float angle)
        {
            if (angle > 180) angle -= 360f;
            else if (angle < -180) angle += 360f;
            return angle;
        }

        private void LateUpdate()
        {
            LookAtPosition(GetLookAtPosition());
        }

        //获取看向的位置
        private Vector3 GetLookAtPosition()
        {
            AnimatorStateInfo animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (animatorStateInfo.IsTag("IgnoreHeadTrack"))
                return transform.position + transform.up * headHeight + transform.forward;

            Vector3 position = mainCamera.transform.position + mainCamera.transform.forward * 100f;
            if (!autoTurnback) return position;
            Vector3 direction = position - (transform.position + transform.up * headHeight);
            Quaternion lookRotation = Quaternion.LookRotation(direction, transform.up);
            Vector3 angle = lookRotation.eulerAngles - transform.eulerAngles;
            float x = NormalizeAngle(angle.x);
            float y = NormalizeAngle(angle.y);
            bool isInRange = x >= verticalAngleLimit.x && x <= verticalAngleLimit.y
                && y >= horizontalAngleLimit.x && y <= horizontalAngleLimit.y;
            return isInRange ? position : (transform.position + transform.up * headHeight + transform.forward);
        }
    }
}