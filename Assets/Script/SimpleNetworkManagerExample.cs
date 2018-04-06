// See LICENSE file in the root directory
//

using LGK.Networking.LLAPI.Client;
using LGK.Networking.LLAPI.Server;
using UnityEngine;
using UnityEngine.Assertions;

namespace LGK.Networking.Example
{
    public class SimpleNetworkManagerExample : MonoBehaviour
    {
        ServerNetworkManager m_ServerNetworkManager;
        ClientNetworkManager m_ClientNetworkManager;

        void Start()
        {
            m_ServerNetworkManager = new ServerNetworkManager(new ServerConfig());

            m_ServerNetworkManager.ConnectedEvent += Server_OnClientConnected;
            m_ServerNetworkManager.DisconnectedEvent += Server_OnClientDisconnected;
            m_ServerNetworkManager.RegisterHandler(TestMessage.MsgCode, ServerManager_TestMessage);

            m_ClientNetworkManager = new ClientNetworkManager(new ClientConfig());
            m_ClientNetworkManager.ConnectingEvent += ClientManager_Connecting;
            m_ClientNetworkManager.ConnectingFailedEvent += ClientManager_ConnectingFailed;
            m_ClientNetworkManager.ConnectedEvent += ClientManager_Connected;
            m_ClientNetworkManager.DisconnectedEvent += ClientManager_Diconnected;
            m_ClientNetworkManager.RegisterHandler(TestMessage.MsgCode, ClientManager_TestMessage);

            Simulation();
        }

        void Simulation()
        {
            Debug.Log("Starting server, status:" + m_ServerNetworkManager.Listen(55555));
            Debug.Log("Starting client manager, status: " + m_ClientNetworkManager.Connect("localhost", 55555));

            Debug.Log("Press `S` to send from server to client!");
            Debug.Log("Press `C` to send from server to client!");
        }

        void Update()
        {
            if (m_ServerNetworkManager.IsActive)
            {
                if (Input.GetKeyDown(KeyCode.S))
                {
                    SendHeloToClient();
                }

                m_ServerNetworkManager.ProcessMessage();
            }

            if (m_ClientNetworkManager.IsActive)
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    SendHelloToServer();
                }

                m_ClientNetworkManager.ProcessMessage();
            }
        }

        void OnDestroy()
        {
            m_ServerNetworkManager.Shutdown();
            m_ClientNetworkManager.Disconnect();
        }

        #region Server Peer

        int m_ClientConnectionId = 0;
        void SendHeloToClient()
        {
            if (m_ClientConnectionId != 0)
            {
                m_ServerNetworkManager.SendReliable(m_ClientConnectionId, TestMessage.MsgCode, new TestMessage("Hello from server"));
            }
        }

        void Server_OnClientConnected(IConnection conn)
        {
            Assert.IsTrue(conn.IsConnected == true);

            Debug.Log("Server_OnClientConnected : " + conn.ConnectionId);

            m_ClientConnectionId = conn.ConnectionId;
        }

        void Server_OnClientDisconnected(IConnection conn)
        {
            Assert.IsTrue(conn.IsConnected == false);

            Debug.Log("Server_OnClientDisconnected : " + conn.ConnectionId);
        }

        void ServerManager_TestMessage(IConnection conn, NetworkReader reader)
        {
            var message = new TestMessage();
            message.Deserialize(conn, reader);

            UnityEngine.Debug.Log(message.message);
        }

        #endregion

        #region Client Manager

        void SendHelloToServer()
        {
            m_ClientNetworkManager.SendReliable(TestMessage.MsgCode, new TestMessage("Hello from client"));
        }

        void ClientManager_Connecting()
        {
            Assert.IsTrue(m_ClientNetworkManager.Connection.IsConnected == false);

            Debug.Log("ClientManager_Connecting");
        }

        void ClientManager_ConnectingFailed(NetworkError error)
        {
            Assert.IsTrue(m_ClientNetworkManager.Connection.IsConnected == false);
            Assert.IsTrue(m_ClientNetworkManager.Connection.LastError == error);

            Debug.Log("ClientManager_ConnectingFailed " + error);
        }

        void ClientManager_Connected()
        {
            Assert.IsTrue(m_ClientNetworkManager.Connection.IsConnected == true);
            Assert.IsTrue(m_ClientNetworkManager.Connection.LastError == NetworkError.None);

            Debug.Log("ClientManager_ConnectingConnected");
        }

        void ClientManager_Diconnected()
        {
            Assert.IsTrue(m_ClientNetworkManager.Connection.IsConnected == false);
            Assert.IsTrue(m_ClientNetworkManager.Connection.LastError == NetworkError.None);

            Debug.Log("ClientManager_Diconnected");
        }

        void ClientManager_TestMessage(IConnection conn, NetworkReader reader)
        {
            var message = new TestMessage();
            message.Deserialize(conn, reader);

            UnityEngine.Debug.Log(message.message);
        }

        #endregion

        public class TestMessage : INetworkMessage
        {
            public const ushort MsgCode = 10;

            public string message;

            public TestMessage()
            {
            }

            public TestMessage(string message)
            {
                this.message = message;
            }

            #region INetworkDeserializer implementation

            public void Deserialize(IConnection conn, NetworkReader reader)
            {
                message = reader.ReadString();
            }

            #endregion

            #region INetworkSerializer implementation

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(message);
            }

            #endregion
        }
    }
}

