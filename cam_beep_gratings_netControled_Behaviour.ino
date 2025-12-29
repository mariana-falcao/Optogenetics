// Pin settings  
const int           camPin = 9;             // OC1A = Camera trigger (Timer 1)
const int           beepPin = 3;            // OC2B = Beep marker (Timer 2)

// Trial settings 
unsigned long       trialDuration = 7000;   // ms - total trial duration
unsigned long       gratingDuration = 5000; // ms - grating display time (0-5s)
unsigned long       beepStartTime_ms = 6000; // ms - when beep starts (at 6s)
unsigned int        fps = 30;               // camera FPS (input from PC)
unsigned long       startTime = 0;
bool                running = false;

// Timers values
unsigned int       prescalerT1 = 1024UL;  
unsigned int       prescalerT2 = 128UL;
float              dutyCycleT1 = 0.1;
float              dutyCycleT2 = 0.2;

// Beep settings
unsigned int        beepFreq = 700;
bool                beepRunning = false;
unsigned long       beepStartTime = 0;
const unsigned long beepDuration = 100; // ms

// Event flags
bool                gratingOn = false;
bool                gratingStopped = false;
bool                blackScreenSet = false;
bool                beepTriggered = false;

// Helper function: Timer1 prescaler 
void setTimer1Prescaler(unsigned int prescaler) {
  TCCR1B &= ~((1 << CS12) | (1 << CS11) | (1 << CS10));
  switch (prescaler) {
    case 1:    TCCR1B |= (1 << CS10); break;
    case 8:    TCCR1B |= (1 << CS11); break;
    case 64:   TCCR1B |= (1 << CS11) | (1 << CS10); break;
    case 256:  TCCR1B |= (1 << CS12); break;
    case 1024: TCCR1B |= (1 << CS12) | (1 << CS10); break;
    default:   TCCR1B |= (1 << CS11) | (1 << CS10); break; // default 64
  }
}

// Helper function: Parse command 
bool parseStartCommand(String cmd) {
  // Expect: "START 30 7 0.1 0.2 1024 128"
  // Format: START <fps> <duration_sec> <dutyCycleT1> <dutyCycleT2> <prescalerT1> <prescalerT2>
  cmd.trim();
  if (!cmd.startsWith("START")) return false;

  cmd = cmd.substring(5);
  cmd.trim();

  int tempFps = -1;
  int tempDuration = -1;
  float tempDutyCycleT1 = -1.0;
  float tempDutyCycleT2 = -1.0;
  unsigned int tempPrescalerT1 = prescalerT1;
  unsigned int tempPrescalerT2 = prescalerT2;

  String token;
  int spaceIdx;

  // fps
  spaceIdx = cmd.indexOf(' ');
  if (spaceIdx == -1) return false;
  token = cmd.substring(0, spaceIdx); token.trim();
  tempFps = token.toInt();
  cmd = cmd.substring(spaceIdx + 1); cmd.trim();

  // duration (seconds)
  spaceIdx = cmd.indexOf(' ');
  if (spaceIdx == -1) return false;
  token = cmd.substring(0, spaceIdx); token.trim();
  tempDuration = token.toInt();
  cmd = cmd.substring(spaceIdx + 1); cmd.trim();

  // dutyCycleT1
  spaceIdx = cmd.indexOf(' ');
  if (spaceIdx == -1) return false;
  token = cmd.substring(0, spaceIdx); token.trim();
  token.replace(',', '.');
  tempDutyCycleT1 = token.toFloat();
  cmd = cmd.substring(spaceIdx + 1); cmd.trim();

  // dutyCycleT2
  spaceIdx = cmd.indexOf(' ');
  if (spaceIdx == -1) return false;
  token = cmd.substring(0, spaceIdx); token.trim();
  token.replace(',', '.');
  tempDutyCycleT2 = token.toFloat();
  cmd = cmd.substring(spaceIdx + 1); cmd.trim();

  // prescalerT1
  spaceIdx = cmd.indexOf(' ');
  if (spaceIdx == -1) return false;
  token = cmd.substring(0, spaceIdx); token.trim();
  tempPrescalerT1 = (unsigned int)token.toInt();
  cmd = cmd.substring(spaceIdx + 1); cmd.trim();

  // prescalerT2
  token = cmd; token.trim();
  tempPrescalerT2 = (unsigned int)token.toInt();

  // validate
  if (tempFps > 0 && tempDuration > 0 && tempDutyCycleT1 >= 0.0 && tempDutyCycleT2 >= 0.0) {
    fps = (unsigned int)tempFps;
    trialDuration = (unsigned long)tempDuration * 1000UL;
    gratingDuration = 5000UL; // Fixed: grating shows for 5 seconds
    beepStartTime_ms = 6000UL; // Fixed: beep starts at 6 seconds
    dutyCycleT1 = tempDutyCycleT1;
    dutyCycleT2 = tempDutyCycleT2;
    prescalerT1 = tempPrescalerT1;
    prescalerT2 = tempPrescalerT2;
    return true;
  }
  return false;
}

// Camera PWM (Timer1, pin 9)
void startCameraPWM(unsigned int fps, unsigned int prescalerT1, float dutyCycleT1) {
  TCCR1A = 0;
  TCCR1B = 0;
  TCNT1 = 0;
  unsigned long top = (16000000UL / ((unsigned long)prescalerT1 * fps)) - 1;
  ICR1 = top;
  OCR1A = top * dutyCycleT1;  
  TCCR1A |= (1 << COM1A1);                
  TCCR1A |= (1 << WGM11);
  TCCR1B |= (1 << WGM12) | (1 << WGM13); 
  setTimer1Prescaler(prescalerT1);
}

void stopCameraPWM() {
  TCCR1A &= ~(1 << COM1A1);
  digitalWrite(camPin, LOW);
}

// Beep PWM (Timer2, pin 3)
void startBeepPWM(unsigned int beepFreq, unsigned int prescalerT2, float dutyCycleT2) {
  TCCR2A = 0;
  TCCR2B = 0;
  TCNT2 = 0;
  unsigned long top = (16000000UL / ((unsigned long)prescalerT2 * beepFreq)) - 1;
  if (top > 255) top = 255;
  OCR2A = (uint8_t)top;
  OCR2B = (uint8_t)(top * dutyCycleT2);
  TCCR2A |= (1 << COM2B1);                  
  TCCR2A |= (1 << WGM21) | (1 << WGM20);
  TCCR2B |= (1 << WGM22);
  switch (prescalerT2) {
    case 1:   TCCR2B |= (1 << CS20); break;
    case 8:   TCCR2B |= (1 << CS21); break;
    case 32:  TCCR2B |= (1 << CS21) | (1 << CS20); break;
    case 64:  TCCR2B |= (1 << CS22); break;
    case 128: TCCR2B |= (1 << CS22) | (1 << CS20); break;
    case 256: TCCR2B |= (1 << CS22) | (1 << CS21); break;
    case 1024:TCCR2B |= (1 << CS22) | (1 << CS21) | (1 << CS20); break;
    default:  TCCR2B |= (1 << CS22); break;
  }
  beepRunning = true;
}

void stopBeepPWM() {
  TCCR2A = 0;
  TCCR2B = 0;
  digitalWrite(beepPin, LOW);
  beepRunning = false;
}

// SETUP
void setup() {
  Serial.begin(115200);
  pinMode(camPin, OUTPUT);
  pinMode(beepPin, OUTPUT);
  digitalWrite(camPin, LOW);
  digitalWrite(beepPin, LOW);
  Serial.println("Arduino ready. Send: START <fps> <duration_sec> <duty1> <duty2> <prescaler1> <prescaler2>");
}

// MAIN LOOP
void loop() {
  // Check for serial commands
  if (Serial.available()) {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();

    if (cmd.startsWith("START") && !running) {
      if (parseStartCommand(cmd)) {
        // Reset flags
        gratingOn = false;
        gratingStopped = false;
        blackScreenSet = false;
        beepTriggered = false;
        
        // Start trial
        startTime = millis();
        running = true;
        
        // Start camera
        startCameraPWM(fps, prescalerT1, dutyCycleT1);
        
        // Send GRATING_ON command to PC
        Serial.println("GRATING_ON");
        gratingOn = true;
        
        Serial.println("TRIAL_START");
      } else {
        Serial.println("ERROR: Invalid command format");
      }
    }
  }
    
  // Trial state machine
  if (running) {
    unsigned long elapsed = millis() - startTime;
    
    // At 5 seconds: Stop grating, switch to black screen
    if (!gratingStopped && elapsed >= gratingDuration) {
      gratingStopped = true;
      
      // Turn off grating
      Serial.println("GRATING_OFF");
      gratingOn = false;
      
      // Set black screen
      Serial.println("BLACK_SCREEN");
      blackScreenSet = true;
    }
    
    // At 6 seconds: Start beep (black screen continues)
    if (!beepTriggered && elapsed >= beepStartTime_ms) {
      beepTriggered = true;
      beepStartTime = millis();
      startBeepPWM(beepFreq, prescalerT2, dutyCycleT2);
      Serial.println("BEEP_START");
    }
    
    // Stop beep after 100ms
    if (beepRunning && millis() - beepStartTime >= beepDuration) {
      stopBeepPWM();
      Serial.println("BEEP_STOP");
    }
    
    // At 7 seconds: End trial
    if (elapsed >= trialDuration) {
      stopCameraPWM();
      stopBeepPWM();
      running = false;
      Serial.println("TRIAL_END");
    }
  }
}