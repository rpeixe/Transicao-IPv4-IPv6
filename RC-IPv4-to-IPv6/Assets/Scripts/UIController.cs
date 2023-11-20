using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [SerializeField] private GameObject packetPanel;
    [SerializeField] private TMP_Text packetVersion;
    [SerializeField] private TMP_Text packetSource;
    [SerializeField] private TMP_Text packetDestination;
    [SerializeField] private TMP_Text packetPayload;

    [SerializeField] private GameObject nodePanel;
    [SerializeField] private TMP_Text ipText;
    [SerializeField] private Toggle ipv4EnabledButton;
    [SerializeField] private Toggle ipv6EnabledButton;
    [SerializeField] private Toggle tunnelEnabledButton;
    [SerializeField] private Toggle nat64EnabledButton;
    [SerializeField] private Button sendPacketButton;
    [SerializeField] private TMP_Text sendPacketText;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        PlayerController.RouterSelected += OnRouterSelected;
        Router.PacketUpdated += UpdatePacket;
        Router.TransmissionStarted += OnTransmissionStarted;
    }

    private void OnDisable()
    {
        PlayerController.RouterSelected -= OnRouterSelected;
        Router.PacketUpdated -= UpdatePacket;
        Router.TransmissionStarted -= OnTransmissionStarted;
    }

    public void UpdatePacket(IPPacket packet)
    {
        packetVersion.text = "Version: " + packet.version.ToString();
        packetSource.text = packet.source;
        packetDestination.text = packet.destination;

        if (packet.payload is IPPacket p)
        {
            packetPayload.text = "IPv" + p.version.ToString() + " Packet";
        }
        else
        {
            packetPayload.text = packet.payload.ToString();
        }
    }

    public void ShowPacketWindow()
    {
        packetPanel.SetActive(true);
    }

    public void HidePacketWindow()
    {
        packetPanel.SetActive(false);
    }

    public void UpdateNode(Router selectedRouter)
    {
        if (selectedRouter.Ipv6Enabled)
        {
            ipText.text = selectedRouter.Ipv6;
            if (selectedRouter.Ipv4Enabled)
            {
                ipText.text += "\n" + selectedRouter.Ipv4;
            }
        }
        else
        {
            ipText.text = selectedRouter.Ipv4;
        }

        ipv4EnabledButton.isOn = selectedRouter.Ipv4Enabled;
        ipv6EnabledButton.isOn = selectedRouter.Ipv6Enabled;
        tunnelEnabledButton.isOn = selectedRouter.tunnelEnabled;
        nat64EnabledButton.isOn = selectedRouter.nat64;
        nat64EnabledButton.interactable = (selectedRouter.natFor.Count > 0);
        sendPacketText.text = "Send Packet";
    }

    public void ShowNodeWindow(Router selectedRouter)
    {
        UpdateNode(selectedRouter);
        nodePanel.SetActive(true);
    }

    public void HideNodeWindow()
    {
        nodePanel.SetActive(false);
    }

    public void OnTransmissionStarted(IPPacket packet)
    {
        HideNodeWindow();
        ShowPacketWindow();
    }

    public void OnRouterSelected(Router router)
    {
        HidePacketWindow();
        ShowNodeWindow(router);
    }

    public void OnSendPacketButtonClicked()
    {
        if (PlayerController.Instance.selectingRouterToSend)
        {
            PlayerController.Instance.selectingRouterToSend = false;
            sendPacketText.text = "Send Packet";
        }
        else
        {
            PlayerController.Instance.selectingRouterToSend = true;
            sendPacketText.text = "Cancel";
        }
    }
}
