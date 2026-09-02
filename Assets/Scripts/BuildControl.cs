using UnityEngine;

public class BuildControl : MonoBehaviour
{
    public bool MobileBuild;
    public int MobileDSPBufferSize = 64;

    private void Awake()
    {
        if (MobileBuild)
        {
            //PlayerPrefs.SetInt("MobileBuild", 1);
            AudioConfiguration config = AudioSettings.GetConfiguration();
            //Debug.Log(config.dspBufferSize);
            config.dspBufferSize = MobileDSPBufferSize; AudioSettings.Reset(config);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }
        else
        {
            //PlayerPrefs.SetInt("MobileBuild", 0);
        }

    }
}
