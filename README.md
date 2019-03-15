# Volume Remote Service
What is it? Welp, currently not much. It changes my home theater volume relative to my system volume.

## Reason this exists 
I recently got a new home theater system, and stupidly decided that it was a good idea to hook it up to my computer as my daily driver. The Problem? Welp, i am using a digital input/output, meaning i do not have volume control, its just full blast from my PC all the time.
The Solution? Writing code of course! I knew the sound system had some form of an API (it had a remote control app), so i decided to use that in a windows service to change the volume depending on my system volume.

## Todo
- [ ] Implement auto turn on and off at system boot / shutdown
- [ ] Mute system when theater system is set to another source
  - [ ] When unmuting system, switch source back to computer
- [ ] Allow user to select which source this applies too
  - [ ] if another source is selected, turn off the amp
- [ ] Gui to configure the system service


Disclaimer: Messy as shit code for now (i just wanted to be able to change the volume of my pc! :P). You. Have. Been. Warned.
