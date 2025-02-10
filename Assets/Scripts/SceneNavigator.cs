using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    public void LoadFaceTrackingScene()
    {
        SceneManager.LoadSceneAsync("FaceTracking");
    }

    public void LoadVirtualTryOnScene()
    {
        SceneManager.LoadSceneAsync("VirtualTryOns");
    }
}
