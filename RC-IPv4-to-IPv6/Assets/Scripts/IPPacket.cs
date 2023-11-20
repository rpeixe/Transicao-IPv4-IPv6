using System;
using System.Collections;
using System.Collections.Generic;
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

        if (receiver.nat == sender.nat || receiver.nat == sender)
        {
            // Rede interna

            destination = receiver.GetIP(version);
        }
        else
        {
            // Rede externa

            destination = receiver.GetPublicIP(version);
        }
        if (version == 6 && !receiver.Ipv6Enabled && sender.nat && sender.nat.nat64)
        {
            destination = "64:ff9b::" + receiver.GetPublicIP(4);
        }

        source = sender.GetIP(version);
    }
}
