# Experiment Control Code

This repository contains Arduino and C# scripts used to run behavioral and optogenetic experiments involving camera triggering, auditory cues, and visual stimulation.

---

## Behaviour Experiments

### Required Files

- **cam_beep_gratings_netControled_Behaviour**  
  Arduino script — upload to Arduino (ensure correct baud rate)

- **Behaviour**  
  C# script — provides user inputs such as delay between grating exposure and beep

---

## Optogenetics Experiments

### Required Files

- **cam_beep_netControled_3Timers**  
  Arduino script — upload to Arduino (ensure correct baud rate)

- **cam_Control_userInputSAVE**  
  C# script — allows delay control between beam exposure and beep, and saves video recordings

---

## Additional Files Included

- **cam_Control_userInput**  
  Same functionality as `cam_Control_userInputSAVE` but **does not save videos**

- **cam_beep_netControled_diffTimers**  
  Only triggers the camera and beeper

- **gratingsTest / GratingTest**  
  Creates gratings for a user-specified duration *(included inside the `Behaviour` code)*

- **CustomCameraOverlay**  
  Allows to position fish head to target always the same region of the brain with the light
