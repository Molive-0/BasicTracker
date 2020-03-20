# Writeup
## Introduction
_Hello!_

Welcome to the write up for up Basic Tracker, a chiptune music tracker written in C#. It was specifically written not to a client's needs but rather mainly to see if I could make a "tracker" using only text based rendering, which I have mostly achieved.\footnote{It is simple design wise to make the windows forms sections into console sections, I just don't have time. This would also increase platform portability.} It is also to fill a mild gap in the market of a "SNES Tracker", that is music software for creating music that can be played back on the [Super Nintendo Entertainment System][snes].

## What is a tracker?
A music tracker is a piece of software which allows people to write music using a column based command interface. As opposed to modern programs, which use a piano roll style editing interface where notes are written from left to right as rectangles, a tracker has a grid in which commands to start, stop and edit notes are placed.  

The first tracker was the "Ultimate Soundtracker"\footnote{Ultimate being a misnomer here, as there was a lot to improve on.}, which was a music creation software for the Amiga line of home computers. As with other trackers, it features a series of columns, representing the channels that the Amiga could play sound on. As the song played back, a cursor moves across the screen executing the instructions in the grid as it hits them.  
Shortly after there was the release of possibly the most famous tracker, Protracker, which built off of the code of the original to add more features and to fix various bugs.

## Design
My tracker is influenced by three main sources.
### GUI
For the GUI and user facing side it is mainly based on my own experience using the Windows tracker program Open ModPlug Tracker (OpenMPT), shown in the below image.

![A screen shot of Modplug Tracker, with the song Shooting Star by Saga Musix loaded.](modplug.png)

A lot of the features that you can see in the final design of Basic Tracker are shown in this screen shot, for example the column for each channel and the respective headings, the row numbering on the left, the ordering row, the octave, tempo, and ticks counter at the very top, and so on.  
There are a few differences in this compared to Basic Tracker, notably the more than 8 channels and the more than 32 rows. OpenMPT is much more forward thinking than Basic Tracker in that it can open many file formats from the past, and so can support loading files with many more that 8 channels. Basic Tracker is designed to only run at 8 channels and 32 rows, as that makes the code simpler and it makes the screen design easier.  
In this and most other trackers the pattern moves under the cursor. This is mainly so you can fit more than 32 rows on screen, but as I move the cursor under the row I am limited to the user's screen size.

Here is a design stage drawing of the screen.

![An ascii art mockup of the GUI for Basic Tracker](screendesign.png)

As you can see this is very close to the final version, but a few changes were made, notably the replacement of the mostly useless length of the song with the instrument selector and the placement of the row numbers to the left of the columns. The entire tracker was also going to be black and white for a lot of development, and the colours were added halfway through after switching to the low level console drawing method which made them possible.

### File Format and features
The file format was one of the first things to be created, and is based heavily on the file format for Impulse Tracker files. This was done because Impulse Tracker is a well used and stable tracker and therefore the file format is very suitable for compressing and storing the data for a song in very small amounts. I do not think I have seen a Basic Tracker file pass a few kilobytes in size.  
\pagebreak 
The design stage for the file format is as follows:

**Header Data**  
|     | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | A | B | C | D | E | F |
|:-----:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
|0000 |'B'|'S'|'C'|'M'| Song Name, max 30 characters, NULL is underscore   ||||||||||||
|0010 | Song Name continued... ||||||||||||||||
|0020 |Song N.|| Author Name, max 30 characters, NULL is underscore ||||||||||||||
|0030 |Author Name continued||||||||||||||||
|0040 |OrdNum ||PatNum || Cwt/v || Cmwt  ||      Reserved     ||||||IS |IT|
|0050 | Orders, Length = OrdNum                                       ||||||||||||||||
|xxxx | 'Long' Offset of patterns, Length = PatNum*4 (offset 1)       ||||||||||||||||

```
(offset 1) = 0050h+OrdNum

      Cwt:      Created with tracker.
                 Basic Tracker y.xx = 1yxxh
      Cmwt:     Compatible with tracker with version greater than value.
                 (ie. format version) (not checked)
      OrdNum:   Number of orders in song.
      PatNum:   Number of patterns in song
      
      Res.: Reserved, unused
      IS:       Initial Speed of song.
      IT:       Initial Tempo of song
      Orders:   This is the order in which the patterns are played.
                 Valid values are from 0->65530.
                 65534 = "---", End of song marker
                 65535 = "+++", Skip to next order
```
**Pattern data**
|     | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | A | B | C | D | E | F |
|:-----:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
|0000 |Length || Packed data................                           ||||||||||||||
```
Length:   Length of packed pattern, not including the 2 byte header
                Note that the pattern + the 2 byte header will ALWAYS
                be less than 64k

Patterns are unpacked by the following pseudocode:
Assume previous values only exist for the pattern they are in.

For each row:
GetNextChannelMarker:
        Read byte into channelvariable.
        if(channelvariable = 0) then end of row
        Channel = (channelvariable-1) & 7              ; Channel is 0 based.
        if(channelvariable & 128) then read byte into maskvariable
          else maskvariable = previousmaskvariable for current channel
if(maskvariable & 1), then read note. (byte value)
                // Note ranges from 0->119 (C-0 -> B-9)
                // 255 = note off, 254 = notecut
                // Others = note fade (already programmed into IT's player
                //                     but not available in the editor)
if(maskvariable & 2), then read instrument (byte value)
                // Instrument ranges from 1->99
if(maskvariable & 4), then read volume/panning (byte value)
                // Volume ranges from 0->64
                // Panning ranges from 0->64, mapped onto 128->192
                // Prepare for the following also:
                //  65->74 = Fine volume up
                //  75->84 = Fine volume down
                //  85->94 = Volume slide up
                //  95->104 = Volume slide down
                //  105->114 = Pitch Slide down
                //  115->124 = Pitch Slide up
                //  193->202 = Portamento to
                //  203->212 = Vibrato
Effects 65 is equivalent to D0F, 66 is equivalent to D1F -> 74 = D9F
        Similarly for 75-84 (DFx), 85-94 (Dx0), 95->104 (D0x).
(Fine) Volume up/down all share the same memory (NOT shared with Dxx
        in the effect column tho).
Pitch slide up/down affect E/F/(G)'s memory - a Pitch slide
        up/down of x is equivalent to a normal slide by x*4
Portamento to (Gx) affects the memory for Gxx and has the equivalent
        slide given by this table:
SlideTable      DB      1, 4, 8, 16, 32, 64, 96, 128, 255
Vibrato uses the same 'memory' as Hxx/Uxx.
if(maskvariable & 8), then read command (byte value) and commandvalue
                // Valid ranges from 0->37 (0=no effect, 1=A, 2=B, 3=C, etc.)
if(maskvariable & 16), then note = lastnote for channel
        if(maskvariable & 32), then instrument = lastinstrument for channel
        if(maskvariable & 64), then volume/pan = lastvolume/pan for channel
        if(maskvariable & 128), then {
                command = lastcommand for channel and
                commandvalue = lastcommandvalue for channel
        }
        Goto GetNextChannelMarker
```

## User Feedback

> **Molive**  13:56  
> Hey guys, for my A-level project I've created a tracker entirely in C# and the windows console. Apparently I need an end-user to test it and to give me feedback so I can write about it in a report
> 
> **ruairi**  13:56  
> shit that's awesome
> 
> **Molive**  13:56  
> I'd be great if someone could test it and tell me what could be improved and stuff  
> 13:56  
> https://1drv.ms/u/s!Aje7F8jr0d_BhrpudsGel9rk2oFjBg?e=GOjQoV
> 13:57  
> Thanks!
> 
> **ruairi**  14:08  
> I've had a brief play, it's pretty solid  
> 14:09  
> wouldn't mind a page up / page down and tab to next column but I appreciate it's a proof of concept
> 
> **Molive**  14:09  
> F1 has all the keys in a huge block of text  
> 14:09  
> holding ctrl moves per column  
> 14:10  
> I think I need to rewrite that help menu actually, haha
> 
> **Mantratronic**  14:11  
> will give it a go, but next time that i can do it properly will be tomorrow afternoon  
> 14:11  
> also what @ruairi said!
> 
> **ruairi**  14:12  
> @Molive A beautiful guide in Markdown would be appreciated ;)
> 
> **Hoffman**  14:12  
> ouch  
> 14:12  
> _(image of the old help menu)_
> 
> 14:13  
> a markdown page would be nice, but also maybe incorporating the the help as pages inside the console window?
> 
> **d0pefish**  14:14  
> ```csharp
> MessageBox.Show("
> Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.
> ");
> ``` 
> (edited)
> 
> **Bossman**  14:23  
> @Molive would love to take a look, but that link is broken for me  
> 14:23    
> oh wait... turns off VPN  
> 14:26  
> All the ASIOs  
> _(screen showing Bossman's 16 install ASIO drivers, from the first screen in the program)_
> 
> **Bossman**  14:29  
> Between VPN fail, Chrome telling me not to trust "not commonly downloaded" files and Windows Defender, it was a battle to run this thing  
> 14:29  
> but once up, seems solid  
> 14:30  
> Good work @Molive
> 
> **ruairi**  14:34  
> lol two words of feedback @Bossman :D
> 
> **Molive**  14:34  
> Thanks!
> 
> **Bossman**  14:35  
> @ruairi I'm working. Proper analysis later  
> 14:35  
> Wanted to grab the download before I forgot
> 
> **df0**  16:41  
> certainly is the biggest tracker @ 151MB!
> 
> **df0**  16:41  
> _(Deltafire's computer has thrown a message because my program could contain a virus)_
> 
> 16:42  
> it really doesn't want to run it!  
> _(Windows smartscreen has also triggered on the program)_
> 
> 16:45  
> tab and shift-tab to move between tracks would be cool  
> 16:45  
> i know ctrl does it, but i suspect most people expect tab to  
> 16:46  
> menus would be super-cool!
> 
> **Hoffman**  17:03  
> a page showing what all the commands are would be REALLY helpful
> 
> **LiSU**  21:09  
> an option to export as a pokey\footnote{"POKEY" was the name of the sound chip from that Atari 8-bit range of home computers. It also controlled things like the keyboard.} tune would be even cooler! (I'm suffering from the lack of pokey trackers)
> 
> **Molive**  23:53  
> It's kinda designed with the SNES in mind, you should be able to export it directly to SNES  
> 23:54  
> I might be able to add other converters but that's outside my A-level. A summer project maybe?  
> 
> **Molive** 23:55  
> > replied to certainly is the biggest tracker @ 151MB!  
>  
> It, uh, kinda contains the entire net runtime :D

[snes]: https://www.wikipedia.org/wiki/snes