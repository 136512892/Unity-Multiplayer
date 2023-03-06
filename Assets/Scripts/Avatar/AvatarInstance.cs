/****************************************************
*	文件：AvatarInstance.cs
*	作者：张寿昆(CoderZ)
*	邮箱：136512892@qq.com
*	日期：2022/12/12 14:54:05
*	功能：Avatar实例
*****************************************************/

using UnityEngine;
using SK.Framework;

using proto.AvatarProperty;

namespace Multiplayer
{
    /// <summary>
    /// Avatar实例
    /// </summary>
    public class AvatarInstance : MonoBehaviour
    {
        //动画组件
        private Animator animator;
        //TEMP 用户ID
        [SerializeField] private string userId;
        //是否是本地Avatar实例
        [SerializeField] private bool isSelf = false;
        //同步Avatar属性的时间间隔
        [SerializeField, Range(.01f, 1f)] private float syncInterval = .03f; //0.03秒：一秒大约发送33次同步数据
        //插值速度
        [SerializeField] private float lerpSpeed = 100f;
        //数据同步计时器
        private float syncTimer;
        //Avatar属性
        private readonly AvatarProperty avatarProperty = new AvatarProperty();

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get { return userId; } }
        /// <summary>
        /// 是否是本地Avatar实例
        /// </summary>
        public bool IsSelf { get { return isSelf; } }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="isSelf">是否是本地Avatar实例</param>
        public void Init(string userId, bool isSelf)
        {
            this.userId = userId;
            this.isSelf = isSelf;

            //获取动画组件
            animator = GetComponentInChildren<Animator>();

            //将Avatar实例添加到Avatar管理器中
            Main.Custom.AvatarManager.Add(this);
        }

        private void Update()
        {
            //未初始化
            if (animator == null) return;
            //如果是本地Avatar实例 需要定时发送同步数据
            if (IsSelf)
            {
                //计时
                syncTimer += Time.deltaTime;
                //是否到达数据同步时间点
                if (syncTimer >= syncInterval)
                {
                    //重置计时器
                    syncTimer = 0f;

                    //用户ID
                    avatarProperty.userId = userId;
                    //Avatar坐标与旋转数据
                    avatarProperty.posX = transform.position.x;
                    avatarProperty.posY = transform.position.y;
                    avatarProperty.posZ = transform.position.z;
                    avatarProperty.rotX = transform.eulerAngles.x;
                    avatarProperty.rotY = transform.eulerAngles.y;
                    avatarProperty.rotZ = transform.eulerAngles.z;
                    //Animator Movement混合树速度
                    avatarProperty.speed = animator.GetFloat(AnimParams.Speed);
                    //发送数据
                    Main.Custom.Network.Send(avatarProperty);
                }
            }
        }

        /// <summary>
        /// 设置Avatar属性
        /// </summary>
        /// <param name="avatarProperty">Avatar属性</param>
        public void SetProperty(AvatarProperty avatarProperty)
        {
            //设置坐标
            Vector3 pos = transform.position;
            pos.x = avatarProperty.posX;
            pos.y = avatarProperty.posY;
            pos.z = avatarProperty.posZ;
            //transform.position = pos;
            //插值方式
            transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * lerpSpeed);
            //设置旋转
            Vector3 rot = transform.eulerAngles;
            rot.x = avatarProperty.rotX;
            rot.y = avatarProperty.rotY;
            rot.z = avatarProperty.rotZ;
            transform.eulerAngles = rot;
            //设置Animator Movement混合树速度  小于0.01时均取值0
            animator?.SetFloat(AnimParams.Speed, avatarProperty.speed > .01f ? avatarProperty.speed : 0f);
        }
    }
} 