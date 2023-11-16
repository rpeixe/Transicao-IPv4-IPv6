using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public Router selectedRouter;

    void Awake()
    {
        Instance = this;
    }

    public void OnRouterClicked(Router router)
    {
        if (selectedRouter == null)
        {
            selectedRouter = router;
        }
        else
        {
            int ipVersion;
            if (selectedRouter.Ipv6Enabled && router.Ipv6Enabled)
            {
                ipVersion = 6;
            }
            else if (selectedRouter.Ipv4Enabled && router.Ipv4Enabled)
            {
                ipVersion = 4;
            }
            else
            {
                Debug.Log("Versoes incompativeis");
                selectedRouter = null;
                return;
            }
            IPPacket packet = new IPPacket(ipVersion, selectedRouter, router, "Olá!");
            UIController.Instance.UpdatePacket(packet);
            StartCoroutine(selectedRouter.SendPacket(packet, 0));
            selectedRouter = null;
        }
    }

}
