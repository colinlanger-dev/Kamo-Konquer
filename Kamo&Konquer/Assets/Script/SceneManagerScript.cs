using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerScript : MonoBehaviour
{
    public static bool isHost;
    public static string joinAddress = "127.0.0.1";

    public void HostGame()
    {
        isHost = true;
        SceneManager.LoadScene(1);
    }

    public void JoinGame()
    {
        isHost=false;
        SceneManager.LoadScene(1);
    }

}
