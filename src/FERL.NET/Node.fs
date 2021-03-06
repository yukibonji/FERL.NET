﻿module Node

open System
open System.Net
open System.Net.Sockets
open NodeCommon

let EpmdPort = 4369
let PortForRequest = 122
let PortForResponse = 119

let BuildPeerNode nodeName =
    let assignedNodeName = BuildNodeName nodeName
    let pid = CreatePid assignedNodeName 0
    {NodeName = assignedNodeName; TcpListener = null; Pid = pid; Port = 0; Cookie = null}

let BuildSelfNode nodeName erlangCookie port =
    let tcpListener = new TcpListener(IPAddress, port)
    do tcpListener.Start()
    let assignedPort = match port with
                       | _ when port <> 0 -> port
                       | _ -> (tcpListener.LocalEndpoint :?> IPEndPoint).Port
    let assignedNodeName = BuildNodeName nodeName
    let pid = CreatePid assignedNodeName assignedPort
    {NodeName = assignedNodeName; TcpListener = tcpListener; Pid = pid; Port = assignedPort; Cookie = erlangCookie}

let GetNodeNameParts (nodeName:string) =
    let nodeNameParts = nodeName.Split('@')
    nodeNameParts.[0], nodeNameParts.[1]

let LookupNodeInformationInEmpd node =
    let nodeShortName, nodeHostName = GetNodeNameParts node.NodeName
    let epmdTcpClient = new TcpClient(nodeHostName, EpmdPort)
    let epmdResponseStream = epmdTcpClient.GetStream()
    let response = epmdResponseStream.ReadByte()
    match response with
    | _ when response = PortForResponse ->
        let result = epmdResponseStream.ReadByte()
        let rec ReadPortFromStream (stream:NetworkStream) (port:string) =
            match port.Length with
            | length when length < 5 -> Int32.Parse(port)
            | _ ->
                let newPort = port + stream.ReadByte().ToString() 
                ReadPortFromStream stream newPort
        let port = ReadPortFromStream epmdResponseStream ""
//		let ntype = epmdResponseStream.ReadByte()
//		let proto = epmdResponseStream.ReadByte()
//		let distHigh = //ibuf.read2BE();
//		let distLow = //ibuf.read2BE();
        {NodeName = node.NodeName; TcpListener = node.TcpListener; Pid = node.Pid; Port = port; Cookie = node.Cookie}
    | _ -> node

let ConnectTo node =
    try
        let _, nodeHostName = GetNodeNameParts node.NodeName
        let socket = new TcpClient(nodeHostName, node.Port)
        socket.NoDelay <- true
    with
    | _ -> failwith "Failed to connect"
//    sendName(peer.distChoose, self.flags);
//    recvStatus();
//    int her_challenge = recvChallenge();
//    byte[] our_digest = genDigest(her_challenge, self.cookie());
//    int our_challenge = genChallenge();
//    sendChallengeReply(our_challenge, our_digest);
//    recvChallengeAck(our_challenge);
//    cookieOk = true;
//    sendCookie = false;

let ConnectNodes selfNode peerNode = 
    let connectedPeerNode = LookupNodeInformationInEmpd peerNode
    do ConnectTo peerNode
    {SelfNode = selfNode; PeerNode = peerNode; IsConnected = true}