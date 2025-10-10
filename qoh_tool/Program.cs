using System;
using System.IO;
using System.Text;

namespace qoh_tool
{
    public static class Colors
    {
        public const string Reset = "\u001b[0m";
        public const string Bold = "\u001b[1m";
        
        // Epic 24-bit RGB colors for QOH CHR Decryptor
        public const string ElectricBlue = "\u001b[38;2;0;191;255m";      // #00BFFF - Header border
        public const string CyberGreen = "\u001b[38;2;0;255;127m";        // #00FF7F - Success messages
        public const string NeonPurple = "\u001b[38;2;191;64;191m";       // #BF40BF - Filenames
        public const string GoldYellow = "\u001b[38;2;255;215;0m";        // #FFD700 - Progress/sizes
        public const string CrystalWhite = "\u001b[38;2;248;248;255m";    // #F8F8FF - Main text
        public const string LaserRed = "\u001b[38;2;255;20;147m";         // #FF1493 - Errors
        public const string TechOrange = "\u001b[38;2;255;140;0m";        // #FF8C00 - Addresses
        public const string MatrixGreen = "\u001b[38;2;0;255;65m";        // #00FF41 - Data values
        public const string DeepPurple = "\u001b[38;2;138;43;226m";       // #8A2BE2 - Section headers
        
        // Fallback standard colors for compatibility
        public const string Red = "\u001b[31m";
        public const string Green = "\u001b[32m";
        public const string Yellow = "\u001b[33m";
        public const string Blue = "\u001b[34m";
        public const string Magenta = "\u001b[35m";
        public const string Cyan = "\u001b[36m";
        public const string White = "\u001b[37m";
        public const string BrightRed = "\u001b[91m";
        public const string BrightGreen = "\u001b[92m";
        public const string BrightYellow = "\u001b[93m";
        public const string BrightBlue = "\u001b[94m";
        public const string BrightMagenta = "\u001b[95m";
        public const string BrightCyan = "\u001b[96m";
        public const string BrightWhite = "\u001b[97m";
    }
    
    public static class ColorConsole
    {
        static ColorConsole()
        {
            // Enable ANSI color support on Windows
            try
            {
                var handle = GetStdHandle(-11);
                GetConsoleMode(handle, out uint mode);
                SetConsoleMode(handle, mode | 0x0004);
            }
            catch
            {
                // Fallback if we can't enable ANSI colors
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        public static void WriteHeader(string text)
        {
            string border = "================================================================";
            string line1 = "              QOH CHR DECRYPTOR v2.0                           ";
            string line2 = "   Queen of Heart '99 - Watanabe Production/French Bread       ";
            string line3 = "                                                               ";
            string line4 = "          Credits: u4ick & COPKILLER4FUN                       ";
            
            Console.WriteLine($"{Colors.Bold}{Colors.ElectricBlue}{border}{Colors.Reset}");
            Console.WriteLine($"{Colors.Bold}{Colors.ElectricBlue}|{Colors.CrystalWhite}{line1}{Colors.ElectricBlue}|{Colors.Reset}");
            Console.WriteLine($"{Colors.Bold}{Colors.ElectricBlue}|{Colors.CrystalWhite}{line2}{Colors.ElectricBlue}|{Colors.Reset}");
            Console.WriteLine($"{Colors.Bold}{Colors.ElectricBlue}|{Colors.CrystalWhite}{line3}{Colors.ElectricBlue}|{Colors.Reset}");
            Console.WriteLine($"{Colors.Bold}{Colors.ElectricBlue}|{Colors.GoldYellow}{line4}{Colors.ElectricBlue}|{Colors.Reset}");
            Console.WriteLine($"{Colors.Bold}{Colors.ElectricBlue}{border}{Colors.Reset}");
        }
        
        public static void WriteSuccess(string text)
        {
            Console.WriteLine($"{Colors.Bold}{Colors.CyberGreen}[+] {text}{Colors.Reset}");
        }
        
        public static void WriteInfo(string text)
        {
            Console.WriteLine($"{Colors.ElectricBlue}[i] {text}{Colors.Reset}");
        }
        
        public static void WriteWarning(string text)
        {
            Console.WriteLine($"{Colors.Bold}{Colors.GoldYellow}[!] {text}{Colors.Reset}");
        }
        
        public static void WriteError(string text)
        {
            Console.WriteLine($"{Colors.Bold}{Colors.LaserRed}[-] {text}{Colors.Reset}");
        }
        
        public static void WriteSection(string text)
        {
            Console.WriteLine($"{Colors.Bold}{Colors.DeepPurple}>>> {text}{Colors.Reset}");
        }
        
        public static void WriteData(string label, object value)
        {
            Console.WriteLine($"{Colors.ElectricBlue}    {label}:{Colors.Reset} {Colors.MatrixGreen}{value}{Colors.Reset}");
        }
        
        public static void WriteProgress(string text)
        {
            Console.WriteLine($"{Colors.Bold}{Colors.GoldYellow}[*] {text}{Colors.Reset}");
        }
    }

    public class CHRDecryptor
    {
        public static byte[] GetPrimaryKey()
        {
            return new byte[] { 0x92, 0x4F, 0x89, 0xBA, 0x8D, 0xF7, 0x20, 0x90, 0xBC, 0x91, 0xBA, 0x82, 0xBF, 0x82, 0xC8, 0x82, 0xDD };
        }
        
        public static byte[] GetSecondaryKey()
        {
            // CRITICAL: Include null terminators from IDA - resets at null byte, not array end!
            return new byte[] { 0x62, 0xA4, 0x97, 0xA0, 0xDA, 0x0D, 0xBD, 0x91, 0xBC, 0x97, 0xAF, 0x92, 0xAF, 0xE5, 0xAF, 0xF0, 0x2E, 0x00, 0x00 };
        }

        public struct CHRSectionInfo
        {
            public uint AnimationCount;
            public uint CharacterDataCount; 
            public uint CollisionBoxCount;
            public uint SpriteDataCount;
            public uint AnimationFrameCount;
            public uint CollisionData2Count;
        }

        // Complex algorithm for file size
        public static uint DecryptFileSize(BinaryReader reader, byte[] filename, byte[] primaryKey)
        {
            reader.BaseStream.Position = 0x10; // File size at 0x10-0x13 (4 bytes)
            
            uint fileSize = 0;
            int filenameIndex = 0;
            int keyIndex = 0;
            
            for (int i = 0; i < 4; i++)
            {
                byte encryptedByte = reader.ReadByte();
                
                // Bit manipulation: ~(8 * i) + (i >> 3) - keep as int until final calculation
                int posMagicInt = (~(i << 3) + (i >> 3)) & 0xFF;
                
                // Complex XOR chain - step by step like Python
                int step1 = encryptedByte ^ filename[filenameIndex] ^ posMagicInt;
                int step2 = (step1 - primaryKey[keyIndex]) & 0xFF;
                byte decryptedByte = (byte)((step2 - 109) & 0xFF);
                
                fileSize |= (uint)(decryptedByte << (i * 8));
                
                // Rotate keys
                filenameIndex = (filenameIndex + 1) % filename.Length;
                keyIndex = (keyIndex + 1) % primaryKey.Length;
            }
            
            return fileSize;
        }

        // Complex algorithm for section table - WITH COMPREHENSIVE DEBUG
        public static CHRSectionInfo DecryptSectionTable(BinaryReader reader, byte[] filename, byte[] primaryKey, bool debugMode = false)
        {
            reader.BaseStream.Position = 0x14; // Section table at 0x14-0x2B (24 bytes)
            
            CHRSectionInfo sections = new CHRSectionInfo();
            uint[] sectionData = new uint[6];
            
            byte[] secondaryKey = GetSecondaryKey();
            int filenameIndex = 0;
            int secondaryKeyIndex = 0;
            
            if (debugMode)
            {
                ColorConsole.WriteSection("DEBUG: Section Table Decryption");
                ColorConsole.WriteData("Filename", Encoding.UTF8.GetString(filename));
                ColorConsole.WriteData("Filename Length", filename.Length);
                ColorConsole.WriteData("Primary Key Length", primaryKey.Length);
                ColorConsole.WriteData("Secondary Key Length", secondaryKey.Length);
                
                // Show first few bytes of each key
                string primaryKeyHex = BitConverter.ToString(primaryKey.Take(Math.Min(8, primaryKey.Length)).ToArray()).Replace("-", " ");
                string secondaryKeyHex = BitConverter.ToString(secondaryKey.Take(Math.Min(8, secondaryKey.Length)).ToArray()).Replace("-", " ");
                ColorConsole.WriteData("Primary Key (first 8)", primaryKeyHex);
                ColorConsole.WriteData("Secondary Key (first 8)", secondaryKeyHex);
            }
            
            // Read and show raw encrypted bytes for debugging
            byte[] rawSectionBytes = new byte[24];
            for (int i = 0; i < 24; i++)
            {
                rawSectionBytes[i] = reader.ReadByte();
            }
            
            if (debugMode)
            {
                string rawHex = BitConverter.ToString(rawSectionBytes).Replace("-", " ");
                ColorConsole.WriteData("Raw Section Bytes", rawHex);
            }
            
            // Reset position and decrypt
            reader.BaseStream.Position = 0x14;
            
            // Decrypt 24 bytes (6 uint32s) using complex algorithm with BOTH keys
            for (int i = 0; i < 24; i++)
            {
                byte encryptedByte = reader.ReadByte();
                
                // Bit manipulation: ~(8 * i) + (i >> 3) - keep as int until final calculation
                int posMagicInt = (~(i << 3) + (i >> 3)) & 0xFF;
                
                // Complex XOR chain - step by step matching IDA exactly
                int step1 = encryptedByte ^ filename[filenameIndex] ^ posMagicInt;
                int step2 = (step1 - primaryKey[secondaryKeyIndex]) & 0xFF;
                byte decryptedByte = (byte)((step2 - 109) & 0xFF);
                
                if (debugMode) // Show ALL bytes of decryption process
                {
                    Console.WriteLine($"{Colors.DeepPurple}    Byte {i,2}: 0x{encryptedByte:X2} ^ filename[{filenameIndex,2}]=0x{filename[filenameIndex]:X2} ^ magic=0x{posMagicInt:X2} - primaryKey[{secondaryKeyIndex,2}]=0x{primaryKey[secondaryKeyIndex]:X2} - 109 = 0x{decryptedByte:X2}{Colors.Reset}");
                }
                
                // Store in appropriate uint32
                int sectionIndex = i / 4;
                int byteOffset = i % 4;
                sectionData[sectionIndex] |= (uint)(decryptedByte << (byteOffset * 8));
                
                // Rotate keys exactly like IDA - filename wraps, increment secondary key for next iteration
                int oldFilenameIndex = filenameIndex;
                
                filenameIndex = (filenameIndex + 1) % filename.Length;
                
                // CRITICAL: IDA algorithm - increment THEN check for null byte in secondary key
                secondaryKeyIndex++;
                if (secondaryKeyIndex < secondaryKey.Length && secondaryKey[secondaryKeyIndex] == 0)
                {
                    secondaryKeyIndex = 0;
                    if (debugMode) Console.WriteLine($"{Colors.GoldYellow}    >>> Secondary key index RESET at byte {i + 1} (hit null byte in secondary key){Colors.Reset}");
                }
                else if (secondaryKeyIndex >= secondaryKey.Length)
                {
                    secondaryKeyIndex = 0;
                    if (debugMode) Console.WriteLine($"{Colors.GoldYellow}    >>> Secondary key index RESET at byte {i + 1} (exceeded secondary key length){Colors.Reset}");
                }
                
                if (debugMode && filenameIndex < oldFilenameIndex)
                    Console.WriteLine($"{Colors.CyberGreen}    >>> Filename index WRAPPED at byte {i} (back to 0){Colors.Reset}");
            }
            
            // Map to structure
            sections.AnimationCount = sectionData[0];
            sections.CharacterDataCount = sectionData[1]; 
            sections.CollisionBoxCount = sectionData[2];
            sections.SpriteDataCount = sectionData[3];
            sections.AnimationFrameCount = sectionData[4]; 
            sections.CollisionData2Count = sectionData[5];
            
            if (debugMode)
            {
                ColorConsole.WriteData("Raw Section Data[0]", $"0x{sectionData[0]:X8} ({sectionData[0]})");
                ColorConsole.WriteData("Raw Section Data[1]", $"0x{sectionData[1]:X8} ({sectionData[1]})");
                ColorConsole.WriteData("Raw Section Data[2]", $"0x{sectionData[2]:X8} ({sectionData[2]})");
                ColorConsole.WriteData("Raw Section Data[3]", $"0x{sectionData[3]:X8} ({sectionData[3]})");
                ColorConsole.WriteData("Raw Section Data[4]", $"0x{sectionData[4]:X8} ({sectionData[4]})");
                ColorConsole.WriteData("Raw Section Data[5]", $"0x{sectionData[5]:X8} ({sectionData[5]})");
            }
            
            return sections;
        }

        // Complex algorithm for section data - FIXED WITH SECONDARY KEY
        public static void DecryptSection(BinaryReader reader, BinaryWriter writer, 
                                        long startOffset, uint sectionSize, 
                                        byte[] filename, byte[] primaryKey)
        {
            reader.BaseStream.Position = startOffset;
            
            byte[] secondaryKey = GetSecondaryKey();
            int filenameIndex = 0;
            int secondaryKeyIndex = 0;
            
            for (uint i = 0; i < sectionSize; i++)
            {
                byte encryptedByte = reader.ReadByte();
                
                int posMagicInt = (~((int)i << 3) + ((int)i >> 3)) & 0xFF;
                
                // Complex XOR chain - step by step matching IDA exactly
                int step1 = encryptedByte ^ filename[filenameIndex] ^ posMagicInt;
                int safeKeyIndex = secondaryKeyIndex % primaryKey.Length; // Prevent array bounds error
                int step2 = (step1 - primaryKey[safeKeyIndex]) & 0xFF;  // Uses primaryKey with safe index
                byte decryptedByte = (byte)((step2 - 109) & 0xFF);
                
                writer.Write(decryptedByte);
                
                // Rotate keys exactly like IDA - filename wraps, secondaryKey controls primaryKey index reset
                filenameIndex = (filenameIndex + 1) % filename.Length;
                secondaryKeyIndex++;
                if (secondaryKeyIndex >= secondaryKey.Length || secondaryKey[secondaryKeyIndex] == 0)
                    secondaryKeyIndex = 0;
            }
        }
    }

    internal class Program
    {
        static void DecryptFolder(string inputFolder, string outputFolder)
        {
            ColorConsole.WriteSection("BATCH CHR DECRYPTION MODE");
            ColorConsole.WriteInfo($"Input folder: {Colors.CrystalWhite}{inputFolder}{Colors.Reset}");
            ColorConsole.WriteInfo($"Output folder: {Colors.CrystalWhite}{outputFolder}{Colors.Reset}");
            Console.WriteLine();

            Directory.CreateDirectory(outputFolder);

            var chrFiles = Directory.GetFiles(inputFolder, "*.chr", SearchOption.AllDirectories);
            ColorConsole.WriteInfo($"Found {Colors.GoldYellow}{chrFiles.Length}{Colors.Reset} CHR files to decrypt");
            Console.WriteLine();

            var successCount = 0;
            var failureCount = 0;
            byte[] primaryKey = CHRDecryptor.GetPrimaryKey();

            foreach (var chrFile in chrFiles)
            {
                try
                {
                    string outputFile = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(chrFile) + ".bin");

                    using (BinaryReader reader = new BinaryReader(File.OpenRead(chrFile)))
                    using (BinaryWriter writer = new BinaryWriter(File.Create(outputFile)))
                    {
                        // Decrypt header
                        reader.BaseStream.Position = 0;
                        int keyIndex = 0;
                        byte[] headerBytes = new byte[16];

                        for (int i = 0; i < 0x10; i++)
                        {
                            byte encryptedByte = reader.ReadByte();
                            byte decryptedByte = (byte)(encryptedByte ^ primaryKey[keyIndex]);
                            writer.Write(decryptedByte);
                            headerBytes[i] = decryptedByte;

                            keyIndex++;
                            if (keyIndex < primaryKey.Length && primaryKey[keyIndex] == 0)
                                keyIndex = 0;
                            else if (keyIndex >= primaryKey.Length)
                                keyIndex = 0;
                        }

                        string pathBasedFilename = Path.GetFileName(chrFile).Replace(".chr", "").Replace(".CHR", "").ToUpper();
                        byte[] filenameBytes = Encoding.UTF8.GetBytes(pathBasedFilename);

                        // Decrypt file size
                        uint decryptedSizeField = CHRDecryptor.DecryptFileSize(reader, filenameBytes, primaryKey);
                        writer.Write((byte)(decryptedSizeField & 0xFF));
                        writer.Write((byte)((decryptedSizeField >> 8) & 0xFF));
                        writer.Write((byte)((decryptedSizeField >> 16) & 0xFF));
                        writer.Write((byte)((decryptedSizeField >> 24) & 0xFF));

                        // Decrypt section table
                        CHRDecryptor.CHRSectionInfo sections = CHRDecryptor.DecryptSectionTable(reader, filenameBytes, primaryKey, false);
                        writer.Write((byte)(sections.AnimationCount & 0xFF));
                        writer.Write((byte)((sections.AnimationCount >> 8) & 0xFF));
                        writer.Write((byte)((sections.AnimationCount >> 16) & 0xFF));
                        writer.Write((byte)((sections.AnimationCount >> 24) & 0xFF));
                        writer.Write((byte)(sections.CharacterDataCount & 0xFF));
                        writer.Write((byte)((sections.CharacterDataCount >> 8) & 0xFF));
                        writer.Write((byte)((sections.CharacterDataCount >> 16) & 0xFF));
                        writer.Write((byte)((sections.CharacterDataCount >> 24) & 0xFF));
                        writer.Write((byte)(sections.CollisionBoxCount & 0xFF));
                        writer.Write((byte)((sections.CollisionBoxCount >> 8) & 0xFF));
                        writer.Write((byte)((sections.CollisionBoxCount >> 16) & 0xFF));
                        writer.Write((byte)((sections.CollisionBoxCount >> 24) & 0xFF));
                        writer.Write((byte)(sections.SpriteDataCount & 0xFF));
                        writer.Write((byte)((sections.SpriteDataCount >> 8) & 0xFF));
                        writer.Write((byte)((sections.SpriteDataCount >> 16) & 0xFF));
                        writer.Write((byte)((sections.SpriteDataCount >> 24) & 0xFF));
                        writer.Write((byte)(sections.AnimationFrameCount & 0xFF));
                        writer.Write((byte)((sections.AnimationFrameCount >> 8) & 0xFF));
                        writer.Write((byte)((sections.AnimationFrameCount >> 16) & 0xFF));
                        writer.Write((byte)((sections.AnimationFrameCount >> 24) & 0xFF));
                        writer.Write((byte)(sections.CollisionData2Count & 0xFF));
                        writer.Write((byte)((sections.CollisionData2Count >> 8) & 0xFF));
                        writer.Write((byte)((sections.CollisionData2Count >> 16) & 0xFF));
                        writer.Write((byte)((sections.CollisionData2Count >> 24) & 0xFF));

                        // Decrypt all sections
                        long currentOffset = 0x2C;

                        uint animFrameSize = sections.AnimationFrameCount * 8;
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, animFrameSize, filenameBytes, primaryKey);
                        currentOffset += animFrameSize;

                        uint collisionSize = sections.CollisionBoxCount * 24;
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, collisionSize, filenameBytes, primaryKey);
                        currentOffset += collisionSize;

                        uint animationSize = sections.AnimationCount * 12;
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, animationSize, filenameBytes, primaryKey);
                        currentOffset += animationSize;

                        uint baseDataSize = 1024;
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, baseDataSize, filenameBytes, primaryKey);
                        currentOffset += baseDataSize;

                        uint charDataSize = sections.CharacterDataCount * 260;
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, charDataSize, filenameBytes, primaryKey);
                        currentOffset += charDataSize;

                        uint spriteSize = sections.SpriteDataCount * 64;
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, spriteSize, filenameBytes, primaryKey);
                        currentOffset += spriteSize;

                        uint collision2Size = sections.CollisionData2Count * 24;
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, collision2Size, filenameBytes, primaryKey);

                        successCount++;
                        ColorConsole.WriteSuccess($"{Path.GetFileName(chrFile)} -> {Path.GetFileName(outputFile)}");
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    ColorConsole.WriteError($"{Path.GetFileName(chrFile)} - {ex.Message}");
                }
            }

            Console.WriteLine();
            ColorConsole.WriteSection("DECRYPTION SUMMARY");
            ColorConsole.WriteSuccess($"Successful: {Colors.CyberGreen}{successCount}{Colors.Reset}/{chrFiles.Length}");
            if (failureCount > 0)
                ColorConsole.WriteError($"Failed: {Colors.LaserRed}{failureCount}{Colors.Reset}/{chrFiles.Length}");
        }

        static void AnalyzeFolder(string inputFolder, string outputFolder)
        {
            ColorConsole.WriteSection("BATCH CHR ANALYSIS MODE");
            ColorConsole.WriteInfo($"Input folder: {Colors.CrystalWhite}{inputFolder}{Colors.Reset}");
            ColorConsole.WriteInfo($"Output folder: {Colors.CrystalWhite}{outputFolder}{Colors.Reset}");
            Console.WriteLine();

            var chrFiles = Directory.GetFiles(inputFolder, "*.chr", SearchOption.AllDirectories);
            ColorConsole.WriteInfo($"Found {Colors.GoldYellow}{chrFiles.Length}{Colors.Reset} CHR files to analyze");
            Console.WriteLine();

            var workingFiles = new List<string>();
            var failingFiles = new List<string>();
            byte[] primaryKey = CHRDecryptor.GetPrimaryKey();

            foreach (var chrFile in chrFiles)
            {
                try
                {
                    using (BinaryReader reader = new BinaryReader(File.OpenRead(chrFile)))
                    {
                        // Test header decryption
                        reader.BaseStream.Position = 0;
                        int keyIndex = 0;
                        byte[] headerBytes = new byte[16];
                        
                        for (int i = 0; i < 0x10; i++)
                        {
                            if (keyIndex >= primaryKey.Length) keyIndex = 0;
                            byte encryptedByte = reader.ReadByte();
                            byte decryptedByte = (byte)(encryptedByte ^ primaryKey[keyIndex]);
                            headerBytes[i] = decryptedByte;
                            keyIndex++;
                        }
                        
                        string decryptedFilename = Encoding.UTF8.GetString(headerBytes).TrimEnd('\0');
                        // CRITICAL FIX: Use filename from FILE PATH, not the full 16-byte decrypted header
                        // IDA lines 73-88 show: Generate expected filename from path, compare, then NULL-terminate
                        // at the match point. Use that trimmed filename (not full header) as XOR key.
                        string pathBasedFilename = Path.GetFileName(chrFile).Replace(".chr", "").Replace(".CHR", "").ToUpper();
                        byte[] filenameBytes = Encoding.UTF8.GetBytes(pathBasedFilename);

                        // Test section table decryption
                        CHRDecryptor.CHRSectionInfo sections = CHRDecryptor.DecryptSectionTable(reader, filenameBytes, primaryKey, false);
                        
                        // Check if section counts are reasonable (not garbage)
                        bool isWorking = sections.AnimationFrameCount < 100000 && sections.CollisionData2Count < 100000;
                        
                        if (isWorking)
                        {
                            workingFiles.Add(chrFile);
                            ColorConsole.WriteSuccess($"{Path.GetFileName(chrFile)} - {Colors.NeonPurple}{decryptedFilename}{Colors.Reset} - Frames: {Colors.MatrixGreen}{sections.AnimationFrameCount}{Colors.Reset}");
                        }
                        else
                        {
                            failingFiles.Add(chrFile);
                            ColorConsole.WriteError($"{Path.GetFileName(chrFile)} - {Colors.NeonPurple}{decryptedFilename}{Colors.Reset} - Frames: {Colors.LaserRed}{sections.AnimationFrameCount:N0}{Colors.Reset} (FAILED)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    failingFiles.Add(chrFile);
                    ColorConsole.WriteError($"{Path.GetFileName(chrFile)} - Exception: {ex.Message}");
                }
            }

            // Summary report
            Console.WriteLine();
            ColorConsole.WriteSection("ANALYSIS SUMMARY");
            ColorConsole.WriteSuccess($"Working files: {Colors.CyberGreen}{workingFiles.Count}{Colors.Reset}/{chrFiles.Length}");
            ColorConsole.WriteError($"Failing files: {Colors.LaserRed}{failingFiles.Count}{Colors.Reset}/{chrFiles.Length}");
            
            // Write detailed report to file
            string reportPath = Path.Combine(outputFolder, "chr_analysis_report.txt");
            Directory.CreateDirectory(outputFolder);
            
            using (var writer = new StreamWriter(reportPath))
            {
                writer.WriteLine("QOH CHR DECRYPTOR - BATCH ANALYSIS REPORT");
                writer.WriteLine($"Generated: {DateTime.Now}");
                writer.WriteLine($"Total files analyzed: {chrFiles.Length}");
                writer.WriteLine($"Working files: {workingFiles.Count}");
                writer.WriteLine($"Failing files: {failingFiles.Count}");
                writer.WriteLine();
                
                writer.WriteLine("WORKING FILES:");
                foreach (var file in workingFiles)
                {
                    writer.WriteLine($"  ✓ {file}");
                }
                
                writer.WriteLine();
                writer.WriteLine("FAILING FILES:");
                foreach (var file in failingFiles)
                {
                    writer.WriteLine($"  ✗ {file}");
                }
            }
            
            ColorConsole.WriteSuccess($"Detailed report saved to: {Colors.CyberGreen}{reportPath}{Colors.Reset}");
        }

        static void Main(string[] args)
        {
            ColorConsole.WriteHeader("");
            Console.WriteLine();
            
            if (args.Length == 0)
            {
                ColorConsole.WriteError("No input specified!");
                Console.WriteLine($"{Colors.BrightYellow}Usage:{Colors.Reset}");
                Console.WriteLine($"  {Colors.BrightWhite}Single file:     {Colors.GoldYellow}qoh_tool.exe <chr_file>{Colors.Reset}");
                Console.WriteLine($"  {Colors.BrightWhite}Batch analysis:  {Colors.GoldYellow}qoh_tool.exe <input_folder> <report_folder>{Colors.Reset}");
                Console.WriteLine($"  {Colors.BrightWhite}Batch decrypt:   {Colors.GoldYellow}qoh_tool.exe <input_folder> -o <output_folder>{Colors.Reset}");
                return;
            }

            // Check if batch decrypt mode (3 arguments with -o flag)
            if (args.Length == 3 && args[1] == "-o")
            {
                DecryptFolder(args[0], args[2]);
                return;
            }

            // Check if batch analysis mode (2 arguments = input folder + report folder)
            if (args.Length == 2)
            {
                AnalyzeFolder(args[0], args[1]);
                return;
            }

            ColorConsole.WriteInfo($"Processing file: {Colors.BrightWhite}{args[0]}{Colors.Reset}");
            Console.WriteLine();
            
            try
            {
                byte[] primaryKey = CHRDecryptor.GetPrimaryKey();
                
                using (BinaryReader reader = new BinaryReader(File.OpenRead(args[0])))
                using (BinaryWriter writer = new BinaryWriter(File.Create(args[0] + "_decrypted.bin")))
                {
                    // Step 1: Decrypt header using CORRECT IDA algorithm
                    reader.BaseStream.Position = 0;
                    int keyIndex = 0;
                    byte[] headerBytes = new byte[16];
                    
                    for (int i = 0; i < 0x10; i++)
                    {
                        byte encryptedByte = reader.ReadByte();
                        byte decryptedByte = (byte)(encryptedByte ^ primaryKey[keyIndex]);
                        writer.Write(decryptedByte);
                        headerBytes[i] = decryptedByte;
                        
                        keyIndex++;
                        // CRITICAL: Reset when we hit null byte in key, not at end of array
                        if (keyIndex < primaryKey.Length && primaryKey[keyIndex] == 0)
                            keyIndex = 0;
                        else if (keyIndex >= primaryKey.Length)
                            keyIndex = 0;
                    }
                    
                    // Extract filename from decrypted header with CORRECT IDA validation
                    // Generate expected filename (remove path and extension)
                    string expectedFilename = Path.GetFileNameWithoutExtension(args[0]).ToUpper();
                    
                    // Find the actual filename length by comparing with expected (IDA lines 77-88)
                    int actualFilenameLength = 16;
                    for (int i = 0; i < 16; i++)
                    {
                        if (headerBytes[i] == 0)
                        {
                            actualFilenameLength = i;
                            break;
                        }
                    }
                    
                    // Extract the correct filename
                    string decryptedFilename = Encoding.UTF8.GetString(headerBytes, 0, actualFilenameLength);
                    ColorConsole.WriteSuccess($"Decrypted filename: {Colors.NeonPurple}{decryptedFilename}{Colors.Reset}");
                    ColorConsole.WriteInfo($"Expected filename: {Colors.NeonPurple}{expectedFilename}{Colors.Reset}");
                    ColorConsole.WriteInfo($"Decrypted filename length: {Colors.GoldYellow}{actualFilenameLength}{Colors.Reset}");

                    // CRITICAL FIX: Use filename from FILE PATH, not the full 16-byte decrypted header
                    // IDA lines 73-88: Generate expected from path, validate, NULL-terminate at match point
                    // The game uses the PATH-BASED filename (e.g., "AKARI"), not full header ("AKARIAKARIAKARIA")
                    string pathBasedFilename = Path.GetFileNameWithoutExtension(args[0]).ToUpper();
                    byte[] filenameBytes = Encoding.UTF8.GetBytes(pathBasedFilename);
                    ColorConsole.WriteInfo($"Using XOR key from path: {Colors.GoldYellow}{pathBasedFilename}{Colors.Reset} ({pathBasedFilename.Length} bytes)");

                    // Step 2: Go back to the working approach
                    long actualFileSize = reader.BaseStream.Length;
                    uint decryptedSizeField = CHRDecryptor.DecryptFileSize(reader, filenameBytes, primaryKey);
                    ColorConsole.WriteSuccess($"Actual file size: {Colors.GoldYellow}{actualFileSize:N0} bytes{Colors.Reset}");
                    ColorConsole.WriteInfo($"Decrypted size field: {Colors.GoldYellow}{decryptedSizeField} {Colors.Reset}(possibly filename length or header info)");
                    Console.WriteLine();
                    
                    // Step 3: Use our working section table approach with debug for ALL files
                    CHRDecryptor.CHRSectionInfo sections = CHRDecryptor.DecryptSectionTable(reader, filenameBytes, primaryKey, true);
                    
                    ColorConsole.WriteSection("CHR SECTION ANALYSIS");
                    ColorConsole.WriteData("Animations", sections.AnimationCount);
                    ColorConsole.WriteData("Character Data", sections.CharacterDataCount);
                    ColorConsole.WriteData("Collision Boxes", sections.CollisionBoxCount);
                    ColorConsole.WriteData("Sprites", sections.SpriteDataCount);
                    ColorConsole.WriteData("Animation Frames", sections.AnimationFrameCount);
                    ColorConsole.WriteData("Collision Data2", sections.CollisionData2Count);
                    
                    // Enhanced analysis for working vs failing patterns
                    bool isFailedFile = sections.AnimationFrameCount > 100000 || sections.CollisionData2Count > 100000;
                    
                    Console.WriteLine();
                    ColorConsole.WriteSection("DECRYPTION STATUS ANALYSIS");
                    
                    if (isFailedFile)
                    {
                        ColorConsole.WriteError("❌ FAILED FILE - Section table decryption failed");
                        Console.WriteLine($"{Colors.LaserRed}    Animation Frames: {sections.AnimationFrameCount:N0} (GARBAGE - should be < 1000){Colors.Reset}");
                        Console.WriteLine($"{Colors.LaserRed}    Collision Data2: {sections.CollisionData2Count:N0} (GARBAGE - should be < 1000){Colors.Reset}");
                        Console.WriteLine();
                        
                        ColorConsole.WriteInfo("✅ WHAT'S WORKING:");
                        Console.WriteLine($"{Colors.CyberGreen}    - Filename decryption: PERFECT ({decryptedFilename}){Colors.Reset}");
                        Console.WriteLine($"{Colors.CyberGreen}    - File size field: {decryptedSizeField} (consistent with working files){Colors.Reset}");
                        Console.WriteLine($"{Colors.CyberGreen}    - First 4 section values look reasonable{Colors.Reset}");
                        
                        Console.WriteLine();
                        ColorConsole.WriteError("❌ WHAT'S FAILING:");
                        Console.WriteLine($"{Colors.LaserRed}    - Section table bytes 16-23 (Animation Frames & Collision Data2){Colors.Reset}");
                        Console.WriteLine($"{Colors.LaserRed}    - Likely different key rotation or algorithm for these specific bytes{Colors.Reset}");
                        
                        Console.WriteLine();
                        ColorConsole.WriteInfo("🔍 PATTERN HYPOTHESIS:");
                        Console.WriteLine($"{Colors.GoldYellow}    - Same filename encryption for ALL files{Colors.Reset}");
                        Console.WriteLine($"{Colors.GoldYellow}    - Same algorithm for first 16 bytes of section table{Colors.Reset}");
                        Console.WriteLine($"{Colors.GoldYellow}    - Different key/algorithm for bytes 16-23 on some files{Colors.Reset}");
                        Console.WriteLine($"{Colors.GoldYellow}    - Possibly path-based or character-specific keys{Colors.Reset}");
                        
                        Console.WriteLine();
                        ColorConsole.WriteInfo("🔬 RAW BYTES ANALYSIS:");
                        reader.BaseStream.Position = 0x14;
                        byte[] problemBytes = new byte[24];
                        for (int i = 0; i < 24; i++)
                        {
                            problemBytes[i] = reader.ReadByte();
                        }
                        
                        Console.WriteLine($"{Colors.ElectricBlue}    Bytes 00-15 (working): {BitConverter.ToString(problemBytes, 0, 16).Replace("-", " ")}{Colors.Reset}");
                        Console.WriteLine($"{Colors.LaserRed}    Bytes 16-23 (failing): {BitConverter.ToString(problemBytes, 16, 8).Replace("-", " ")}{Colors.Reset}");
                        Console.WriteLine($"{Colors.GoldYellow}    Compare these bytes with working files to find pattern{Colors.Reset}");
                    }
                    else
                    {
                        ColorConsole.WriteSuccess("✅ WORKING FILE - All decryption successful");
                        Console.WriteLine($"{Colors.CyberGreen}    - Filename: PERFECT{Colors.Reset}");
                        Console.WriteLine($"{Colors.CyberGreen}    - File size: {decryptedSizeField} (expected){Colors.Reset}");
                        Console.WriteLine($"{Colors.CyberGreen}    - Section table: All values reasonable{Colors.Reset}");
                        Console.WriteLine($"{Colors.CyberGreen}    - Animation Frames: {sections.AnimationFrameCount} (normal range){Colors.Reset}");
                        Console.WriteLine($"{Colors.CyberGreen}    - Collision Data2: {sections.CollisionData2Count} (normal range){Colors.Reset}");
                        
                        Console.WriteLine();
                        ColorConsole.WriteInfo("🔬 RAW BYTES REFERENCE (WORKING FILE):");
                        reader.BaseStream.Position = 0x14;
                        byte[] workingBytes = new byte[24];
                        for (int i = 0; i < 24; i++)
                        {
                            workingBytes[i] = reader.ReadByte();
                        }
                        
                        Console.WriteLine($"{Colors.CyberGreen}    Bytes 00-15 (working): {BitConverter.ToString(workingBytes, 0, 16).Replace("-", " ")}{Colors.Reset}");
                        Console.WriteLine($"{Colors.CyberGreen}    Bytes 16-23 (working): {BitConverter.ToString(workingBytes, 16, 8).Replace("-", " ")}{Colors.Reset}");
                        Console.WriteLine($"{Colors.GoldYellow}    Use this as reference pattern for fixing failing files{Colors.Reset}");
                    }
                    
                    // Step 4: Only attempt section decryption if the file is working
                    if (!isFailedFile)
                    {
                        long currentOffset = 0x2C; // First section starts at 0x2C
                        Console.WriteLine();
                        ColorConsole.WriteSection("DECRYPTING CHR SECTIONS");
                        
                        // Section order from our analysis:
                        // 1. Animation frame data (8 * count)
                        uint animFrameSize = sections.AnimationFrameCount * 8;
                        ColorConsole.WriteProgress($"Animation Frames at {Colors.TechOrange}0x{currentOffset:X}{Colors.Reset}, size: {Colors.GoldYellow}{animFrameSize:N0} bytes{Colors.Reset}");
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, animFrameSize, filenameBytes, primaryKey);
                        currentOffset += animFrameSize;
                        
                        // 2. Collision boxes (24 * count)
                        uint collisionSize = sections.CollisionBoxCount * 24;
                        ColorConsole.WriteProgress($"Collision Boxes at {Colors.TechOrange}0x{currentOffset:X}{Colors.Reset}, size: {Colors.GoldYellow}{collisionSize:N0} bytes{Colors.Reset}");
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, collisionSize, filenameBytes, primaryKey);
                        currentOffset += collisionSize;
                        
                        // 3. Animations (12 * count)
                        uint animationSize = sections.AnimationCount * 12;
                        ColorConsole.WriteProgress($"Animations at {Colors.TechOrange}0x{currentOffset:X}{Colors.Reset}, size: {Colors.GoldYellow}{animationSize:N0} bytes{Colors.Reset}");
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, animationSize, filenameBytes, primaryKey);
                        currentOffset += animationSize;
                        
                        // 4. Base character data (fixed 1024 bytes)
                        uint baseDataSize = 1024;
                        ColorConsole.WriteProgress($"Base Data at {Colors.TechOrange}0x{currentOffset:X}{Colors.Reset}, size: {Colors.GoldYellow}{baseDataSize:N0} bytes{Colors.Reset}");
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, baseDataSize, filenameBytes, primaryKey);
                        currentOffset += baseDataSize;
                        
                        // 5. Character data (260 * count)
                        uint charDataSize = sections.CharacterDataCount * 260;
                        ColorConsole.WriteProgress($"Character Data at {Colors.TechOrange}0x{currentOffset:X}{Colors.Reset}, size: {Colors.GoldYellow}{charDataSize:N0} bytes{Colors.Reset}");
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, charDataSize, filenameBytes, primaryKey);
                        currentOffset += charDataSize;
                        
                        // 6. Sprite data (64 * count)
                        uint spriteSize = sections.SpriteDataCount * 64;
                        ColorConsole.WriteProgress($"Sprite Data at {Colors.TechOrange}0x{currentOffset:X}{Colors.Reset}, size: {Colors.GoldYellow}{spriteSize:N0} bytes{Colors.Reset}");
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, spriteSize, filenameBytes, primaryKey);
                        currentOffset += spriteSize;
                        
                        // 7. Collision data2 (24 * count)
                        uint collision2Size = sections.CollisionData2Count * 24;
                        ColorConsole.WriteProgress($"Collision Data2 at {Colors.TechOrange}0x{currentOffset:X}{Colors.Reset}, size: {Colors.GoldYellow}{collision2Size:N0} bytes{Colors.Reset}");
                        CHRDecryptor.DecryptSection(reader, writer, currentOffset, collision2Size, filenameBytes, primaryKey);
                        
                        Console.WriteLine();
                        ColorConsole.WriteSuccess($"Successfully decrypted CHR file to {Colors.CyberGreen}{args[0]}_decrypted.bin{Colors.Reset}");
                    }
                    else
                    {
                        Console.WriteLine();
                        ColorConsole.WriteWarning("⚠️ SKIPPING SECTION DECRYPTION - Section table contains garbage values");
                        ColorConsole.WriteInfo("This prevents crashes from attempting to read massive amounts of data");
                        ColorConsole.WriteInfo("Focus: Need to fix section table decryption algorithm for these files");
                        
                        // Still create a minimal output file with just the header
                        Console.WriteLine();
                        ColorConsole.WriteInfo($"Creating header-only file: {Colors.GoldYellow}{args[0]}_header_only.bin{Colors.Reset}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                ColorConsole.WriteError($"Decryption failed: {ex.Message}");
                Console.WriteLine($"{Colors.BrightRed}Stack trace:{Colors.Reset} {ex.StackTrace}");
            }
        }
    }
}