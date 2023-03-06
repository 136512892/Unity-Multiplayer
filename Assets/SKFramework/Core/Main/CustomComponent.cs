using UnityEngine;
using Multiplayer;
using SK.Framework.Sockets;

namespace SK.Framework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("SKFramework/Custom")]
    public class CustomComponent : MonoBehaviour
    {
        public NetworkManager Network;

        public AvatarManager AvatarManager;
    }
}