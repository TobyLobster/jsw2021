using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace EncodeData
{
    public class JSWChecker
    {
        public JSWChecker()
        {
        }

        public enum CheckState
        {
            None,
            EnemySpriteFrames,
            DecodeTable,
            SpriteData
        }

        Regex hexPattern = new Regex(@"\$([0-9A-Fa-f][0-9A-Fa-f])");
        Regex binaryPattern = new Regex(@"\%([#\.][#\.][#\.][#\.][#\.][#\.][#\.][#\.])");

        // ********************************************************************
        List<int> GetBytes(string line)
        {
            var result = new List<int>();

            if(line.StartsWith("!byte"))
            {
                // Get hex bytes
                var matches = hexPattern.Matches(line);

                for(var i =0; i < matches.Count; i++)
                {
                    var hex = matches[i].Groups[1].Value;
                    result.Add(int.Parse(hex, System.Globalization.NumberStyles.HexNumber));
                }

                // Or get binary bytes (but don't mix the two on the same line!)
                matches = binaryPattern.Matches(line);
                for(var i =0; i < matches.Count; i++)
                {
                    var binary = matches[i].Groups[1].Value.Replace("#", "1").Replace(".", "0");
                    result.Add(Convert.ToInt32(binary,2));
                }
            }
            return result;
        }



        // ********************************************************************
        public void AddToDecodedBytes(
            ref List<int> decodedBytes,
            ref List<int> previousBytes,
            int val)
        {
            decodedBytes.Add(val);

            if (val != previousBytes[3])
            {
                previousBytes.Add(val);
                previousBytes.RemoveAt(0);
            }
        }

        int nybbleIndex = 0;

        // ********************************************************************
        int GetNextNybble(List<int> spriteData)
        {
            int by = spriteData[nybbleIndex/2];
            nybbleIndex++;
            if ((nybbleIndex & 1) == 1)
            {
                return by % 16;
            }
            else
            {
                return by >> 4;
            }
        }

        // ********************************************************************
        public int enemySprites = 0;                        // A constant
        public List<int> spriteData = new List<int>();      //
        public List<int> decodeTable = new List<int>();     //

        // ********************************************************************
        public void DecodeSprite(StreamWriter outputFile, int n)
        {
            // Skip to sprite n
            bool isAtStartOfSprite;
            int sizeInBytes = 32;
            int spriteIndex = n;
            int enemySpriteCounter = enemySprites;
            int decoded = 0;
            nybbleIndex = 0;
            while (true)
            {
                isAtStartOfSprite = (decoded & (sizeInBytes-1)) == 0;
                if (isAtStartOfSprite)
                {
                    if (enemySpriteCounter == 0)
                    {
                        sizeInBytes = 8;
                    }
                    if (spriteIndex == 0)
                    {
                        break;
                    }
                    spriteIndex--;
                    if( enemySpriteCounter > 0)
                    { 
                        enemySpriteCounter--;
                    }
                }

                var command = GetNextNybble(spriteData);
                decoded++;      // One byte will be decoded

                if (command < 4)
                {
                    if(isAtStartOfSprite)
                    {
                        for(int i = 0; i < sizeInBytes; i++)
                        {
                            GetNextNybble(spriteData);
                            GetNextNybble(spriteData);
                        }
                        decoded += sizeInBytes - 1;
                    }
                }
                else if (command >= 10)
                {
                    GetNextNybble(spriteData);
                    if (command == 15)
                    {
                        GetNextNybble(spriteData);
                    }
                }
            }

//            if (n == 128)
//            {
//                Console.WriteLine();
//            }

            var decodedBytes = new List<int>();

            // Decode sprite
            var previousBytes = new List<int>();
            previousBytes.Add(0);
            previousBytes.Add(0);
            previousBytes.Add(0);
            previousBytes.Add(0);

            isAtStartOfSprite = true;
            while(decodedBytes.Count < sizeInBytes)
            {
                var command = GetNextNybble(spriteData);
                if (command < 4)
                {
                    if(isAtStartOfSprite)
                    {
                        for(int i = 0; i < sizeInBytes; i++)
                        {
                            var val = GetNextNybble(spriteData);
                            val += GetNextNybble(spriteData) * 16;
                            AddToDecodedBytes(ref decodedBytes, ref previousBytes, val);
                        }
                    }
                    else
                    { 
                        AddToDecodedBytes(ref decodedBytes, ref previousBytes, previousBytes[command]);
                    }
                }
                else if (command == 4)
                {
                    var val = previousBytes[3];
                    var rollLeft = ((val << 1) & 255) + ((val & 128) >> 7);
                    AddToDecodedBytes(ref decodedBytes, ref previousBytes, rollLeft);
                }
                else if (command == 5)
                {
                    var val = previousBytes[3];
                    var rollRight = (val >> 1) + 128 * (val & 1);
                    AddToDecodedBytes(ref decodedBytes, ref previousBytes, rollRight);
                }
                else if (command < 10)
                {
                    var commonIndex = command - 6;
                    AddToDecodedBytes(ref decodedBytes, ref previousBytes, decodeTable[commonIndex]);
                }
                else if (command < 15)
                {
                    var commonIndex = 4 + ((command - 10) * 16 + GetNextNybble(spriteData));

                    AddToDecodedBytes(ref decodedBytes, ref previousBytes, decodeTable[commonIndex]);
                }
                else if (command == 15)
                {
                    var val = GetNextNybble(spriteData);
                    val += 16 * GetNextNybble(spriteData);
                    AddToDecodedBytes(ref decodedBytes, ref previousBytes, val);
                }
                else
                {
                    Debug.Assert(false);
                }
                isAtStartOfSprite = false;
            }

            //
            // Show sprite
            //
            var byteIndex = 0;
            var size = 8;
            if (sizeInBytes == 32)
            {
                size = 16;
            }
            outputFile.WriteLine("; Sprite " + n);
            for(int row = 0; row < size; row++)
            {
                if (size == 16)
                { 
                    string line1 = Convert.ToString(decodedBytes[byteIndex++], 2).PadLeft(8, '0').Replace("0", ".").Replace("1", "#");
                    string line2 = Convert.ToString(decodedBytes[byteIndex++], 2).PadLeft(8, '0').Replace("0", ".").Replace("1", "#");

                    outputFile.WriteLine("; " + line2 + line1);
                }
                else
                {
                    string line = Convert.ToString(decodedBytes[byteIndex++], 2).PadLeft(8, '0').Replace("0", ".").Replace("1", "#");
                    outputFile.WriteLine("; " + line);
                }
            }
            outputFile.WriteLine();
        }

        // ********************************************************************
        public void CheckOutputFile(string filepath)
        {
            // enemy_sprites_frames
            // contains the number of sprite frames for each enemy
            // until enemy_sprites_frames_end

            // sprite_decode_table
            // Common bytes
            // until sprite_decode_table_end

            // sprite_data
            // Encoded sprite data
            // until sprite_data_end

            var enemySpriteFrames = new List<int>();
            decodeTable = new List<int>();
            spriteData = new List<int>();

            nybbleIndex = 0;
            var lines = File.ReadAllLines(filepath);
            var state = CheckState.None;
            foreach(var rawLine in lines)
            {
                // Remove comments
                var line = rawLine.Split(new char[] { ';' })[0].Trim();

                if (line == "enemy_sprites_frames")
                {
                    state = CheckState.EnemySpriteFrames;
                }
                else if(line == "enemy_sprites_frames_end")
                {
                    state = CheckState.None;
                }
                else if(line == "sprite_decode_table")
                {
                    state = CheckState.DecodeTable;
                }
                else if(line == "sprite_decode_table_end")
                {
                    state = CheckState.None;
                }
                else if(line == "sprite_data")
                {
                    state = CheckState.SpriteData;
                }
                else if(line == "sprite_data_end")
                {
                    state = CheckState.None;
                }
                else
                {
                    var newBytes = GetBytes(line);

                    if (newBytes.Count > 0)
                    { 
                        switch(state)
                        {
                            case CheckState.EnemySpriteFrames:
                                enemySpriteFrames.AddRange(newBytes);
                                break;
                            case CheckState.DecodeTable:
                                decodeTable.AddRange(newBytes);
                                break;
                            case CheckState.SpriteData:
                                spriteData.AddRange(newBytes);
                                break;
                        }
                    }
                }
            }

            // Count enemy sprites
            enemySprites = 0;
            for(int i = 1; i < enemySpriteFrames.Count; i++)
            {
                enemySprites += (enemySpriteFrames[i] & 0x7f);
            }

            // Check that the sprites can be decoded one by one
            var checkerFilepath = filepath + "_checker.txt";
            using (StreamWriter outputFile = new StreamWriter(checkerFilepath))
            {
                for(int i = 0; i < 384; i++)
                {
                    DecodeSprite(outputFile, i);
                }
            }
        }
    }
}
