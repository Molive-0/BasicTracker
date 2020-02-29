# Basic Tracker Readme

Hello, and welcome to Basic Tracker, a music tracker program I have created for my A-Level.  
Trackers are known for being able to render on a text only display -to this end, Basic Tracker is rendered only using the default C# console (There are a few parts in Windows Forms, due to time constraints). This is meant to be a simplish implementation of a tracker program based on the hardware of the [Super Nintendo Entertainment System][snes] and the effects of [Impulse Tracker][impulse].

As opposed to most trackers, the cursor moves over the pattern rather than the pattern moving under the cursor.

## Usage

This is not a tutorial on how to use a tracker, and so this document will assume you know how to use one already.

### General
For this tracker, all patterns are 32 lines long. The tempo is in BPM and the speed is how many ticks happen per row.

There are 5 preset instruments to choose from, from 1-5: a sine wave, a square wave, a triangle wave, a sawtooth wave, and white noise.  
Instruments 6-A are LFO versions of instruments 1-5.

### Movement
To move the cursor around the screen, use the arrow keys. Holding CTRL while moving allows you to move whole columns at a time.

To start playback, press space. To stop, press space again.  
Pressing the enter key moves the playback cursor to the position of the edit cursor, and plays that row by itself.  
To stop the sound system outputting sound press Tab.

If you are on a pattern where the playback cursor is not, and you press space to start playback you will be moved to the pattern where the cursor is. If you wanted to play that pattern pressing enter first will try to move the cursor to you, and failing that move you to the cursor. The playback head cannot reach patterns that are not in the ordering list.

### Notes
The columns for each channel feature (from left to right) note + octave (red), instrument (blue), volume (green) and effect (yellow).

The tracker can play any note from C_0 to B_8. This is inputted ProTracker style with the keyboard mapped like a piano, with the first octave at Z to M and a second, higher octave at Q to U, up to P as a third above the second octave. If you want to change octave simply use the numerical keypad to get to a new one. If you don't have a keypad, use ";" and "'" increment and decrement it.

Instrument is auto inserted for every note inserted. It is stored in hexadecimal and can be over-written by simply selecting it using the cursor and pressing the desired hex digit on the keyboard. Instrument zero is silence. The instrument that is inserted when a note is input can be seen on the header bar, and changed with "[" and "]".

The volume was meant to have more purpose, but has since been delegated to only setting the volume. To set the volume on a note, move the cursor to the left most green character and press "V". The volume is stored in hexadecimal like the instrument; to set it move the cursor to the hex digit you want to edit and press the desired new hexdigit. To remove a volume set press backspace on any of the green characters that makes it up.

The effect performs many different functions. The effects available are the same as that in Impulse Tracker, except that Oxx has been replaced with OpenMPT's parameter extension. They are set in the same way that the volume is set, the effect letter at the front and the parameter in hexadecimal afterwards. Backspace deletes the effect.

### Patterns
To create more patterns make sure the cursor is in the pattern area and press "CTRL-N". This creates a pattern and the end of the pattern list. If you have more than one pattern you can view the others by pressing "CTRL+[" and "CTRL+]". A pattern can not be played unless it exists somewhere within the ordering list. If you wish to delete a pattern, press "CTRL-Backspace".

### Ordering list

The ordering list is the order at which the playback cursor moves between patterns, a pattern may appear more than once in the ordering list. To change to editing the ordering list instead of the pattern, press F6. Pressing F6 again moves the cursor back.

Whilst editing the ordering list pressing left and right moves you forward and backward through the list. Pressing Enter on any entry allows you to set it to a pattern number between 1 and 65535. If you want to add another item to the list pressing "CTRL-N" inserts one at the current position.

If you wish to remove an item pressing backspace removes it from the list. If you wish to remove the pattern as well (as long as it appears nowhere else) pressing "CTRL-Backspace" removes both.

### Function keys

Whilst on the pattern area, pressing F1 brings up this help menu. pressing F2 allows you to change the title of the song (max 30 characters), pressing F3 allows you to change the author or musician of the song (max 30 characters), pressing F4 allows you to edit the tempo (first hex character then second hex character, cannot be zero) and pressing F5 allows you to edit the speed (same restrictions as tempo). I advise against high speeds as it significatly slows down the playback rate.

### File
If you wish to save your work, pressing \"CTRL-S\" opens a save dialog. Similarly pressing "CTRL-O" allows you to open a file you have previously saved. 
>NOTE: opening a file will overwrite the currently opened song and any unsaved work will be lost!

To exit the program at any point press ESC.

## Effects reference
 This effects reference is based on the one hosted in the Open MPT documentation.

### Effect Column

| eff |    Name   | mem | Description |
| --- | --------- | --- | ------------|
| Axx |	Set Speed |	No  |	Sets the module Speed (ticks per row).  **DOES NOT WORK**|
| Bxx |	Position Jump |	— |	Causes playback to jump to pattern position xx. <br> B00 would restart a song from the beginning (first pattern in the Order List).|
| Cxx |	Pattern Break |	— | Jumps to row xx of the next pattern in the Order List. <br> If the current pattern is the last pattern in the Order List, Cxx will jump to row xx of the first pattern. |
| Dxy |	Volume Slide  |	Yes |	Slides the current note volume up or down. <br><br> D0y decreases note volume by y units on every tick of the row except the first. <br> Dx0 increases note volume by x units on every tick of the row except the first. <br> DFy finely decreases note volume by only applying y units on the first tick of the row. <br> y cannot be Fh <br> DxF finely increases note volume by only applying x units on the first tick of the row. <br> x cannot be Fh.
| Exx |	Portamento Down |	Yes |	Decreases current note pitch by xx units on every tick of the row except the first. <br> EFx finely decreases note pitch by only applying x units on the first tick of the row. <br> EEx extra-finely decreases note pitch by applying with 4 times the precision of EFx. |
| Fxx |	Portamento Up | 	Yes  |	Increases current note pitch by xx units on every tick of the row except the first. <br> EFx finely increases note pitch by only applying x units on the first tick of the row. <br> EEx extra-finely increases note pitch by applying with 4 times the precision of EFx.
| Gxx |	Tone Portamento |	Yes |	Slides the pitch of the previous note towards the current note by xx units on every tick of the row except the first.|
| Hxy |	Vibrato |	Yes |	Executes vibrato with speed x and depth y on the current note. <br> Modulates with selected vibrato waveform. |
| Ixy |	Tremor |	Yes |	Rapidly switches the sample volume on and off. Volume is on for x ticks and off for y ticks. |
| Jxy |	Arpeggio |	Yes |	Plays an arpeggiation of three notes in one row, cycling between the current note, current note + x semitones, and current note + y semitones. |
| Kxy |	Volume Slide + Vibrato |	Yes |	Functions like Dxy with H00. Parameters are used like Dxy. |
| Lxy |	Volume Slide + Tone Portamento |	Yes |	Functions like Dxy with G00. Parameters are used like Dxy. **DOES NOT WORK**|
| Mxx |	Set Channel Volume |	— |	Sets the current channel volume, which multiplies all note volumes it encompasses. **Makes things far too loud**|
| Nxy |	Channel Volume Slide |	Yes 	|Similar to Dxy, but applies to the current channel's volume. **Makes things far too loud**|
| Oxx |	Parameter Extension |	Yes |	Extends the parameter of the last Bxx or Txx command. If placed after such a command, the parameter values are combined. xx is added to the parameter of the original command × 256. |
| Pxy |	Panning Slide |	Yes |	Slides the current channel's panning position left or right. <br> P0y slides the panning to the right by y units on every tick of the row except the first. <br> Px0 slides the panning to the left by x units on every tick of the row except the first. <br>PFy finely slides the panning to the right by only applying y units on the first tick of the row. <br>y cannot be Fh. <br>PxF finely slides the panning to the left by only applying x units on the first tick of the row. <br>x cannot be Fh.
| Qxy |	Retrigger |	Yes 	|Retriggers the current note every y ticks and changes the volume based on the x value (see the [Retrigger Volume table](#retrigger-volume-table) for more details). | 
| Rxy |	Tremolo |	Yes |	Executes tremolo with speed x and depth y on the current note. Modulates with selected tremolo waveform. |
| S3x |	Set Vibrato Waveform |	— |	Sets the waveform of future Vibrato effects **DOES NOT WORK** | 
| S4x |	Set Tremolo Waveform |	— |	Sets the waveform of future Tremolo effects **DOES NOT WORK** | 
| S5x |	Set Panbrello Waveform |	— |	Sets the waveform of future Panbrello effects **DOES NOT WORK** | 
| S6x |	Fine Pattern Delay |	— |	Extends the current row by x ticks. **DOES NOT WORK** <br> If multiple S6x commands are on the same row, the sum of their parameters is used. |
| S8x |	Set Panning |	— |	(Xxx is a much finer panning effect.) Sets the current channel's panning position. Ranges from 0h (left) to Fh (right). |
| S90 |	Surround Sound Off |	— |	Disables "surround" sound for the channel. **DOES NOT WORK** |
| S91 |	Surround Sound On |	— |	Enables "surround" sound for the channel. **DOES NOT WORK** |
| S92 |	FM Synthesis Off |	— |	Disables frequency modulation for the channel. |
| S93 |	FM Synthesis On |	— |	Enables frequency modulation for the channel.  Works by using the output of the channel to the left as a modulator. |
| SB0 |	Pattern Loop Start |	— |	Marks the current row position to be used as the start of a pattern loop. |
| SBx |	Pattern Loop |	— |	Each time this command is reached, jumps to the row marked by SB0 until x jumps have occurred in total. **LOOPS FOREVER** <br> If SBx is used in a pattern with no SB0 effect, SBx will use the row position marked by any previous SB0 effect. Pattern loops cannot span multiple patterns.|
| SCx |	Note Cut |	— |	Stops the current sample after x ticks. <br> If x is greater than or equal to the current module Speed, this command is ignored. |
| SDx |	Note Delay |	— |	Delays the note or instrument change in the current pattern cell by x ticks. **DOES NOT WORK** <br> If x is greater than or equal to the current module Speed, the current cell's contents are not played. |
| SEx |	Pattern Delay |	— |	Repeats the current row x times. **SOFTLOCK** <br> Notes are not retriggered on every repetition, but effects are still processed. <br> If multiple SEx commands are on the same row, only the leftmost command is used. |
| T0x |	Decrease Tempo |	Yes |	Decreases the module Tempo by x BPM on every tick of the row except the first. **CRASH**
| T1x |	Increase Tempo |	Yes |	Increases the module Tempo by x BPM on every tick of the row except the first. **CRASH**
| Txx |	Set Tempo |	No |	Sets the module Tempo if xx is greater than or equal to 20h. |
| Uxy |	Fine Vibrato |	Yes 	|Similar to Hxy, but with 4 times the precision.
| Vxx |	Set Global Volume |	— |	Sets the global volume.
| Wxy |	Global Volume Slide |	Yes 	|Similar to Dxy, but applies to the global volume. |
| Xxx |	Set Panning |	— |	Sets the current channel's panning position. Ranges from 00h (left) to FFh (right). |
| Yxy |	Panbrello |	Yes |	Executes panbrello with speed x and depth y on the current note. Modulates with selected panbrello waveform.
| Zxx | Affect Filter Coefficients | No | **Not implemented** |

### Volume Column
| eff |    Name   | mem | Description |
| --- | --------- | --- | ------------|
| Vxx |	Set Volume |	No  |	Sets the channel note volume|

### Retrigger Volume Table
| Parameter | Effect | Parameter |	Effect |
| --------- | ------ | --------- | ------- |
| 0 | 	No volume change | 8 | No volume change
| 1 | 	Volume - 1  | 9 |	Volume + 1
| 2 |	Volume - 2 	| A |	Volume + 2
| 3 |	Volume - 4 	| B |	Volume + 4
| 4 |	Volume - 8 	| C |	Volume + 8
| 5 |	Volume - 16 | D |	Volume + 16
| 6 |	Volume × ⅔ 	| E |	Volume × 1.5
| 7 |	Volume × ½ 	| F |	Volume × 2 

## Key command table
The key commands, but formatted as a table.
### Ordering Mode
| key | action |
| --- | ------ |
| Right arrow |move right
| Left arrow | move left
| Control + arrow | move faster
| Backspace | removes order
| Control Backspace | removes order and pattern
| Control N| new order
| Enter| edit current order
| Spacebar| toggle playback
| Tab| kill audio subsystem
| F6| return to normal mode
### Normal Mode
| key | action |
| --- | ------ |
| Up, down, left, right| move cursor
| Control left, right| move by channel
| Semicolon| lower default octave
| Apostrophe| raise default octave
| Numpad| set default octave
| Open square bracket| lower instrument
| Close square bracket| raise instrument 
| F1| “help” screen
| F2| edit title
| F3| edit author
| F4| edit “starting” tempo
| F5| edit “starting” speed
| F6| enter ordering mode
| Enter| move playback head to cursor, play single line
| Spacebar| toggle playback
| Tab| kill audio subsystem
| Control N| new pattern
| Control D| duplicate this pattern
| Control Z| Copy this channel to the clipboard
| Control X| Paste over this channel from the clipboard
| Shift Z| rotate this channel downwards
| Control Open square bracket| previous pattern
| Control close square bracket| next pattern
| Control backspace| delete pattern
| Backspace| remove data from under cursor
| Control O| open file
| Control S| save file

## Known issues
_Axx_ does not work correctly (bug)  
_Bxx_ and _Cxx_ do not work together (bug)  
_Mxx_ and _Nxx_ make things far too loud (bug)  
_Txx_, for all intents and purposes, crashes the program (bug)  
_Ixx_ might be doing something bad??? Not sure (bug)  
_Lxx_ only does the volume decrease (bug)  
_Volume_ can overflow I think (feature)  
There are _clicky noises_ every time a note starts?? (feature)  
_S0x_, _S1x_, _S2x_, _S7x_, and _SAx_ are not implemented (intended)  
_SBx_ loops forever (bug)  
_Yxx_ sometimes has overflow errors (bugeature)  
_S91_ and _S90_ do nothing (bug)    
_SDx_ does nothing, apparently (bug)  
_SEx_ is a softlock (bug)  
_S6x_ “just breaks everything” – my notes (bug)  
_S3x_, _S4x_, and _S5x_ are essentially useless due to an oversight in the application of the waves. You can see what things other than the sine wave sound like, but it’s not great. (bug)  
Major crashes involving the _ordering row_ and the playback cursor when things are _deleted from underneath it_ (BUG)  
Don’t _drag the screen bigger_ and stuff? It _really_ doesn’t like that. (unsure on fix)  
Some issues with _volume being saved correctly_ to disk means that everything that wasn’t assigned a volume is at _full volume_ when you reload it (BUG)  
Apart from all those errors, it’s _perfectly fine_!

## Compliation
Compiles using Visual Studio 2019 on Windows. Requires .NET Core 3.1.100.
Uses Naudio and Netwtonsoft's Json as Nuget Dependancies.
Install doxygen and miktex for doc complation.
Use publish to compile to a single, large exe file.

## Future features
 * Volume effects
 * Filter effect
 * Echo effects
 * Compile to SNES
 * Fix _all_ the bugs
 * Support Linux and MacOS

## Final words

I'd appreciate feedback! You can reach me by email at moliveofscratch@gmail.com. I need people to give advice on how to make this program better for the A-Level write-up so it'd be very useful.  
Thanks!  
    ~Molive



[snes]: www.wikipedia.org/wiki/snes
[impulse]: www.wikipedia.org/wiki/Impulse_Tracker