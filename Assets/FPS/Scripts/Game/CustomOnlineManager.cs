using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Unity.FPS.Game
{
    public class CustomOnlineManager : MonoBehaviour
    {
        public bool isHost = false;        // Set this true for the first player (the host)
        public string hostIp = "127.0.0.1";
        public int port = 7777;

        private UdpClient udp;
        private IPEndPoint remoteEndPoint;
        private Thread receiveThread;
        private bool running;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (isHost)
            {
                StartHost();
            }
            else
            {
                StartClient();
            }
        }

        private void StartHost()
        {
            try
            {
                udp = new UdpClient(port);
                udp.Client.Blocking = true;

                Debug.Log($"Host started on port {port}");

                running = true;
                receiveThread = new Thread(ReceiveLoopHost);
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"Host start failed: {e}");
            }
        }

        private void StartClient()
        {
            try
            {
                udp = new UdpClient();
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(hostIp), port);

                Debug.Log($"Client started. Target host {hostIp}:{port}");

                running = true;
                receiveThread = new Thread(ReceiveLoopClient);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                // Simple connect message for now
                SendToServer("CONNECT");
            }
            catch (Exception e)
            {
                Debug.LogError($"Client start failed: {e}");
            }
        }

        private void ReceiveLoopHost()
        {
            IPEndPoint any = new IPEndPoint(IPAddress.Any, 0);

            while (running)
            {
                try
                {
                    byte[] data = udp.Receive(ref any);
                    string text = Encoding.UTF8.GetString(data);

                    // For now just log and echo back
                    Debug.Log($"[HOST] From {any}: {text}");

                    byte[] response = Encoding.UTF8.GetBytes("ACK:" + text);
                    udp.Send(response, response.Length, any);
                }
                catch (SocketException)
                {
                    // Likely closing
                }
                catch (Exception e)
                {
                    Debug.LogError($"[HOST] Receive error: {e}");
                }
            }
        }

        private void ReceiveLoopClient()
        {
            IPEndPoint any = new IPEndPoint(IPAddress.Any, 0);

            while (running)
            {
                try
                {
                    byte[] data = udp.Receive(ref any);
                    string text = Encoding.UTF8.GetString(data);

                    Debug.Log($"[CLIENT] From host: {text}");
                }
                catch (SocketException)
                {
                    // Likely closing
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CLIENT] Receive error: {e}");
                }
            }
        }

        public void SendToServer(string message)
        {
            if (isHost)
            {
                Debug.LogWarning("Host cannot call SendToServer. Use broadcast logic instead.");
                return;
            }

            if (udp == null || remoteEndPoint == null) return;

            byte[] data = Encoding.UTF8.GetBytes(message);
            try
            {
                udp.Send(data, data.Length, remoteEndPoint);
            }
            catch (Exception e)
            {
                Debug.LogError($"SendToServer error: {e}");
            }
        }

        public void SendFromHost(IPEndPoint target, string message)
        {
            if (!isHost)
            {
                Debug.LogWarning("Client cannot call SendFromHost.");
                return;
            }

            if (udp == null) return;

            byte[] data = Encoding.UTF8.GetBytes(message);
            try
            {
                udp.Send(data, data.Length, target);
            }
            catch (Exception e)
            {
                Debug.LogError($"SendFromHost error: {e}");
            }
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void Shutdown()
        {
            running = false;

            try
            {
                udp?.Close();
            }
            catch { }

            try
            {
                if (receiveThread != null && receiveThread.IsAlive)
                {
                    receiveThread.Abort();
                }
            }
            catch { }
        }
    }
}