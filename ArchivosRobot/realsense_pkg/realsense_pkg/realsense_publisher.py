#!/usr/bin/env python3

import rclpy
from rclpy.node import Node
from sensor_msgs.msg import Image, CompressedImage
from cv_bridge import CvBridge

import pyrealsense2 as rs
import numpy as np
import cv2


class RealsensePublisher(Node):

    def __init__(self):
        super().__init__('realsense_publisher')

        self.bridge = CvBridge()

        self.pub_raw = self.create_publisher(Image, '/camera/color/image_raw', 10)
        self.pub_compressed = self.create_publisher(CompressedImage, '/camera/color/image_raw/compressed', 10)

        self.width = 640
        self.height = 480
        self.fps = 15

        self.pipeline = rs.pipeline()
        config = rs.config()
        config.enable_stream(rs.stream.color, self.width, self.height, rs.format.bgr8, self.fps)

        self.pipeline.start(config)

        self.get_logger().info("RealSense publisher iniciado")

        # Timer en vez de while (mejor en ROS2)
        self.timer = self.create_timer(1.0 / self.fps, self.publish_frame)

    def publish_frame(self):

        frames = self.pipeline.poll_for_frames()
        if not frames:
            return

        color_frame = frames.get_color_frame()
        if not color_frame:
            return

        image = np.asanyarray(color_frame.get_data())

        msg = self.bridge.cv2_to_imgmsg(image, encoding="bgr8")
        msg.header.stamp = self.get_clock().now().to_msg()
        msg.header.frame_id = "camera"

        self.pub_raw.publish(msg)

        compressed_msg = CompressedImage()
        compressed_msg.header.stamp = msg.header.stamp
        compressed_msg.format = "jpeg"

        _, buffer = cv2.imencode('.jpg', image, [int(cv2.IMWRITE_JPEG_QUALITY), 80])
        compressed_msg.data = buffer.tobytes()

        self.pub_compressed.publish(compressed_msg)


def main(args=None):
    rclpy.init(args=args)

    node = RealsensePublisher()

    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass

    node.destroy_node()
    rclpy.shutdown()

