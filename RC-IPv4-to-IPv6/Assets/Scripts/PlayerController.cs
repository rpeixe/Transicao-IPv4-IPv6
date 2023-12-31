using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public Router selectedRouter;

    public bool selectingRouterToSend = false;
    public bool inTransit = false;

    public static event Action<Router> RouterSelected;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Router.Clicked += OnRouterClicked;
        Router.TransmissionFinished += OnTransmissionFinished;
    }

    private void OnDisable()
    {
        Router.Clicked -= OnRouterClicked;
        Router.TransmissionFinished -= OnTransmissionFinished;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void OnRouterClicked(Router router)
    {
        if (inTransit)
        {
            return;
        }

        if (selectingRouterToSend == false)
        {
            selectedRouter = router;
            RouterSelected?.Invoke(router);
        }
        else
        {
            int ipVersion;

            // Seleciona a versão do pacote
            if (selectedRouter.CanReachByIpv6(router))
            {
                ipVersion = 6;
            }
            else if (selectedRouter.CanReachByIpv4(router))
            {
                ipVersion = 4;
            }
            else
            {
                return;
            }

            IPPacket packet = new IPPacket(ipVersion, selectedRouter, router, "Olá!");
            inTransit = true;

            UIController.Instance.UpdatePacket(packet);
            selectingRouterToSend = false;
            StartCoroutine(selectedRouter.SendPacket(packet, 0));
            DeselectRouter();
        }
    }

    public void OnTransmissionFinished(IPPacket packet)
    {
        inTransit = false;
    }

    public void OnIPv4Toggled(bool state)
    {
        selectedRouter.SetIpv4Enabled(state);
        UIController.Instance.UpdateNode(selectedRouter);
    }

    public void OnIPv6Toggled(bool state)
    {
        selectedRouter.SetIpv6Enabled(state);
        UIController.Instance.UpdateNode(selectedRouter);
    }

    public void OnTunnelToggled(bool state)
    {
        selectedRouter.SetTunnelEnabled(state);
        UIController.Instance.UpdateNode(selectedRouter);
    }

    public void OnNat64Toggled(bool state)
    {
        selectedRouter.SetNat64Enabled(state);
        UIController.Instance.UpdateNode(selectedRouter);
    }

    public void DeselectRouter()
    {
        selectedRouter = null;
    }
}
