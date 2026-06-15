using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class RealSenseStereoSubscriber : MonoBehaviour
{
    public Renderer targetRenderer;

    private ROSConnection ros;

    private Texture2D rgbTexture;
    private Texture2D depthTexture;

    private byte[] latestRgbData;
    private byte[] latestDepthData;

    private bool newRgb = false;
    private bool newDepth = false;

    void Start()
    {
        rgbTexture = new Texture2D(2, 2);
        depthTexture = new Texture2D(640, 480, TextureFormat.RFloat, false);

        targetRenderer.material.SetTexture("_MainTex", rgbTexture);
        targetRenderer.material.SetTexture("_DepthTex", depthTexture);

        ros = ROSConnection.GetOrCreateInstance();
        
        //"/camera/color/image_raw/compressed"

        ros.Subscribe<CompressedImageMsg>(
            "/camera/dino/compressed",
            RgbCallback);

        ros.Subscribe<ImageMsg>(
            "/camera/depth/image_rect_raw",
            DepthCallback);
    }

    void RgbCallback(CompressedImageMsg msg)
    {
        latestRgbData = msg.data;
        newRgb = true;
    }

    void DepthCallback(ImageMsg msg)
    {
        latestDepthData = msg.data;
        newDepth = true;
    }

    void Update()
    {
        if (newRgb)
        {
            rgbTexture.LoadImage(latestRgbData);
            newRgb = false;
        }

        if (newDepth)
        {
            UpdateDepthTexture(latestDepthData);
            newDepth = false;
        }
    }

    void UpdateDepthTexture(byte[] depthData)
    {
        float[] depthFloat = new float[640 * 480];

        for (int i = 0; i < depthFloat.Length; i++)
        {
            ushort depthValue = System.BitConverter.ToUInt16(depthData, i * 2);
            depthFloat[i] = depthValue * 0.001f;
        }

        depthTexture.SetPixelData(depthFloat, 0);
        depthTexture.Apply();
    }
}
