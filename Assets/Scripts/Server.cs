using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

public class Server : MonoBehaviour {

    private const int MAX_USER = 100;
    private const int PORT = 26000;
    private const int WEB_PORT = 26001;
    private const int BYTE_SIZE = 1024;
    
    private byte reliableChannel;
    private int hostId;
    private int webHostId;

    private bool isStarted = false;
    private byte error;

    private Mongo db;

    // Use this for initialization
    private void Start () {
        DontDestroyOnLoad(gameObject);
        Init();
	}
    private void Update() {
        UpdateMessagePump();
    }

    public void Init() {
        db = new Mongo();
        db.Init();

        NetworkTransport.Init();

        ConnectionConfig cc = new ConnectionConfig();
        reliableChannel = cc.AddChannel(QosType.Reliable);

        HostTopology topo = new HostTopology(cc, MAX_USER);

        hostId = NetworkTransport.AddHost(topo, PORT, null);
        webHostId = NetworkTransport.AddWebsocketHost(topo, WEB_PORT, null);

        Debug.Log(string.Format("Opening connection in port {0} and webport {1}", PORT, WEB_PORT));
        isStarted = true;
    }
    public void Shutdown() {
        isStarted = false;
        NetworkTransport.Shutdown();
    }

    public void UpdateMessagePump() {
        if (!isStarted) {
            return;
        }

        int recHostId;
        int connectionId;
        int channelId;

        byte[] recBuffer = new byte[BYTE_SIZE];
        int dataSize;

        NetworkEventType type = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, BYTE_SIZE, out dataSize, out error);
        switch (type) {
            case NetworkEventType.Nothing:
                break;

            case NetworkEventType.ConnectEvent:
                Debug.Log(string.Format("User {0} has connected through host {1}!", connectionId, recHostId));
                break;


            case NetworkEventType.DisconnectEvent:
                Debug.Log(string.Format("User {0} has disconnected :(", connectionId));
                break;

            case NetworkEventType.DataEvent:
                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(recBuffer);
                NetMsg msg = (NetMsg)formatter.Deserialize(ms);

                OnData(connectionId, channelId, recHostId, msg);
                break;

            default:
            case NetworkEventType.BroadcastEvent:
                Debug.Log("Unexpected network event type");
                break;
        }

    }

    private void OnData(int cnnId, int channelId, int recHostId, NetMsg msg) {
        Debug.Log("Recieved a message of type " + msg.OP);
        switch (msg.OP) {
            case NetOP.None:
                Debug.Log("Unexpected NET OP");
                break;

            case NetOP.CreateAccount:
                CreateAccount(cnnId, channelId, recHostId, (Net_CreateAccount)msg);
                break;

            case NetOP.LoginRequest:
                LoginRequest(cnnId, channelId, recHostId, (Net_LoginRequest)msg);
                break;

            case NetOP.AddFollow:
                AddFollow(cnnId, channelId, recHostId, (Net_AddFollow)msg);
                break;
            case NetOP.RemoveFollow:
                RemoveFollow(cnnId, channelId, recHostId, (Net_RemoveFollow)msg);
                break;
            case NetOP.RequestFollow:
                RequestFollow(cnnId, channelId, recHostId, (Net_RequestFollow)msg);
                break;
        }
    }

    private void RequestFollow(int cnnId, int channelId, int recHostId, Net_RequestFollow msg)
    {

    }

    private void RemoveFollow(int cnnId, int channelId, int recHostId, Net_RemoveFollow msg)
    {
    }

    private void AddFollow(int cnnId, int channelId, int recHostId, Net_AddFollow msg)
    {
        Net_OnAddFollow oaf = new Net_OnAddFollow();

        if (db.InsertFollow(msg.Token, msg.UsernameDiscriminatorOrEmail)){
            if (Utility.IsEmail(msg.UsernameDiscriminatorOrEmail)) {
                oaf.Follow = db.FindAccountByEmail(msg.UsernameDiscriminatorOrEmail).GetAccount();
            }
            else {
                string[] data = msg.UsernameDiscriminatorOrEmail.Split('#');
                if (data[1] == null)
                {
                    return; 
                }
                oaf.Follow = db.FindAccountByUsernameAndDiscriminator(data[0], data[1]).GetAccount();
            }
        }

        SendClient(recHostId, cnnId, oaf);
    }

    public void SendClient(int recHost, int cnnId, NetMsg msg)
    {
        byte[] buffer = new byte[BYTE_SIZE];

        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(buffer);
        formatter.Serialize(ms, msg);

        if (recHost == 0)
        {
            NetworkTransport.Send(hostId, cnnId, reliableChannel, buffer, BYTE_SIZE, out error);
        }
        else {
            NetworkTransport.Send(webHostId, cnnId, reliableChannel, buffer, BYTE_SIZE, out error);
        }
    }

    private void CreateAccount(int cnnId, int channelId, int recHostId, Net_CreateAccount ca)
    {
        Net_OnCreateAccount oca = new Net_OnCreateAccount();

        if (db.InsertAccount(ca.Username, ca.Password, ca.Email))
        {
            oca.Success = 1;
            oca.Information = "Account was created :)";
        }
        else {
            oca.Success = 0;
            oca.Information = "There was an error creating the account";
        }

        SendClient(recHostId, cnnId, oca);
    }

    private void LoginRequest(int cnnId, int channelId, int recHostId, Net_LoginRequest lr) {

        string randomToken = Utility.GenerateRandom(64);
        Model_Account account = db.LoginAccount(lr.UsernameOrEmail, lr.Password, cnnId, randomToken);
        Net_OnLoginRequest olr = new Net_OnLoginRequest();

        if (account != null)
        {
            olr.Success = 1;
            olr.Information = "You logged in as " + account.Username;
            olr.Username = account.Username;
            olr.Discriminator = account.Discriminator;
            olr.Token = account.Token;
            olr.ConnectionId = cnnId;

        }
        else {
            olr.Success = 0;
        }


        SendClient(recHostId, cnnId, olr);
    }
}
