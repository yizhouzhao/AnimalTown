using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using BestHTTP;
using BestHTTP.SocketIO;

public class CharacterSocketClient : MonoBehaviour
{
    private SocketManager _manager;
    
    [SerializeField]
    public string address = "http://localhost:8080/socket.io/";
    void Start()
    {
        Debug.Log("starting client");
        SocketOptions options = new SocketOptions();
        _manager = new SocketManager(new Uri(address), options);
        _manager.Socket.On("connect", OnConnect);
        _manager.Socket.On("pickup", OnPickUp);
        _manager.Socket.On("drop", OnDrop);
        _manager.Socket.On("move", OnMove);
        _manager.Socket.On("eat", OnEat);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnConnect(Socket socket, Packet packet, params object[] args)
    {
        Debug.Log("connected");
        _manager.Socket.Emit("hear", "hello!");
    }
    
    void OnPickUp(Socket socket, Packet packet, params object[] args)
    {
        Debug.Log("pick up: " + args[0] as string);
        SendMessage("PickupDropObject");
    }
    
    void OnDrop(Socket socket, Packet packet, params object[] args)
    {
        Debug.Log("drop: " + args[0] as string);
    }
    
    void OnMove(Socket socket, Packet packet, params object[] args)
    {
        Debug.Log("moving to: ");
    }
    
    void OnEat(Socket socket, Packet packet, params object[] args)
    {
        Debug.Log("eat: " + args[0]);
    }
}
