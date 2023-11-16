using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class Router : MonoBehaviour
{
    public List<Router> connectedRouters;
    public List<(Router, Router, int)> routingTable = new List<(Router, Router, int)>();
    public List<Router> tunnelDestinations = new List<Router>();

    public string Ipv4;
    public string Ipv6;

    public bool Ipv4Enabled = true;
    public bool Ipv6Enabled = false;
    public bool tunnelEnabled = false;

    public bool nat64 = false;
    public List<Router> natFor = new List<Router>();
    public Router nat;

    public event Action<Router> Clicked;
    public event Action<IPPacket> PacketUpdated;
    public event Action<IPPacket> TransmissionStarted;
    public event Action<IPPacket, Router, Router> PacketSent;
    public event Action<IPPacket> TransmissionFinished;

    [SerializeField] private TextMeshProUGUI IpText;
    [SerializeField] private GameObject linePrefab;

    void Start()
    {
        Clicked += PlayerController.Instance.OnRouterClicked;
        PacketUpdated += UIController.Instance.UpdatePacket;
        TransmissionStarted += PacketManager.Instance.CreatePacket;
        TransmissionFinished += PacketManager.Instance.DestroyPacket;
        PacketSent += PacketManager.Instance.MovePacket;
        UpdateIpText();
        CreateRoutingTable();
        DrawLines();
    }

    private void DrawLines()
    {
        foreach (var router in connectedRouters)
        {
            LineRenderer line = Instantiate(linePrefab). GetComponent<LineRenderer>();
            line.SetPosition(0, transform.position);
            line.SetPosition(1, router.transform.position);
        }
    }

    private void UpdateIpText()
    {
        if (Ipv6Enabled)
        {
            IpText.text = Ipv6;
            if (Ipv4Enabled)
            {
                IpText.text += "\n" + Ipv4;
            }
        }
        else
        {
            IpText.text = Ipv4;
        }
    }

    public string GetIP(int version)
    {
        if (version == 6)
        {
            return Ipv6;
        }
        else if (version == 4)
        {
            return Ipv4;
        }
        else
        {
            return "Erro";
        }
    }

    private void CreateRoutingTable()
    {
        foreach (Router router in connectedRouters)
        {
            routingTable.Add((router, router, 1));
            router.BuildRoutingTable(router, this, routingTable, 2);
        }
    }

    public void BuildRoutingTable(Router initialJump, Router previous, List<(Router, Router, int)> routingTable, int hops)
    {
        foreach (Router router in connectedRouters)
        {
            if (router == previous)
            {
                continue;
            }
            routingTable.Add((router, initialJump, hops));
            router.BuildRoutingTable(initialJump, this, routingTable, hops + 1);
        }
    }

    private void OnMouseDown()
    {
        Clicked?.Invoke(this);
    }

    public IEnumerator SendPacket(IPPacket packet, int hops)
    {
        if (hops == 0)
        {
            TransmissionStarted?.Invoke(packet);
            yield return null;
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        if (packet.receiver == this)
        {
            if (packet.payload is IPPacket encapsulatedPacket)
            {
                Debug.Log(name + ": Pacote desencapsulado");

                PacketUpdated?.Invoke(encapsulatedPacket);
                StartCoroutine(SendPacket(encapsulatedPacket, hops + 1));
            }
            else
            {
                TransmissionFinished?.Invoke(packet);
                Debug.Log(name + ": Chegou! Mensagem:");
                Debug.Log(packet.payload);
            }
        }
        else if (natFor.Contains(packet.sender))
        {
            IPPacket newPacket = new IPPacket(packet.version, this, packet.receiver, packet.payload);
            PacketUpdated?.Invoke(newPacket);
            StartCoroutine(SendPacket(newPacket, hops + 1));
        }
        else
        {
            Router nextRouter = routingTable.FirstOrDefault(tuple => tuple.Item1 == packet.receiver).Item2;
            if (packet.version == 4 && nextRouter.Ipv4Enabled
                || packet.version == 6 && nextRouter.Ipv6Enabled)
            {
                Debug.Log(name + ": Pacote encaminhado.");
                PacketSent?.Invoke(packet, this, nextRouter);
                StartCoroutine(nextRouter.SendPacket(packet, hops + 1));
            }
            else
            {
                if (tunnelEnabled)
                {
                    int min = routingTable.FirstOrDefault(tuple => tuple.Item1 == packet.receiver).Item3;
                    Router closestRouter = this;
                    foreach (Router tunnelDestination in tunnelDestinations)
                    {
                        if (tunnelDestination.routingTable.FirstOrDefault(tuple => tuple.Item1 == packet.receiver).Item3 < min)
                        {
                            min = tunnelDestination.routingTable.FirstOrDefault(tuple => tuple.Item1 == packet.receiver).Item3;
                            closestRouter = tunnelDestination;
                        }
                    }
                    if (closestRouter != this)
                    {
                        Debug.Log(name + ": Pacote encapsulado");
                        IPPacket newPacket = new IPPacket(4, this, closestRouter, packet);
                        PacketUpdated?.Invoke(newPacket);
                        StartCoroutine(SendPacket(newPacket, hops + 1));
                    }
                    else
                    {
                        Debug.Log(name + ": Não é possível encaminhar :(");
                    }
                }
                else
                {
                    Debug.Log(name + ": Não é possível encaminhar :(");
                    TransmissionFinished?.Invoke(packet);
                }
            }
        }
    }
}
