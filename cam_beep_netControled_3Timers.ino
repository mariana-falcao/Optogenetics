// =====================
// Pin settings
// =====================
const int camPin    = 9;   // OC1A (Timer1) camera trigger
const int beepPin   = 3;   // OC2B (Timer2) beep
const int timer0Pin = 4;   // regular digital pin (Timer0: timed via millis)

// =====================
// Trial settings
// =====================
unsigned int  fps = 700;
unsigned long trialInterval = 0;   // TOTAL trial duration (ms)
unsigned long startTime = 0;
bool          running = false;

// =====================
// Timer parameters
// =====================
unsigned int prescalerT1 = 1024;
unsigned int prescalerT2 = 128;
float dutyCycleT1 = 0.1;
float dutyCycleT2 = 0.2;

// =====================
// Beep settings
// =====================
unsigned int beepFreq = 700;
bool beepRunning = false;
unsigned long beepStartTime = 0;
const unsigned long beepDuration = 100; // ms

// =====================
// Event flags
// =====================
bool pinHighTriggered = false;
bool pinLowTriggered  = false;
bool beepTriggered    = false;

// =====================
// Timestamp helper
// =====================
void sendTimestamp(const char* label, unsigned long elapsed) {
  Serial.print(label);
  Serial.print(" ");
  Serial.println(elapsed);
}

// =====================
// Timer1 prescaler
// =====================
void setTimer1Prescaler(unsigned int prescaler) {
  TCCR1B &= ~((1 << CS12) | (1 << CS11) | (1 << CS10));
  switch (prescaler) {
    case 1:    TCCR1B |= (1 << CS10); break;
    case 8:    TCCR1B |= (1 << CS11); break;
    case 64:   TCCR1B |= (1 << CS11) | (1 << CS10); break;
    case 256:  TCCR1B |= (1 << CS12); break;
    case 1024: TCCR1B |= (1 << CS12) | (1 << CS10); break;
  }
}

// =====================
// Parse START command
// =====================
bool parseStartCommand(String cmd) {
  // START <fps> <duration> <duty1> <duty2> <presc1> <presc2>
  cmd.trim();
  if (!cmd.startsWith("START")) return false;

  cmd = cmd.substring(5);
  cmd.trim();

  int   tempFps = -1;
  int   tempDuration = -1; // delay between pin LOW and beep (seconds)
  float tempDuty1 = -1;
  float tempDuty2 = -1;
  unsigned int tempPresc1 = prescalerT1;
  unsigned int tempPresc2 = prescalerT2;

  String token;
  int idx;

  idx = cmd.indexOf(' ');
  token = cmd.substring(0, idx); tempFps = token.toInt();
  cmd = cmd.substring(idx + 1); cmd.trim();

  idx = cmd.indexOf(' ');
  token = cmd.substring(0, idx); tempDuration = token.toInt();
  cmd = cmd.substring(idx + 1); cmd.trim();

  idx = cmd.indexOf(' ');
  token = cmd.substring(0, idx); token.replace(',', '.'); tempDuty1 = token.toFloat();
  cmd = cmd.substring(idx + 1); cmd.trim();

  idx = cmd.indexOf(' ');
  token = cmd.substring(0, idx); token.replace(',', '.'); tempDuty2 = token.toFloat();
  cmd = cmd.substring(idx + 1); cmd.trim();

  idx = cmd.indexOf(' ');
  token = cmd.substring(0, idx); tempPresc1 = token.toInt();
  cmd = cmd.substring(idx + 1); cmd.trim();

  tempPresc2 = cmd.toInt();

  if (tempFps > 0 && tempDuration >= 0 && tempDuty1 >= 0 && tempDuty2 >= 0) {
    fps = tempFps;
    dutyCycleT1 = tempDuty1;
    dutyCycleT2 = tempDuty2;
    prescalerT1 = tempPresc1;
    prescalerT2 = tempPresc2;

    // total trial = duration + 3 seconds
    trialInterval = (unsigned long)(tempDuration + 3) * 1000UL;
    return true;
  }

  return false;
}

// =====================
// Camera PWM (Timer1)
// =====================
void startCameraPWM(unsigned int fps, unsigned int prescaler, float duty) {
  TCCR1A = 0;
  TCCR1B = 0;
  TCNT1 = 0;

  unsigned long top = (16000000UL / ((unsigned long)prescaler * fps)) - 1;
  ICR1 = top;
  OCR1A = top * duty;

  TCCR1A |= (1 << COM1A1) | (1 << WGM11);
  TCCR1B |= (1 << WGM12) | (1 << WGM13);
  setTimer1Prescaler(prescaler);
}

void stopCameraPWM() {
  TCCR1A &= ~(1 << COM1A1);
  digitalWrite(camPin, LOW);
}

// =====================
// Beep PWM (Timer2)
// =====================
void startBeepPWM(unsigned int freq, unsigned int prescaler, float duty) {
  TCCR2A = 0;
  TCCR2B = 0;
  TCNT2 = 0;

  unsigned long top = (16000000UL / ((unsigned long)prescaler * freq)) - 1;
  if (top > 255) top = 255;

  OCR2A = (uint8_t)top;
  OCR2B = (uint8_t)(top * duty);

  TCCR2A |= (1 << COM2B1) | (1 << WGM21) | (1 << WGM20);
  TCCR2B |= (1 << WGM22);

  switch (prescaler) {
    case 1:   TCCR2B |= (1 << CS20); break;
    case 8:   TCCR2B |= (1 << CS21); break;
    case 32:  TCCR2B |= (1 << CS21) | (1 << CS20); break;
    case 64:  TCCR2B |= (1 << CS22); break;
    case 128: TCCR2B |= (1 << CS22) | (1 << CS20); break;
    case 256: TCCR2B |= (1 << CS22) | (1 << CS21); break;
    case 1024:TCCR2B |= (1 << CS22) | (1 << CS21) | (1 << CS20); break;
  }

  beepRunning = true;
}

void stopBeepPWM() {
  TCCR2A = 0;
  TCCR2B = 0;
  digitalWrite(beepPin, LOW);
  beepRunning = false;
}

// =====================
// SETUP
// =====================
void setup() {
  Serial.begin(115200);

  pinMode(camPin, OUTPUT);
  pinMode(beepPin, OUTPUT);
  pinMode(timer0Pin, OUTPUT);

  digitalWrite(camPin, LOW);
  digitalWrite(beepPin, LOW);
  digitalWrite(timer0Pin, LOW);

  Serial.println("Arduino ready");
}

// =====================
// LOOP
// =====================
void loop() {

  if (Serial.available()) {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();

    if (cmd.startsWith("START") && !running) {
      if (parseStartCommand(cmd)) {
        startTime = millis();
        running = true;

        pinHighTriggered = false;
        pinLowTriggered  = false;
        beepTriggered    = false;

        startCameraPWM(fps, prescalerT1, dutyCycleT1);
        sendTimestamp("START", 0);
      }
    }
  }

  if (running) {
    unsigned long elapsed = millis() - startTime;

    // t = 1s → pin HIGH
    if (!pinHighTriggered && elapsed >= 1000) {
      digitalWrite(timer0Pin, HIGH);
      pinHighTriggered = true;
      sendTimestamp("PIN_HIGH", elapsed);
    }

    // t = 2s → pin LOW
    if (!pinLowTriggered && elapsed >= 2000) {
      digitalWrite(timer0Pin, LOW);
      pinLowTriggered = true;
      sendTimestamp("PIN_LOW", elapsed);
    }

    // t = (duration + 2)s → beep
    if (!beepTriggered && elapsed >= (trialInterval - 1000)) {
      startBeepPWM(beepFreq, prescalerT2, dutyCycleT2);
      beepTriggered = true;
      beepStartTime = millis();
      sendTimestamp("BEEP", elapsed);
    }

    // stop beep
    if (beepRunning && millis() - beepStartTime >= beepDuration) {
      stopBeepPWM();
    }

    // end trial
    if (elapsed >= trialInterval) {
      stopCameraPWM();
      stopBeepPWM();
      running = false;

      sendTimestamp("END", elapsed);
      Serial.println("DONE");
    }
  }
}
