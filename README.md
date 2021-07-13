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
* 'Watch Tower' crash bug fixed.
* Arrow rendering bug fixed.
* Player start position fixed.
* Uses the RETURN key for jump.
* Works on the Master too.

## Disclaimer
The improvements that follow are made possible only by the advent of modern PCs, with modern tools, emulators and the combined resources of internet. It should be said that the original BBC version written by Dave Mann (using the pseudonym Chris Robson) was a great achievement and remains very playable today.

## What I did
Step one is some admin. I create a single source file. The original code/data is split over two files, and code execution flows between them via jump tables. This would have been useful back in the day when memory was tight for developing on the BBC Micro itself. By splitting the source like this you could assemble half of the code as you worked on it and have a chance to fit that source code into memory. In a modern development environment (we have computers with loads of memory) this dichotomy isn't needed, so I put all the code and data together in one file and removed the jump tables. It took some effort to make sure that every detail is labelled correctly, and to remove any assumptions about memory layout in the code so all the code and data can be relocated in memory without causing bugs. Most commonly there were a few places where specific data was assumed to lie on page boundaries. This is usually done for performance benefits with a side benefit of saving a few bytes of memory, but in this case the performance and memory benefit was negligible. The convenience of being able to move, add, remove and change code freely is compelling.

The data had many small pockets of unused memory, so I coalesced all these together into one place. I also moved the memory required for the screen to the end of RAM ($5600 to $7fff. 32 characters in each row for 21 character rows) so the rest of the game lies contiguously below the screen memory.

### The 'Watch Tower' bug
![Watch Tower](watch.png)

Enough admin, onto the first bug fix. The original BBC version has a bug where the game crashes as soon as the player visits 'Watch Tower'. This bug is present on the [Complete BBC Micro Games archive version (maybe disk based?)](http://www.bbcmicro.co.uk/game.php?id=439)  but not the [Level 7 disassembly (maybe cassette based?)](http://www.level7.org.uk/miscellany/jet-set-willy-disassembly.txt). The reason for the bug is that the code that loads and runs the second file of the game is located exactly where this room definition is supposed to be. The original room definition has now been restored, and the bug is fixed.

### Removing OS Usage
At this point I start to remove all use of the OS. The game uses OSWRCH to write text (and more), OSWORD for sound, and OSBYTE for keyboard, vsync etc. Although I need to write more code to replace these OS routines, it does save memory overall in that the game can use more memory locations if the OS no longer uses them. Use of OSWRCH is replaced first, then sound routines are replaced, then keyboard. I can use more of zero page for variables, which saves memory for each instance a variable is accessed.

### Interrupts and Palette Changes
The only part of the OS that continues to run (necessarily) is the handling of IRQs. The game uses these interrupts to switch palette colours at any character row down the screen. At each character row in the game area, one palette change can occur. A different palette altogether is switched in for the 'footer' area of the screen. Interrupts are also used for updating the music and sound, and updating timers for the game. But to use this new palette changing facility, I need to be able to edit the room data.

### The Room Data
The rooms are compressed. Each room is encoded as a stream of bits, with different numbers of bits required for different data. This is described in [the Level 7 disassembly](http://www.level7.org.uk/miscellany/jet-set-willy-disassembly.txt). To be able to edit this data, I first need to decode the existing encoded bytes into an editable text file. I wrote a C# .NET Core console application to do this. The result is 'definitions.txt', a text file describing exactly the information required by the game to show each room. I also include the sprite definitions in this text file too.

The next step is to write a tool that can read 'definitions.txt' and produce an encoded version of it in bytes (as ASM assembly source). This is a second C# .NET Core console application. I took the time to make sure that the resulting bytes are identical to the original bytes. Now every time I assemble the game, I encode the latest data too.

### The Bathroom (before and after)
![Bathroom](bathroom.png)

The level and sprite data is now editable, so I add new data. The tile sprite types (i.e. wall, platform, deadly, slope, conveyor, scenery) for a room now have two colours each instead of one (the Spectrum calls these two colours PAPER and INK). Walls in the Bathroom can be red and yellow as per the Spectrum for example, rather than being one single colour always against black.

I also add new data for each room to allow a palette change per character row. e.g. In 'The Bathroom', the enemy at the top of the room moving left and right is now coloured green (as per the Spectrum) by changing a colour of the palette to green for those two rows. Note that each row can still only show at most four colours. This leads to some compromises, notice the wall behind the toilet is black and white not yellow and blue.

Now I have these colourful abilities I take a sweep through the whole mansion, painting by numbers. It really brightens the place up. More sweeps happened later where I checked the positions and definitions of the tiles, the enemies' initial positions, directions, speeds, and extents. There were many many changes. I also corrected the position and titles of each of the rooms (e.g. correcting 'Coservatory Roof' to 'Conservatory Roof') and expanded the compression for room names to accommodate full stops in the room titles. All these changes aligned the game closer to the Spectrum version.

I also added a new 'scenery' tile type to help get the room definitions closer to the Spectrum in one or two places.

I moved the start position of the player to the correct position (at the end the bath, as per the Spectrum). Willy faces right initially. I've not replicated the Spectrum bug where Willy starts looking left if the previous game ended with willy left. The philosophy here is to not slavishly follow every little quirk of the Spectrum version, but I do use it to guide towards a good Jet Set Willy experience.

We start the game at 7:00am as per the Spectrum (not 7:00pm), working through until 1:00am at a similar rate to the Spectrum.

For reference, here is 'The Bathroom' on the Spectrum:

![Bathroom](bathroom_spec.png)

### Border Colour
One feature from the Spectrum that didn't make it into the final game was a border colour. The border helps define the edges of the room giving a more enclosed feel. The BBC Micro has no support for a hardware border colour. Experiments showed it was just about possible to change the palette at the top and bottom of the screen (although there were timing issues since the entire palette needed changing very quickly) but for a border to look good you really need the left and right edges too. This couldn't be done with palette changes so would need bytes written to screen memory. This would limit the border colour to one of the four colours on screen, and would take a lot of extra memory that is in short supply. Reluctantly this feature had to go.

### Arrows
Arrows were missing from rooms with ropes (this was because of a collision issue: ropes would notice something was colliding with it and assumed it was Willy). I fixed this by making the arrows a different logical colour from Willy and checking specifically for Willy's colour on a rope collision. I then reinstated all arrows as needed. I retimed all the arrows as per the Spectrum, and fixed a bug in the rendering of arrows that left a hole in the wall of 'A bit of tree'. Arrow sounds are now timed as per the Spectrum to give the player warning of their arrival.

### The Rope
I fixed the swing offsets of the rope to match the Spectrum, and moved 'The Beach' rope two character cells left to match the Spectrum. I tweaked the logic to make the player move a little better on the rope. The rope is flicker free.

### Sprites
I added back in the missing enemy guard sprite as found in 'Rescuing Esmerelda' and 'Above the West Bedroom'.

I also removed several unused tile sprites.

I added some code to reflect enemy sprites from their definitions into the screen ready cache. This means we can store 4 sprites rather than 8 for some enemies that move left and right (this affects the Monk, Saw, Pig, Bird, and Penguin).

All sprites are now compressed to save memory, and are decompressed at runtime as needed. The compression is nybble based, decompressing one byte at a time:

    0-3     this byte is the same as a byte previously decoded in this sprite (previous byte, previous byte but one, but two, but three)
    4-9     this byte is one of the 6 predetermined most common bytes (stored in a table)
    10-14   The next nybble together with this nybble specifies one of the 80 next most common bytes (stored in a table)
    15      The next two nybbles specify the value of the byte

Thus we save memory on bytes that can be encoded as 0-9, break even on encoding 10-14, and use an extra nybble when encoding 15 is required. Additionally, some sprites don't compress well. We encode these instead as raw bytes (pairs of nybbles), with the first nybble of the sprite encoded as 0 to indicate a raw encoding (since value 0 or any value 0-3 wouldn't otherwise occur at the start of a sprite).

### Enemies
Vertical enemies spin at a medium speed, with the Razor Blade enemies spinning fast. The Monk in the Chapel remains steadfastly looking left (as if possessed?), as per the Spectrum.

### Items
Items twinkle individually, rather than in waves of colour previously. e.g. see 'Ballroom West'.

### Lives
The remaining lives are shown by a line of Willy characters walking right. This is unlike the Spectrum where they are static, but apes Manic Miner instead.

### Animated Scenery

![First Landing](first.gif)

I added the feature from the Spectrum that the cross in the 'First Landing' flashes, and other places such as 'Nomen Luni' other scenery flashes too.

### The Game Over Screen
This animates in a standard palette of colours, and flashes the 'GAME OVER' letters individually.

### The Title Screen

![Title Screen](title.gif)

The title screen uses the same palette changing technology as described above to cycle through colours. The scrolling text moves a little smoother now while still retaining the speed (moving four pixels at a time instead of eight). The scrolling text is tidied up slightly (the name of the game is 'Jet Set Willy' not 'Jetset Willy', and I switched to 'BBC Micro' not 'BBC micro'). The Moonlight Sonata plays, with less screech than the Spectrum.

### Spectrum Font
The spectrum font was added and used throughout. Prior to this point I was reading the OS definitions for the characters from ROM and this needed Master specific code. In the end I found enough space to encode the characters we need from the Spectrum font, which feels nicer. The font sprites are compressed in the same way as all the other sprites.

### Music
I have updated the in game tune to be longer, more accurate, and gentler on the ears. Moonlight Sonata plays on the title screen.

## Thanks
* Thanks to Graham Nelson for suggesting various good ideas, including the palette swapping trick and sprite compression scheme.

*  http://www.level7.org.uk/miscellany/jet-set-willy-disassembly.txt
An excellent disassembly for understanding the BBC Micro version. This was the starting point.

* https://skoolkit.ca/disassemblies/jet_set_willy/hex/
The definitive place for discovering exactly how the original Spectrum game works.

* http://mdfs.net/Software/JSW/BBC/
More disassemblies, including patched versions of JSW 2.
