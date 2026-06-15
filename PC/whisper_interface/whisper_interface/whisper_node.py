import rclpy
from rclpy.node import Node
from std_msgs.msg import String, Bool, Float32MultiArray

import numpy as np
from faster_whisper import WhisperModel
import re


class WhisperNode(Node):

    def __init__(self):
        super().__init__('whisper_node')

        self.publisher_prompt = self.create_publisher(
            String,
            '/dino/prompt',
            10
        )
        
        self.publisher_status = self.create_publisher(
        	Bool,
        	'/dino/status',
        	10
        )
        
        self.publisher_text = self.create_publisher(
        	String,
        	'/whisper/text',
        	10
        )       
        
        self.create_subscription(
        	Float32MultiArray,
        	'/whisper/audio',
        	self.audio_callback,
        	10        
        
        )

        self.get_logger().info("Loading Whisper model...")

        self.model = WhisperModel("medium", compute_type="float16")

        self.get_logger().info("Whisper ready.")
        
        self.object_map = {
        	"botella": "bottle",
        	"botellas": "bottles",
        	"persona": "person",
        	"personas": "person",
        	"silla": "chair",
        	"sillas": "chairs",
        	"mesa": "table",
        	"mesas": "tables",
        	"televisión": "TV",
        	"ordenador": "computer",
        	"teclado": "keyboard",
        	"raton": "mouse",
        	"perro": "dog",
        	"gato": "cat",
        	"león": "lion",
        	"coche": "car",
        	"moto": "motorbike",
        	"autobús": "bus",
        	"avión": "airplane",
        	"manzana": "apple",
        	"manzanas": "apples",
        	"pelota": "ball"
        	
        }
        
        self.color_map = {
        	"rojo": "red",
        	"roja": "red",
        	"rojos": "red",
        	"rojas": "red",
        	"azul": "blue",
        	"azules": "blue",
        	"verde": "green",
        	"verdes": "green",
        	"amarillo": "yellow",
        	"amarilla": "yellow",
        	"amarillos": "yellow",
        	"amarillas": "yellow",
        	"negro": "black",
        	"negra": "black",
        	"negros": "black",
        	"negras": "black",
        	"blanco": "white",
        	"blanca": "white",
        	"blancos": "white",
        	"blancas": "white",
        	"gris": "grey",
        	"grises": "grey"
        }
        
        self.action_map = {
        	"busca": "search",
        	"buscar": "search",
        	"busques": "search",
        	"encuentra": "search",
        	"detecta": "search"
        }
        
    def parse_command(self, text):
        text = text.lower()
        
        words = re.findall(r'\w+', text) 
        result = {
            "action": None,
            "object": None,
            "color": None,
            "negation": False,
        }
        
        for w in words:
            if w in self.action_map:
                result["action"] = self.action_map[w]
            if w in self.object_map:
                result["object"] = self.object_map[w] 
            if w in self.color_map:
                result["color"] = self.color_map[w] 
            if w in ["no"]:
                result["negation"] = True
                
        return result
        
    def interpret_command(self, text):
	
        text = text.lower()
        
        words = text.split()

        if ("activar" in words or "activa" in words) and ("dino" in words or "detector" in words or "dino." or "detector."):
            msg = Bool()
            msg.data = True
            self.publisher_status.publish(msg)

            self.get_logger().info("DINO ACTIVADO")
            return

        if ("desactivar" in words or "desactiva" in words) and ("dino" in words or "detector" in words or "dino." or "detector."):
            msg = Bool()
            msg.data = False
            self.publisher_status.publish(msg)

            self.get_logger().info("DINO DESACTIVADO")
            return	
            
        parsed = self.parse_command(text)
        
        print("Parsed: ", parsed)
        
        if parsed["negation"]:
        	msg = Bool()
        	msg.data = False
        	self.publisher_status.publish(msg)
        	self.get_logger().info("DINO DESACTIVADO")
        	return
        	
        if parsed["object"]:
        	prompt = parsed["object"]
        	if parsed["color"]:
        		prompt = f"{parsed['color']} {prompt}"
        	
        	msg = String()
        	msg.data = prompt
        	self.publisher_prompt.publish(msg)
        	self.get_logger().info(f"Buscando: {prompt}")
        	return
        
        self.get_logger().info("No se ha reconocido ningún objeto")        	
    

    def audio_callback(self, msg):
        """
        input("\nPress ENTER and speak...")

        self.get_logger().info("Listening...")

        audio = sd.rec(
            int(self.duration * self.sample_rate),
            samplerate=self.sample_rate,
            channels=1,
            dtype='float32'
        )

        sd.wait()

        audio = np.squeeze(audio)

        self.get_logger().info("Transcribing...")

        segments, info = self.model.transcribe(audio)

        text = ""

        for segment in segments:
            text += segment.text

        text = text.strip()

        print("Detected:", text)

        self.interpret_command(text)
        """

        self.get_logger().info(f"Audio Recibido: {len(msg.data)} samples")
        
        audio = np.array(msg.data, dtype=np.float32)
        
        segments, _ = self.model.transcribe(audio)
        
        text = ""
        
        for s in segments:
            text += s.text
            
        text = text.strip()
        
        msg = String()
        msg.data = text
        self.publisher_text.publish(msg)
        
        print("Detected: ", text)
        
        self.interpret_command(text)

def main(args=None):

    rclpy.init(args=args)

    node = WhisperNode()

    rclpy.spin(node)

    node.destroy_node()
    rclpy.shutdown()


if __name__ == '__main__':
    main()
