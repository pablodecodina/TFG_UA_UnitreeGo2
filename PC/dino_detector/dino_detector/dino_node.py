import rclpy
from rclpy.node import Node
from sensor_msgs.msg import Image, CompressedImage
from cv_bridge import CvBridge
from std_msgs.msg import String, Bool

import cv2
import torch
import numpy as np
import time
import threading


from groundingdino.util.inference import load_model, predict, annotate

from groundingdino.datasets.transforms import Compose, RandomResize, ToTensor, Normalize

class DinoNode(Node):

	def __init__(self):
		super().__init__('dino_node')
		
		self.bridge = CvBridge()
		
		self.subscription = self.create_subscription(
			CompressedImage,
			'/camera/color/image_raw/compressed',
			self.image_callback,
			10)
			
		self.publisher = self.create_publisher(
			Image,
			'camera/dino/image',
			10)
			
		self.publisher_compressed = self.create_publisher(
			CompressedImage,
			'camera/dino/compressed',
			10)
			
		self.subscription_prompt = self.create_subscription(
			String,
			'/dino/prompt',
			self.prompt_callback,
			10)
			
		self.subscription_status = self.create_subscription(
			Bool,
			'/dino/status',
			self.status_callback,
			10)
			
		self.dino_active = False
		
		self.get_logger().info("Loading GroundingDINO...")
		
		#Ruta del GROUNDING DINO
		self.model = load_model(
			"/home/pablo/ai_models/GroundingDINO/groundingdino/config/GroundingDINO_SwinT_OGC.py",
			"/home/pablo/ai_models/GroundingDINO/weights/groundingdino_swint_ogc.pth"
		)
		
		# Prompt para la detección
		self.text_prompt = "Blue bottle" 
		
		self.get_logger().info("DINO Ready.")
		self.get_logger().info("Type 'dino' in terminal to toggle detector")
		
		self.frame_skip = 0
		
		threading.Thread(target=self.listen_terminal, daemon=True).start()
		
		
		
	def listen_terminal(self):
		
		while True:
			cmd = input()
			
			if cmd.strip().lower() == "dino":
				self.dino_active = not self.dino_active
				if self.dino_active:
					print("DINO ACTIVATED")
				else:
					print("DINO DEACTIVATED")
		
		
	def image_callback(self, msg):
		self.frame_skip += 1
		start = time.time()

		"""
		frame_bgr = self.bridge.imgmsg_to_cv2(msg, desired_encoding='bgr8')
		"""
		np_arr = np.frombuffer(msg.data, np.uint8)
		frame_bgr = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
		frame_bgr = cv2.resize(frame_bgr, (480, 360))


		if not self.dino_active:
			annotated_bgr = frame_bgr
			
		else:
		
			frame_rgb = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
		
			image_tensor = torch.from_numpy(frame_rgb).float()
			image_tensor = image_tensor.permute(2, 0, 1) 
			
			device = next(self.model.parameters()).device
			image_tensor = image_tensor.to(device)
		
			boxes, logits, phrases = predict(
				model=self.model,
				image=image_tensor,
				caption=self.text_prompt,
				box_threshold=0.45,
				text_threshold=0.25
			)	

			annotated_rgb = annotate(
				image_source=frame_rgb,
				boxes=boxes,
				logits=logits,
				phrases=phrases
			)

			
			annotated_bgr = annotated_rgb

		ros_image = self.bridge.cv2_to_imgmsg(annotated_bgr, encoding='bgr8')
		self.publisher.publish(ros_image)
		
		# Crear mensaje comprimido
		msg_compressed = CompressedImage()
		msg_compressed.header = msg.header
		msg_compressed.format = "jpeg"
		
		success, encoded_image = cv2.imencode(
			'.jpg',
			annotated_bgr,
			[int(cv2.IMWRITE_JPEG_QUALITY), 60]
		)
		
		if success:
			msg_compressed.data = encoded_image.tobytes()
			self.publisher_compressed.publish(msg_compressed)
		
		end = time.time()
		#print("Tiempo procesamiento: ", end - start)
		
	def prompt_callback(self, msg):
	
		self.text_prompt = msg.data
		
		self.get_logger().info(f"New prompt received: {self.text_prompt}")
		
	def status_callback(self, msg):
		
		self.dino_active = msg.data
		
		self.get_logger().info(f"Status Dino: {self.dino_active}")
		

def main(args=None):
	rclpy.init(args=args)
	node = DinoNode()
	rclpy.spin(node)
	node.destroy_node()
	rclpy.shutdown()
	
	
	
if __name__ == '__main__':
	main()
