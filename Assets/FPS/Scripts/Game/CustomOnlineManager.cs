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
        [Tooltip("IP address clients use to connect to this host. Use 0.0.0.0 to listen on all interfaces.")]
        public string hostIp = "0.0.0.0";
        
        [Tooltip("For clients: IP address of the host to connect to. For local testing use 127.0.0.1")]
        public string remoteHostIp = "127.0.0.1";
        
        public int port = 7777;

        private bool isHost = false;
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
            // Try to become host first by binding to the port
            if (TryBecomeHost())
            {
                isHost = true;
                Debug.Log("Became host (successfully bound to port)");
                StartHostReceiveLoop();
            }
            else
            {
                // Failed to bind, so become a client
                isHost = false;
                Debug.Log("Failed to bind to port, becoming client");
                StartClient();
            }
        }

        private bool TryBecomeHost()
        {
            try
            {
                udp = new UdpClient(port);
                udp.Client.Blocking = false; // Non-blocking mode

                Debug.Log($"Host started on port {port}");

                running = true;
                return true;
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Debug.LogWarning($"Port {port} already in use (another host is running)");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to become host: {e}");
                return false;
            }
        }

        private void StartClient()
        {
            try
            {
                udp = new UdpClient();
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteHostIp), port);

                Debug.Log($"Client started. Target host {remoteHostIp}:{port}");

                running = true;
                
                // Send initial connect message
                SendToServer("CONNECT");
                
                StartClientReceiveLoop();
            }
            catch (Exception e)
            {
                Debug.LogError($"Client start failed: {e}");
            }
        }

        private void StartHostReceiveLoop()
        {
            receiveThread = new Thread(ReceiveLoopHost);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        private void StartClientReceiveLoop()
        {
            receiveThread = new Thread(ReceiveLoopClient);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        private void ReceiveLoopHost()
        {
            IPEndPoint any = new IPEndPoint(IPAddress.Any, 0);

            while (running)
            {
                try
                {
                    // Non-blocking receive
                    if (udp.Available > 0)
                    {
                        byte[] data = udp.Receive(ref any);
                        string text = Encoding.UTF8.GetString(data);

                        Debug.Log($"[HOST] From {any}: {text}");

                        // Always respond to keep connection alive
                        byte[] response = Encoding.UTF8.GetBytes("ACK:" + text);
                        udp.Send(response, response.Length, any);
                    }
                    else
                    {
                        Thread.Sleep(10); // Small delay to prevent busy waiting
                    }
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

        public bool IsHost => isHost;

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