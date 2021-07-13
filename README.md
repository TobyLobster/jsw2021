# Jet Set Willy 2021 Edition (BBC Micro)

Similar to my [Manic Miner 2021 Edition](https://github.com/TobyLobster/ManicMiner2021), I set my mind to improving Jet Set Willy on the BBC Micro.
Starting from a [disassembly/reassembly of the original BBC Micro game](https://github.com/TobyLobster/jsw), I made many improvements detailed below.

    jsw.ssd

## Improvements
* More colours on screen.
* More accurate colour choices.
* Corrected room and sprite definitions.
* More tuneful.
* New font.
* Arrows and ropes working together again.
* Watch Tower crash bug fixed.
* Arrow rendering bug fixed.
* Player start position fixed.
* Use RETURN for jump.

## What I did
Step one is some admin. I create a single source file. The original code/data is split over two files, and code execution flows between them via jump tables. This would have been useful back in the day when memory was tight for developing on the BBC Micro itself. By splitting the source like this you could assemble half of the code as you worked on it and have a chance to fit that source code into memory. In a modern development environment (we have computers with loads of memory) this dichotomy isn't needed, so I put all the code and data together in one file and removed the jump tables. It took some effort to make sure that every detail is labelled correctly, and to remove any assumptions about memory layout in the code so all the code and data can be relocated in memory without causing bugs. Most commonly there were a few places where specific data was assumed to lie on page boundaries. This is usually done for performance benefits with a side benefit of saving a few bytes of memory, but in this case the performance and memory benefit was negligible. The convenience of being able to move, add, remove and change code freely is compelling.

The data had many small pockets of unused memory, so I coalesced all these together into one place. I also moved the memory required for the screen to the end of RAM ($5600 to $7fff. 32 characters in each row for 21 character rows) so the rest of the game lies together below the screen memory.

Enough admin, onto the first bug fix. The original BBC version has a bug where the game crashes as soon as the player visits 'The Watch Tower'. This bug is present on the [Complete BBC Micro Games archive version (maybe disk based?)](http://www.bbcmicro.co.uk/game.php?id=439)  but not the [Level 7 disassembly (maybe cassette based?)](http://www.level7.org.uk/miscellany/jet-set-willy-disassembly.txt). The reason for the bug is that the code that loads and runs the second file of the game is located exactly where this room definition is supposed to be. The original room definition has now been restored, and the bug is fixed.

I moved the start position of the player to the correct position (in the bath, as per the Spectrum). Willy faces right initially. I've not replicated the Spectrum bug where Willy starts looking left if the previous game ended with willy left. The philosophy here is to not slavishly follow every little quirk of the Spectrum version, but I do use it to guide towards a more authentic Jet Set Willy experience.

I have updated the in game tune to be longer, more accurate, and kinder on the ears.

At this point I start to remove all use of the OS. The game uses OSWRCH to write text (and more), OSWORD for sound, and OSBYTE for keyboard, vsync etc. Although I need to write more code to replace these OS routines, it does save memory overall in that the game can use more memory locations if the OS no longer uses them. Use of OSWRCH is replaced first, then sound routines and replaced, then keyboard. I can use more of zero page for variables, which saves memory for each instance a variable is accessed.

The only part of the OS that continues to run (necessarily) is the handling of IRQs. The game uses these interrupts to switch palette colours at different character rows down the screen, for updating the music and sound, and updating timers for the game. At each character row in the game area, one palette change can occur. A different palette is switched in for the 'footer' area of the screen. But to use this data, I need to be able to edit the rooms.

### The Rooms
The rooms are compressed. Each room is encoded as a stream of bits, with different numbers of bits required for different data. This is described in [the Level 7 disassembly](http://www.level7.org.uk/miscellany/jet-set-willy-disassembly.txt). To be able to edit this data, I first need to decode the existing encoded bytes into an editable text file. I wrote a C# console application to do this. The result is 'definitions.txt', a text file describing exactly the information required by the game to show each room. I also include the sprite definitions in this text file too, so I can edit them as well.

The next step is to write a tool that can take that text file and produce an encoded version of it in bytes (as ASM assembly source). This is a second C# console application. I take the time to make sure that the resulting bytes are identical to the original bytes. Every time I assemble the game, I encode the latest data too.

Now the level and sprite data is editable, I add data so that each type of sprite that defines the room (an 8x8 'tile') can have two colours instead of one. Walls in the Bathroom can be red and yellow for example, rather than being one colour against black.

I also add data for each room to have palette changes per character row. e.g. In 'The Bathroom', the enemy at the top of the room moves left and right and can be coloured green (as per the Spectrum) to give more colours.

![Bathroom](bathroom.gif)
The Bathroom, before and after

Now I have these colourful abilities I take a sweep through the whole mansion, painting by numbers. It really brightens the place up. This was not the only sweep. More sweeps happened later where I checked the positions and definitions of the tiles, the enemies initial positions, directions, speeds, and extents. There were a *lot* of changes. I also correct the position and names of each of the rooms (e.g. 'Coservatory Roof') expanding the compression for room names to accommodate full stops. All aligning to be closer to the Spectrum version.

I also added a 'SCENERY' tile type to help get the room definitions closer to the Spectrum in one or two places.

## Arrows
I reinstated arrows back onto the rooms with ropes as needed, which were missing from the BBC version. I retimed the arrows as per the Spectrum, and fixed a bug in the rendering of arrows that left a hole in the wall of 'A bit of tree'. Arrow sounds timed as per the Spectrum to give the player warning of their arrival.

## Sprites
I added some code to reflect sprites from their definitions into cache. This means we can store 4 sprites not 8 for some enemies going both left and right (The Monk, Saw, Pig, Bird, and Penguin).

I added back in the missing enemy guard sprites as found in 'Rescuing Esmerelda' and 'Above the West Bedroom'.

I also removed several unused tile sprites.

All sprites are now compressed to save memory, and are decompressed at runtime as needed. The compression is nybble based, decompressing a byte at a time:

    0-3     this byte is the same as a byte previously decoded in this sprite (previous byte, previous byte but one, but two, but three)
    4-9     one of the 6 predetermined most common bytes (stored in a table)
    10-14   The next nybble with this nybble specifies on of the 80 next most common bytes (stored in a table)
    15      The next two nybbles specify the value of the byte

Thus we save memory on bytes that can be encoded as 0-9, break even on encoding 10-14, and use an extra nybble when encoding 15 is required. Additionally, some sprites don't compress well. We encode these instead as raw bytes (pairs of nybbles), with the first nybble of the sprite encoded as 0 (since value 0 or any value 0-3 wouldn't otherwise occur at the start of a sprite).

## Enemies
Vertical enemies spin at a medium speed, with the Razor Blade enemies spinning fast. The Monk in the Chapel remains looking left (as if possessed?), as per Spectrum.

## Items
Items flash more individually, rather than in waves of colour previously. e.g. see 'Ballroom West'.

## Time
We start at 7am as per the Spectrum (not 7pm), working through until 1am at a similar rate to the Spectrum.

## Lives
The Lives are shown by a line of Willy characters walking right. This is unlike the Spectrum where they are static, but copies Manic Miner instead.

## Animated Scenery

![First Landing](first.gif)

I added the feature from the Spectrum that the cross in the 'First Landing' flashes, and other places such as 'Nomen Luni' other scenery flashes too.

### The Game Over Screen
This animates in a standard palette of colours, and flashes the 'GAME OVER' letters individually.

### The Title Screen
The scrolling text moves a little smoother than it used to be while still retaining the speed (moving four pixels at a time instead of eight). The scrolling text has been tidied up slightly ('Jet Set Willy' not 'Jetset Willy', and 'BBC Micro' not 'BBC micro'). The Moonlight Sonata plays.

## Spectrum Font
The spectrum font was added and used throughout. Prior to this point I was reading the OS definitions which needed to be different on the Master, but I found enough space to encode the characters we need from the Spectrum font, which feels better. The font characters are compressed in the same way as the other sprites.

## The Rope
I fixed the swing of the rope to match the Spectrum, and moved the beach rope two character cells left to match the Spectrum. I tweaked the logic to make the player move a little better on the rope. The rope is flicker free.

## Help
*  http://www.level7.org.uk/miscellany/jet-set-willy-disassembly.txt
An excellent disassembly for understanding the BBC Micro version. This was the starting point.

* https://skoolkit.ca/disassemblies/jet_set_willy/hex/
The definitive place for discovering exactly how the original Spectrum game works.
