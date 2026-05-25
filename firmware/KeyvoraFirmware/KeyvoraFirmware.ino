/*
 * Keyvora Firmware
 * ATmega32U4 - Arduino Pro Micro (5V/16MHz)
 *
 * Lightweight event-driven button reader.
 * Only sends button press events via USB Serial.
 * All business logic is handled on the PC side.
 */

// ============================================================
// Pin Configuration
// ============================================================
constexpr uint8_t BTN_COUNT = 6;

constexpr uint8_t BTN_PINS[BTN_COUNT] = { 2, 3, 4, 5, 6, 7 };

// Reserved for future expansion
constexpr uint8_t BTN_FUTURE_PINS[2] = { 8, 9 };

// ============================================================
// Debounce Configuration
// ============================================================
constexpr unsigned long DEBOUNCE_DELAY_MS = 50;

// ============================================================
// State
// ============================================================
struct ButtonState {
  uint8_t pin;
  bool lastState;
  bool currentState;
  unsigned long lastDebounceTime;
};

ButtonState g_buttons[BTN_COUNT];

// ============================================================
// Initialization
// ============================================================
void setup() {
  Serial.begin(115200);

  for (uint8_t i = 0; i < BTN_COUNT; i++) {
    g_buttons[i].pin = BTN_PINS[i];
    g_buttons[i].lastState = HIGH;       // INPUT_PULLUP → HIGH = released
    g_buttons[i].currentState = HIGH;
    g_buttons[i].lastDebounceTime = 0;

    pinMode(BTN_PINS[i], INPUT_PULLUP);
  }

  // Future pins as INPUT_PULLUP (ready without conflict)
  for (uint8_t i = 0; i < 2; i++) {
    pinMode(BTN_FUTURE_PINS[i], INPUT_PULLUP);
  }

  // Startup blink: PC-side knows device is ready
  pinMode(LED_BUILTIN, OUTPUT);
  for (uint8_t i = 0; i < 3; i++) {
    digitalWrite(LED_BUILTIN, HIGH);
    delay(100);
    digitalWrite(LED_BUILTIN, LOW);
    delay(100);
  }
}

// ============================================================
// Main Loop
// ============================================================
void loop() {
  unsigned long now = millis();

  for (uint8_t i = 0; i < BTN_COUNT; i++) {
    ButtonState& btn = g_buttons[i];
    bool rawState = digitalRead(btn.pin);

    // Debounce: state must be stable for DEBOUNCE_DELAY_MS
    if (rawState != btn.lastState) {
      btn.lastDebounceTime = now;
    }

    if ((now - btn.lastDebounceTime) > DEBOUNCE_DELAY_MS) {
      if (rawState != btn.currentState) {
        btn.currentState = rawState;

        // Only send event on PRESS (LOW = pressed with INPUT_PULLUP)
        if (btn.currentState == LOW) {
          Serial.print(F("BTN_"));
          Serial.println(i + 1);
        }
      }
    }

    btn.lastState = rawState;
  }

  // Small delay to prevent tight-loop saturation
  // Keeps CPU available for serial TX
  delay(5);
}
