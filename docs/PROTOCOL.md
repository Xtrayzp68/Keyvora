# Keyvora — Serial Protocol v1.0

## Overview

Communication between the Arduino and PC uses USB Serial CDC at 115200 baud.

- **Format:** Text, one message per line (terminated by `\n`)
- **Direction:** Arduino → PC (primary), PC → Arduino (future)
- **Encoding:** ASCII
- **Fire-and-forget:** No ACK required

## Messages

### Button Press (Arduino → PC)

Arduino event, debounced, sent only on press (not on release):

```
BTN_1
BTN_2
BTN_3
BTN_4
BTN_5
BTN_6
```

### Reserved — Future Messages

```
BTN_1_UP            Button release
BTN_1_LONG          Long press (>1s)
PING                Connection test
PONG                Reply to PING
RGB_1_FF0000        Set button 1 LED to red
OLED_LINE_1:Hello   OLED display text
ENC_LEFT            Rotary encoder turn left
ENC_RIGHT           Rotary encoder turn right
ENC_PRESS           Rotary encoder button press
```

## Timing

- Serial baud: 115200
- Debounce: 50ms
- Main loop delay: 5ms
- Startup: 3x 100ms LED blink signals ready state
