using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Colyseus;
using System.Collections.Generic;
using Marvin.JsonPatch;
using Newtonsoft.Json;

public class DemoModel
{
    public string a;
    public int b;
}


public class Message
{
    public object[] data;
}

public class RoomState
{
    public List<string> messages = new List<string>();
}

public class SimpleDTO

{
    public List<DemoModel> List { get; set; }
    public IList<DemoModel> GenericList { get; set; }
    public int IntegerValue { get; set; }
    public string StringProperty { get; set; }
    public string AnotherStringProperty { get; set; }
    public decimal DecimalValue { get; set; }
    public double DoubleValue { get; set; }
    public float FloatValue { get; set; }
    public Guid GuidValue { get; set; }
}

public class ChatGUI : MonoBehaviour
{
    public Text messagesText;
    public Text usersText;
    public InputField messageInput;

    private string userName = LoginGUI.userName;

    public GameObject chatPanel;
    private ArrayList chatRecords;
    private ArrayList userList;

    public bool debug = true;

    Room chatRoom;

    void Awake()
    {
        chatPanel.SetActive(false);
    }


    //When quit, release resource
    void Update()
    {
        if (Input.GetKey(KeyCode.End) || Input.GetKey(KeyCode.Return))
        {
            OnSendMessageClick();
        }

        UpdateChatWindow();
    }

    void UpdateChatWindow()
    {
        if (chatRecords == null)
            return;

        string messages = "";

        if (chatRecords.Count > 20)
            chatRecords.RemoveRange(0, chatRecords.Count - 20);

        if (LoginGUI.chatRoom != null && LoginGUI.chatRoom.state != null && LoginGUI.chatRoom.state.messages != null)
        {
            foreach (var mess in LoginGUI.chatRoom.state.messages)
            {
                messages += mess + "\n";
            }
        }

        if (chatRecords != null)
        {
            // var copy = chatRecords.ToArray();
            foreach (ChatRecord cr in chatRecords)
            {
                messages += cr.name + ": " + cr.dialog + "\n";
            }
        }
        messagesText.text = messages;


        string users = "";
        if (userList != null)
        {
            foreach (var user in userList)
            {
                users += user + "\n";
            }
        }
        usersText.text = users;
    }




    public void StartChat(Room chatRoom)
    {
        chatPanel.SetActive(true);

        chatRecords = new ArrayList();
        userList = new ArrayList();

        this.chatRoom = chatRoom;
        // chatRoom.state.Listen("messages", "add", this.OnAddMessages);
        // chatRoom.state.Listen("messages", "replace", this.OnAddMessages);
        // chatRoom.state.Listen("players", "add", this.OnAddPlayer);
        // chatRoom.state.Listen("players/:id/:axis", "replace", this.OnPlayerMove);
        // chatRoom.state.Listen("players/:id", "remove", this.OnPlayerRemoved);
    }

    private void OnAddMessages(string[] path, object value)
    {
        Debug.Log("OnAddMessages | " + ChatUtils.PathToString(path) + " | " + ChatUtils.ValueToString(value));


        var messages = (List<object>)value;
        chatRecords = new ArrayList();
        foreach (string mess in messages)
        {
            chatRecords.Add(new ChatRecord("userName", mess));
        }
    }



    public void OnSendMessageClick()
    {
        SendMessage(messageInput.text);
        messageInput.text = "";
    }

    void SendMessage(string message)
    {
        var mess = new Dictionary<string, object>();
        mess["message"] = message;
        chatRoom.Send(mess);
    }

    public void OnExitClick()
    {
        chatRoom.Leave();
        chatPanel.SetActive(false);
    }


    void OnAddPlayer(string[] path, object value)
    {
        Debug.Log("OnAddPlayer | " + ChatUtils.PathToString(path) + " | " + ChatUtils.ValueToString(value));

    }

    void OnPlayerRemoved(string[] path, object value)
    {
        Debug.Log("OnPlayerRemoved | " + ChatUtils.PathToString(path) + " | " + ChatUtils.ValueToString(value));
    }
}