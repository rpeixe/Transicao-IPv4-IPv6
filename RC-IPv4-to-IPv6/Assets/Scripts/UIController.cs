using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [SerializeField] private TMP_Text packetVersion;
    [SerializeField] private TMP_Text packetSource;
    [SerializeField] private TMP_Text packetDestination;
    [SerializeField] private TMP_Text packetPayload;

    private void Awake()
    {
        Instance = this;
    }

    public void UpdatePacket(IPPacket packet)
    {
        packetVersion.text = "Version: " + packet.version.ToString();
        packetSource.text = packet.source;
        packetDestination.text = packet.destination;

        if (packet.payload is IPPacket)
        {
            packetPayload.text = "IPv6 Packet";
        }
        else
        {
            packetPayload.text = packet.payload.ToString();
        }
    }
}
