
GoPro C# Development
====================


Features
--------

BLE, WIFI and USB-C connection, control of GoPro HERO 9/10. To use USB-C a driver is required.
Depends on Open GoPro API. https://gopro.github.io/OpenGoPro/

For USB-C connection, in GoPro 9, need to use "GoPro Connect".  
In GoPro 10, no need to change anything.  


Build
-----

Visual Studio is required to run the solution. Visit https://visualstudio.microsoft.com/downloads/ to download.  
The target .NET framework is v4.7.2  

[![.NET Core Desktop](https://github.com/lhypds/gopro-csharp-dev/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/lhypds/gopro-csharp-dev/actions/workflows/dotnet-desktop.yml)


Requirements
------------

GoPro camera must be paired before any other operations will succeed. Put the camera in pairing mode before attempting pairing with the app.  


Usage
-----

1. Open and run the demo in Visual Studio to show the GUI  
2. `Scan` for GoPro devices  
3. `Pair` to the discovered device that is not `GoPro Cam`. In the .gif below, this is `GoPro 0456` (Only needs to be done once, or if camera is factory reset)  
4. After pairing is successful, `connect` to the same GoPro device  
5. Now use any of the GUI buttons to read WiFi info, enable WiFi AP, set shutter, etc.  


Screenshots
-----------

![image](https://user-images.githubusercontent.com/4526937/169522539-c09d4bc8-018f-4ce8-8809-2faeed2ed3ff.png)


Known Issues
------------

* Bluetooth connection issue happen randomly  
`Command Response Exception: Operation aborted (Exception from HRESULT: 0x80004004 (E_ABORT))`  
Solve: restart application  


Trouble Shooting
----------------

* USB-C cable connected but cannot ping device  
Install driver GoPro.Webcam.msi  
Download https://community.gopro.com/s/article/GoPro-Webcam?language=en_US#GetStartedPC  
