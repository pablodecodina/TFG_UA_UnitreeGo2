using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public TextMeshProUGUI micText;
    public TextMeshProUGUI micText2;
    public TextMeshProUGUI dinoStatusText;
    public TextMeshProUGUI dinoStatusText2;
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI whisperText;
    
    public TextMeshProUGUI standText;
    
    public TextMeshProUGUI velocityTextLinear;
    public TextMeshProUGUI velocityTextAngular;
    
    [Header("Direction Arrows")]
    public Image arrowForward;
    public Image arrowBackward;
    public Image arrowLeft;
    public Image arrowRight;
    
    public void SetMic(bool active)
    {
    	micText.text = active ? "MIC ON" : " ";
    	micText2.text = active ? " " : "MIC OFF";
	}
	
	public void SetDino(bool active)
	{
		dinoStatusText.text = active ? "DINO ON" : " ";
		dinoStatusText2.text = active ? " " : "DINO OFF";
	}
	
	public void SetPrompt(string prompt)
	{
		promptText.text = "Prompt: " + prompt;
	}    
	
	public void SetWhisper(string text)
	{
		whisperText.text = text;
	}
	
	public void SetVelocity(float linearX, float angularZ)
	{
		velocityTextLinear.text = $"X={linearX:F2}";
		velocityTextAngular.text = $"Z={angularZ:F2}";
	}
	
	public void SetStand(string stand_status)
	{
		if (stand_status == "down")
		{
			standText.text = "STAND DOWN";
		}
		else if (stand_status == "stand")
		{
			standText.text = "STAND UP";
		}
	}
	
	public void UpdateArrows(float linearX, float angularZ){
		float threshold = 0.05f;
		Color active = Color.green;
		Color inactive = new Color(1,1,1,0.2f);
		
		arrowForward.color = (linearX > threshold) ? active : inactive;
		arrowBackward.color = (linearX < -threshold) ? active : inactive;
		arrowLeft.color = (angularZ > threshold) ? active : inactive;
		arrowRight.color = (angularZ < -threshold) ? active : inactive;
	}
}
