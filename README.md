To run behaviour experiments these are the files needed: 
 - cam_beep_gratings_netControled_Behaviour 
    (upload this file in arduino with the right baud rate)
 - Behaviour 
    (C# script that allows user inputs such as the delay time between gratings expusure and beep)


To run optogenetics experiments these are the files needed: 
 - cam_beep_netControled_3Timers
    (upload this file in arduino with the right baud rate)
 - cam_Control_userInputSAVE
    (C# script that allows user inputs such as the delay time between beam exposure and beep)


In this folder there are also other files such as: 
- cam_Control_userInput - same as cam_Control_userInputSAVE bur does not save the videos 
- cam_beep_netControled_diffTimers - only triggers the camera and the beeper
- gratingsTest/GratingTest - creates gratings for the desired amount of time given by the user input (this is included in the 'Behaviour' code)