using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class IPPacket
{
    public int version; // 4 ou 6
    public Router sender;
    public Router receiver;
    public object payload;
    public string source;
    public string destination;

    public IPPacket(int version, Router sender, Router receiver, object payload)
    {
        this.version = version;
        this.sender = sender;
        this.receiver = receiver;
        this.payload = payload;

        source = sender.GetIP(version);
        destination = receiver.GetIP(version);
    }
}
