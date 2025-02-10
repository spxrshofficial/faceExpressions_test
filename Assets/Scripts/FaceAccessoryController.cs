using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class FaceAccessoryController : MonoBehaviour
{
    public ARFaceManager faceManager;          // Reference to ARFaceManager
    private GameObject currentFace;            // The active AR Face prefab
    private List<GameObject> shades = new List<GameObject>();  // List of shades

    void Update()
    {
        // Check if a face has been detected
        if (currentFace == null && faceManager.trackables.count > 0)
        {
            foreach (ARFace face in faceManager.trackables)
            {
                currentFace = face.gameObject;
                InitializeShades();
                break;
            }
        }
    }

    private void InitializeShades()
    {
        // Find all child objects (shades) of the AR Default Face
        foreach (Transform child in currentFace.transform)
        {
            // Skip the canonical_face_mesh (occluder)
            if (child.name != "canonical_face_mesh")
            {
                shades.Add(child.gameObject);
                child.gameObject.SetActive(false);  // Hide all shades initially
            }
        }

        if (shades.Count > 0)
        {
            shades[0].SetActive(true);  // Show the first shade by default
        }
    }

    public void SwitchShade(int index)
    {
        if (shades.Count == 0)
        {
            Debug.LogWarning("Shades not initialized yet.");
            return;
        }

        for (int i = 0; i < shades.Count; i++)
        {
            shades[i].SetActive(i == index);
        }
    }
}
