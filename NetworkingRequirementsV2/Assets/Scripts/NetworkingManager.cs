using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum sceneList
{
    loginChatScene, //0
    walkAroundScene //1
}
public struct CS_to_Plugin_Functions
{
    public IntPtr MsgReceivedPtr;

    // The functions don't need to be the same though
    // Init isn't in C++
    public bool Init()
    {
        MsgReceivedPtr = Marshal.GetFunctionPointerForDelegate(new Action<IntPtr>(NetworkingManager.MsgReceived));

        return true;
    }
}

public class ClientConnectionData
{
    public string name;
    public string status;
    public int id;
    public UserElement go;
    public bool inWalkAround = false;
    public PawnElement playerObj;
    public Vector3 positionLastUpdate = new Vector3(0f, 0f, 0f);

    public string positionToString()
    {
        string posString = playerObj.transform.localPosition.x.ToString() + ";"
                        + playerObj.transform.localPosition.y.ToString() + ";"
                        + playerObj.transform.localPosition.z.ToString() + ";"
                        + playerObj.transform.localRotation.x.ToString() + ";"
                        + playerObj.transform.localRotation.y.ToString() + ";"
                        + playerObj.transform.localRotation.z.ToString() + ";"
                        + playerObj.transform.localRotation.w.ToString();
        return posString;
    }
}

public class MsgToPopulate
{
    public string msg;
    public int id;
}

public class NetworkingManager : MonoBehaviour
{

    // Same old DLL init stuff
    private const string path = "/Plugins/NetworkingTutorialDLL.dll";

    private IntPtr Plugin_Handle;
    private CS_to_Plugin_Functions Plugin_Functions;

    public delegate void InitDLLDelegate(CS_to_Plugin_Functions funcs);
    public InitDLLDelegate InitDLL;

    public delegate void InitServerDelegate(string IP, int port);
    public InitServerDelegate InitServer;

    public delegate void InitClientDelegate(string IP, int port, string name);
    public InitClientDelegate InitClient;

    public delegate void SendPacketToServerDelegate(string msg);
    public SendPacketToServerDelegate SendPacketToServer;

    public delegate void CleanupDelegate();
    public CleanupDelegate Cleanup;

    // MUST be called before you call any of the DLL functions
    private void InitDLLFunctions()
    {
        InitDLL = ManualPluginImporter.GetDelegate<InitDLLDelegate>(Plugin_Handle, "InitDLL");
        InitServer = ManualPluginImporter.GetDelegate<InitServerDelegate>(Plugin_Handle, "InitServer");
        InitClient = ManualPluginImporter.GetDelegate<InitClientDelegate>(Plugin_Handle, "InitClient");
        SendPacketToServer = ManualPluginImporter.GetDelegate<SendPacketToServerDelegate>(Plugin_Handle, "SendPacketToServer");
        Cleanup = ManualPluginImporter.GetDelegate<CleanupDelegate>(Plugin_Handle, "Cleanup");
    }

    // Fields we need later
    [Header("Chat System")]
    public GameObject loginCanvas;
    public GameObject chatCanvas;
    public GameObject textboxPrefab;
    public GameObject textboxParent;
    public GameObject userPrefab;
    public GameObject userParent;
    public InputField messageInput;
    public InputField nameInput;
    public InputField ipAddressInput;
    public Text inputWarning;

    [Header("Walk Around")]
    public GameObject playerPrefab;
    public GameObject pawnPrefab;
    public Transform pawnParent;


    [Header("Networking")]
    private static bool mutex = false;
    static List<MsgToPopulate> msgs = new List<MsgToPopulate>();
    static List<ClientConnectionData> clients = new List<ClientConnectionData>();
    static ClientConnectionData user = new ClientConnectionData();

    private float mutexCounter = 0;
    private float activityCounter = 0;
    private float UpdateCounter = 0;
    private float UPDATE_INTERVAL = .2f;

    //Scene Switching Variables
    private sceneList currScene = sceneList.loginChatScene;


    //Singleton
    private static NetworkingManager Instance;
    // Init the DLL
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        Plugin_Handle = ManualPluginImporter.OpenLibrary(Application.dataPath + path);
        Plugin_Functions.Init();

        InitDLLFunctions();

        InitDLL(Plugin_Functions);
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void FixedUpdate()
    {
        mutexCounter += Time.fixedDeltaTime;
        activityCounter += Time.fixedDeltaTime;
        UpdateCounter += Time.fixedDeltaTime;

        if (mutexCounter >= 0.5f)
        {
            mutex = true;
            if (currScene == sceneList.loginChatScene)
                UpdateDataChatSystem();
            else if (currScene == sceneList.walkAroundScene)
                UpdateDataMovementSystem();

            mutex = false;
        }

        if (UpdateCounter >= UPDATE_INTERVAL)
        {
            if (currScene == sceneList.walkAroundScene)
            {
                //If the player isn't standing still
                if (user.positionLastUpdate != user.playerObj.transform.localPosition)
                {
                    Debug.Log("SEND>> Transform");
                    SendCurrentTransform();
                }
                user.positionLastUpdate = user.playerObj.transform.localPosition;
            }
            UpdateCounter = 0f;
        }

        if (activityCounter >= 10.0f)
        {
            user.status = "IDLE";
            SendPacketToServer("s;" + user.id.ToString() + ";IDLE");
            activityCounter = 0;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SendCurrentMessage();
        }
    }

    // Update client and message data
    private void UpdateDataChatSystem()
    {
        if (msgs.Count > 0)
        {
            for (int i = msgs.Count - 1; i >= 0; i--)
            {
                GameObject go = Instantiate(textboxPrefab, textboxParent.transform);
                for (int j = 0; j < clients.Count; j++)
                {
                    if (clients[j].id == msgs[i].id)
                    {
                        go.GetComponent<TextElement>().UpdateText(clients[j].name, msgs[i].msg);

                        break;
                    }
                }
                msgs.Remove(msgs[i]);
            }
        }
        if (clients.Count > 0)
        {
            if (currScene == sceneList.loginChatScene) //If login or chat scene
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].go == null)
                    {
                        Debug.Log("INIT>>CLIENT_DISPLAY");
                        clients[i].go = Instantiate(userPrefab, userParent.transform).GetComponent<UserElement>();
                    }
                    if (i == 0)
                    {
                        clients[i].go.UpdateUser(user.name, user.status);
                    }
                    else
                    {
                        clients[i].go.UpdateUser(clients[i].name, clients[i].status);
                    }
                }
            }
        }
    }

    // Update client movement data
    private void UpdateDataMovementSystem()
    {
        if (clients.Count > 1)
        {
            for (int i = 1; i < clients.Count; i++)
            {
                if (clients[i].inWalkAround)
                {
                    if (clients[i].playerObj == null)
                    {
                        Debug.Log("INIT>>CLIENT_MODEL");
                        clients[i].playerObj = Instantiate(pawnPrefab, pawnParent).GetComponent<PawnElement>();
                    }
                    else
                    {
                        Debug.Log("UPDATE>>ENEMY_POS");
                        clients[i].playerObj.UpdateTransform(UPDATE_INTERVAL);
                    }
                }
            }
        }
    }

    // Init the server
    public void StartServer()
    {
        if (CheckLoginCredentials())
            InitServer("127.0.0.1", 54000);
    }
    // Init the client
    public bool CheckLoginCredentials()
    {
        if (ipAddressInput.text != "" && nameInput.text != "")
        {
            user.name = nameInput.text;
            user.status = "ONLINE";
            return true;
        }
        inputWarning.gameObject.SetActive(true);
        return false;
    }
    public void ProceedToChatRoom()
    {
        if (CheckLoginCredentials())
        {
            loginCanvas.SetActive(false);
            chatCanvas.SetActive(true);
        }
    }
    public void StartClient()
    {
        if (CheckLoginCredentials())
            InitClient(ipAddressInput.text, 54000, nameInput.text);
    }

    // Where we'll process incoming messages
    public static void MsgReceived(IntPtr p_in)
    {
        string p = Marshal.PtrToStringAnsi(p_in);
        Debug.Log("RECEIVED: " + p);

        while (mutex)
        { } // wait 
            // Look up mutex, semaphores

        switch (p[0])
        {
            // Got an ID packet
            case 'i': //i;id
                {
                    string[] ar = p.Split(';');
                    user.id = int.Parse(ar[1]);

                    ClientConnectionData temp = new ClientConnectionData();

                    temp.name = user.name;
                    temp.status = user.status;
                    temp.id = user.id;

                    clients.Add(temp);
                    // may want to use TryParse

                    break;
                }
            case 'c': // c;NAME;STATUS;00
                {
                    ClientConnectionData temp = new ClientConnectionData();
                    string[] ar = p.Split(';');

                    temp.name = ar[1];
                    temp.status = ar[2];
                    temp.id = int.Parse(ar[3]);

                    clients.Add(temp);

                    break;
                }
            case 's': // s;00;STATUS
                {
                    string[] ar = p.Split(';');
                    int id = int.Parse(ar[1]);

                    for (int i = 0; i < clients.Count; i++)
                    {
                        if (clients[i].id == id)
                        {
                            clients[i].status = ar[2];

                            return;
                        }
                    }

                    break;
                }
            case 'm':
                {
                    string[] ar = p.Split(';');
                    int id = int.Parse(ar[1]);
                    string msg = ar[2];

                    MsgToPopulate msgp = new MsgToPopulate();
                    msgp.msg = msg;
                    msgp.id = id;
                    msgs.Add(msgp);

                    break;
                }
            case 't': // t;00;p1;p2;p3;r1;r2;r3;r4
                {
                    string[] ar = p.Split(';');
                    int id = int.Parse(ar[1]);
                    //Position
                    float px = float.Parse(ar[2]);
                    float py = float.Parse(ar[3]);
                    float pz = float.Parse(ar[4]);
                    //Rotation
                    float rx = float.Parse(ar[5]);
                    float ry = float.Parse(ar[6]);
                    float rz = float.Parse(ar[7]);
                    float rw = float.Parse(ar[8]);

                    //Update Current Transform
                    for (int i = 0; i < clients.Count; i++)
                    {
                        if (clients[i].id == id)
                        {
                            if (!clients[i].inWalkAround)
                            {
                                clients[i].inWalkAround = true;
                            }
                            else
                            {
                                clients[i].playerObj.previousPos = clients[i].playerObj.currentPos;
                                clients[i].playerObj.previousRot = clients[i].playerObj.currentRot;
                                clients[i].playerObj.currentPos = new Vector3(px, py, pz);
                                clients[i].playerObj.currentRot = new Quaternion(rx, ry, rz, rw);
                            }
                            return;
                        }
                    }

                    break;
                }
        }

    }

    public void SendCurrentMessage()
    {
        if (messageInput.text != "")
        {
            MsgToPopulate msgp = new MsgToPopulate();
            msgp.msg = messageInput.text;
            msgp.id = user.id;
            msgs.Add(msgp);
            user.status = "CHATTING";

            SendPacketToServer("m;" + msgp.id.ToString() + ";" + msgp.msg);
            SendPacketToServer("s;" + msgp.id.ToString() + ";CHATTING");
            messageInput.text = "";


            activityCounter = 0;
        }
    }

    public bool PlayerMoved()
    {
        if (currScene == sceneList.walkAroundScene)
        {
            //if move X/Z plane
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
                return true;
            if (Input.GetKey(KeyCode.Space))
                return true;
        }
        return false;
    }

    public void SendCurrentTransform()
    {
        SendPacketToServer("t;" + user.id.ToString() + ";" + user.positionToString());
    }

    public void EnterGame()
    {
        SceneManager.LoadScene("gameScene", LoadSceneMode.Single);
        currScene = sceneList.walkAroundScene;
        user.inWalkAround = true;
    }

    //When the scene loads -- Not 100% sure how this works but it does lol
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (currScene == sceneList.walkAroundScene)
        {
            pawnParent = GameObject.Find("ClientsOnline").GetComponent<Transform>();
            user.playerObj = Instantiate(playerPrefab).GetComponent<PawnElement>();
            SendPacketToServer("s;" + user.id.ToString() + ";IN GAME");
            Debug.Log("INIT>> Player Model");
        }
    }

    private void OnApplicationQuit()
    {
        SendPacketToServer("s;" + user.id.ToString() + ";OFFLINE");
        Cleanup();
        ManualPluginImporter.CloseLibrary(Plugin_Handle);
    }
}
