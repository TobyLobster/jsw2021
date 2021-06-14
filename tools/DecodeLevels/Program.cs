using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DecodeLevels
{
    enum Dir
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3
    }

    class TileType
    {
        public int sprite = -1;
        public int colour;
    }

    class Title
    {
        public int tab;
        public string name;
    }

    class Enemy
    {
        public int sprite;
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
        public bool valid;
        public int y;
        public int timing;

        public Arrow()
        {
            valid = false;
            y = -1;
            timing = -1;
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
        public abstract void Write(StreamWriter outputFile);
    }

    class UseTileCommand : TileCommand
    {
        public int tileType;

        public UseTileCommand(int tt)
        {
            tileType = tt;
        }

        public override void Write(StreamWriter outputFile)
        {
            MainClass.WriteLine(outputFile, 8, "Use " + MainClass.GetTileName(tileType));
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

        public override void Write(StreamWriter outputFile)
        {
            MainClass.WriteLine(outputFile, 8, "Move to (" + X + "," + Y + ")");
        }
    }

    class StripTileCommand : TileCommand
    {
        public bool dir;
        public int len;

        public StripTileCommand(bool d, int length)
        {
            dir = d;
            len = length;
        }

        public override void Write(StreamWriter outputFile)
        {
            MainClass.WriteLine(outputFile, 8, "Draw " + (dir ? "vertical" : "horizontal") + " strip until " + (dir ? "Y" : "X") + "=" + len, "'Draw strip' moves the cursor to the end tile");
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

        public override void Write(StreamWriter outputFile)
        {
            MainClass.WriteLine(outputFile, 8, "Draw block to (" + extentX + "," + finalY + ")", "'Draw block' preserves cursor Y, and moves cursor X to one beyond final extent");
        }
    }

    class ListTileCommand : TileCommand
    {
        public List<TilePos> points = new List<TilePos>();

        public override void Write(StreamWriter outputFile)
        {
            if (points.Count == 1)
            {
                outputFile.Write("        Draw 1 single tile at");
            } else
            {
                outputFile.Write("        Draw " + points.Count + " single tiles at");
            }
            for(int i = 0; i < points.Count; i++)
            {
                outputFile.Write(" (" + points[i].X + "," + points[i].Y + ")");
            }
            outputFile.WriteLine("");
        }
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

        public override void Write(StreamWriter outputFile)
        {
            MainClass.WriteLine(outputFile, 8, "Draw slope " + (dir ? @"moving left" : @"moving right") + " until Y=" + finalY, "'Draw slope' moves the cursor to the end tile");
        }
    }

    class TriangleTileCommand : TileCommand
    {
        public bool dir;
        public int extent;

        public TriangleTileCommand(bool d, int ext)
        {
            dir = d;
            extent = ext;
        }

        public override void Write(StreamWriter outputFile)
        {
            MainClass.WriteLine(outputFile, 8, "Draw triangle " + (dir ? @"moving left" : @"moving right") + " until Y=" + extent);
        }
    }

    class Sprite
    {
        public List<int> bytes = new List<int>();
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

    class Room
    {
        public int roomNumber;
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
            using (StreamWriter outputFile = new StreamWriter(outputFilepath, true))
            {
                if (roomNumber != 1)
                {
                    MainClass.WriteLine(outputFile, 0, "");
                }
                MainClass.WriteLine(outputFile, 0, "Room " + roomNumber);

                if (roomNumber > 0)
                {
                    string items = "Number of items: " + itemsCollected.Count;
                    if (itemsCollected.Count > 0)
                    {
                        items += " (";
                    }
                    for(int j = 0; j < itemsCollected.Count; j++)
                    {
                        if (j > 0)
                        {
                            items += ", ";
                        }
                        items += (itemsCollected[j]) ? "collected" : "not collected";
                    }
                    if (itemsCollected.Count > 0)
                    {
                        items += ")";
                    }
                    MainClass.WriteLine(outputFile, 4, items);
                    outputFile.Write("    Exits: left " + exits[0] + ", ");
                    outputFile.Write("right " + exits[1] + ", ");
                    outputFile.Write("up " + exits[2] + ", ");
                    outputFile.WriteLine("down " + exits[3]);

                    MainClass.WriteLine(outputFile, 4, "Conveyor direction: " + (conveyorDir ? "right" : "left"));
                    MainClass.WriteLine(outputFile, 4, "Slope direction: " + (slopeDir ? @"\" : @"/"));
                    MainClass.WriteLine(outputFile, 4, "Rope present: " + (ropePresent ? "yes" : "no"));
                    MainClass.WriteLine(outputFile, 4, "Deadly tile logical colour  : " + deadlyTile.colour);
                    MainClass.WriteLine(outputFile, 4, "Conveyor tile logical colour: " + conveyorTile.colour);
                    MainClass.WriteLine(outputFile, 4, "Slope tile logical colour   : " + slopeTile.colour);
                    MainClass.WriteLine(outputFile, 4, "Wall tile logical colour    : " + wallTile.colour);
                    MainClass.WriteLine(outputFile, 4, "Platform tile logical colour: " + platformTile.colour);

                    MainClass.WriteLine(outputFile, 4, "Item tile sprite    : " + itemTile.sprite);
                    MainClass.WriteLine(outputFile, 4, "Deadly tile sprite  : " + deadlyTile.sprite);
                    MainClass.WriteLine(outputFile, 4, "Conveyor tile sprite: " + conveyorTile.sprite);
                    MainClass.WriteLine(outputFile, 4, "Slope tile sprite   : " + slopeTile.sprite);
                    MainClass.WriteLine(outputFile, 4, "Wall tile sprite    : " + wallTile.sprite);
                    MainClass.WriteLine(outputFile, 4, "Platform tile sprite: " + platformTile.sprite);

                    outputFile.Write("    Palette: ");
                    outputFile.Write(MainClass.GetColourName(palette[0]) + ", ");
                    outputFile.Write(MainClass.GetColourName(palette[1]) + ", ");
                    outputFile.Write(MainClass.GetColourName(palette[2]) + ", ");
                    outputFile.Write(MainClass.GetColourName(palette[3]));
                    outputFile.WriteLine();

                    MainClass.WriteLine(outputFile, 4, "Title: tab " + title.tab + ", \"" + title.name + "\"");

                    // Tiles
                    MainClass.WriteLine(outputFile, 4, "Tile commands:");
                    foreach(var command in commands)
                    {
                        command.Write(outputFile);
                    }
                }

                // Enemies
                MainClass.WriteLine(outputFile, 4, "Enemies: " + enemies.Count);
                int i = 0;
                foreach(var enemy in enemies)
                {
                    string dirMess = "?????";
                    switch (enemy.dir)
                    {
                        case Dir.Left: dirMess = "left"; break;
                        case Dir.Right: dirMess = "right"; break;
                        case Dir.Up: dirMess = "up"; break;
                        case Dir.Down: dirMess = "down"; break;
                    }

                    MainClass.WriteLine(outputFile, 8, "Enemy: " + i);
                    MainClass.WriteLine(outputFile, 12, "Sprite: " + enemy.sprite);
                    MainClass.WriteLine(outputFile, 12, "Initial Pos: (" + enemy.initialX + "," + enemy.initialY + ")");
                    MainClass.WriteLine(outputFile, 12, "Min Extent: " + enemy.min);
                    MainClass.WriteLine(outputFile, 12, "Max Extent: " + enemy.max);
                    MainClass.WriteLine(outputFile, 12, "Initial Dir: " + dirMess);
                    MainClass.WriteLine(outputFile, 12, "Speed: " + enemy.speed);
                    MainClass.WriteLine(outputFile, 12, "Logical colour: " + enemy.logicalColour);
                    i++;
                }

                // Arrows

                MainClass.WriteLine(outputFile, 4, "Arrows: " + arrows.Where((x) => x.valid).Count());
                foreach (var arrow in arrows)
                {
                    if (arrow.valid)
                    {
                        MainClass.WriteLine(outputFile, 8, "Arrow: Y " + arrow.y + ", timing index " + arrow.timing);
                    }
                    else
                    {
                        MainClass.WriteLine(outputFile, 8, "Arrow: no");
                    }
                }

                // Write blank line after last room
                if (roomNumber == 0)
                {
                    MainClass.WriteLine(outputFile, 0, "");
                }
            }
        }
    }

    // ************************************************************************
    class MainClass
    {
        static List<int> room_bytes = new List<int>();
        static List<int> background_sprite_bytes = new List<int>();
        static List<int> enemy_sprite_bytes = new List<int>();
        static int room_bit_counter = 0;
        static List<Room> rooms = new List<Room>();
        static List<Sprite> backgroundSprites = new List<Sprite>();
        static List<Sprite> enemySprites = new List<Sprite>();
        static List<int> enemyFrameLengths = new List<int>();
        static string outputFilepath = @"/Users/tobynelson/Code/6502/jsw2021/rooms.txt";

        // ********************************************************************
        public static string GetColourName(int colour)
        {
            switch(colour)
            {
                case 0: return "black";
                case 1: return "red";
                case 2: return "green";
                case 3: return "yellow";
                case 4: return "blue";
                case 5: return "magenta";
                case 6: return "cyan";
                case 7: return "white";
            }
            return "unknown (" + colour + ")";
        }


        // ********************************************************************
        public static string GetTileName(int tile)
        {
            switch (tile)
            {
                case 0:  return "PLATFORM";
                case 1:  return "WALL";
                case 2:  return "SLOPE";
                case 3:  return "CONVEYOR";
                case 4:  return "DEADLY";
                case 5:  return "ITEM";
                default: return "?";
            }
        }

        // ********************************************************************
        public static int GetBits(int num_bits)
        {
            int result = 0;
            for(int i = 0; i < num_bits; i++)
            {
                if (room_bytes.Count == 0)
                {
                    Debug.Assert(false);
                    // Fill in with zero bytes if we run out
                    // room_bytes.Add(0);
                }
                result = result * 2 + (room_bytes[0] & 1);
                room_bit_counter++;
                room_bytes[0] = room_bytes[0] / 2;
                if (room_bit_counter == 8)
                {
                    room_bytes.Remove(0);
                    room_bit_counter = 0;
                }
            }
            return result;
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
        public static void DecodeBackgroundSpriteEnd(int spriteNum)
        {
            if (spriteNum < 0)
            {
                return;
            }

            var sprite = new Sprite();
            foreach(var by in background_sprite_bytes)
            {
                sprite.bytes.Add(by);
            }
            backgroundSprites.Add(sprite);

            background_sprite_bytes.Clear();
        }


        // ********************************************************************
        public static void DecodeEnemySpriteEnd(int spriteNum)
        {
            if (spriteNum < 0)
            {
                return;
            }

            enemy_sprite_bytes.Reverse();
            var sprite = new Sprite();
            foreach(var by in enemy_sprite_bytes)
            {
                sprite.bytes.Add(by);
            }
            enemySprites.Add(sprite);

            enemy_sprite_bytes.Clear();
        }

        // ********************************************************************
        public static void DecodeRoomEnd(int roomNumber)
        {
            if (roomNumber < 0) {
                return;
            }
            room_bit_counter = 0;

            Room room = new Room();
            room.roomNumber = roomNumber;

            if (roomNumber > 0)
            {
                int numItems = GetBits(4);
                for(int i = 0; i < numItems; i++)
                {
                    room.itemsCollected.Add(GetBits(1) == 1);
                }

                room.exits.Add(GetBits(6));  // Left
                room.exits.Add(GetBits(6));  // Right
                room.exits.Add(GetBits(6));  // Up
                room.exits.Add(GetBits(6));  // Down
                room.conveyorDir = GetBits(1) != 0;
                room.slopeDir    = GetBits(1) != 0;
                room.ropePresent = GetBits(1) != 0;

                room.deadlyTile.colour = GetBits(2);
                room.conveyorTile.colour = GetBits(2);
                room.slopeTile.colour = GetBits(2);
                room.wallTile.colour = GetBits(2);
                room.platformTile.colour = GetBits(2);

                room.itemTile.sprite = GetBits(8);
                room.deadlyTile.sprite = GetBits(8);
                room.conveyorTile.sprite = GetBits(8);
                room.slopeTile.sprite = GetBits(8);
                room.wallTile.sprite = GetBits(8);
                room.platformTile.sprite = GetBits(8);

                room.palette.Add(GetBits(3));
                room.palette.Add(GetBits(3));
                room.palette.Add(GetBits(3));
                room.palette.Add(GetBits(3));
                room.palette.Reverse();

                room.title.tab = GetBits(4);

                bool next_letter_uppercase = true;
                string title = "";
                var letter = 0;
                do
                {
                    char c = '?';
                    letter = GetBits(5);
                    switch(letter)
                    {
                        case 0x1b:
                            c = '\'';
                            break;
                        case 0x1c:
                            // End of string
                            break;
                        case 0x1d:
                            c = ' ';
                            break;
                        case 0x1e:
                            c = ' ';
                            next_letter_uppercase = true;
                            break;
                        case 0x1f:
                            if (next_letter_uppercase)
                            {
                                next_letter_uppercase = false;
                                title += "The";
                            }
                            else { 
                                title += "the";
                            }
                            c = ' ';
                            break;
                        default:
                            if (next_letter_uppercase)
                            {
                                next_letter_uppercase = false;
                                c = (char) ((int) 'A' + letter);
                            }
                            else
                            {
                                c = (char) ((int) 'a' + letter);
                            }
                            break;
                    }
                    if (letter == 0x1c) {
                        break;
                    }
                    if (letter != 0x1f) { 
                        if (c == '?')
                        {
                            Console.WriteLine("oops");
                        }
                        title += c;
                    }
                    if (title.Length > 32)
                    {
                        break;
                    }
                }
                while (letter != 0x1c);
                room.title.name = title;

                // Tiles
                bool finished = false;
                while(!finished)
                {
                    int num3s = 0;
                    int command = 0;
                    do
                    {
                        command = GetBits(2);
                        if (command == 3)
                        {
                            num3s++;
                        }
                    }
                    while (command == 3);

                    switch (num3s)
                    {
                        case 0:
                            if (command == 0)
                            {
                                int newTile = GetBits(3);
                                room.commands.Add(new UseTileCommand(newTile));
                            }
                            else if (command == 1)
                            {
                                int x = GetBits(5);
                                int y = GetBits(4);
                                room.commands.Add(new XYTileCommand(x, y));
                            }
                            else if (command == 2)
                            {
                                bool dir = GetBits(1) != 0;
                                int len = GetBits(5);
                                room.commands.Add(new StripTileCommand(dir, len));
                            }
                            break;
                        case 1:
                            if (command == 0)
                            {
                                int extent_x = GetBits(5);
                                int final_y = GetBits(4);
                                room.commands.Add(new BlockTileCommand(extent_x, final_y));
                            }
                            else if (command == 1)
                            {
                                int num_tiles = GetBits(4) + 1;
                                var list = new ListTileCommand();
                                for(int i = 0; i < num_tiles; i++)
                                {
                                    int x = GetBits(5);
                                    int y = GetBits(4);
                                    list.points.Add(new TilePos(x,y));
                                }
                                room.commands.Add(list);
                            }
                            else if (command == 2)
                            {
                                bool dir = GetBits(1) != 0;
                                int final_y = GetBits(4);
                                room.commands.Add(new SlopeTileCommand(dir, final_y));
                            }
                            break;
                        case 2:
                            if (command == 0)
                            {
                                finished = true;
                            }
                            else if (command == 1)
                            {
                                // unused
                            }
                            else if (command == 2)
                            {
                                var dir = GetBits(1) != 0;
                                int extent = GetBits(4);
                                room.commands.Add(new TriangleTileCommand(dir, extent));
                            }
                            break;
                    }
                }
            }

            // Enemies
            int num_enemies = GetBits(3);
            room.enemies.Clear();
            for(int i = 0; i < num_enemies; i++)
            {
                var enemy = new Enemy();
                enemy.sprite = GetBits(6);
                enemy.initialX = GetBits(5);
                enemy.initialY = GetBits(4);
                enemy.min = GetBits(5);
                enemy.max = GetBits(5);
                int dir = GetBits(1);
                int delta = GetBits(1);
                enemy.dir = (Dir) (dir * 2 + delta);
                enemy.speed = GetBits(3);
                enemy.logicalColour = GetBits(2);
                room.enemies.Add(enemy);
            }

            // Arrows
            room.arrows.Clear();
            for (int i = 0; i < 2; i++)
            {
                if (GetBits(1) != 0)
                {
                    int y = GetBits(4);
                    int timing = GetBits(3);

                    room.arrows.Add(new Arrow(y, timing));
                }
                else
                {
                    room.arrows.Add(new Arrow());
                }
            }

            rooms.Add(room);
            room_bytes.Clear();
        }

        public static int state_room = -1;

        // ********************************************************************
        public static void ProcessRoomLines(string line)
        {
            var pattern = new Regex(@"^room_([0-9a-fA-F][0-9a-fA-F])_data");
            var roomMatch = pattern.Match(line);
            if (roomMatch.Success) {
                DecodeRoomEnd(state_room);
                state_room = int.Parse(roomMatch.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
            }
            if (line.Trim() == "room_data_end") {
                DecodeRoomEnd(state_room);
                state_room = -1;
            }
            if (state_room >= 0)
            {
                var pattern2 = new Regex(@"\$([0-9a-fA-F][0-9a-fA-F])");
                var hexMatches = pattern2.Matches(line);
                for(int j = 0; j < hexMatches.Count; j++) {
                    var hexMatch = hexMatches[j];
                    int val = int.Parse(hexMatch.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                    room_bytes.Add(val);
                }
            }
        }

        public static int state_enemies_sprite = -1;
        // ********************************************************************
        public static void ProcessEnemySpriteLines(string line)
        {
            var pattern = new Regex(@"^enemy_sprite_([0-9a-fA-F][0-9a-fA-F])_frame_([0-7])");
            var enemyMatch = pattern.Match(line);
            if (enemyMatch.Success) {
                DecodeEnemySpriteEnd(state_enemies_sprite);
                state_enemies_sprite = int.Parse(enemyMatch.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
            }
            if (line.Trim() == "packed_enemy_sprites_end") {
                DecodeEnemySpriteEnd(state_enemies_sprite);
                state_enemies_sprite = -1;
            }
            if (state_enemies_sprite >= 0)
            {
                var pattern2 = new Regex(@"\$([0-9a-fA-F][0-9a-fA-F])");
                var hexMatches = pattern2.Matches(line);
                for(int j = 0; j < hexMatches.Count; j += 2) {
                    var hexString = hexMatches[j].Groups[1].Value + hexMatches[j + 1].Groups[1].Value;
                    int val = int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
                    enemy_sprite_bytes.Add(val);
                }
            }
        }

        public static bool stateInEnemyFrameLengths = false;
        // ********************************************************************
        public static void ProcessEnemyFrameOffsetLines(string line)
        {
            if (line.Trim() == "enemy_sprites_frames") {
                stateInEnemyFrameLengths = true;
            }
            if (line.Trim() == "enemy_sprites_frames_end") {
                stateInEnemyFrameLengths = false;
            }

            if (stateInEnemyFrameLengths)
            {
                var pattern2 = new Regex(@"\$([0-9a-fA-F][0-9a-fA-F])");
                var hexMatches = pattern2.Matches(line);
                for(int j = 0; j < hexMatches.Count; j++) {
                    var hexString = hexMatches[j].Groups[1].Value;
                    int val = int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
                    enemyFrameLengths.Add(val);
                }
            }
        }

        public static int state_background_sprite = -1;

        // ********************************************************************
        public static void ProcessBackgroundSpriteLines(string line)
        {
            var pattern = new Regex(@"^background_sprite_([0-9a-fA-F][0-9a-fA-F]) *$");
            var spriteMatch = pattern.Match(line);
            if (spriteMatch.Success) {
                DecodeBackgroundSpriteEnd(state_background_sprite);
                state_background_sprite = int.Parse(spriteMatch.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
            }
            if (line.Trim() == "background_sprite_data_end")
            {
                DecodeBackgroundSpriteEnd(state_background_sprite);
                state_background_sprite = -1;
            }
            if (state_background_sprite >= 0)
            {
                var pattern2 = new Regex(@"\$([0-9a-fA-F][0-9a-fA-F])");
                var hexMatches = pattern2.Matches(line);
                for(int j = 0; j < hexMatches.Count; j++) {
                    var hexMatch = hexMatches[j];
                    int val = int.Parse(hexMatch.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                    background_sprite_bytes.Add(val);
                }
            }
        }

        // ********************************************************************
        public static List<Usage> GetEnemyUsages(int spriteNum)
        {
            // find usages
            List<Usage> usages = new List<Usage>();
            foreach(var room in rooms)
            {
                int uses = 0;
                foreach(var enemy in room.enemies)
                {
                    if (enemy.sprite == spriteNum)
                    {
                        uses++;
                    }
                }
                if (uses > 0)
                {
                    if (room.title.name == null)
                    {
                        Console.WriteLine();
                    }
                    usages.Add(new Usage(room.title.name, uses));
                }
            }

            return usages;
        }

        // ********************************************************************
        public static List<Usage> GetUsages(int spriteNum, out bool isUsed)
        {
            // find usages
            List<Usage> usages = new List<Usage>();
            foreach(var room in rooms)
            {
                for(int tileType = 0; tileType < 6; tileType++)
                { 
                    Usage newUsage = new Usage(room.title.name + " " + MainClass.GetTileName(tileType), 0);
                    bool found = false;

                    if (room.platformTile.sprite == spriteNum)
                    {
                        if (tileType == 0)
                        {
                            found = true;
                        }
                    }
                    if (room.wallTile.sprite == spriteNum)
                    {
                        if (tileType == 1)
                        {
                            found = true;
                        }
                    }
                    if (room.slopeTile.sprite == spriteNum)
                    {
                        if (tileType == 2)
                        {
                            found = true;
                        }
                    }
                    if (room.conveyorTile.sprite == spriteNum)
                    {
                        if (tileType == 3)
                        {
                            found = true;
                        }
                    }
                    if (room.deadlyTile.sprite == spriteNum)
                    {
                        if (tileType == 4)
                        {
                            found = true;
                        }
                    }
                    if (room.itemTile.sprite == spriteNum)
                    {
                        if (tileType == 5)
                        {
                            found = true;
                        }
                    }

                    if (found)
                    {
                        usages.Add(newUsage);
                        var usingSprite = false;

                        foreach(var command in room.commands)
                        {
                            if (command is UseTileCommand useTileCommand)
                            {
                                if (tileType == useTileCommand.tileType)
                                { 
                                    switch(useTileCommand.tileType)
                                    {
                                        case 0:
                                            usingSprite = room.platformTile.sprite == spriteNum;
                                            break;
                                        case 1:
                                            usingSprite = room.wallTile.sprite == spriteNum;
                                            break;
                                        case 2:
                                            usingSprite = room.slopeTile.sprite == spriteNum;
                                            break;
                                        case 3:
                                            usingSprite = room.conveyorTile.sprite == spriteNum;
                                            break;
                                        case 4:
                                            usingSprite = room.deadlyTile.sprite == spriteNum;
                                            break;
                                        case 5:
                                            usingSprite = room.itemTile.sprite == spriteNum;
                                            break;
                                    }
                                }
                            }
                            else if ((command is BlockTileCommand) ||
                                        (command is ListTileCommand) ||
                                        (command is StripTileCommand) ||
                                        (command is TriangleTileCommand))
                            {
                                if (usingSprite)
                                {
                                    newUsage.uses++;
                                }
                            }
                            else if (command is SlopeTileCommand)
                            {
                                newUsage.uses++;
                            }

                        }
                    }
                }
            }

            usages = usages.Where((x) => x.uses > 0).ToList();

            isUsed = false;
            foreach(var use in usages)
            {
                if (use.uses > 0)
                {
                    isUsed = true;
                }
            }

            return usages;
        }

        // ********************************************************************
        public static void Main(string[] args)
        {
            int i = 0;
            string[] lines;

            Console.WriteLine("Level Decoder!");
            room_bytes.Clear();
            background_sprite_bytes.Clear();
            rooms.Clear();
            lines = File.ReadAllLines("/Users/tobynelson/Code/6502/jsw2021/jsw1.a");

            // Create or truncate output file
            using (StreamWriter outputFile = new StreamWriter(outputFilepath))
            {
            }

            state_room = -1;
            state_background_sprite = -1;
            for (i = 0; i < lines.Length; i++)
            {
                // Remove comments
                var line = lines[i].Split(new char[] { ';' })[0];

                ProcessRoomLines(line);
                ProcessBackgroundSpriteLines(line);
                ProcessEnemySpriteLines(line);
                ProcessEnemyFrameOffsetLines(line);
            }

            //
            // Append to output file
            //
            foreach(var room in rooms)
            {
                room.Output(outputFilepath);
            }

            // output background sprites
            int spriteNum = 0;
            using (StreamWriter outputFile = new StreamWriter(outputFilepath, true))
            {
                foreach(var sprite in backgroundSprites)
                {
                    WriteLine(outputFile, 0, "BackgroundSprite " + spriteNum);

                    var usages = GetUsages(spriteNum, out bool isUsed);

                    for(int k = 0; k < Math.Max(usages.Count, sprite.bytes.Count); k++)
                    {
                        var comment = "";
                        if (k < usages.Count)
                        {
                            comment = usages[k].usageName;
                            if (usages[k].uses == 0) {
                                comment += " [unused]";
                            }
                            else {
                                comment += " [x" + usages[k].uses + "]";
                            }
                        }
                        if ((k == 0) && (!isUsed))
                        {
                            comment = "[unused]";
                        }
                        var sb = (k < sprite.bytes.Count) ? ToBinaryString(sprite.bytes[k]) : "";
                        WriteLine(outputFile, 4, sb, comment);
                    }
                    WriteLine(outputFile, 0, "");
                    spriteNum++;
                }
            }

            using (StreamWriter outputFile = new StreamWriter(outputFilepath, true))
            {
                // output enemy sprites
                spriteNum = 0;
                var enemyNum = 1;
                var frameOffsetWithinEnemy = 0;
                foreach(var sprite in enemySprites)
                {
                    if (frameOffsetWithinEnemy == 0)
                    { 
                        WriteLine(outputFile, 0, "Enemy " + enemyNum);
                    }
                    WriteLine(outputFile, 4, "Sprite");

                    var usages = GetEnemyUsages(enemyNum);

                    for(int k = 0; k < Math.Max(usages.Count, sprite.bytes.Count); k++)
                    {
                        var comment = "";
                        if (frameOffsetWithinEnemy == 0)
                        { 
                            if (k < usages.Count)
                            {
                                comment = usages[k].usageName;
                                if (usages[k].uses == 0) {
                                    comment += " [unused]";
                                }
                                else {
                                    if((comment != "") && (comment != null))
                                    { 
                                        comment += " [x" + usages[k].uses + "]";
                                    }
                                }
                            }
                        }
                        var sb = (k < sprite.bytes.Count) ? ToBinaryString(sprite.bytes[k], 16) : "";
                        WriteLine(outputFile, 8, sb, comment);
                    }
                    WriteLine(outputFile, 0, "");

                    frameOffsetWithinEnemy++;
                    if (frameOffsetWithinEnemy >= enemyFrameLengths[enemyNum])
                    {
                        enemyNum++;
                        frameOffsetWithinEnemy = 0;
                    }

                    spriteNum++;
                }
            }
        }
    }
}
