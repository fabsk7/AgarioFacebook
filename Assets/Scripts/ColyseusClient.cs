using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Colyseus;
using MsgPack;
using System.Runtime.Serialization.Formatters.Binary;

public class ColyseusClient : MonoBehaviour {


    public string serverName = "localhost";
    public string port = "3553";
    public string roomName = "chat";
	public GameObject myPlayer;
	public GameObject playerPrefab;
	public Dictionary<string, Player> playersDict = new Dictionary<string, Player>();

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

		chatRoom.state.Listen("players/:", "add", this.OnAddPlayer);
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

            if (i % 10 == 0) {
				chatRoom.Send("move:" + myPlayer.transform.localPosition.x + "," + myPlayer.transform.localPosition.y);
				i = 0;
            }
			yield return new WaitForEndOfFrame();
        }

        OnApplicationQuit();
    }

	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Return))
		{
			chatRoom.Send("setMass:" + 1);
		}
	}

	void OnPlayerUpdate(string[] path, MessagePackObject value)
	{
		Debug.Log("OnPlayerUpdate | " + PathToString(path) + " | " + ValueToString(value));
	}

	void OnOpenHandler (object sender, EventArgs e)
    {
        Debug.Log("Connected to server. Client id: " + colyseus.id);
    }

    void OnRoomJoined (object sender, EventArgs e)
    {
        Debug.Log("Joined room successfully.");
		Debug.Log("MEU ID: " + colyseus.id);

    }

	//instancia um novo jogador.
	void OnAddPlayer(string[] path, MessagePackObject value)
	{
		Debug.Log("OnAddPlayer | " + PathToString(path) + " | " + ValueToString(value));

		string playerId = path[0];
		int mass = value.AsDictionary()["mass"].AsInt32();
		Vector2 position = ListToVector2(value.AsDictionary()["position"].AsList());

		//verifica se o meu player.
		if(playerId != colyseus.id)
		{
			GameObject playerObj = Instantiate(playerPrefab);
			playerObj.GetComponent<Player>().id = playerId;
			playerObj.GetComponent<Player>().color = UnityEngine.Random.ColorHSV();
			playerObj.GetComponent<Player>().mass = mass;
			playerObj.transform.localPosition = position;

			if(!playersDict.ContainsKey(playerId))
				playersDict.Add(playerId, playerObj.GetComponent<Player>());
			else
				playersDict[playerId] = playerObj.GetComponent<Player>();
		}
		else
		{
			myPlayer.GetComponent<Player>().id = playerId;
			myPlayer.GetComponent<Player>().color = UnityEngine.Random.ColorHSV();
			myPlayer.GetComponent<Player>().mass = mass;

			if(!playersDict.ContainsKey(playerId))
				playersDict.Add(playerId, myPlayer.GetComponent<Player>());
			else
				playersDict[playerId] = myPlayer.GetComponent<Player>();
		}

		Debug.Log(playerId + " entrou!");
	}

	void OnPlayerMove(string[] path, MessagePackObject value)
	{
		Debug.Log("OnPlayerMove | " + PathToString(path) + " | " + ValueToString(value));
	}

	void OnPlayerRemoved(string[] path, MessagePackObject value)
	{
		Debug.Log("OnPlayerRemoved | " + PathToString(path) + " | " + ValueToString(value));
		string playerId = path[0];

		if(playersDict.ContainsKey(playerId))
		{
			Destroy(playersDict[playerId]);
			playersDict.Remove(playerId);
		}

		Debug.Log(playerId + " saiu!");
	}

	//Atualiza as coisas
	void OnChangeFallback(string[] path, string operation, MessagePackObject value)
	{
		Debug.Log("OnChangeFallback | " + operation + " | " + PathToString(path)  + " | " + value.ToString());

		return;

		if(path[0] == "players")
		{
			if (operation == "replace")
			{
				string playerID = path[1];

				if (playersDict.ContainsKey(playerID))
				{
					if(path[2] == "mass")
					{
						playersDict[playerID].GetComponent<Player>().SetMass(value.AsInt32());
					}
					else if(path[2] == "position")
					{
						Vector2 position = ListToVector2(value.AsList());
						playersDict[playerID].transform.localPosition = position;
					}
				}
			}
			else if(operation == "add")
			{
				//OnAddPlayer(new string[] { playerID }, valueDict[playerID]);
			}
		}
	}

    void OnUpdateHandler (object sender, RoomUpdateEventArgs e)
    {
		//foreach(string str in e.state.Keys)
        Debug.Log("OnUpdateHandler: " + ValueToString(e.state));
		//Debug.Log(" >> " + e.state.AsDictionary()["players"].AsDictionary().Count);

		//separando os players.
		if(e.state.AsDictionary().ContainsKey("players"))
		{
			foreach(string playerId in e.state.AsDictionary()["players"].AsDictionary().Keys)
			{
				MessagePackObject mpObj = e.state.AsDictionary()["players"].AsDictionary()[playerId];
				//Debug.Log(playerId);
				Vector2 position = ListToVector2(mpObj.AsDictionary()["position"].AsList());
				int mass = mpObj.AsDictionary()["mass"].AsInt32();

				//Atualiza informacoes desse player pelo id.
				if(playersDict.ContainsKey(playerId))
				{
					playersDict[playerId].UpdateStates(position, mass);
				}
				else
				{
					//se n existir adiciona esse player.
					OnAddPlayer(new string[]{playerId},  mpObj);
				}
			}
		}
    }

    void OnApplicationQuit()
    {
        // Ensure the connection with server is closed immediatelly
        colyseus.Close();
    }


	private string PathToString(string[] path)
	{
		string fullPath = "";
		for (int i = 0; i < path.Length; i++)
		{
			fullPath += path[i];
			if (i != path.Length - 1)
				fullPath += ".";
		}
		return fullPath;
	}

	private string ValueToString(object value)
	{
		if (value is Dictionary<string, object>)
		{
			string val = "";
			var dic = (Dictionary<string, object>)value;
			foreach (var key in dic.Keys)
			{
				val += key + ":" + dic[key];
					val += ", ";
			}

			return val;
		}
		else
		{
			return value.ToString();
		}
	}

	static byte[] ObjectToByteArray(object obj)
	{
		if (obj == null)
			return null;
		BinaryFormatter bf = new BinaryFormatter();
		using (MemoryStream ms = new MemoryStream())
		{
			bf.Serialize(ms, obj);
			return ms.ToArray();
		}
	}

	private Dictionary<string, object> ValueToDict(object value)
	{
		if (value is Dictionary<string, object>)
		{
			Dictionary<string, object> dict = new Dictionary<string, object>();
			var dic = (Dictionary<string, object>)value;
			
			foreach (var key in dic.Keys)
			{
				dict.Add(key, dic[key]);
			}

			return dict;
		}
		else
		{
			Debug.Log("Erro: " + value.ToString());
			return new Dictionary<string, object>();
		}
	}

	static Vector2 ListToVector2(IList<MessagePackObject> value)
	{
		if(value.Count > 1)
		{	
			float x =  value[0].AsSingle();
			float y =  value[1].AsSingle();

			return new Vector2(x, y);
		}
		else
		{
			return Vector2.zero;
		}
	}

	static Vector2 ObjectToVector2(object value)
	{
		List<object> vecLst = value as List<object>;

		if(vecLst.Count > 1)
		{	
			float x =  Convert.ToSingle(vecLst[0]);
			float y =  Convert.ToSingle(vecLst[1]);

			return new Vector2(x, y);
		}
		else
		{
			return Vector2.zero;
		}
	}
}
