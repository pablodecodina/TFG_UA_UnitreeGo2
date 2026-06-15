using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class HUDROS : MonoBehaviour
{
    ROSConnection ros;
    public HUDController hud;
    
    void Start()
    {
    	ros = ROSConnection.GetOrCreateInstance();
    	
    	ros.Subscribe<BoolMsg>("/dino/status", DinoStatusCallback);
    	ros.Subscribe<StringMsg>("/dino/prompt", PromptCallback);
    	ros.Subscribe<StringMsg>("/whisper/text", WhisperCallback);
    	ros.Subscribe<StringMsg>("/robot/sit_stand", StandCallback);
    }
    
    void DinoStatusCallback(BoolMsg msg)
    {
    	hud.SetDino(msg.data);
    }
    
    void PromptCallback(StringMsg msg)
    {
    	hud.SetPrompt(msg.data);
    }
    
    void WhisperCallback(StringMsg msg)
    {
    	hud.SetWhisper(msg.data);
    }
    
    void StandCallback(StringMsg msg)
    {
    	hud.SetStand(msg.data);
    }
}
