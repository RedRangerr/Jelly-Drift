using RiptideNetworking;
using UnityEngine;
using RiptideNetworking.Utils;
namespace NetworkingScripts.Server
{
    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager _singleton;
        public static NetworkManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public RiptideNetworking.Server Server { get; private set; }
        public Client Client { get; private set; }

        
        [SerializeField] private ushort port;
        [SerializeField] private ushort maxClientCount;

        private void Awake()
        {
            Singleton = this;
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
        }

        public void StartServer()
        {
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

            Server = new RiptideNetworking.Server();
            Client = new Client();
            Server.Start(port, maxClientCount);
            Client.Connect("127.0.0.1:7777");
        }

        public void JoinServer(string ip)
        {
            Client.Connect(ip);
        }
        
        private void RegisterCallbacks()
        {
            
        }
        
        private void FixedUpdate()
        {
            if (Server != null && Server.IsRunning)
            {
                Server.Tick();
            }
            
            if (Client != null && Client.IsConnected)
                Client.Tick();
        }

        private void OnApplicationQuit()
        {
            Server.Stop();
        }
    }
}