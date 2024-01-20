using System.Collections;
using UnityEngine;
using Photon.Pun;

public class InternetChecker : Singleton<InternetChecker>
{
    private bool isConnectedToInternet = false;
    private WaitForSeconds waitforSeconds;


    private void Start()
    {
        waitforSeconds = new WaitForSeconds(10f);
        StartCoroutine(CheckForInternetConnection());
    }

    private IEnumerator CheckForInternetConnection()
    {
        while(true)
        {
            if(Application.internetReachability != NetworkReachability.NotReachable)
            {
                isConnectedToInternet = true;
                if (!PhotonNetwork.IsConnected)
                {
                    PhotonNetwork.ConnectUsingSettings();
                }
            }
            else
            {
                isConnectedToInternet = false;
            }

            yield return waitforSeconds;
        }
    }
}
