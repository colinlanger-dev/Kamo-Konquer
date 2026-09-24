using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameManagerScript : MonoBehaviour
{
    public NetworkManager networkManager;
    public TMP_Text KamoText;
    public TMP_Text AzadText;
    void Start()
    {

        if (SceneManagerScript.isHost)
        {
            networkManager.StartHost();
        }
        else
        {
            networkManager.StartClient();
        }

    }


    public void joinKamo()
    {
        KamoText.text = "";
    }

    public void joinAzad()
    {
        KamoText.text = "";
    }


}
