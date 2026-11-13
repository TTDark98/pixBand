using UnityEngine;

public class DualRigManager : MonoBehaviour
{
    public enum ActiveRigMode
    {
        FirstPersonWASD,
        XRSimulatedVR
    }

    [Header("Rig References")]
    [SerializeField] private GameObject firstPersonRig;
    [SerializeField] private GameObject xrOriginRig;
    [SerializeField] private GameObject xrDeviceSimulator;

    [Header("Configuration")]
    [SerializeField] private ActiveRigMode activeMode = ActiveRigMode.FirstPersonWASD;
    [SerializeField] private KeyCode switchKey = KeyCode.Tab;

    private AudioListener fpListener;
    private AudioListener xrListener;

    public ActiveRigMode CurrentMode => activeMode;
    public KeyCode SwitchKey => switchKey;

    private void Awake()
    {
        if (xrDeviceSimulator == null)
        {
            xrDeviceSimulator = GameObject.Find("XR Device Simulator");
        }

        CacheListeners();
        ApplyActiveMode(activeMode);
    }

    private void CacheListeners()
    {
        if (firstPersonRig != null)
            fpListener = firstPersonRig.GetComponentInChildren<AudioListener>(true);

        if (xrOriginRig != null)
            xrListener = xrOriginRig.GetComponentInChildren<AudioListener>(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            ActiveRigMode nextMode = (activeMode == ActiveRigMode.FirstPersonWASD)
                ? ActiveRigMode.XRSimulatedVR
                : ActiveRigMode.FirstPersonWASD;
            ApplyActiveMode(nextMode);
        }
    }

    public void ApplyActiveMode(ActiveRigMode mode)
    {
        bool wasFP = (activeMode == ActiveRigMode.FirstPersonWASD);
        activeMode = mode;
        bool isFP = (activeMode == ActiveRigMode.FirstPersonWASD);

        // Seamless position & rotation transfer between rigs
        if (wasFP && !isFP && firstPersonRig != null && xrOriginRig != null)
        {
            xrOriginRig.transform.position = firstPersonRig.transform.position;
            xrOriginRig.transform.rotation = firstPersonRig.transform.rotation;
        }
        else if (!wasFP && isFP && firstPersonRig != null && xrOriginRig != null)
        {
            firstPersonRig.transform.position = xrOriginRig.transform.position;
            firstPersonRig.transform.rotation = xrOriginRig.transform.rotation;
        }

        if (firstPersonRig != null)
        {
            firstPersonRig.SetActive(isFP);
            if (fpListener != null) fpListener.enabled = isFP;
        }

        if (xrOriginRig != null)
        {
            xrOriginRig.SetActive(!isFP);
            if (xrListener != null) xrListener.enabled = !isFP;

            Camera xrCam = xrOriginRig.GetComponentInChildren<Camera>(true);
            if (xrCam != null)
            {
                xrCam.clearFlags = CameraClearFlags.SolidColor;
                xrCam.backgroundColor = new Color(0.03f, 0.04f, 0.06f);
            }
        }

        // Only show and run XR Device Simulator when in XR Simulated Mode
        if (xrDeviceSimulator != null)
        {
            xrDeviceSimulator.SetActive(!isFP);
        }

        Debug.Log($"[DualRigManager] Switched active rig to: {activeMode}");
    }
}
