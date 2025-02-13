using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
#if UNITY_IOS && !UNITY_EDITOR
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.XR.ARKit;
#endif

[RequireComponent(typeof(ARFace))]
public class ARKitFaceInteraction : MonoBehaviour
{
    // UI elements—these will be auto-found by name if not manually assigned.
    public TextMeshProUGUI blinkCounterText;
    public TextMeshProUGUI mouthStateText;
    public TextMeshProUGUI headTiltValueText;
    public TextMeshProUGUI smileCounterText;
    public TextMeshProUGUI eyebrowValueText;
    public TextMeshProUGUI headPosXText;
    public TextMeshProUGUI headNodYesText;
    public TextMeshProUGUI headShakeNoText;

    private ARFace m_Face;
#if UNITY_IOS && !UNITY_EDITOR
    private ARKitFaceSubsystem m_ARKitFaceSubsystem;
#endif

    // Counters and previous state variables.
    private int blinkCount = 0;
    private int smileCount = 0;
    private int eyebrowRaiseCount = 0;
    private bool prevBlinking = false;
    private bool prevSmiling = false;
    private bool prevEyebrowRaised = false;
    private string prevMouthState = "Closed";
    private string prevHeadTilt = "Neutral";

    void Awake()
    {
        m_Face = GetComponent<ARFace>();
        if (m_Face == null)
        {
            Debug.LogError("ARKitFaceInteraction requires an ARFace component on the same GameObject.");
        }

        // Auto-find UI elements if not assigned.
        if (blinkCounterText == null)
            blinkCounterText = GameObject.Find("BlinkCounter")?.GetComponent<TextMeshProUGUI>();
        if (mouthStateText == null)
            mouthStateText = GameObject.Find("MouthState")?.GetComponent<TextMeshProUGUI>();
        if (headTiltValueText == null)
            headTiltValueText = GameObject.Find("HeadTiltValue")?.GetComponent<TextMeshProUGUI>();
        if (smileCounterText == null)
            smileCounterText = GameObject.Find("SmileCounter")?.GetComponent<TextMeshProUGUI>();
        if (eyebrowValueText == null)
            eyebrowValueText = GameObject.Find("EyebrowValue")?.GetComponent<TextMeshProUGUI>();
        if (headPosXText == null)
            headPosXText = GameObject.Find("HeadPosX")?.GetComponent<TextMeshProUGUI>();
        if (headNodYesText == null)
            headNodYesText = GameObject.Find("HeadNodYes")?.GetComponent<TextMeshProUGUI>();
        if (headShakeNoText == null)
            headShakeNoText = GameObject.Find("HeadShakeNo")?.GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        m_Face.updated += OnFaceUpdated;
#if UNITY_IOS && !UNITY_EDITOR
        ARFaceManager faceManager = FindObjectOfType<ARFaceManager>();
        if (faceManager != null)
        {
            m_ARKitFaceSubsystem = faceManager.subsystem as ARKitFaceSubsystem;
            if (m_ARKitFaceSubsystem == null)
                Debug.LogWarning("ARKitFaceSubsystem is not available.");
        }
#endif
    }

    void OnDisable()
    {
        m_Face.updated -= OnFaceUpdated;
    }

    void OnFaceUpdated(ARFaceUpdatedEventArgs eventArgs)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (m_ARKitFaceSubsystem == null)
            return;

        // Initialize variables.
        float jawOpen = 0f;
        float blinkLeft = 0f;
        float blinkRight = 0f;
        float smileLeft = 0f;
        float smileRight = 0f;
        float browInnerUp = 0f;
        float browOuterUpTotal = 0f;
        int browOuterCount = 0;

        using (var blendShapes = m_ARKitFaceSubsystem.GetBlendShapeCoefficients(m_Face.trackableId, Allocator.Temp))
        {
            foreach (var coeff in blendShapes)
            {
                switch (coeff.blendShapeLocation)
                {
                    case ARKitBlendShapeLocation.JawOpen:
                        jawOpen = coeff.coefficient;
                        break;
                    case ARKitBlendShapeLocation.EyeBlinkLeft:
                        blinkLeft = coeff.coefficient;
                        break;
                    case ARKitBlendShapeLocation.EyeBlinkRight:
                        blinkRight = coeff.coefficient;
                        break;
                    case ARKitBlendShapeLocation.MouthSmileLeft:
                        smileLeft = coeff.coefficient;
                        break;
                    case ARKitBlendShapeLocation.MouthSmileRight:
                        smileRight = coeff.coefficient;
                        break;
                    case ARKitBlendShapeLocation.BrowInnerUp:
                        browInnerUp = coeff.coefficient;
                        break;
                    case ARKitBlendShapeLocation.BrowOuterUpLeft:
                        browOuterUpTotal += coeff.coefficient;
                        browOuterCount++;
                        break;
                    case ARKitBlendShapeLocation.BrowOuterUpRight:
                        browOuterUpTotal += coeff.coefficient;
                        browOuterCount++;
                        break;
                }
            }
        }

        // Compute average values.
        float eyebrowAverage = (browInnerUp + (browOuterCount > 0 ? browOuterUpTotal / browOuterCount : 0f)) / 2f;

        // Determine gestures using thresholds.
        bool isBlinking = (blinkLeft > 0.5f && blinkRight > 0.5f);
        bool isMouthOpen = (jawOpen > 0.3f);
        bool isSmiling = ((smileLeft + smileRight) / 2f > 0.5f);

        // Update blink counter.
        if (isBlinking && !prevBlinking)
        {
            blinkCount++;
            if (blinkCounterText != null)
                blinkCounterText.text = blinkCount.ToString();
        }
        prevBlinking = isBlinking;

        // Update mouth state.
        string currentMouthState = isMouthOpen ? "Open" : "Closed";
        if (currentMouthState != prevMouthState)
        {
            if (mouthStateText != null)
                mouthStateText.text = currentMouthState;
            prevMouthState = currentMouthState;
        }

        // Update head tilt (z-rotation).
        float headTiltZ = m_Face.transform.eulerAngles.z;
        if (headTiltZ > 180f)
            headTiltZ -= 360f;
        if (headTiltValueText != null)
            headTiltValueText.text = headTiltZ.ToString("F1");

        // Update smile counter.
        if (isSmiling && !prevSmiling)
        {
            smileCount++;
            if (smileCounterText != null)
                smileCounterText.text = smileCount.ToString();
        }
        prevSmiling = isSmiling;

        // Map head X position into viewport coordinates, then normalize to range -1 to 1.
        if (headPosXText != null)
        {
            // Use Camera.main to get the main camera.
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(m_Face.transform.position);
            float normalizedX = (viewportPos.x - 0.5f) * 2f;
            headPosXText.text = normalizedX.ToString("F2");
        }

        // Detect head nod ("Yes") using the face's x-rotation (pitch).
        float headPitch = m_Face.transform.eulerAngles.x;
        if (headPitch > 180f)
            headPitch -= 360f;
        if (headNodYesText != null)
            headNodYesText.text = (headPitch < -20f) ? "Yes" : "Neutral";

        // Detect head shake ("No") using the face's y-rotation (yaw).
        float headYaw = m_Face.transform.eulerAngles.y;
        if (headYaw > 180f)
            headYaw -= 360f;
        if (headShakeNoText != null)
            headShakeNoText.text = (Mathf.Abs(headYaw) > 5f) ? "No" : "Neutral";

        // Update eyebrow raise counter.
        // Increment counter when the average exceeds 0.40 and wasn't above threshold in the previous update.
        bool currentEyebrowRaised = (eyebrowAverage > 0.40f);
        if (currentEyebrowRaised && !prevEyebrowRaised)
        {
            eyebrowRaiseCount++;
            if (eyebrowValueText != null)
                eyebrowValueText.text = eyebrowRaiseCount.ToString();
        }
        prevEyebrowRaised = currentEyebrowRaised;
#endif
    }
}
