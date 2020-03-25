﻿using System;
using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using MessagePack;
using UniRx;
using UnityEngine;

public static class CultClient {
    public static Action<string> Logger = s => 
        MainThreadDispatcher.Post(_=>Debug.Log(s),null);

    public static NetManager Client;
    private static NetPeer _peer;
    private static Dictionary<Type, NotAnActionCollection> _messageCallbacks = new Dictionary<Type, NotAnActionCollection>();

    public static void Send<T>(T m) where T : Message
    {
        Logger($"Sending message {MessagePackSerializer.SerializeToJson(m as Message)}");
        _peer.Send(m as Message);
    }

    public static void ClearMessageListeners()
    {
        _messageCallbacks.Clear();
    }

    public static void AddMessageListener<T>(Action<T> callback) where T : Message
    {
        if (!_messageCallbacks.ContainsKey(typeof(T)))
            _messageCallbacks[typeof(T)] = new ActionCollection<T>();
        ((ActionCollection<T>)_messageCallbacks[typeof(T)]).Add(callback);
    }

    public static void Connect(string host = "localhost", int port = 3075)
    {
        EventBasedNetListener listener = new EventBasedNetListener();
        Client = new NetManager(listener)
        {
//            UnsyncedEvents = true,
//            MergeEnabled = true,
//            NatPunchEnabled = true
        };
        Client.Start(3074);
        _peer = Client.Connect(host, port, "aetheria-cc65a44d");
        Observable.EveryUpdate().Subscribe(_ => Client.PollEvents());
        listener.NetworkErrorEvent += (point, code) => Logger($"{point.Address}: Error {code}");
        listener.NetworkReceiveEvent += (peer, reader, method) =>
        {
            Logger($"Received message: {MessagePackSerializer.ConvertToJson(new ReadOnlyMemory<byte>(reader.RawData))}");
            var message = MessagePackSerializer.Deserialize<Message>(reader.RawData);
            var type = message.GetType();
            if (_messageCallbacks.ContainsKey(type))
                typeof(ActionCollection<>).MakeGenericType(new[] {type}).GetMethod("Invoke").Invoke(_messageCallbacks[type], new object[] {message});
        };
        listener.PeerConnectedEvent += peer =>
        {
            Logger($"Peer {peer.EndPoint.Address}:{peer.EndPoint.Port} connected.");
            _peer = peer;
//			onConnect();
        };
        listener.PeerDisconnectedEvent += (peer, info) =>
        {
            Logger($"Peer {peer.EndPoint.Address}:{peer.EndPoint.Port} disconnected: {info.Reason}.");
            _peer = null;
        };
//        listener.NetworkLatencyUpdateEvent += (peer, latency) => Logger($"Ping received: {latency} ms");
    }
}