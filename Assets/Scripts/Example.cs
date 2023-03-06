using ProtoBuf;
using UnityEngine;
using SK.Framework;

namespace Multiplayer
{
    public class Example : MonoBehaviour
    {
        //�û�ID
        private string userId;
        //����Avatarʵ��
        [SerializeField] private AvatarInstance self;

        private void OnGUI()
        {
            //δ���������������ʱ �༭�û�ID
            GUI.enabled = !Main.Custom.Network.IsConnected;
            GUILayout.BeginHorizontal();
            GUILayout.Label("UserID��");
            userId = GUILayout.TextField(userId);
            GUILayout.EndHorizontal();
            
            //�û�ID���ȱ�������6
            GUI.enabled = !Main.Custom.Network.IsConnected && userId.Length >= 6;
            if (GUILayout.Button("Connect", GUILayout.Width(200f), GUILayout.Height(50f)))
            {
                Main.Custom.AvatarManager = new AvatarManager();
                self.Init(userId, true);
                Main.Custom.Network.Connect("127.0.0.1", 8801);
            }

            GUI.enabled = Main.Custom.Network.IsConnected;
            if (GUILayout.Button("Disconnect", GUILayout.Width(200f), GUILayout.Height(50f)))
            {
                Main.Custom.AvatarManager = null;
                Main.Custom.Network.Close();
            }
        }

        private void Start()
        {
            userId = RandomUserId();
            Main.Events.Subscribe<IExtensible>(ProtoEventID.AvatarProperty, OnAvatarPropertyEvent);
        }

        private void OnDestroy()
        {
            Main.Events?.Unsubscribe<IExtensible>(ProtoEventID.AvatarProperty, OnAvatarPropertyEvent);
        }

        private void OnAvatarPropertyEvent(IExtensible proto)
        {
            var ap = proto as proto.AvatarProperty.AvatarProperty;
            Debug.Log(ap.userId);
            var instance = Main.Custom.AvatarManager.Get(ap.userId);
            if (instance == null)
            {
                instance = Instantiate(Resources.Load<GameObject>("Avatar_01")).AddComponent<AvatarInstance>();
                instance.Init(ap.userId, false);
            }
            instance.SetProperty(ap);
        }

        private string RandomUserId()
        {
            return string.Format("1990000{0}{1}{2}{3}", Random.Range(1,10), Random.Range(1,10), Random.Range(1, 10), Random.Range(1, 10));
        }
    }
}