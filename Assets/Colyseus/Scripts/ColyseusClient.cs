using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Colyseus;
using MsgPack;

public class ColyseusClient : MonoBehaviour {


    public string serverName = "localhost";
    public string port = "3553";
    public string roomName = "chat";
	public GameObject playerPrefab;
	public Dictionary<string, GameObject> playersDict = new Dictionary<string, GameObject>();

	private Client colyseus;
	private Room chatRoom;

    // Use this for initialization
    IEnumerator Start () {
        String uri = "ws://" + serverName + ":" + port;
        colyseus = new Client(uri);
        colyseus.OnOpen += OnOpenHandler;
        yield return StartCoroutine(colyseus.Connect());

        chatRoom = colyseus.Join(roomName);
        chatRoom.OnJoin += OnRoomJoined;
        chatRoom.OnUpdate += OnUpdateHandler;

        chatRoom.state.Listen ("players", "add", this.OnAddPlayer);
        chatRoom.state.Listen ("players/:id/:axis", "replace", this.OnPlayerMove);
        chatRoom.state.Listen ("players/:id", "remove", this.OnPlayerRemoved);
        chatRoom.state.Listen (this.OnChangeFallback);

        int i = 0;

        while (true)
        {
            colyseus.Recv();

            // string reply = colyseus.RecvString();
            if (colyseus.error != null)
            {
                Debug.LogError ("Error: "+colyseus.error);
                break;
            }

            i++;

            if (i % 50 == 0) {
				//chatRoom.Send("axis: 10");
            }
			yield return new WaitForEndOfFrame();
        }

        OnApplicationQuit();
    }

	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Return))
		{
			chatRoom.Send(":add");
		}
	}

    void OnOpenHandler (object sender, EventArgs e)
    {
        Debug.Log("Connected to server. Client id: " + colyseus.id);
    }

    void OnRoomJoined (object sender, EventArgs e)
    {
        Debug.Log("Joined room successfully.");
    }

    void OnAddPlayer (string[] path, MessagePackObject value)
    {
        Debug.Log ("OnAddPlayer");
        Debug.Log (path[0]);
        Debug.Log (value);

		//instancia um novo jogador.
		string playerId = path[0];
		GameObject playerObj = Instantiate(playerPrefab);
		playerObj.GetComponent<Player>().id = playerId;
		playerObj.GetComponent<Player>().color = UnityEngine.Random.ColorHSV();

		if(!playersDict.ContainsKey(playerId))
			playersDict.Add(playerId, playerObj);
		else
			playersDict[playerId] = playerObj;

		Debug.Log(playerId + " entrou!");
    }

    void OnPlayerMove (string[] path, MessagePackObject value)
    {
        Debug.Log ("OnPlayerMove");
		Debug.Log ("playerId: " + path[0] + ", axis: " + path[1]);
        Debug.Log (value);
    }

	void OnPlayerRemoved (string[] path, MessagePackObject value)
    {
        Debug.Log ("OnPlayerRemoved");
        Debug.Log (value);

		string playerId = path[0];

		if(playersDict.ContainsKey(playerId))
		{
			Destroy(playersDict[playerId]);
			playersDict.Remove(playerId);

			Debug.Log(playerId + " saiu!");
		}
    }

    void OnChangeFallback (string[] path, string operation, MessagePackObject value)
    {
        Debug.Log ("OnChangeFallback");
        Debug.Log (operation);
        Debug.Log (path[0]);
        Debug.Log (value);
    }

    void OnUpdateHandler (object sender, RoomUpdateEventArgs e)
    {
        Debug.Log(e.state);
    }

    void OnApplicationQuit()
    {
        // Ensure the connection with server is closed immediatelly
        colyseus.Close();
    }
}
