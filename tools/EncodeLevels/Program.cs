using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EncodeLevels
{
    enum State
    {
        None = 0,
        Room,
        BackgroundSprite,
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
        public int colour;
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
    }

    class Arrow
    {
        public int y;
        public int timing;

        public Arrow(int y_val, int timing_val)
        {
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

        public Sprite(string name)
        {
            spriteId = name;
        }
    }


    class EnemySprites
    {
        public string enemyId;
        public List<Sprite> sprites = new List<Sprite>();

        public EnemySprites(string name)
        {
            enemyId = name;
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
    class Room
    {
        public List<bool> itemsCollected = new List<bool>();
        public List<int> exits = new List<int>();
        public bool conveyorDir = false;
        public bool slopeDir = false;
        public bool ropePresent = false;
        public TileType itemTile = new TileType();
        public TileType deadlyTile = new TileType();
        public TileType conveyorTile = new TileType();
        public TileType slopeTile = new TileType();
        public TileType wallTile = new TileType();
        public TileType platformTile = new TileType();
        public List<int> palette = new List<int>();
        public Title title = new Title();
        public List<TileCommand> commands = new List<TileCommand>();
        public List<Enemy> enemies = new List<Enemy>();
        public List<Arrow> arrows = new List<Arrow>();

        // ********************************************************************
        public void Output(string outputFilepath)
        {
        }

        // ********************************************************************
        public List<int> GetBytes(Processor processor)
        {
            processor.StartBits();

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

            processor.AddBits(2, deadlyTile.colour);
            processor.AddBits(2, conveyorTile.colour);
            processor.AddBits(2, slopeTile.colour);
            processor.AddBits(2, wallTile.colour);
            processor.AddBits(2, platformTile.colour);

            processor.AddBits(8, processor.GetBackgroundSpriteIndex(itemTile.spriteName));
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

            processor.AddBits(4, title.tab);

            // Title
            for(int i = 0; i < title.name.Length; i++)
            {
                if (i < title.name.Length - 3)
                {
                    if (title.name.Substring(i, 3).ToLowerInvariant() == "the")
                    {
                        processor.AddBits(5, 0x1f);
                        i = i + 2;
                        continue;
                    }
                }
                int c = (int) title.name[i];
                if ((c >= 'a') && (c <= 'z'))
                {
                    processor.AddBits(5, c - 'a');
                    continue;
                }
                if ((c >= 'A') && (c <= 'Z'))
                {
                    processor.AddBits(5, c - 'A');
                    continue;
                }
                if (c == ' ')
                {
                    if (i < (title.name.Length - 1))
                    {
                        char nextC = title.name[i + 1];
                        if (char.IsUpper(nextC))
                        {
                            processor.AddBits(5, 0x1e);
                            continue;
                        }
                    }
                    processor.AddBits(5, 0x1d);
                }
            }
            processor.AddBits(5, 0x1c);     // Terminator

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
            for(int i = 0; i < 2; i++)
            {
                processor.AddBit(i < arrows.Count);
                if (i < arrows.Count)
                { 
                    processor.AddBits(4, arrows[i].y);
                    processor.AddBits(3, arrows[i].timing);
                }
            }

            processor.EndBits();
            return new List<int>(Processor.bytes);
        }
    }

    // ************************************************************************
    class Processor
    {
        // For input
        State state = State.None;
        string stateName;
        Room currentRoom;
        List<Room> rooms;
        public List<Sprite> backgroundSprites = new List<Sprite>();
        public bool isBackgroundSpriteLine = false;
        public List<EnemySprites> enemies = new List<EnemySprites>();
        public bool isEnemyLine = false;

        // For output
        public static List<int> bytes = new List<int>();
        public static int bits_within_byte = 8;

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
                if (bytes.Count > 0)
                { 
                    Console.WriteLine(bytes.Last().ToString("X2"));
                }
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
            switch(state)
            {
                case State.Room: LeaveRoomState(); break;
            }
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

            switch(state)
            {
                case State.Room: EnterRoomState(); break;
            }
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

            if (IsMatch(line, @"Deadly tile logical colour *: (.*)", out results))
            {
                currentRoom.deadlyTile.colour = int.Parse(results[0]);
                return;
            }

            if (IsMatch(line, @"Conveyor tile logical colour *: (.*)", out results))
            {
                currentRoom.conveyorTile.colour = int.Parse(results[0]);
                return;
            }

            if (IsMatch(line, @"Slope tile logical colour *: (.*)", out results))
            {
                currentRoom.slopeTile.colour = int.Parse(results[0]);
                return;
            }

            if (IsMatch(line, @"Wall tile logical colour *: (.*)", out results))
            {
                currentRoom.wallTile.colour = int.Parse(results[0]);
                return;
            }

            if (IsMatch(line, @"Platform tile logical colour *: (.*)", out results))
            {
                currentRoom.platformTile.colour = int.Parse(results[0]);
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
                    case "ITEM":     use = 5; break;
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
            if (IsMatch(line, @"Draw (\d+) single tiles at (.*)$", out results))
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

            if (IsMatch(line, @"Enemy *: *(\d+)", out results))
            {
                currentRoom.enemies.Add(new Enemy());
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

            if (IsMatch(line, @"Arrow *: *(\d+) *, *timing +index *(\d+)", out results))
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
                backgroundSprites.Add(new Sprite(results[0]));
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
                backgroundSprites.Last().bytes.Add(val);
            }
            else if (!string.IsNullOrEmpty(line))
            {
                isBackgroundSpriteLine = false;
            }
        }

        // ********************************************************************
        public void ParseEnemiesLine(string line)
        {
            line = line.Trim();
            if (IsMatch(line, @"Enemy +(\d+)", out var results))
            {
                enemies.Add(new EnemySprites(results[0]));
                isEnemyLine = true;
            }
            else if (line == "Sprite")
            {
                enemies.Last().sprites.Add(new Sprite(""));
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
                enemies.Last().sprites.Last().bytes.Add(val);
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
            var enemyPattern = new Regex(@"^Enemy +([A-Za-z0-9_]+)$");

            rooms = new List<Room>();
            backgroundSprites = new List<Sprite>();

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
        public static string ToBinaryString(int val, int digits = 8)
        {
            string result = "";

            for(int i = 0; i < digits; i++)
            {
                char c = ((val & 1) != 0) ? '#' : '.';
                result = c + result;
                val = val / 2;
            }

            return result;
        }

        // ********************************************************************
        public void WriteOutput(string outputFilepath)
        {
            // Create or truncate output file
            using (StreamWriter outputFile = new StreamWriter(outputFilepath))
            {
                // Output room list
                WriteLine(outputFile, 0, "; Table of room addresses");
                WriteLine(outputFile, 0, "");
                WriteLine(outputFile, 0, "; Dec Hex  Name");
                WriteLine(outputFile, 0, "; ---------------------------------------------------------------------------------------");
                WriteLine(outputFile, 0, ";   0  00  Game Over screen");

                for(int i = 0; i < rooms.Count; i++)
                {
                    var room = rooms[i];
                    string message = "; " + (i+1).ToString().PadLeft(3) + "  " + (i+1).ToString("X2").ToLowerInvariant() + "  " + room.title.name;
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

                    WriteLine(outputFile, 0, "room_" + (i+1).ToString("X2").ToLowerInvariant() + "_data");
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
                        if (offset_in_line == 8)
                        {
                            lines += Environment.NewLine;
                            offset_in_line = 0;
                        }
                    }
                    WriteLine(outputFile, 0, lines);
                    WriteLine(outputFile, 0, "");
                }
            }

        }

        // ********************************************************************
        public void Process(string inputFilepath, string outputFilepath)
        {
            ReadInput(inputFilepath);
            WriteOutput(outputFilepath);
        }
    }

    // ************************************************************************
    class MainClass
    {
        static string inputFilepath = "/Users/tobynelson/Code/6502/jsw2021/rooms.txt";
        static string outputFilepath = "/Users/tobynelson/Code/6502/jsw2021/rooms.a";

        // ********************************************************************
        public static void Main(string[] args)
        {
            Console.WriteLine("Encode Levels");

            var processor = new Processor();
            processor.Process(inputFilepath, outputFilepath);
        }
    }
}
