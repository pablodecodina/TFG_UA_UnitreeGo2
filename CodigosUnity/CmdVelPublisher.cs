using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using UnityEngine.InputSystem;
using RosMessageTypes.Std;
using System.Collections;

public class CmdVelPublisherVR : MonoBehaviour
{
    ROSConnection ros;
    
    public HUDController hud;

    [Header("Topic config")]
    public string topicName = "/robot/cmd_vel";
    public float maxLinear = 0.4f;
    public float maxAngular = 1.2f;

    [Header("Input Actions")]
    public InputActionProperty moveLinear;    // Joystick izquierdo
    public InputActionProperty rotateAngular; // Joystick derecho
    public InputActionProperty enableHeadRotation; // Boton A
    public InputActionProperty toggleSitStand; // Boton X

    [Header("Head rotation config")]
    public float headYawSensitivity = 5.0f;   // Sensibilidad del giro de cabeza
    public float headYawThreshold = 0.01f;      // Umbral mínimo de giro de cabeza (grados)

    private float previousYaw = 0.0f;
    private float timeSinceStart = 0f;
    private float delayBeforePublishing = 1.0f;

    private float[] angularZBuffer = new float[8];  // Buffer de persistencia
    private int bufferIndex = 0;
    private bool wasMoving = false;
    
    public string pitchTopic = "/robot/cmd_pitch";
    public string commandTopic = "/robot/sit_stand";
    
    private bool isStanding = true;
    private bool buttonPressedLastFrame = false;
    private bool canToggle = true;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TwistMsg>(topicName);
        ros.RegisterPublisher<Float32Msg>(pitchTopic);
        ros.RegisterPublisher<StringMsg>(commandTopic);

        if (Camera.main != null)
        {
            previousYaw = Camera.main.transform.eulerAngles.y;
        }
    }

    void Update()
    {
        timeSinceStart += Time.deltaTime;
        if (timeSinceStart < delayBeforePublishing)
            return;

        // Leer inputs de movimiento
        Vector2 linearInput = moveLinear.action.ReadValue<Vector2>();
        Vector2 angularInput = rotateAngular.action.ReadValue<Vector2>();
        

        
        // SIT AND STAND
        

        bool buttonPressed = toggleSitStand.action.ReadValue<float>() > 0.5f;
        
        // spam avoidance
        
        if (buttonPressed && !buttonPressedLastFrame && canToggle)
        {
        	canToggle = false;
        	StringMsg cmd = new StringMsg();
        	if (isStanding)
        	{
        		cmd.data = "down";
        		isStanding = false;
        	}
        	else
        	{
        		cmd.data = "stand";
        		isStanding = true;
        	}
        	
        	ros.Publish(commandTopic, cmd);
        	StartCoroutine(BlockInput());
        }
        
        buttonPressedLastFrame = buttonPressed;
        
        if (!isStanding){
        	return;
        }
        
        
                
        // PITCH
        float pitchInput = angularInput.y;
        
        float pitchDeadZone = 0.1f;
        if (Mathf.Abs(pitchInput) < pitchDeadZone)
        	pitchInput = 0f;
        	
        float pitchValue = Mathf.Clamp(pitchInput, -1f, 1f) * 0.5f; // CAMBIAR A 0.75f SEGUN PRUEBAS
        
        Float32Msg pitchMsg = new Float32Msg(pitchValue);
        ros.Publish(pitchTopic, pitchMsg);
        
        
        // MOVIMIENTOS
        
        bool headControlActive = enableHeadRotation.action.ReadValue<float>() > 0.5f;

        float linearX = linearInput.y * maxLinear;
        float linearY = -linearInput.x * maxLinear;

        // Aplicar umbral para evitar movimientos laterales indeseados
        float lateralDeadZone = 0.1f;
        if (Mathf.Abs(linearY) < lateralDeadZone)
            linearY = 0f;

        // Obtener yaw de la cabeza
        float currentYaw = Camera.main.transform.eulerAngles.y;
        float deltaYaw = Mathf.DeltaAngle(previousYaw, currentYaw);
        previousYaw = currentYaw;

        // Seleccionar fuente de angular.z
        float angularZ = 0.0f;
        float joystickThreshold = 0.05f;

        if (Mathf.Abs(angularInput.x) > joystickThreshold)
        {
            float amplified = Mathf.Clamp(angularInput.x * 1.5f, -1f, 1f);
            angularZ = -amplified * maxAngular;
        }
        else if (headControlActive && Mathf.Abs(deltaYaw) > headYawThreshold)
        {
            angularZ = -deltaYaw * headYawSensitivity;
        }

        // Guardar angularZ en el buffer circular
        angularZBuffer[bufferIndex] = angularZ;
        bufferIndex = (bufferIndex + 1) % angularZBuffer.Length;

        // Usar el mayor valor reciente
        float smoothedAngularZ = 0f;
        foreach (float val in angularZBuffer)
        {
            if (Mathf.Abs(val) > Mathf.Abs(smoothedAngularZ))
                smoothedAngularZ = val;
        }

        // Determinar si hay movimiento significativo
        bool isCurrentlyMoving =
            Mathf.Abs(linearX) > 0.01f ||
            Mathf.Abs(linearY) > 0.01f ||
            Mathf.Abs(smoothedAngularZ) > 0.01f;

        if (isCurrentlyMoving)
        {
            TwistMsg twist = new TwistMsg
            {
                linear = new Vector3Msg(linearX, linearY, 0),
                angular = new Vector3Msg(0, 0, smoothedAngularZ)
            };
            
            hud.SetVelocity(linearX, smoothedAngularZ);
            hud.UpdateArrows(linearX, smoothedAngularZ);
            
            ros.Publish(topicName, twist);
            wasMoving = true;
        }
        else if (wasMoving)
        {
            TwistMsg twist = new TwistMsg
            {
                linear = new Vector3Msg(0, 0, 0),
                angular = new Vector3Msg(0, 0, 0)
            };
            
            hud.SetVelocity(0, 0);
            hud.UpdateArrows(0, 0);

            ros.Publish(topicName, twist);
            wasMoving = false;
        }
        Debug.Log("Linear: " + moveLinear.action.ReadValue<Vector2>());
        Debug.Log("Angular: " + rotateAngular.action.ReadValue<Vector2>());
    }
    
    void OnEnable()
    {
    	moveLinear.action.Enable();
    	rotateAngular.action.Enable();
    	enableHeadRotation.action.Enable();
    	toggleSitStand.action.Enable();
    }
    
    void OnDisable()
    {
    	moveLinear.action.Disable();
    	rotateAngular.action.Disable();
    	enableHeadRotation.action.Disable();
    	toggleSitStand.action.Disable();
    }
    
    IEnumerator BlockInput()
    {
    	yield return new WaitForSeconds(2.0f);
    	canToggle = true;
    }
}
