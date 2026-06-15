import rclpy
from rclpy.node import Node
from geometry_msgs.msg import Twist
from std_msgs.msg import Float32, String
from unitree_api.msg import Request, Response # type: ignore
from rclpy.qos import QoSProfile, HistoryPolicy, DurabilityPolicy
import json

class CmdVelToRequestBridge(Node):
    def __init__(self):
        super().__init__('cmd_vel_to_request_bridge')

        # Configuración del perfil de calidad de servicio (QoS)
        qos_profile = QoSProfile(
            history=HistoryPolicy.KEEP_LAST,
            depth=10,
            durability=DurabilityPolicy.VOLATILE
        )

        # Publicador y suscriptor
        self.publisher_ = self.create_publisher(Request, '/api/sport/request', qos_profile)
        self.subscription = self.create_subscription(Twist, '/robot/cmd_vel',  self.cmd_vel_callback, qos_profile)
       # self.pitch_sub = self.create_subscription(Float32, '/robot/cmd_pitch', self.pitch_callback, qos_profile)
        self.command_sub = self.create_subscription(String, '/robot/sit_stand', self.command_callback, qos_profile)
        self.get_logger().info('Nodo /cmd_vel → /api/sport/request activo')

    def cmd_vel_callback(self, msg):

        # Extracción de los comandos de velocidad lineal y angular
        x = round(msg.linear.x, 2)
        y = round(msg.linear.y, 2)
        z = round(msg.angular.z, 2)

        # Creación del mensaje tipo Request con formato JSON
        request_msg = Request()
        request_msg.parameter = json.dumps({
            "x": x,
            "y": y,
            "z": z
        })

        # API ID correspondiente al control de movimiento
        request_msg.header.identity.api_id = 1008

        # Publicación del mensaje Request
        self.publisher_.publish(request_msg)
        
    """
    def pitch_callback(self, msg):
        pitch = round(msg.data, 2)

        request_msg = Request()
        request_msg.parameter = json.dumps({
            "pitch": pitch
         })
        
        request_msg.header.identity.api_id = 1007
        
        self.publisher_.publish(request_msg)
       
    """
    def command_callback(self, msg):
        cmd = msg.data.lower()
        request_msg = Request()
        
        if cmd == "stand":
        	request_msg.header.identity.api_id = 1004
        elif cmd == "down":
        	request_msg.header.identity.api_id = 1005
        else:
        	return
        	
        request_msg.parameter = json.dumps({})
        self.publisher_.publish(request_msg)
            
def main(args=None):
    rclpy.init(args=args)
    node = CmdVelToRequestBridge()
    rclpy.spin(node)
    node.destroy_node()
    rclpy.shutdown()

if __name__ == '__main__':
    main()
