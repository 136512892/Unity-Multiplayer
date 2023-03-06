/****************************************************
*	文件：AvatarManager.cs
*	作者：张寿昆(CoderZ)
*	邮箱：136512892@qq.com
*	日期：2022/12/12 15:06:08
*	功能：Avatar管理器
*****************************************************/

using UnityEngine;
using System.Collections.Generic;

namespace Multiplayer
{
    /// <summary>
    /// Avatar管理器
    /// </summary>
    public class AvatarManager
    {
        //字典用于存储Avatar实例
        private readonly Dictionary<string, AvatarInstance> avatarInstances;

        public AvatarManager()
        {
            avatarInstances = new Dictionary<string, AvatarInstance>();
        }

        /// <summary>
        /// 添加Avatar实例
        /// </summary>
        /// <param name="avatarInstance">Avatar实例</param>
        /// <returns>0：添加成功 -1：AvatarInstance不可为空 -2：已存在该用户ID的Avatar实例，添加失败</returns>
        public int Add(AvatarInstance avatarInstance)
        {
            //判断非空
            if (avatarInstance == null) return -1;
            //判断字典中不存在该用户ID的Avatar实例
            if (avatarInstances.ContainsKey(avatarInstance.UserId)) return -2;
            //添加到字典中
            avatarInstances.Add(avatarInstance.UserId, avatarInstance);
            Debug.Log(string.Format("添加Avatar实例：UserID-{0}, 当前数量：{1}", avatarInstance.UserId, avatarInstances.Count));
            return 0;
        }

        /// <summary>
        /// 删除Avatar实例
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>删除成功返回true 否则返回flase</returns>
        public bool Delete(string userId)
        {
            if (avatarInstances.TryGetValue(userId, out AvatarInstance instance))
            {
                //从字典中移除
                avatarInstances.Remove(userId);
                //物体销毁
                Object.Destroy(instance.gameObject);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 删除Avatar实例
        /// </summary>
        /// <param name="avatarInstance">Avatar实例</param>
        /// <returns>删除成功返回true 否则返回flase</returns>
        public bool Delete(AvatarInstance avatarInstance)
        {
            return Delete(avatarInstance.UserId);
        }

        /// <summary>
        /// 清除所有Avatar实例
        /// </summary>
        public void Clear()
        {
            foreach (var kv in avatarInstances)
            {
                Object.Destroy(kv.Value);
            }
            avatarInstances.Clear();
        }

        /// <summary>
        /// 获取Avatar实例
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns></returns>
        public AvatarInstance Get(string userId)
        {
            avatarInstances.TryGetValue(userId, out AvatarInstance avatarInstance);
            return avatarInstance;
        }
    }
}