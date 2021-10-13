
GoPro C# Development
====================


Build
-----

Visual Studio is required to run the solution. Visit https://visualstudio.microsoft.com/downloads/ to download.  
The target .NET framework is v4.7.2  


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


Known Issue
-----------

* Sometimes GATT notify has exception  
![image](https://user-images.githubusercontent.com/4526937/137074041-2d261f70-2086-4225-ad67-75c0c7bf16f8.png)  
Solve by restart and retry connect  
