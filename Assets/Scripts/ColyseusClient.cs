using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Colyseus;
using GameDevWare.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public class ColyseusClient : MonoBehaviour {


    public string serverName = "localhost";
    public string port = "3553";
    public string roomName = "chat";
	public GameObject myPlayer;
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

		chatRoom.state.Listen("players", "add", this.OnAddPlayer);
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

            if (i % 30 == 0) {
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

	void OnPlayerUpdate(string[] path, object value)
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
    }

	void OnAddPlayer(string[] path, object value)
	{
		Debug.Log("OnAddPlayer | " + PathToString(path) + " | " + ValueToString(value));

		Debug.Log(ValueToDict(value)["mass"]);

		int mass = (int)Convert.ToSingle(ValueToDict(value)["mass"]);

		//instancia um novo jogador.
		string playerId = path[0];
		Debug.Log("MEU ID: " + colyseus.id);

		//verifica se o meu player.
		if(playerId != colyseus.id)
		{
			GameObject playerObj = Instantiate(playerPrefab);
			playerObj.GetComponent<Player>().id = playerId;
			playerObj.GetComponent<Player>().color = UnityEngine.Random.ColorHSV();
			playerObj.GetComponent<Player>().mass = mass;

			if(!playersDict.ContainsKey(playerId))
				playersDict.Add(playerId, playerObj);
			else
				playersDict[playerId] = playerObj;
		}
		else
		{
			myPlayer.GetComponent<Player>().id = playerId;
			myPlayer.GetComponent<Player>().color = UnityEngine.Random.ColorHSV();
			myPlayer.GetComponent<Player>().mass = mass;

			if(!playersDict.ContainsKey(playerId))
				playersDict.Add(playerId, myPlayer);
			else
				playersDict[playerId] = myPlayer;
		}

		Debug.Log(playerId + " entrou!");
	}

	void OnPlayerMove(string[] path, object value)
	{
		Debug.Log("OnPlayerMove | " + PathToString(path) + " | " + ValueToString(value));
	}

	void OnPlayerRemoved(string[] path, object value)
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
	void OnChangeFallback(string[] path, string operation, object value)
	{
		Debug.Log("OnChangeFallback | " + operation + " | " + PathToString(path) + " | " + ValueToString(value) + " | " + value.ToString());
		
		//separa cada informacao.
		Dictionary<string, object> valueDict = ValueToDict(value);
		//se vir sem valor n serve pra nada.
		if (valueDict.Count == 0)
			return;

		foreach (string key in valueDict.Keys)
		{
			Dictionary<string, object> data = ValueToDict(valueDict[key]);

			//Debug.Log(data["position"]);
			string playerID = key;

			if (operation == "replace")
			{
				if (playersDict.ContainsKey(playerID))
				{
					List<object> posValue = data["position"] as List<object>;
					if (posValue.Count > 0)
					{
						float x =  Convert.ToSingle(posValue[0]);
						float y =  Convert.ToSingle(posValue[1]);
						Vector2 pos = new Vector2(x, y);
						playersDict[playerID].transform.localPosition = pos;
					}
				}
			}
			else if(operation == "add")
			{
				OnAddPlayer(new string[] { playerID }, valueDict[playerID]);
			}
		}
	}

    void OnUpdateHandler (object sender, RoomUpdateEventArgs e)
    {
		//foreach(string str in e.state.Keys)
        Debug.Log("OnUpdateHandler: " + ValueToString(e.state));
		//Debug.Log(" >> " + e.state.AsDictionary()["players"].AsDictionary().Count);

		//separando os players.
		/*if(e.state.ContainsKey("players"))
		{
			foreach(string playerId in e.state["players"].AsDictionary().Keys)
			{
				//posicao.
				Vector2 position = new Vector2((float)e.state.AsDictionary()["players"].
					AsDictionary()[playerId].
					AsDictionary()["posX"].AsDouble(),
					(float)e.state.AsDictionary()["players"].
					AsDictionary()[playerId].
					AsDictionary()["posY"].AsDouble());

				//massa.
				int mass = e.state.AsDictionary()["players"].
					AsDictionary()[playerId].
					AsDictionary()["mass"].AsInt32();
				
				//Debug.Log(playerId);
				//Atualiza informacoes desse player pelo id.
				if(playersDict.ContainsKey(playerId))
				{
					playersDict[playerId].GetComponent<Player>().UpdateStates(position, mass);
				}
			}
		}*/
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
		if (value is IndexedDictionary<string, object>)
		{
			string val = "";
			var dic = (IndexedDictionary<string, object>)value;
			foreach (var key in dic.Keys)
			{
			}

			for (int i = 0; i < dic.Keys.Count; i++)
			{
				var key = dic.Keys[i];
				val += key + ":" + dic[key];
				if (i != dic.Keys.Count - 1)
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
		if (value is IndexedDictionary<string, object>)
		{
			Dictionary<string, object> dict = new Dictionary<string, object>();
			var dic = (IndexedDictionary<string, object>)value;
			
			for (int i = 0; i < dic.Keys.Count; i++)
			{
				var key = dic.Keys[i];
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
}
