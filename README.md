# BambuVideoStream
.Net app to push MQTT sensor data from Bambu Lab 3D printer to OBS for video streaming.

> Improvements have been made in this fork to make the setup more streamlined, reliable, and configurable without needing experience in .NET programming.

## Prerequisites
Instructions for streaming the Bambu webcam with OBS are here: https://wiki.bambulab.com/en/software/bambu-studio/virtual-camera. Before you run this app, you should have the Bambu Studio software and OBS Studio installed and running.

You will need your printer's local IP address, password, and serial number to connect to MQTT. All of this can be found on the printer's LCD screen. The password and IP address can be found in the WiFi settings, and the serial number can be found in the printer's information page.

### First time setup
1. Download the latest release from the Releases page.		
1. Extract the zip file to a folder on your computer.
1. Open the `appsettings.json` file in a text editor.
1. Replace all values with `<brackets>` in the `BambuSettings` and `ObsSettings` sections with the appropriate values.
    1. NOTE: The `ObsSettings:PathToSDP` value shouldn't need to be changed for Windows users. The app will attempt to automatically find the default .sdp location in your user directory.
1. (Configure any other control settings as you prefer.)

## Usage
1. Open Bambu Studio, navigate to the Device tab, and start streaming. ("Go live")
1. Open OBS studio, and make sure the [Websocket server is enabled](https://obsproject.com/kb/remote-control-guide).
1. Run the BambuVideoStream app!

## Credits

Much of this work is derived from this thread: https://community.home-assistant.io/t/bambu-lab-x1-x1c-mqtt/489510.