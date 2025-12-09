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
        public string hostIp = "127.0.0.1";
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
            // Try to connect as a client first to see if a host already exists
            if (TryConnectAsClient())
            {
                isHost = false;
                Debug.Log("Connected as client to existing host");
                StartClientReceiveLoop();
            }
            else
            {
                // No host found, try to become the host
                if (TryBecomeHost())
                {
                    isHost = true;
                    Debug.Log("Became host (no existing host found)");
                    StartHostReceiveLoop();
                }
                else
                {
                    Debug.LogError("Failed to become host and could not connect to existing host");
                }
            }
        }

        private bool TryConnectAsClient()
        {
            try
            {
                udp = new UdpClient();
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(hostIp), port);

                Debug.Log($"Attempting to connect as client to {hostIp}:{port}");

                running = true;
                
                // Send a test message to see if host responds
                byte[] testData = Encoding.UTF8.GetBytes("PING");
                udp.Send(testData, testData.Length, remoteEndPoint);

                // Try to receive with a timeout to see if host responds
                udp.Client.ReceiveTimeout = 2000; // 2 second timeout
                IPEndPoint any = new IPEndPoint(IPAddress.Any, 0);
                byte[] response = udp.Receive(ref any);
                
                udp.Client.ReceiveTimeout = 0; // Reset to blocking
                return true;
            }
            catch
            {
                // No host available
                running = false;
                try { udp?.Close(); } catch { }
                return false;
            }
        }

        private bool TryBecomeHost()
        {
            try
            {
                udp = new UdpClient(port);
                udp.Client.Blocking = true;

                Debug.Log($"Host started on port {port}");

                running = true;
                return true;
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Debug.LogWarning($"Port {port} already in use (host is running elsewhere)");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to become host: {e}");
                return false;
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