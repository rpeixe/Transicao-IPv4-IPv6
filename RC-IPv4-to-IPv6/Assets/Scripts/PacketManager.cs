using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacketManager : MonoBehaviour
{
    public static PacketManager Instance { get; private set; }

    [SerializeField] private GameObject packetPrefab;

    private GameObject packetObject;
    private Vector3 start = Vector3.zero;
    private Vector3 target = Vector3.zero;

    float timeElapsed;
    float lerpDuration = 1f;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (packetObject != null)
        {
            if (timeElapsed < lerpDuration)
            {
                packetObject.transform.position = new Vector3(Mathf.Lerp(start.x, target.x, timeElapsed / lerpDuration),
                    Mathf.Lerp(start.y, target.y, timeElapsed / lerpDuration), 0);
                timeElapsed += Time.deltaTime;
            }
            else
            {
                packetObject.transform.position = target;
            }
        }
    }

    public void CreatePacket(IPPacket packet)
    {
        if (packetObject != null)
        {
            Destroy(packetObject);
        }
        start = packet.sender.transform.position;
        target = packet.sender.transform.position;
        packetObject = Instantiate(packetPrefab, packet.sender.transform.position, Quaternion.identity);
    }

    public void DestroyPacket(IPPacket packet)
    {
        Destroy(packetObject);
    }

    public void MovePacket(IPPacket packet, Router sender, Router receiver)
    {
        start = sender.transform.position;
        target = receiver.transform.position;
        timeElapsed = 0;
    }
}
