# TFG_UA_UnitreeGo2
Esta carpeta contiene todos los códigos fuente y los paquetes de ROS 2 Humble con los que se ha desarrollado todo el proyecto.
Los distintos archivos se han separado según la parte del sistema desde donde son lanzados.

- En la carpeta ArchivosRobot: se encuentran los paquetes que se lanzan en el terminal del robot. Estos son el paquete que lanza el nodo de imagen con
la cámara RealSense (realsense_pkg) y el paquete que lanza el nodo de gestión y publicación de comandos de movimiento al robot (robot_control_pkg).

- En la carpeta PC: se encuentran el resto de paquetes implementados en este proyecto. En el paquete dino_detector se ha creado un launch que incializa tanto el ROS TCP Endpoint como el detector de objetos GroundingDINO 
(se lanza con el comando ros2 launch dino_detector system.launch). También se encuentra el paquete whisper_interface para lanzar el nodo whisper_node. Por último se encuentra el paquete oficial del repositorio de 
ROS-TCP-Endpoint pero cuyo nodo se pude instanciar directamente desde el launch del paquete dino_detector.

-En la carpeta CodigosUnity: se encuentran los códigos fuentes de la escena de Unity encargados de realizar tanto las conexiones con ros, como la gestión de la imagen o los comandos de movimiento del robot. Con todos estos datos, se interpreta también el estado del HUD en tiempo real. También se encuentra la carpeta UnityProject que contiene los Assets, los Project Setting y los Packages de para la importación de la escena. Además se han enumerado los requisitos en el documento requirements.txt
