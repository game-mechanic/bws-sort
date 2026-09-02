using UnityEngine;
using UnityEngine.SceneManagement;

public class Load : MonoBehaviour
{
    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
