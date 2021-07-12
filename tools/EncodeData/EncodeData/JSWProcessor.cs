using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EncodeData
{
    enum State
    {
        None = 0,
        Room,
        BackgroundSprite,
        FontSprite,
        Enemies
    }

    enum Dir
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3
    }

    class TileType
    {
        public string spriteName = "";
        public int fg_colour;
        public int bg_colour;
    }

    class Title
    {
        public int tab;
        public string name;
    }

    class Enemy
    {
        public string sprite = "";
        public int initialX;
        public int initialY;
        public int min;
        public int max;
        public Dir dir;
        public int speed;
        public int logicalColour;
        public bool withReverse;

        public Enemy(bool reverse)
        {
            withReverse = reverse;
        }
    }

    class Arrow
    {
        public bool valid;
        public int y;
        public int timing;

        public Arrow()
        {
            valid = false;
        }

        public Arrow(int y_val, int timing_val)
        {
            valid = true;
            y = y_val;
            timing = timing_val;
        }
    }

    class TilePos
    {
        public int X;
        public int Y;

        public TilePos(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    abstract class TileCommand
    {
    }

    class UseTileCommand : TileCommand
    {
        public int tileType;

        public UseTileCommand(int tt)
        {
            tileType = tt;
        }

    }

    class XYTileCommand : TileCommand
    {
        public int X;
        public int Y;

        public XYTileCommand(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    class StripTileCommand : TileCommand
    {
        public bool dir;
        public int extent;

        public StripTileCommand(bool d, int ext)
        {
            dir = d;
            extent = ext;
        }
    }

    class BlockTileCommand : TileCommand
    {
        public int extentX;
        public int finalY;

        public BlockTileCommand(int eX, int fY)
        {
            extentX = eX;
            finalY = fY;
        }
    }

    class ListTileCommand : TileCommand
    {
        public List<TilePos> points = new List<TilePos>();
    }

    class SlopeTileCommand : TileCommand
    {
        public bool dir;
        public int finalY;

        public SlopeTileCommand(bool d, int fY)
        {
            dir = d;
            finalY = fY;
        }
    }

    class TriangleTileCommand : TileCommand
    {
        public bool dir;
        public int final_y;

        public TriangleTileCommand(bool d, int ext)
        {
            dir = d;
            final_y = ext;
        }
    }

    class Sprite
    {
        public string spriteId;
        public List<int> bytes = new List<int>();
        public int width;
        public int height;

        public Sprite(string name, int width, int height)
        {
            spriteId = name;
            this.width = width;
            this.height = height;
            Debug.Assert((width == 8) || (width == 16));
            Debug.Assert((height == 8) || (height == 16));
        }

        public void AddRow(int val)
        {
            Debug.Assert(val >= 0);
            if (width == 8)
            {
                Debug.Assert(val < 256);
                bytes.Add(val);
            }
            else if (width == 16)
            {
                Debug.Assert(val < 65536);

                bytes.Add(val % 256);
                bytes.Add(val / 256);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        public int GetRow(int rowIndex)
        {
            if (width == 8)
            {
                return bytes[rowIndex];
            }
            else if (width == 16)
            {
                return bytes[rowIndex*2] + 256 * bytes[1 + (rowIndex*2)];
            }
            else
            {
                Debug.Assert(false);
                return 0;
            }
        }
    }


    class EnemySprites
    {
        public string enemyId;
        public bool withReverse;
        public List<Sprite> sprites = new List<Sprite>();

        public EnemySprites(string name, bool reverse)
        {
            enemyId = name;
            withReverse = reverse;
        }
    }

    class Usage
    {
        public string usageName;
        public int uses;

        public Usage(string usageName, int uses)
        {
            this.usageName = usageName;
            this.uses = uses;
        }
    }

    // ************************************************************************
    class PaletteChange
    {
        public int row;
        public int logicalColour;
        public int physicalColour;

        public PaletteChange(int row, int logicalColour, int physicalColour)
        {
            this.row = row;
            this.logicalColour = logicalColour;
            this.physicalColour = physicalColour;
        }
    }

    // ************************************************************************
    class Room
    {
        public int room_number = -1;
        public List<bool> itemsCollected = new List<bool>();
        public List<int> exits = new List<int>();
        public bool conveyorDir = false;
        public bool slopeDir = false;
        public bool ropePresent = false;
        public TileType itemTile = new TileType();
        public TileType sceneryTile = new TileType();
        public TileType deadlyTile = new TileType();
        public TileType conveyorTile = new TileType();
        public TileType slopeTile = new TileType();
        public TileType wallTile = new TileType();
        public TileType platformTile = new TileType();
        public List<int> palette = new List<int>();
        public List<PaletteChange> paletteChanges = new List<PaletteChange>();
        public Title title = new Title();
        public List<TileCommand> commands = new List<TileCommand>();
        public List<Enemy> enemies = new List<Enemy>();
        public List<Arrow> arrows = new List<Arrow>();

        // ********************************************************************
        public List<int> GetBytes(JSWProcessor processor)
        {
            processor.StartBits();

            if (!string.IsNullOrEmpty(title.name))
            { 
                // items collected
                processor.AddBits(4, itemsCollected.Count);
                foreach(var item in itemsCollected)
                {
                    processor.AddBit(item);
                }

                // exits
                foreach(var exit in exits)
                { 
                    processor.AddBits(6, exit);
                }

                processor.AddBit(conveyorDir);
                processor.AddBit(slopeDir);
                processor.AddBit(ropePresent);

                processor.AddBits(2, sceneryTile.fg_colour);
                processor.AddBits(2, sceneryTile.bg_colour);
                processor.AddBits(2, deadlyTile.fg_colour);
                processor.AddBits(2, deadlyTile.bg_colour);
                processor.AddBits(2, conveyorTile.fg_colour);
                processor.AddBits(2, conveyorTile.bg_colour);
                processor.AddBits(2, slopeTile.fg_colour);
                processor.AddBits(2, slopeTile.bg_colour);
                processor.AddBits(2, wallTile.fg_colour);
                processor.AddBits(2, wallTile.bg_colour);
                processor.AddBits(2, platformTile.fg_colour);
                processor.AddBits(2, platformTile.bg_colour);

                processor.AddBits(8, processor.GetBackgroundSpriteIndex(itemTile.spriteName));
                processor.AddBits(8, processor.GetBackgroundSpriteIndex(sceneryTile.spriteName));
                processor.AddBits(8, processor.GetBackgroundSpriteIndex(deadlyTile.spriteName));
                processor.AddBits(8, processor.GetBackgroundSpriteIndex(conveyorTile.spriteName));
                processor.AddBits(8, processor.GetBackgroundSpriteIndex(slopeTile.spriteName));
                processor.AddBits(8, processor.GetBackgroundSpriteIndex(wallTile.spriteName));
                processor.AddBits(8, processor.GetBackgroundSpriteIndex(platformTile.spriteName));

                // Palette
                palette.Reverse();
                foreach(var entry in palette)
                {
                    processor.AddBits(3, entry);
                }
                palette.Reverse();

                // Palette changes
                processor.AddBits(4, paletteChanges.Count);
                foreach(var change in paletteChanges)
                {
                    processor.AddBits(4, change.row);
                    processor.AddBits(5, change.logicalColour + 4 * change.physicalColour);
                }

                processor.AddBits(4, title.tab);

                // Encode title

                // The title is encoded in 5 bit chunks.
                // The first chunk is one less than the title length (in chunks)
                // Each chunk is then:
                // 0-25     $00-$19     a letter of the alphabet
                // 26       $1a         the next letter is capitalised
                // 27       $1b         apostrophe
                // 28       $1c         full stop
                // 29       $1d         space
                // 30       $1e         space + the next letter is capitalised
                // 31       $1f         "the" (can be capitalised)

                var titleBits = new List<int>();
                var isUpperNext = true;
                for(int i = 0; i < title.name.Length; i++)
                {
                    if (i < title.name.Length - 3)
                    {
                        if (title.name.Substring(i, 3).ToLowerInvariant() == "the")
                        {
                            titleBits.Add(0x1f);
                            i = i + 2;
                            isUpperNext = false;
                            continue;
                        }
                    }
                    int c = (int) title.name[i];
                    if ((c >= 'a') && (c <= 'z'))
                    {
                        Debug.Assert(isUpperNext == false);
                        titleBits.Add(c - 'a');
                        isUpperNext = false;
                        continue;
                    }
                    if ((c >= 'A') && (c <= 'Z'))
                    {
                        if (!isUpperNext)
                        {
                            titleBits.Add(0x1a);
                        }
                        titleBits.Add(c - 'A');
                        isUpperNext = false;
                        continue;
                    }
                    if (c == '\'')
                    {
                        titleBits.Add(0x1b);
                        continue;
                    }
                    if (c == '.')
                    {
                        titleBits.Add(0x1c);
                        continue;
                    }
                    if (c == ' ')
                    {
                        if (i < (title.name.Length - 1))
                        {
                            string left = title.name.Substring(i + 1).TrimStart();
                            if (left.Length > 0)
                            {
                                char nextC = left[0];
                                if (char.IsUpper(nextC))
                                {
                                    titleBits.Add(0x1e);
                                    isUpperNext = true;
                                    continue;
                                }
                            }
                        }
                        titleBits.Add(0x1d);
                    }
                }

                // Length (5 bits) followed by the encoded title bits
                processor.AddBits(5, titleBits.Count - 1);
                foreach(var titleBit in titleBits)
                {
                    processor.AddBits(5, titleBit);
                }

                // Commands
                foreach(var command in commands)
                {
                    if (command is UseTileCommand use)
                    {
                        processor.AddBits(2, 0);
                        processor.AddBits(3, use.tileType);
                    }
                    else if (command is XYTileCommand move)
                    {
                        processor.AddBits(2, 1);
                        processor.AddBits(5, move.X);
                        processor.AddBits(4, move.Y);
                    }
                    else if (command is StripTileCommand strip)
                    {
                        processor.AddBits(2, 2);
                        processor.AddBits(1, strip.dir ? 1 : 0);
                        processor.AddBits(5, strip.extent);
                    }
                    else if (command is BlockTileCommand block)
                    {
                        processor.AddBits(2, 3);
                        processor.AddBits(2, 0);
                        processor.AddBits(5, block.extentX);
                        processor.AddBits(4, block.finalY);
                    }
                    else if (command is ListTileCommand list)
                    {
                        processor.AddBits(2, 3);
                        processor.AddBits(2, 1);
                        processor.AddBits(4, list.points.Count - 1);
                        foreach(var point in list.points)
                        {
                            processor.AddBits(5, point.X);
                            processor.AddBits(4, point.Y);
                        }
                    }
                    else if (command is SlopeTileCommand slope)
                    {
                        processor.AddBits(2, 3);
                        processor.AddBits(2, 2);
                        processor.AddBits(1, slope.dir ? 1 : 0);
                        processor.AddBits(4, slope.finalY);
                    }
                    else if (command is TriangleTileCommand tri)
                    {
                        processor.AddBits(2, 3);
                        processor.AddBits(2, 3);
                        processor.AddBits(2, 2);
                        processor.AddBits(1, tri.dir ? 1 : 0);
                        processor.AddBits(4, tri.final_y);
                    }
                }

                processor.AddBits(2, 3);        // End of tile data
                processor.AddBits(2, 3);
                processor.AddBits(2, 0);
            }

            // Enemies
            processor.AddBits(3, enemies.Count);
            foreach(var enemy in enemies)
            {
                processor.AddBits(6, processor.GetEnemySpriteIndex(enemy.sprite));
                processor.AddBits(5, enemy.initialX);
                processor.AddBits(4, enemy.initialY);
                processor.AddBits(5, enemy.min);
                processor.AddBits(5, enemy.max);
                processor.AddBit((enemy.dir == Dir.Up) || (enemy.dir == Dir.Down));
                processor.AddBit((enemy.dir == Dir.Right) || (enemy.dir == Dir.Down));
                processor.AddBits(3, enemy.speed);
                processor.AddBits(2, enemy.logicalColour);
            }

            // Arrows
            foreach(var arrow in arrows)
            {
                processor.AddBit(arrow.valid);
                if (arrow.valid)
                {
                    processor.AddBits(4, arrow.y);
                    processor.AddBits(1, arrow.timing);
                }
            }

            processor.EndBits();
            return new List<int>(JSWProcessor.bytes);
        }
    }

    // ************************************************************************
    class JSWProcessor
    {
        // For input
        State state = State.None;
        string stateName;
        Room currentRoom;
        List<Room> rooms;
        public List<Sprite> backgroundSprites = new List<Sprite>();
        public List<Sprite> fontSprites = new List<Sprite>();
        public bool isBackgroundSpriteLine = false;
        public bool isFontSpriteLine = false;
        public List<EnemySprites> enemies = new List<EnemySprites>();
        public bool isEnemyLine = false;

        // For output
        public static List<int> bytes = new List<int>();
        public static int bits_within_byte = 8;
        const int numberOfPreviousBytes = 4;


        // ********************************************************************
        public void StartBits()
        {
            bytes.Clear();
            bits_within_byte = 8;
        }

        // ********************************************************************
        public void EndBits()
        {
            while (bits_within_byte < 8)
            {
                AddBit(false);
            }
        }

        // ********************************************************************
        public void AddBit(bool bit)
        {
            if (bits_within_byte > 7)
            {
                // DEBUG
                /*
                if (bytes.Count > 0)
                { 
                    Console.WriteLine(bytes.Last().ToString("X2"));
                }
                */
                bytes.Add(0);
                bits_within_byte = 0;
            }

            // shift right
            bytes[bytes.Count - 1] /= 2;

            // add bit at top
            if (bit)
            {
                bytes[bytes.Count - 1] |= 128;
            }
            bits_within_byte++;
        }

        // ********************************************************************
        public void AddBits(int numBits, int value)
        {
            if (value >= (1 << numBits))
            {
                Console.WriteLine("Error encoding bits, number too large.");
            }
            for(int i = 0; i < numBits; i++)
            {
                AddBit((value & (1 << (numBits-1-i))) != 0);
            }
        }

        // ********************************************************************
        public static bool IsMatch(string line, string pattern, out List<string> results)
        {
            var match = new Regex(pattern).Match(line);
            if (match.Success)
            { 
                results = match.Groups.Cast<Group>().Skip(1).Where(o => o.Value != "").Select(o => o.Value).ToList();
            }
            else
            {
                results = new List<string>();
            }
            return match.Success;
        }

        // ********************************************************************
        public void LeaveState(State state)
        {
            LeaveRoomState();
        }

        // ********************************************************************
        public void EnterState(State state)
        {
            switch(state)
            {
                case State.Room: EnterRoomState(); break;
            }
        }

        // ********************************************************************
        public void ChangeState(State state, string name)
        {
            LeaveState(state);

            this.state = state;
            this.stateName = name;

            EnterState(this.state);
        }

        // ********************************************************************
        public int GetColourNumber(string colourName)
        {
            switch(colourName)
            {
                case "black":   return 0;
                case "red":     return 1;
                case "green":   return 2;
                case "yellow":  return 3;
                case "blue":    return 4;
                case "magenta": return 5;
                case "cyan":    return 6;
                case "white":   return 7;
            }
            return -1;
        }

        // ********************************************************************
        public int GetBackgroundSpriteIndex(string spriteId)
        {
            for(int i = 0; i < backgroundSprites.Count; i++)
            {
                if (backgroundSprites[i].spriteId == spriteId)
                {
                    return i;
                }
            }
            return -1;
        }

        // ********************************************************************
        public int GetEnemySpriteIndex(string enemyId)
        {
            for(int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].enemyId == enemyId)
                {
                    return i + 1;
                }
            }
            return 0;
        }

        // ********************************************************************
        public void LeaveRoomState()
        {
            if (currentRoom != null)
            { 
                rooms.Add(currentRoom);
            }
            currentRoom = null;
        }

        // ********************************************************************
        public void EnterRoomState()
        {
            currentRoom = new Room();
            currentRoom.room_number = int.Parse(stateName);
        }

        // ********************************************************************
        public void ParseRoomLine(string line)
        {
            List<string> results;
            line = line.Trim();

            if (IsMatch(line, @"Number of items *: *(\d+) *(.*) *$", out results))
            {
                var numItems = int.Parse(results[0]);
                if (results.Count > 1)
                { 
                    currentRoom.itemsCollected = results[1].Replace("(", "").Replace(")", "").Split(new char[] { ',' }).Select((x) => x.Trim() == "collected" ? true : false).ToList();
                    if (numItems != currentRoom.itemsCollected.Count)
                    {
                        Console.WriteLine("WARNING: Number of items " + numItems + " does not match the number of collected states " + currentRoom.itemsCollected.Count);
                    }
                }
                return;
            }

            if (IsMatch(line, @"Exits *: +left (\d+) *, *right *(\d+) *, *up *(\d+) *, *down *(\d+)", out results))
            {
                currentRoom.exits.Add(int.Parse(results[0]));
                currentRoom.exits.Add(int.Parse(results[1]));
                currentRoom.exits.Add(int.Parse(results[2]));
                currentRoom.exits.Add(int.Parse(results[3]));
                return;
            }

            if (IsMatch(line, @"Conveyor direction *: (.*)", out results))
            {
                var dirString = results[0].Trim();

                var dict = new Dictionary<string, bool> { { "left", false }, { "right", true } };
                if (!dict.TryGetValue(dirString, out currentRoom.conveyorDir))
                {
                    Console.WriteLine("WARNING: Could not parse conveyor direction " + dirString);
                }
                return;
            }

            if (IsMatch(line, @"Slope direction *: (.*)", out results))
            {
                var dirString = results[0].Trim();

                var dict = new Dictionary<string, bool> { { @"/", false }, { @"\", true } };
                if (!dict.TryGetValue(dirString, out currentRoom.slopeDir))
                {
                    Console.WriteLine("WARNING: Could not parse slope direction " + dirString);
                }
                return;
            }

            if (IsMatch(line, @"Rope present *: (.*)", out results))
            {
                var dirString = results[0].Trim();

                var dict = new Dictionary<string, bool> { { @"no", false }, { @"yes", true } };
                if (!dict.TryGetValue(dirString, out currentRoom.ropePresent))
                {
                    Console.WriteLine("WARNING: Could not parse rope presence " + dirString);
                }
                return;
            }

            if (IsMatch(line, @"Scenery tile logical colours *: (.*)", out results))
            {
                currentRoom.sceneryTile.bg_colour = int.Parse(results[0].Split(new char[] { ' ' })[0]);
                currentRoom.sceneryTile.fg_colour = int.Parse(results[0].Split(new char[] { ' ' })[1]);
                return;
            }

            if (IsMatch(line, @"Deadly tile logical colours *: (.*)", out results))
            {
                currentRoom.deadlyTile.bg_colour = int.Parse(results[0].Split(new char[] { ' ' })[0]);
                currentRoom.deadlyTile.fg_colour = int.Parse(results[0].Split(new char[] { ' ' })[1]);
                return;
            }

            if (IsMatch(line, @"Conveyor tile logical colours *: (.*)", out results))
            {
                currentRoom.conveyorTile.bg_colour = int.Parse(results[0].Split(new char[] { ' ' })[0]);
                currentRoom.conveyorTile.fg_colour = int.Parse(results[0].Split(new char[] { ' ' })[1]);
                return;
            }

            if (IsMatch(line, @"Slope tile logical colours *: (.*)", out results))
            {
                currentRoom.slopeTile.bg_colour = int.Parse(results[0].Split(new char[] { ' ' })[0]);
                currentRoom.slopeTile.fg_colour = int.Parse(results[0].Split(new char[] { ' ' })[1]);
                return;
            }

            if (IsMatch(line, @"Wall tile logical colours *: (.*)", out results))
            {
                currentRoom.wallTile.bg_colour = int.Parse(results[0].Split(new char[] { ' ' })[0]);
                currentRoom.wallTile.fg_colour = int.Parse(results[0].Split(new char[] { ' ' })[1]);
                return;
            }

            if (IsMatch(line, @"Platform tile logical colours *: (.*)", out results))
            {
                currentRoom.platformTile.bg_colour = int.Parse(results[0].Split(new char[] { ' ' })[0]);
                currentRoom.platformTile.fg_colour = int.Parse(results[0].Split(new char[] { ' ' })[1]);
                return;
            }


            if (IsMatch(line, @"Scenery tile sprite *: (.*)", out results))
            {
                currentRoom.sceneryTile.spriteName = results[0];
                return;
            }

            if (IsMatch(line, @"Item tile sprite *: (.*)", out results))
            {
                currentRoom.itemTile.spriteName = results[0];
                return;
            }

            if (IsMatch(line, @"Deadly tile sprite *: (.*)", out results))
            {
                currentRoom.deadlyTile.spriteName = results[0];
                return;
            }

            if (IsMatch(line, @"Conveyor tile sprite *: (.*)", out results))
            {
                currentRoom.conveyorTile.spriteName = results[0];
                return;
            }

            if (IsMatch(line, @"Slope tile sprite *: (.*)", out results))
            {
                currentRoom.slopeTile.spriteName = results[0];
                return;
            }

            if (IsMatch(line, @"Wall tile sprite *: (.*)", out results))
            {
                currentRoom.wallTile.spriteName = results[0];
                return;
            }

            if (IsMatch(line, @"Platform tile sprite *: (.*)", out results))
            {
                currentRoom.platformTile.spriteName = results[0];
                return;
            }

            if (IsMatch(line, @"Palette *: ([a-z]+) *, *([a-z]+) *, *([a-z]+) *, *([a-z]+)", out results))
            {
                currentRoom.palette.Add(GetColourNumber(results[0]));
                currentRoom.palette.Add(GetColourNumber(results[1]));
                currentRoom.palette.Add(GetColourNumber(results[2]));
                currentRoom.palette.Add(GetColourNumber(results[3]));
                return;
            }

            if (IsMatch(line, @"Palette *change *: +(\d+) +(\d+) +([a-z]+) *", out results))
            {
                int logicalColour = int.Parse(results[1]);
                int physicalColour;

                if (results[2] == "cancel")
                {
                    physicalColour = currentRoom.palette[logicalColour];
                }
                else
                { 
                    physicalColour = GetColourNumber(results[2]);
                }
                currentRoom.paletteChanges.Add(new PaletteChange(int.Parse(results[0]), logicalColour, physicalColour));
                return;
            }

            if (IsMatch(line, @"Title *: +tab +(\d+) *, *""(.*)""", out results))
            {
                currentRoom.title.tab = int.Parse(results[0]);
                currentRoom.title.name = results[1];
                return;
            }

            // Tile commands
            if (IsMatch(line, @"Use ([A-Z]+)", out results))
            {
                int use = 0;
                switch(results[0])
                {
                    case "PLATFORM": use = 0; break;
                    case "WALL":     use = 1; break;
                    case "SLOPE":    use = 2; break;
                    case "CONVEYOR": use = 3; break;
                    case "DEADLY":   use = 4; break;
                    case "SCENERY":  use = 5; break;
                    case "ITEM":     use = 6; break;
                }

                currentRoom.commands.Add(new UseTileCommand(use));
                return;
            }

            if (IsMatch(line, @"Move to \((\d+) *, *(\d+)\)", out results))
            {
                var x = int.Parse(results[0]);
                var y = int.Parse(results[1]);
                currentRoom.commands.Add(new XYTileCommand(x, y));
                return;
            }

            if (IsMatch(line, @"Draw vertical strip until Y=(\d+)", out results))
            {
                var finalY = int.Parse(results[0]);
                currentRoom.commands.Add(new StripTileCommand(true, finalY));
                return;
            }

            if (IsMatch(line, @"Draw horizontal strip until X=(\d+)", out results))
            {
                var finalX = int.Parse(results[0]);
                currentRoom.commands.Add(new StripTileCommand(false, finalX));
                return;
            }

            if (IsMatch(line, @"Draw block to \((\d+) *, *(\d+)\)", out results))
            {
                var x = int.Parse(results[0]);
                var y = int.Parse(results[1]);
                currentRoom.commands.Add(new BlockTileCommand(x, y));
                return;
            }
            if (IsMatch(line, @"Draw slope moving (left|right) until Y=(\d+)", out results))
            {
                var dir = (results[0] == "left");
                var final_y = int.Parse(results[1]);
                currentRoom.commands.Add(new SlopeTileCommand(dir, final_y));
            }
            if (IsMatch(line, @"Draw triangle moving (left|right) until Y=(\d+)", out results))
            {
                var dir = (results[0] == "left");
                var final_y = int.Parse(results[1]);
                currentRoom.commands.Add(new TriangleTileCommand(dir, final_y));
            }
            if (IsMatch(line, @"Draw (\d+) single tiles? at (.*)$", out results))
            {
                var list = new ListTileCommand();

                var matches = new Regex(@"\((\d+) *, *(\d+)\)").Matches(results[1]);
                for(int i = 0; i < matches.Count; i++)
                {
                    list.points.Add(new TilePos(
                        int.Parse(matches[i].Groups[1].Value),
                        int.Parse(matches[i].Groups[2].Value) ));
                }
                currentRoom.commands.Add(list);
                return;
            }

            if (IsMatch(line, @"Enemy *: *(\d+)( +[Ww]ith [Rr]everse)?", out results))
            {
                currentRoom.enemies.Add(new Enemy(results.Count > 1));
                return;
            }
            if (IsMatch(line, @"Sprite *: *(.*)$", out results))
            {
                var enemy = currentRoom.enemies.Last();
                enemy.sprite = results[0];
                return;
            }
            if (IsMatch(line, @"Initial Pos *: *\((\d+) *, *(\d+) *\)", out results))
            {
                var enemy = currentRoom.enemies.Last();
                enemy.initialX = int.Parse(results[0]);
                enemy.initialY = int.Parse(results[1]);
                return;
            }
            if (IsMatch(line, @"Min Extent *: *(.*)$", out results))
            {
                var enemy = currentRoom.enemies.Last();
                enemy.min = int.Parse(results[0]);
                return;
            }
            if (IsMatch(line, @"Max Extent *: *(.*)$", out results))
            {
                var enemy = currentRoom.enemies.Last();
                enemy.max = int.Parse(results[0]);
                return;
            }
            if (IsMatch(line, @"Initial Dir *: *(.*)$", out results))
            {
                var enemy = currentRoom.enemies.Last();
                switch (results[0])
                {
                    case "left": enemy.dir = Dir.Left; break;
                    case "right": enemy.dir = Dir.Right; break;
                    case "up": enemy.dir = Dir.Up; break;
                    case "down": enemy.dir = Dir.Down; break;
                    default:
                        {
                            Console.WriteLine("WARNING: Could not parse Initial Dir line '" + line + "'");
                        }
                        break;
                }
                return;
            }
            if (IsMatch(line, @"Speed *: *(.*)$", out results))
            {
                var enemy = currentRoom.enemies.Last();
                enemy.speed = int.Parse(results[0]);
                return;
            }
            if (IsMatch(line, @"Logical colour *: *(.*)$", out results))
            {
                var enemy = currentRoom.enemies.Last();
                enemy.logicalColour = int.Parse(results[0]);
                return;
            }

            if (IsMatch(line, @"Arrow *: *no", out results))
            {
                currentRoom.arrows.Add(new Arrow());
            }
            if (IsMatch(line, @"Arrow *: *Y *(\d+) *, *X +index *(\d+)", out results))
            {
                currentRoom.arrows.Add(new Arrow(int.Parse(results[0]), int.Parse(results[1])));
            }
        }

        // ********************************************************************
        public void ParseBackgroundSpriteLine(string line)
        {
            line = line.Trim();
            if (IsMatch(line, @"BackgroundSprite +(\d+)", out var results))
            {
                backgroundSprites.Add(new Sprite(results[0], 8, 8));
                isBackgroundSpriteLine = true;
            }
            else if (IsMatch(line, @"^([\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#])$", out results))
            {
                if (!isBackgroundSpriteLine)
                {
                    Console.WriteLine("WARNING: Could not parse sprite '" + backgroundSprites.Last().spriteId + "'");
                }

                // Get bits into bytes
                var val = 0;
                for(int i = 0; i < 8; i++)
                {
                    if (results[0][i] == '#')
                    {
                        val += 1 << (7-i);
                    }
                }
                backgroundSprites.Last().AddRow(val);
            }
            else if (!string.IsNullOrEmpty(line))
            {
                isBackgroundSpriteLine = false;
            }
        }

        // ********************************************************************
        public void ParseFontSpriteLine(string line)
        {
            line = line.Trim();
            if (IsMatch(line, @"FontSprite *", out var results))
            {
                fontSprites.Add(new Sprite("", 8, 8));
                isFontSpriteLine = true;
            }
            else if (IsMatch(line, @"^([\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#])$", out results))
            {
                if (!isFontSpriteLine)
                {
                    Console.WriteLine("WARNING: Could not parse font sprite '" + fontSprites.Last().spriteId + "'");
                }

                // Get bits into bytes
                var val = 0;
                for(int i = 0; i < 8; i++)
                {
                    if (results[0][i] == '#')
                    {
                        val += 1 << (7-i);
                    }
                }
                fontSprites.Last().AddRow(val);
            }
            else if (!string.IsNullOrEmpty(line))
            {
                isFontSpriteLine = false;
            }
        }

        // ********************************************************************
        public void ParseEnemiesLine(string line)
        {
            line = line.Trim();
            if (IsMatch(line, @"Enemy +(\d+)( +[Ww]ith [Rr]everse)?", out var results))
            {
                enemies.Add(new EnemySprites(results[0], results.Count > 1));
                isEnemyLine = true;
            }
            else if (line == "Sprite")
            {
                enemies.Last().sprites.Add(new Sprite("", 16, 16));
                isEnemyLine = true;
            }
            else if (IsMatch(line, @"^([\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#][\.\#])$", out results))
            {
                if (!isEnemyLine)
                {
                    Console.WriteLine("WARNING: Could not parse sprite '" + backgroundSprites.Last().spriteId + "'");
                }

                // Get bits into bytes
                var val = 0;
                for(int i = 0; i < 16; i++)
                {
                    if (results[0][i] == '#')
                    {
                        val += 1 << (15-i);
                    }
                }
                enemies.Last().sprites.Last().AddRow(val);
            }
            else if (!string.IsNullOrEmpty(line))
            {
                isEnemyLine = false;
            }
        }

        // ********************************************************************
        public void ReadInput(string inputFilepath)
        {
            var lines = File.ReadAllLines(inputFilepath);

            var roomPattern = new Regex(@"^Room +([A-Za-z0-9_]+)$");
            var backgroundSpritePattern = new Regex(@"^BackgroundSprite +([A-Za-z0-9_]+)$");
            var fontSpritePattern = new Regex(@"^FontSprite *$");
            var enemyPattern = new Regex(@"^Enemy +([A-Za-z0-9_]+)$");

            rooms = new List<Room>();
            backgroundSprites = new List<Sprite>();
            fontSprites = new List<Sprite>();

            foreach(var rawLine in lines)
            {
                // Remove comments
                var line = rawLine.Split(new char[] { ';' })[0];

                // Look for state change
                var roomMatch = roomPattern.Match(line);
                if (roomMatch.Success)
                {
                    ChangeState(State.Room, roomMatch.Groups[1].Value);
                }
                var backgroundSpriteMatch = backgroundSpritePattern.Match(line);
                if (backgroundSpriteMatch.Success)
                {
                    ChangeState(State.BackgroundSprite, backgroundSpriteMatch.Groups[1].Value);
                }
                var fontSpriteMatch = fontSpritePattern.Match(line);
                if (fontSpriteMatch.Success)
                {
                    ChangeState(State.FontSprite, fontSpriteMatch.Groups[1].Value);
                }

                var enemyMatch = enemyPattern.Match(line);
                if (enemyMatch.Success)
                {
                    ChangeState(State.Enemies, enemyMatch.Groups[1].Value);
                }

                switch(state)
                {
                    case State.Room:
                        ParseRoomLine(line);
                        break;
                    case State.BackgroundSprite:
                        ParseBackgroundSpriteLine(line);
                        break;
                    case State.FontSprite:
                        ParseFontSpriteLine(line);
                        break;
                    case State.Enemies:
                        ParseEnemiesLine(line);
                        break;
                }
            }
            ChangeState(State.None, "");          
        }

        // ********************************************************************
        public static void WriteLine(StreamWriter output, int indent, string message, string comment = "")
        {
            string mess = ("".PadRight(indent) + message).PadRight(57);
            if (!string.IsNullOrEmpty(comment))
            {
                 mess += "; " + comment;
            }
            mess = mess.TrimEnd();
            output.WriteLine(mess);
        }

        // ********************************************************************
        public int WriteOutput(string outputFilepath)
        {
            int numSprites = 0;

            // Create or truncate output file
            using (StreamWriter outputFile = new StreamWriter(outputFilepath))
            {
                // Output room list
                WriteLine(outputFile, 0, "; Table of room addresses");
                WriteLine(outputFile, 0, "");
                WriteLine(outputFile, 0, "; Dec Hex  Name");
                WriteLine(outputFile, 0, "; ---------------------------------------------------------------------------------------");

                for(int i = 0; i < rooms.Count; i++)
                {
                    var room = rooms[i];
                    string message = "; " + room.room_number.ToString().PadLeft(3) + "  " + room.room_number.ToString("X2").ToLowerInvariant() + "  " + room.title.name;
                    if (string.IsNullOrEmpty(room.title.name))
                    {
                        message += "Game Over screen";
                    }
                    WriteLine(outputFile, 0, message);
                }
                WriteLine(outputFile, 0, "; ");
                WriteLine(outputFile, 0, "; ***************************************************************************************");
                WriteLine(outputFile, 0, "");

                // output tables
                WriteLine(outputFile, 0, "room_data_address_low_table");

                for(int i = 0; i < rooms.Count; i++)
                {
                    var room = rooms[i];
                    WriteLine(outputFile, 4, "!byte <room_" + i.ToString("X2").ToLowerInvariant() + "_data");
                }
                WriteLine(outputFile, 0, "");
                WriteLine(outputFile, 0, "room_data_address_high_table");
                for(int i = 0; i < rooms.Count; i++)
                {
                    var room = rooms[i];
                    WriteLine(outputFile, 4, "!byte >room_" + i.ToString("X2").ToLowerInvariant() + "_data");
                }
                WriteLine(outputFile, 0, "");
                WriteLine(outputFile, 0, "; ***************************************************************************************");
                WriteLine(outputFile, 0, "room_data");

                for(int i = 0; i < rooms.Count; i++)
                {
                    var room = rooms[i];

                    WriteLine(outputFile, 0, "room_" + room.room_number.ToString("X2").ToLowerInvariant() + "_data");
                    var bytes = room.GetBytes(this);
                    var offset_in_line = 0;
                    var lines = "";
                    for(int j = 0; j < bytes.Count; j++)
                    {
                        if (offset_in_line == 0)
                        {
                            lines += "    !byte ";
                        }
                        else
                        {
                            lines += ", ";
                        }
                        lines += "$" + bytes[j].ToString("X2").ToLowerInvariant();

                        offset_in_line++;
                        if (offset_in_line == 16)
                        {
                            lines += Environment.NewLine;
                            offset_in_line = 0;
                        }
                    }
                    WriteLine(outputFile, 0, lines);
                    WriteLine(outputFile, 0, "");
                }

                WriteLine(outputFile, 0, "; ***************************************************************************************");
                WriteLine(outputFile, 0, "; Enemies");
                WriteLine(outputFile, 0, "");

                int enemyNum = 0;
                int spriteIndex = 0;
                var outputLines = new List<string>();
                foreach(var enemy in enemies)
                {
                    int frame = 0;
                    outputLines.Clear();

                    var spriteMessage = "";

                    if (enemy.sprites.Count == 1)
                    {
                        spriteMessage = " (Sprite " + spriteIndex + ") ";
                    }
                    else
                    { 
                        spriteMessage = " (Sprites " + spriteIndex + "-" + (spriteIndex + enemy.sprites.Count - 1) + ") ";
                    }

                    WriteLine(outputFile, 0, "; Enemy " + enemyNum + spriteMessage + (enemy.withReverse ? " (plus reverse frames)" : ""));

                    for(int i = 0; i < enemy.sprites[0].height; i++)
                    {
                        outputLines.Add("; ");
                    }
                    foreach(var sprite in enemy.sprites)
                    {
                        for(int i = 0; i < sprite.height; i++)
                        {
                            var binary = Convert.ToString(sprite.GetRow(i), 2).PadLeft(16, '0').Replace('0','.').Replace('1','#');
                            outputLines[i] += binary + "    ";
                        }
                        frame++;
                    }
                    foreach(var line in outputLines)
                    { 
                        WriteLine(outputFile, 0, line);
                    }
                    WriteLine(outputFile, 0, "");
                    enemyNum++;
                    spriteIndex += enemy.sprites.Count;
                }

                // Background sprites
                spriteIndex = 0;
                WriteLine(outputFile, 0, "; Background Sprites");
                int fromIndex = 0;
                foreach(var sprite in backgroundSprites)
                {
                    if ((spriteIndex % 10) == 0)
                    { 
                        outputLines.Clear();
                        for(int i = 0; i < 8; i++)
                        {
                            outputLines.Add("; ");
                        }
                    }
                    int index = 0;
                    foreach(var by in sprite.bytes)
                    { 
                        var binary = Convert.ToString(by, 2).PadLeft(8, '0').Replace('0','.').Replace('1','#');
                        outputLines[index] += binary + "    ";
                        index++;
                    }
                    spriteIndex++;
                    if (((spriteIndex % 10) == 0) || (spriteIndex == backgroundSprites.Count))
                    {
                        // Output stuff
                        string title = "; ";
                        for(int i = fromIndex; i < Math.Min(spriteIndex, backgroundSprites.Count); i++)
                        {
                            title += i.ToString().PadRight(8) + "    ";
                        }
                        WriteLine(outputFile, 0, title);
                        foreach(var line in outputLines)
                        { 
                            WriteLine(outputFile, 0, line.TrimEnd());
                        }
                        WriteLine(outputFile, 0, "");
                        fromIndex = spriteIndex;
                    }
                }

                WriteLine(outputFile, 0, "");
                WriteLine(outputFile, 0, "; ***************************************************************************************");
                WriteLine(outputFile, 0, "; number of frames for each enemy sprite");
                WriteLine(outputFile, 0, "enemy_sprites_frames");
                string enemy_frames = "!byte $08";
                int items_on_line = 1;

                foreach(var enemy in enemies)
                {
                    var val = enemy.sprites.Count;
                    if (enemy.withReverse)
                    {
                        val += 128;       // Mark frames for reversal
                    }

                    if (items_on_line > 0)
                    { 
                        enemy_frames += ", ";
                    }
                    enemy_frames += "$" + val.ToString("x2");
                    items_on_line++;
                    if (items_on_line > 15)
                    {
                        enemy_frames += Environment.NewLine + "    !byte ";
                        items_on_line = 0;
                    }
                }
                WriteLine(outputFile, 4, enemy_frames);
                WriteLine(outputFile, 0, "enemy_sprites_frames_end");
                WriteLine(outputFile, 0, "");

                WriteLine(outputFile, 0, "; offset to start frame for each enemy sprite");
                WriteLine(outputFile, 0, "enemy_sprites_frame_offsets");
                enemy_frames = "!byte $00";
                items_on_line = 1;

                int running_total = 0;
                foreach(var enemy in enemies)
                {
                    if (items_on_line > 0)
                    { 
                        enemy_frames += ", ";
                    }

                    enemy_frames += "$" + running_total.ToString("x2");
                    running_total += enemy.sprites.Count;
                    items_on_line++;
                    if (items_on_line > 15)
                    {
                        enemy_frames += Environment.NewLine + "    !byte ";
                        items_on_line = 0;
                    }
                }
                WriteLine(outputFile, 4, enemy_frames);
                WriteLine(outputFile, 0, "enemy_sprites_frame_offsets_end");
                WriteLine(outputFile, 0, "");

                var num_enemy_sprites = 0;
                foreach(var enemy in enemies)
                {
                    num_enemy_sprites += enemy.sprites.Count;
                }
                WriteLine(outputFile, 0, "num_enemy_sprites = " + num_enemy_sprites);
                WriteLine(outputFile, 0, "");

                // Output all sprites
                numSprites = OutputCompressedSprites(outputFile);

            }

            Console.WriteLine("Encoding finished");
            return numSprites;
        }

        // ********************************************************************
        public class ByteEncoding
        {
            public bool isValid = false;
            public List<int> encoding = new List<int>();

            public ByteEncoding()
            {
            }

            public ByteEncoding(int numLeadingZeros, int code, int originalByte)
            {
                isValid = true;

                if (numLeadingZeros > 1)
                {
                    numLeadingZeros = 1;
                    encoding.Add(0);
                    encoding.Add(originalByte % 16);
                    encoding.Add(originalByte / 16);
                    return;
                }
                for(int i = 0; i < numLeadingZeros; i++)
                {
                    encoding.Add(0);
                }
                Debug.Assert(code >= 0);
                Debug.Assert(code < 16);
                encoding.Add(code);
            }
        }

        // ********************************************************************
        public List<KeyValuePair<int, int>> GetSortedList(List<Sprite> sprites)
        {
            var conc = new Dictionary<int, int>();

            // Create concordance (dictionary) of bytes and their usage counts
            foreach(var sprite in sprites)
            {
                foreach(var val in sprite.bytes)
                { 
                    if (conc.ContainsKey(val))
                    {
                        conc[val]++;
                    }
                    else
                    {
                        conc.Add(val, 1);
                    }
                }
            }

            return (from entry in conc orderby entry.Value descending select entry).ToList();
        }

        // ********************************************************************
        public List<KeyValuePair<int, int>> FindCommonestBytes(List<Sprite> sprites, out List<KeyValuePair<int, int>> sortedList)
        {
            var conc = new Dictionary<int, int>();
            var previousBytes = new List<int>();

            previousBytes.Clear();
            for(int i = 0; i < numberOfPreviousBytes; i++)
            {
                previousBytes.Add(-1);
            }

            // Create concordance (dictionary) of bytes and their usage counts
            foreach(var sprite in sprites)
            {
                foreach(var val in sprite.bytes)
                {
                    var skip = false;
                    var prev = previousBytes[numberOfPreviousBytes-1];

                    // if this value is not one of the previous bytes, then add to the concordance
                    if (previousBytes.Contains(val))
                    {
                        skip = true;
                    }

                    // If value is worth noting, then note it in the concordance
                    if (!skip)
                    { 
                        if (conc.ContainsKey(val))
                        {
                            conc[val]++;
                        }
                        else
                        {
                            conc.Add(val, 1);
                        }
                    }

                    // Add new value to previous bytes array, but if not already the previous entry
                    if (prev != val)
                    {
                        previousBytes.Add(val);
                        previousBytes.RemoveAt(0);
                    }
                }
            }

            sortedList = (from entry in conc orderby entry.Value descending select entry).ToList();
            return sortedList;
        }

        // ********************************************************************
        public List<ByteEncoding> EncodeFontStyle(List<Sprite> sprites, out List<KeyValuePair<int, int>> sortedList)
        {
            sortedList = GetSortedList(sprites);

            var byteEncodings = new List<ByteEncoding>();

            int leadingZeroNybbles = 0;
            int codesSoFar = 0;

            for(int i = 0; i < 256; i++)
            {
                byteEncodings.Add(new ByteEncoding());
            }


            int byteCount = 0;
            int offset = 0;
            foreach (var entry in sortedList)
            {
                offset++;
                byteCount++;
                byteEncodings[entry.Key] = new ByteEncoding(leadingZeroNybbles, offset, entry.Key);

                // move on to next code
                codesSoFar++;

                if (offset == 15)
                {
                    offset = 0;
                    leadingZeroNybbles++;
                }
            }
            return byteEncodings;
        }

        // ********************************************************************
        public string ToBinary(int val, int width)
        {
            Debug.Assert(val >= 0);
            Debug.Assert(val < (1 << width));
            return Convert.ToString(val, 2).PadLeft(width, '0').Replace("0", ".").Replace("1", "#");
        }

        // ********************************************************************
        public int OutputCompressedSprites(StreamWriter outputFile)
        {
            WriteLine(outputFile, 0, "; ***************************************************************************************");
            //int offset = 0;
            //int leadingZeroNybbles = 0;

            var numVeryCommonItems = 10 - numberOfPreviousBytes;

            //
            // ALL SPRITES
            //

            var allSprites = new List<Sprite>();
            foreach(var enemy in enemies)
            {
                allSprites.AddRange(enemy.sprites);
            }
            allSprites.AddRange(backgroundSprites);
            var firstSpriteOfFont = allSprites.Count();
            allSprites.AddRange(fontSprites);

            {
                var commonestBytesAndOccurrences = FindCommonestBytes(allSprites, out var sortedList).Take(80 + numVeryCommonItems);
                var commonestFew = commonestBytesAndOccurrences.Take(numVeryCommonItems).Select((x) => x.Key).ToList();
                var commonestBytes = commonestBytesAndOccurrences.Skip(numVeryCommonItems).Take(80).Select((x) => x.Key).ToList();

                WriteLine(outputFile, 0, "sprite_decode_table");
                int byteCount = 0;
                foreach(var common in commonestBytesAndOccurrences)
                {
                    WriteLine(outputFile, 4, "!byte %" + ToBinary(common.Key, 8), common.Value + " useful occurrences");
                    byteCount++;
                }
                WriteLine(outputFile, 0, "sprite_decode_table_end");
                WriteLine(outputFile, 0, "");
                var nybbles = new List<int>();
                var previousBytes = new List<int>();

                var hist = new List<int>();
                for(int i = 0; i < 16; i++)
                {
                    hist.Add(0);
                }

                var histEncodedSizes = new List<int>();
                for(int i = 0; i < 100; i++)
                {
                    histEncodedSizes.Add(0);
                }

                var shortcutTable = new List<int>();
                int shortcutInterval = 8;

                //Console.WriteLine("Hardest sprites:");
                int spriteIndex = 0;
                foreach(var sprite in allSprites)
                {
                    if ((spriteIndex % shortcutInterval) == 0)
                    {
                        shortcutTable.Add(nybbles.Count/2 + (nybbles.Count & 1) * 0x8000);
                    }

                    previousBytes.Clear();
                    for(int i = 0; i < numberOfPreviousBytes; i++)
                    {
                        previousBytes.Add(-1);
                    }
                    var newNybbles = new List<int>();
                    foreach(var val in sprite.bytes)
                    {
                        var prev = previousBytes[numberOfPreviousBytes-1];
                        var index = previousBytes.IndexOf(val);

                        if (commonestFew.Contains(val))
                        {
                            index = commonestFew.IndexOf(val);
                            newNybbles.Add(numberOfPreviousBytes + index);
                            hist[numberOfPreviousBytes + index]++;
                        }
                        else if (index >= 0)
                        {
                            newNybbles.Add(index);
                            hist[index]++;
                        }
                        else if (commonestBytes.Contains(val))
                        {
                            index = commonestBytes.IndexOf(val);
                            var firstNybble = 10 + index/16;
                            var secondNybble = index & 15;
                            Debug.Assert(firstNybble >= 10);
                            Debug.Assert(firstNybble <= 14);
                            newNybbles.Add(firstNybble);
                            newNybbles.Add(secondNybble);
                            hist[firstNybble]++;
                        }
                        else
                        {
                            newNybbles.Add(15);
                            newNybbles.Add(val % 16);
                            newNybbles.Add(val / 16);
                            hist[15]++;
                        }

                        if (val != previousBytes[numberOfPreviousBytes - 1])
                        {
                            previousBytes.Add(val);
                            previousBytes.RemoveAt(0);
                        }
                    }

                    // if the encoding is too long, use a raw encoding
                    var limit = (1 + 2 * sprite.bytes.Count);
                    if (newNybbles.Count > limit)
                    {
                        newNybbles.Clear();
                        newNybbles.Add(0);
                        foreach(var val in sprite.bytes)
                        { 
                            newNybbles.Add(val % 16);
                            newNybbles.Add(val / 16);
                        }
                    }
                    nybbles.AddRange(newNybbles);
                    histEncodedSizes[newNybbles.Count]++;

                    spriteIndex++;

                    /* output hardest sprites to compress
                    if (newNybbles.Count == limit)
                    {
                        Console.WriteLine("Sprite:");
                        for(int row = 0; row < sprite.height; row++)
                        {
                            Console.WriteLine(ToBinary(sprite.GetRow(row), sprite.width));
                        }
                        Console.WriteLine();
                    }
                    */
                }

                WriteLine(outputFile, 0, "; Occurrences of first nybble N:");
                for(int i = 0; i < 16; i++)
                {
                    WriteLine(outputFile, 0, ";     " + i + ": " + hist[i] + " occurrences");
                }

                WriteLine(outputFile, 0, "");
                WriteLine(outputFile, 0, "; Occurrences of different encoding sizes:");
                for(int i = 0; i < 100; i++)
                {
                    if (histEncodedSizes[i] > 0)
                    { 
                        WriteLine(outputFile, 0, ";     " + i + ": " + histEncodedSizes[i] + " occurrences");
                    }
                }
                WriteLine(outputFile, 0, "");
                WriteLine(outputFile, 0, "shortcut_interval = " + shortcutInterval.ToString());
                WriteLine(outputFile, 0, "");

                WriteLine(outputFile, 0, "shortcut_table_low");
                foreach(var entry in shortcutTable)
                {
                    WriteLine(outputFile, 4, "!byte <(sprite_data + $" + entry.ToString("X4").ToLowerInvariant() + ")");
                }
                WriteLine(outputFile, 0, "shortcut_table_high");
                foreach(var entry in shortcutTable)
                {
                    WriteLine(outputFile, 4, "!byte >(sprite_data + $" + entry.ToString("X4").ToLowerInvariant() + ")");
                }
                WriteLine(outputFile, 0, "");

                // Output bytes
                WriteLine(outputFile, 0, "sprite_data");
                if ((nybbles.Count & 1) == 1)
                {
                    nybbles.Add(0);
                }
                for(int i = 0; i < nybbles.Count; i += 2)
                {
                    WriteLine(outputFile, 4, "!byte $" + (nybbles[i] + 16 * nybbles[i+1]).ToString("x2"));
                    byteCount++;
                }
                WriteLine(outputFile, 0, "sprite_data_end");
                WriteLine(outputFile, 0, "");
                WriteLine(outputFile, 0, "; Sprites: " + byteCount + " bytes");
            }

            WriteLine(outputFile, 0, "first_sprite_of_font = " + firstSpriteOfFont);
            WriteLine(outputFile, 0, "total_sprites = " + allSprites.Count);

            //
            // OLD UTF-4 FONT COMPRESSION
            //
            /*
            { 
                int byteCount = 0;
                var byteEncodings = EncodeFontStyle(fontSprites, out var sortedList);

                WriteLine(outputFile, 0, "font_sprite_decode_table");
                foreach (var entry in sortedList)
                {
                    if (offset == 0)
                    {
                        WriteLine(outputFile, 0, "");
                        WriteLine(outputFile, 0, "; fifteen values with " + leadingZeroNybbles + " leading nybbles");
                    }
                    offset++;
                    WriteLine(outputFile, 4, "!byte %" + Convert.ToString(entry.Key, 2).PadLeft(8, '0').Replace("0", ".").Replace("1", "#") + "         ; " + entry.Value + " occurrences");
                    // move on to next code
                    if (offset == 15)
                    {
                        offset = 0;
                        leadingZeroNybbles++;
                    }
                }
                WriteLine(outputFile, 0, "font_sprite_decode_table_end");
                WriteLine(outputFile, 0, "");
                WriteLine(outputFile, 0, "font_sprite_data");

                // Create list of all nybbles
                List<int> nybbles = new List<int>();
                foreach(var sprite in fontSprites)
                {
                    foreach(var val in sprite.bytes)
                    {
                        Debug.Assert(byteEncodings[val].isValid);
                        nybbles.AddRange(byteEncodings[val].encoding);
                    }
                }

                // Output bytes
                if ((nybbles.Count & 1) == 1)
                {
                    nybbles.Add(0);
                }
                for(int i = 0; i < nybbles.Count; i+=2)
                {
                    WriteLine(outputFile, 4, "!byte " + (nybbles[i] + 16 * nybbles[i+1]));
                    byteCount++;
                }
                WriteLine(outputFile, 0, "font_sprite_data_end");
                WriteLine(outputFile, 0, "; number of compressed font sprite bytes = " + byteCount);
                WriteLine(outputFile, 0, "");
                Console.WriteLine("Font sprites: " + byteCount + " bytes");
            }
            */
            return allSprites.Count;
        }

        // ********************************************************************
        public void Process(string inputFilepath, string outputFilepath)
        {
            if (!File.Exists(inputFilepath))
            {
                JSWMessage.Error("Could not open input file {0}", inputFilepath);
                return;
            }
            ReadInput(inputFilepath);
            var numSprites = WriteOutput(outputFilepath);

            var checker = new JSWChecker();
            checker.CheckOutputFile(outputFilepath, numSprites);
        }
    }
}
