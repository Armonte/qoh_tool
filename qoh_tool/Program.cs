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
                
                // Bit manipulation: ~(8 * i) + (i >> 3)
                byte posMagic = (byte)(~(i << 3) + (i >> 3));
                
                // Complex XOR chain
                byte decryptedByte = (byte)((encryptedByte ^ filename[filenameIndex] ^ posMagic) 
                                          - primaryKey[keyIndex] - 109);
                
                fileSize |= (uint)(decryptedByte << (i * 8));
                
                // Rotate keys
                filenameIndex = (filenameIndex + 1) % filename.Length;
                keyIndex = (keyIndex + 1) % primaryKey.Length;
            }
            
            return fileSize;
        }

        // Complex algorithm for section table
        public static CHRSectionInfo DecryptSectionTable(BinaryReader reader, byte[] filename, byte[] primaryKey)
        {
            reader.BaseStream.Position = 0x14; // Section table at 0x14-0x2B (24 bytes)
            
            CHRSectionInfo sections = new CHRSectionInfo();
            uint[] sectionData = new uint[6];
            
            int filenameIndex = 0;
            int keyIndex = 0;
            
            // Decrypt 24 bytes (6 uint32s) using complex algorithm
            for (int i = 0; i < 24; i++)
            {
                byte encryptedByte = reader.ReadByte();
                
                // Bit manipulation: ~(8 * i) + (i >> 3)
                byte posMagic = (byte)(~(i << 3) + (i >> 3));
                
                // Complex XOR chain
                byte decryptedByte = (byte)((encryptedByte ^ filename[filenameIndex] ^ posMagic) 
                                          - primaryKey[keyIndex] - 109);
                
                // Store in appropriate uint32
                int sectionIndex = i / 4;
                int byteOffset = i % 4;
                sectionData[sectionIndex] |= (uint)(decryptedByte << (byteOffset * 8));
                
                // Rotate keys
                filenameIndex = (filenameIndex + 1) % filename.Length;
                keyIndex = (keyIndex + 1) % primaryKey.Length;
            }
            
            // Map to structure
            sections.AnimationCount = sectionData[0];
            sections.CharacterDataCount = sectionData[1]; 
            sections.CollisionBoxCount = sectionData[2];
            sections.SpriteDataCount = sectionData[3];
            sections.AnimationFrameCount = sectionData[4];
            sections.CollisionData2Count = sectionData[5];
            
            return sections;
        }

        // Complex algorithm for section data
        public static void DecryptSection(BinaryReader reader, BinaryWriter writer, 
                                        long startOffset, uint sectionSize, 
                                        byte[] filename, byte[] primaryKey)
        {
            reader.BaseStream.Position = startOffset;
            
            int filenameIndex = 0;
            int keyIndex = 0;
            
            for (uint i = 0; i < sectionSize; i++)
            {
                byte encryptedByte = reader.ReadByte();
                
                // Bit manipulation: ~(8 * i) + (i >> 3)
                byte posMagic = (byte)(~(i << 3) + (i >> 3));
                
                // Complex XOR chain
                byte decryptedByte = (byte)((encryptedByte ^ filename[filenameIndex] ^ posMagic) 
                                          - primaryKey[keyIndex] - 109);
                
                writer.Write(decryptedByte);
                
                // Rotate keys
                filenameIndex = (filenameIndex + 1) % filename.Length;
                keyIndex = (keyIndex + 1) % primaryKey.Length;
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            ColorConsole.WriteHeader("");
            Console.WriteLine();
            
            if (args.Length == 0)
            {
                ColorConsole.WriteError("No input file specified!");
                Console.WriteLine($"{Colors.BrightYellow}Usage: {Colors.BrightWhite}qoh_tool.exe <chr_file>{Colors.Reset}");
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
                    // Step 1: Decrypt header using u4ick's simple XOR method
                    reader.BaseStream.Position = 0;
                    int keyIndex = 0;
                    byte[] headerBytes = new byte[16];
                    
                    for (int i = 0; i < 0x10; i++)
                    {
                        if (keyIndex >= primaryKey.Length) keyIndex = 0;
                        
                        byte encryptedByte = reader.ReadByte();
                        byte decryptedByte = (byte)(encryptedByte ^ primaryKey[keyIndex]);
                        writer.Write(decryptedByte);
                        headerBytes[i] = decryptedByte;
                        
                        keyIndex++;
                    }
                    
                    // Extract filename from decrypted header
                    string filename = Encoding.UTF8.GetString(headerBytes).TrimEnd('\0');
                    ColorConsole.WriteSuccess($"Decrypted filename: {Colors.NeonPurple}{filename}{Colors.Reset}");
                    
                    byte[] filenameBytes = Encoding.UTF8.GetBytes(filename);
                    
                    // Step 2: Get actual file size and decrypt the size field
                    long actualFileSize = reader.BaseStream.Length;
                    uint decryptedSizeField = CHRDecryptor.DecryptFileSize(reader, filenameBytes, primaryKey);
                    ColorConsole.WriteSuccess($"Actual file size: {Colors.GoldYellow}{actualFileSize:N0} bytes{Colors.Reset}");
                    ColorConsole.WriteInfo($"Decrypted size field: {Colors.GoldYellow}{decryptedSizeField} {Colors.Reset}(possibly filename length or header info)");
                    Console.WriteLine();
                    
                    // Step 3: Decrypt section table using our complex algorithm  
                    CHRDecryptor.CHRSectionInfo sections = CHRDecryptor.DecryptSectionTable(reader, filenameBytes, primaryKey);
                    
                    ColorConsole.WriteSection("CHR SECTION ANALYSIS");
                    ColorConsole.WriteData("Animations", sections.AnimationCount);
                    ColorConsole.WriteData("Character Data", sections.CharacterDataCount);
                    ColorConsole.WriteData("Collision Boxes", sections.CollisionBoxCount);
                    ColorConsole.WriteData("Sprites", sections.SpriteDataCount);
                    ColorConsole.WriteData("Animation Frames", sections.AnimationFrameCount);
                    ColorConsole.WriteData("Collision Data2", sections.CollisionData2Count);
                    
                    // Step 4: Decrypt each section using our complex algorithm with proper boundaries
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