using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace DecodeLevels
{
    class MainClass
    {
        static List<int> room_bytes = new List<int>();
        static int room_bit_counter = 0;

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
            if (comment != "")
            {
                 mess += "; " + comment;
            }
            mess = mess.TrimEnd();
            output.WriteLine(mess);
        }

        // ********************************************************************
        public static void DecodeRoomEnd(int room)
        {
            if (room < 0) {
                return;
            }
            room_bit_counter = 0;

            // Append to output file
            using (StreamWriter outputFile = new StreamWriter(@"/Users/tobynelson/temp.txt", true))
            {
                if (room != 1)
                {
                    outputFile.WriteLine("");
                }
                outputFile.WriteLine("Room " + room);

                if (room > 0)
                {
                    int num_items = GetBits(4);
                    outputFile.Write("    Number of items: " + num_items);
                    if (num_items > 0)
                    {
                        outputFile.Write(" (");
                    }
                    for(int i = 0; i < num_items; i++)
                    {
                        if (i > 0)
                        {
                            outputFile.Write(", ");
                        }
                        outputFile.Write((GetBits(1) == 1 ? "collected" : "not collected"));
                    }
                    if (num_items > 0)
                    {
                        outputFile.Write(")");
                    }
                    outputFile.WriteLine("");
                    int room_left = GetBits(6);
                    int room_right = GetBits(6);
                    int room_up = GetBits(6);
                    int room_down = GetBits(6);
                    outputFile.Write("    Exits: left " + room_left + ", ");
                    outputFile.Write("right " + room_right + ", ");
                    outputFile.Write("up " + room_up + ", ");
                    outputFile.WriteLine("down " + room_down);

                    outputFile.WriteLine("    Conveyor direction: " + ((GetBits(1) != 0) ? "right" : "left"));
                    outputFile.WriteLine("    Slope direction: " + ((GetBits(1) != 0) ? @"\" : @"/"));
                    outputFile.WriteLine("    Rope present: " + ((GetBits(1) != 0) ? "yes" : "no"));
                    outputFile.WriteLine("    Deadly tile logical colour  : " + GetBits(2));
                    outputFile.WriteLine("    Conveyor tile logical colour: " + GetBits(2));
                    outputFile.WriteLine("    Slope tile logical colour   : " + GetBits(2));
                    outputFile.WriteLine("    Wall tile logical colour    : " + GetBits(2));
                    outputFile.WriteLine("    Platform tile logical colour: " + GetBits(2));

                    outputFile.WriteLine("    Item tile sprite    : " + GetBits(8));
                    outputFile.WriteLine("    Deadly tile sprite  : " + GetBits(8));
                    outputFile.WriteLine("    Conveyor tile sprite: " + GetBits(8));
                    outputFile.WriteLine("    Slope tile sprite   : " + GetBits(8));
                    outputFile.WriteLine("    Wall tile sprite    : " + GetBits(8));
                    outputFile.WriteLine("    Platform tile sprite: " + GetBits(8));

                    outputFile.Write("    Palette: ");
                    int physical_colour3 = GetBits(3);
                    int physical_colour2 = GetBits(3);
                    int physical_colour1 = GetBits(3);
                    int physical_colour0 = GetBits(3);
                    outputFile.Write(GetColourName(physical_colour0) + ", ");
                    outputFile.Write(GetColourName(physical_colour1) + ", ");
                    outputFile.Write(GetColourName(physical_colour2) + ", ");
                    outputFile.Write(GetColourName(physical_colour3));
                    outputFile.WriteLine();

                    outputFile.Write("    Title tab " + GetBits(4) + ", ");

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
                    outputFile.WriteLine("\"" + title + "\"");

                    // Tiles
                    outputFile.WriteLine("    Tile commands:");
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
                                    outputFile.WriteLine("        Use " + GetTileName(newTile));
                                }
                                else if (command == 1)
                                {
                                    int x = GetBits(5);
                                    int y = GetBits(4);
                                    outputFile.WriteLine("        Move to (" + x + "," + y + ")");
                                }
                                else if (command == 2)
                                {
                                    int dir = GetBits(1);
                                    int len = GetBits(5);
                                    WriteLine(outputFile, 8, "Draw " + ((dir == 0) ? "horizontal" : "vertical") + " strip until " + ((dir == 0) ? "X" : "Y") + "=" + len, "'Draw strip' moves the cursor to the end tile");
                                }
                                break;
                            case 1:
                                if (command == 0)
                                {
                                    int extent_x = GetBits(5);
                                    int final_y = GetBits(4);
                                    WriteLine(outputFile, 8, "Draw block to (" + extent_x + "," + final_y + ")", "'Draw block' preserves cursor Y, and moves cursor X to one beyond final extent");
                                }
                                else if (command == 1)
                                {
                                    int num_tiles = GetBits(4) + 1;
                                    if (num_tiles == 1)
                                    {
                                        outputFile.Write("        Draw 1 single tile at");
                                    } else
                                    {
                                        outputFile.Write("        Draw " + num_tiles + " single tiles at");
                                    }
                                    for(int i = 0; i < num_tiles; i++)
                                    {
                                        int x = GetBits(5);
                                        int y = GetBits(4);
                                        outputFile.Write(" (" + x + "," + y + ")");
                                    }
                                    outputFile.WriteLine("");
                                }
                                else if (command == 2)
                                {
                                    int dir = GetBits(1);
                                    int final_y = GetBits(4);
                                    WriteLine(outputFile, 8, "Draw slope " + ((dir == 0) ? @"moving right" : @"moving left") + " until Y=" + final_y, "'Draw slope' moves the cursor to the end tile");
                                }
                                break;
                            case 2:
                                if (command == 0)
                                {
                                    finished = true;
                                }
                                else if (command == 1)
                                {
                                    outputFile.WriteLine("???????????");
                                }
                                else if (command == 2)
                                {
                                    int dir = GetBits(1);
                                    int extent = GetBits(4);
                                    outputFile.WriteLine("        Draw triangle " + ((dir == 0) ? @"moving right" : @"moving left") + " until Y=" + extent);
                                }
                                break;
                        }
                    }
                }

                // Enemies
                int num_enemies = GetBits(3);
                WriteLine(outputFile, 4, "Enemies: " + num_enemies);
                for(int i = 0; i < num_enemies; i++)
                {
                    int sprite = GetBits(6);
                    int x = GetBits(5);
                    int y = GetBits(4);
                    int min = GetBits(5);
                    int max = GetBits(5);
                    int dir = GetBits(1);
                    int delta = GetBits(1);
                    int speed = GetBits(3);
                    int col = GetBits(2);

                    int direction = dir * 2 + delta;
                    string dirMess = "?????";
                    switch (direction)
                    {
                        case 0: dirMess = "left"; break;
                        case 1: dirMess = "right"; break;
                        case 2: dirMess = "up"; break;
                        case 3: dirMess = "down"; break;
                    }

                    WriteLine(outputFile, 4, "Enemy " + i);
                    WriteLine(outputFile, 8, "Sprite: " + sprite);
                    WriteLine(outputFile, 8, "Initial Pos: (" + x + "," + y + ")");
                    WriteLine(outputFile, 8, "Min Extent: " + min);
                    WriteLine(outputFile, 8, "Max Extent: " + max);
                    WriteLine(outputFile, 8, "Initial Dir: " + dirMess);
                    WriteLine(outputFile, 8, "Speed: " + speed);
                    WriteLine(outputFile, 8, "Logical colour: " + col);
                }

                // Arrows
                for (int i = 0; i < 2; i++)
                {
                    if (GetBits(1) != 0)
                    {
                        int y = GetBits(4);
                        int timing = GetBits(3);

                        WriteLine(outputFile, 4, "Arrow: Y = " + y + ", timing: " + timing);
                    }
                }
            }

            room_bytes.Clear();
        }

        // ********************************************************************
        public static void Main(string[] args)
        {
            int i = 0;
            string[] lines;
            int state_room = -1;

            Console.WriteLine("Level Decoder!");
            room_bytes.Clear();
            lines = File.ReadAllLines("/Users/tobynelson/Code/6502/jsw2021/jsw1.a");

            // Create or truncate output file
            using (StreamWriter outputFile = new StreamWriter(@"/Users/tobynelson/temp.txt"))
            {
            }

            for (i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Split(new char[] { ';' })[0];
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
        }
    }
}
