# BambuVideoStream
.Net app to push MQTT sensor data from Bambu Lab 3D printer to OBS for video streaming.


You will need OBS Studio with all the InputText sources defined. 

instructions for streaming the Bambu webcam with OBS are here: https://wiki.bambulab.com/en/software/bambu-studio/virtual-camera.

Those will get updated when a message is received from MQTT. 

I'm sure there is a better way to do this, but it works for now.

The utility is a .Net app and connects to both MQTT running on the X1 and OBS Studio's websocket connection. 

When a message is received from MQTT, it is parsed and the text input values are updated via the websocket connection to OBS.

Here is a sample recorded stream: https://www.youtube.com/watch?v=MW3osyXAUTI

You will need your printer's local IP address, password and serial number to connect to MQTT. 

Much of this work is derived from this thread: https://community.home-assistant.io/t/bambu-lab-x1-x1c-mqtt/489510.

