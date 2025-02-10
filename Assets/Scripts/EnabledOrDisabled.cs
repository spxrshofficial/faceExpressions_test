using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnabledOrDisabled : MonoBehaviour
{
    public static EnabledOrDisabled Instance;  // Singleton instance

    public GameObject[] Frames; 
    private bool isInitialized = false;

    void Awake()
    {
        Instance = this;  // Set the active instance when the prefab is loaded
    }

    void Update()
    {
        if (!isInitialized && Frames.Length == 0)
        {
            InitializeFrames();
        }
    }

    private void InitializeFrames()
    {
        Frames = GetComponentsInChildren<Transform>(true)
                    .Where(t => t.CompareTag("Frame"))
                    .Select(t => t.gameObject)
                    .ToArray();

        if (Frames.Length > 0)
        {
            Debug.Log("Frames found and initialized.");
            foreach (GameObject frame in Frames)
            {
                frame.SetActive(false);  // Disable all frames
            }
            Frames[0].SetActive(true);   // Enable only the first frame
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning("No frames found under this prefab.");
        }
    }

    public void Trigger(int indexToEnable)
    {
        if (Frames.Length == 0)
        {
            Debug.LogWarning("Frames not initialized yet.");
            return;
        }

        for (int i = 0; i < Frames.Length; i++)
        {
            Frames[i].SetActive(i == indexToEnable);
        }
    }
}
