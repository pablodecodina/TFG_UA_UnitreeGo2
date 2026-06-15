using UnityEngine;
using UnityEngine.XR;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class QuestWhisperPublisher : MonoBehaviour
{
    ROSConnection ros;
    
    public string topicActive = "/whisper/active";
    public string topicAudio = "/whisper/audio";
    
    private AudioClip micClip;
    private bool isRecording = false;
    private int sampleWindow = 128;
    private int sampleRate = 16000;
    private int duration = 5;
    
    public HUDController hud;
    
    void Start()
    {
    	ros = ROSConnection.GetOrCreateInstance();
    	
    	ros.RegisterPublisher<BoolMsg>(topicActive);
    	ros.RegisterPublisher<Float32MultiArrayMsg>(topicAudio);
    	
    	Debug.Log("Whisper Publisher Ready");
    }
    
    void Update()
    {
    	CheckTrigger();
    	
    	if (isRecording)
    	{
    		float level = GetMicLevel();
    		Debug.Log("MIC LEVEL: "+ level);
    		
    		if (level > 0.01f)
    		{
    			Debug.Log("VOICE DETECTED");
    		}
    		else
    		{
    			Debug.Log("SILENCE");
    		}
    	
    	}
    }
    
    void CheckTrigger()
    {
    	InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    	
		bool triggerPressed;
		
		if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed))
		{
			if (triggerPressed && !isRecording)
			{
				Debug.Log("PRESSEEEEEEED");
				StartRecording();
			}
			
			if (!triggerPressed && isRecording)
			{
				StopRecording();
			}
		}
    }
    
    
    void StartRecording()
    {
    	Debug.Log("START RECORDING");
    	
    	hud.SetMic(true);
    	
    	micClip = Microphone.Start(null, false, duration, sampleRate);
    	isRecording = true;
    	
    	ros.Publish(topicActive, new BoolMsg(true));
    }
    
    
    void StopRecording()
    {
    	Debug.Log("STOP RECORDING");
    	
    	hud.SetMic(false);
    	
    	Microphone.End(null);
    	isRecording = false;
    	
    	ros.Publish(topicActive, new BoolMsg(false));
    	
    	SendAudio();
    }
    
    
    void SendAudio()
    {
    	if (micClip == null)
    	{
    		Debug.Log("NO AUDIO CLIP");
    		return;
    	}	
    	
    	float[] data = new float[micClip.samples];
    	micClip.GetData(data, 0);
    	
    	Float32MultiArrayMsg msg = new Float32MultiArrayMsg();
    	msg.data = data;
    	
    	ros.Publish(topicAudio, msg);
    	
    	Debug.Log("Audio Enviado a ROS" + data.Length);	
    }
    
    float GetMicLevel()
    {
    	if (micClip == null) return 0;
    	
    	int micPosition = Microphone.GetPosition(null) - sampleWindow + 1;
    	if (micPosition < 0) return 0;
    	
    	float[] samples = new float[sampleWindow];
    	micClip.GetData(samples, micPosition);
    	float max = 0;
    	for (int i = 0; i < sampleWindow; i++)
    	{
    		float abs = Mathf.Abs(samples[i]);
    		if(abs>max)
    		{
    			max = abs;
    		}
    	}
    	return max;    
    }
    
}
