using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

public class Router : MonoBehaviour
{
    public List<Router> connectedRouters;
    public List<(Router, Router, int)> routingTable = new List<(Router, Router, int)>();

    [HideInInspector] public string Ipv4;
    [HideInInspector] public string Ipv6;

    public bool Ipv4Enabled = true;
    public bool Ipv6Enabled = false;
    public bool tunnelEnabled = false;
    public bool nat64 = false;

    public Router nat;
    public List<Router> natFor = new List<Router>();

    public event Action<Router> Clicked;
    public event Action<IPPacket> PacketUpdated;
    public event Action<IPPacket> TransmissionStarted;
    public event Action<IPPacket, Router, Router> PacketSent;
    public event Action<IPPacket> TransmissionFinished;

    [SerializeField] private TextMeshProUGUI IpText;
    [SerializeField] private GameObject linePrefab;

    private void Awake()
    {
        GenerateIP();
    }

    private void Start()
    {
        Clicked += PlayerController.Instance.OnRouterClicked;
        PacketUpdated += UIController.Instance.UpdatePacket;
        TransmissionStarted += PacketManager.Instance.CreatePacket;
        TransmissionStarted += UIController.Instance.OnTransmissionStarted;
        TransmissionFinished += PacketManager.Instance.DestroyPacket;
        TransmissionFinished += PlayerController.Instance.OnTransmissionFinished;
        PacketSent += PacketManager.Instance.MovePacket;
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

    public void GenerateIP()
    {
        if (!Ipv4Enabled)
        {
            Ipv4 = "";
        }

        if (Ipv4Enabled && Ipv4 == "")
        {
            Ipv4 = "";

            for (int i = 0; i < 4; i++)
            {
                if (nat && i ==0 )
                {
                    Ipv4 += "192";
                }
                else if (nat && i ==1 )
                {
                    Ipv4 += "168";
                }
                else
                {
                    int digit = Random.Range(0, 256);
                    Ipv4 += digit.ToString();
                }
                if (i != 3)
                {
                    Ipv4 += '.';
                }
            }
        }

        if (!Ipv6Enabled)
        {
            Ipv6 = "";
        }

        if (Ipv6Enabled && Ipv6 == "")
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    char c;
                    int digit = Random.Range(0, 16);

                    if (digit < 10)
                    {
                        c = (char)(digit + 48);
                    }
                    else
                    {
                        c = (char)(digit + 87);
                    }

                    Ipv6 += c;
                }
                if (i != 7)
                {
                    Ipv6 += ':';
                }
            }
        }

        UpdateIpText();
    }

    public void SetIpv4Enabled(bool state)
    {
        Ipv4Enabled = state;
        GenerateIP();
    }

    public void SetIpv6Enabled(bool state)
    {
        Ipv6Enabled = state;
        GenerateIP();
    }

    public void SetTunnelEnabled(bool state)
    {
        tunnelEnabled = state;
    }

    public void SetNat64Enabled(bool state)
    {
        nat64 = state;
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

    public string GetPublicIP(int version)
    {
        Router router;
        if (nat)
        {
            router = nat;
        }
        else
        {
            router = this;
        }

        if (version == 6)
        {
            return router.GetIP(6);
        }
        else if (version == 4)
        {
            return router.GetIP(4);
        }
        else
        {
            return "Erro";
        }
    }

    public List<Router> GetPath(Router destination)
    {
        List<Router> result = new List<Router>();
        
        Router current = this;

        while (current != destination)
        {
            result.Add(current);
            current = current.routingTable.FirstOrDefault(tuple => tuple.Item1 == destination).Item2;
        }

        return result;
    }

    public bool IsDualStack()
    {
        return Ipv4Enabled && Ipv6Enabled;
    }

    public bool CanReachByIpv4(Router destination)
    {
        if (!Ipv4Enabled)
        {
            return false;
        }
        if (!destination.Ipv4Enabled)
        {
            if (!destination.nat || !destination.nat.nat64)
            {
                return false;
            }
            else
            {
                return CanReachByIpv4(destination.nat) && destination.nat.CanReachByIpv6(destination);
            }
        }

        List<Router> path = GetPath(destination);

        bool result = true;
        int tunnels = 0;

        foreach (Router router in path)
        {
            if (!router.Ipv4Enabled)
            {
                result = false;
            }
            if (router.tunnelEnabled)
            {
                if (result == true || tunnels == 1)
                {
                    tunnels++;
                }

                if (tunnels == 2)
                {
                    tunnels = 0;

                    result = true;
                }
            }
        }

        return result;
    }

    public bool CanReachByIpv6(Router destination)
    {
        if (!Ipv6Enabled)
        {
            return false;
        }
        if (!destination.Ipv6Enabled)
        {
            if (!nat || !nat.nat64)
            {
                return false;
            }
            else
            {
                return CanReachByIpv6(nat) && nat.CanReachByIpv4(destination);
            }
        }

        List<Router> path = GetPath(destination);

        bool result = true;
        int tunnels = 0;

        foreach (Router router in path)
        {
            if (!router.Ipv6Enabled)
            {
                result = false;
            }
            if (router.tunnelEnabled)
            {
                if (result == true || tunnels == 1)
                {
                    tunnels++;
                }

                if (tunnels == 2)
                {
                    tunnels = 0;

                    result = true;
                }
            }
        }

        return result;
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
        }

        int delay = 1;
        yield return new WaitForSeconds(delay);

        if (packet.receiver == this)
        {
            if (packet.payload is IPPacket encapsulatedPacket)
            {
                Debug.Log(name + ": Pacote desencapsulado");

                PacketUpdated?.Invoke(encapsulatedPacket);
                StartCoroutine(SendPacket(encapsulatedPacket, hops));
            }
            else
            {
                TransmissionFinished?.Invoke(packet);
                Debug.Log(name + ": Chegou! Mensagem:");
                Debug.Log(packet.payload);
            }
            yield break;
        }

        if (natFor.Contains(packet.sender) && !natFor.Contains(packet.receiver))
        {
            int version = packet.version;

            if (nat64 && packet.version == 6 && !packet.receiver.Ipv6Enabled)
            {
                version = 4;
                packet.destination = packet.receiver.GetPublicIP(version);
            }

            packet.version = version;
            packet.source = GetIP(version);

            PacketUpdated?.Invoke(packet);
            yield return new WaitForSeconds(delay);
        }
        else if (natFor.Contains(packet.receiver))
        {
            int version = packet.version;

            if (nat64 && packet.version == 4 && !packet.receiver.Ipv4Enabled)
            {
                version = 6;
                packet.source = "64:ff9b::" + packet.sender.GetPublicIP(4);
            }

            packet.version = version;
            packet.destination = packet.receiver.GetIP(version);

            PacketUpdated?.Invoke(packet);
            yield return new WaitForSeconds(delay);
        }

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
                int version;

                if (packet.version == 4)
                {
                    version = 6;
                }
                else
                {
                    version = 4;
                }

                int min = routingTable.FirstOrDefault(tuple => tuple.Item1 == packet.receiver).Item3;
                Router closestRouter = this;
                List<Router> path = GetPath(packet.receiver);
                path.Reverse();

                foreach (Router router in path)
                {
                    if (router.tunnelEnabled)
                    {
                        if ((version == 4 && CanReachByIpv4(router)) || (version == 6 && CanReachByIpv6(router)))
                        {
                            closestRouter = router;
                            break;
                        }
                    }
                }

                if (closestRouter != this)
                {

                    Debug.Log(name + ": Pacote encapsulado");
                    IPPacket newPacket = new IPPacket(version, this, closestRouter, packet);
                    PacketUpdated?.Invoke(newPacket);
                    StartCoroutine(SendPacket(newPacket, hops));
                }
                else
                {
                    Debug.Log(name + ": Não é possível encaminhar :(");
                    TransmissionFinished?.Invoke(packet);
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
