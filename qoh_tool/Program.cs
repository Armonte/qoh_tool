using System.Drawing;
using System.IO;
using System.Xml;
using System.Drawing;
using System.ComponentModel;
using System.Text;



namespace qoh_tool
{
    

    public class BitmapFile
    {
        public const string BMPMagic = "BM";

        public uint f_size = 0; //unread by many programs
        public ushort misc1 = 0; //unread by many programs
        public ushort misc2 = 0; //unread by many programs

        public uint data_offset = 0; //offset to pixel data

        //BITMAPINFOHEADER
        //public uint bitmapinfo_size = 0x20;
        public int width = 0;
        public int height = 0;

        public ushort color_planes = 1; //why is this always 1 what even is this

        public ushort bpp = 32;
        public uint compression_method = 0; //fuck compression

        public uint bitmap_data_size = 0; //calculate this at export

        public int res_x = 32; //what the hell is this shit idk why pixels per meter matters to anyone
        public int res_y = 32; //what the hell is this shit idk why pixels per meter matters to anyone

        public uint color_palette_c_count = 0;
        public uint color_palette_imp_c_count = 0;

        public byte[] color_bytes;

    }

    internal class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello, World!");

            byte sub_val = 109;

            string name_string = "AOI";

            //byte[] name_bytes = new byte[] { 0x52, 0x49, 0x4E, 0x41, 0x5F, 0x43, 0x52, 0x41 };
            byte[] name_bytes = Encoding.UTF8.GetBytes(name_string);
            Console.WriteLine("nslen from encoding "+name_bytes.Length);

            byte[] magic_key = new byte[] { 0x92, 0x4F, 0x89, 0xBA, 0x8D, 0xF7, 0x20, 0x90, 0xBC, 0x91, 0xBA, 0x82, 0xBF, 0x82, 0xC8, 0x82, 0xDD };

            int name_loop = 0;
            int key_loop = 0;

            int read_bytes = 0;

            byte[] in_file_decrypted = File.ReadAllBytes(args[0]);
            using (BinaryReader read = new BinaryReader(new MemoryStream(in_file_decrypted)))
            {
                //read.BaseStream.Position = 0x14; //start of another encrypted data section 0x18 long
                //read.BaseStream.Position = 0x2C; //start of another encrypted data section

                using (BinaryWriter write = new BinaryWriter(File.Create("test.bin")))
                {
                    while(read.BaseStream.Position < read.BaseStream.Length)
                    {
                        
                        //hardcoded offsets
                        switch(read.BaseStream.Position)
                        {
                            case 0:

                                for (int i = 0; i < 0x10; i++)
                                {
                                    if (key_loop >= magic_key.Length) key_loop = 0;

                                    byte out_n_byte = (byte)(read.ReadByte() ^ (byte)(magic_key[key_loop]));
                                    write.Write(out_n_byte);

                                    //name_bytes[i] = out_n_byte;

                                    key_loop++;
                                }

                                key_loop = 0;
                                
                                break;
                            case 0x4:
                                key_loop = 0;
                                name_loop = 0;
                                read_bytes = 0;
                                break;
                            case 0x10:
                                key_loop = 0;
                                name_loop = 0;
                                read_bytes = 0;
                                break;
                            case 0x14:
                                key_loop = 0;
                                name_loop = 0;
                                read_bytes = 0;
                                break;
                            case 0x2c:
                                key_loop = 0;
                                name_loop = 0;
                                read_bytes = 0;
                                break;

                        }


                        if (key_loop >= magic_key.Length) key_loop = 0;
                        if (name_loop >= name_bytes.Length) name_loop = 0;

                        byte name_key_byte = name_bytes[name_loop];
                        byte magic_key_byte = (byte)(magic_key[key_loop] - 0);

                        //Console.WriteLine("nl " + name_loop + " kl " + key_loop+" nlb "+name_key_byte+" mkb "+magic_key_byte);

                        byte original_byte = read.ReadByte();

                        //some kind of bitmasking
                        byte pos_magic = (byte)(read_bytes << 3);
                        int pos_magic_int = read_bytes >> 3;

                        //Console.WriteLine("pm "+pos_magic+" pmi "+pos_magic_int);

                        pos_magic = (byte)~pos_magic; //bitwise negate

                        //Console.WriteLine("pmneg " + pos_magic + " pmi " + pos_magic_int);

                        pos_magic_int += pos_magic;

                        //Console.WriteLine("pm " + pos_magic + " pmi " + pos_magic_int);

                        byte xor_byte_1 = (byte)(pos_magic_int ^ name_key_byte);

                        //Console.WriteLine("xb "+xor_byte_1);


                        byte out_byte = (byte)(original_byte ^ xor_byte_1);

                        //Console.WriteLine("ob " + out_byte+" mkey byte "+magic_key_byte+" sum "+(out_byte-magic_key_byte));

                        out_byte -= magic_key_byte;
                        out_byte -= 0x6D;

                        //write.Write(out_byte);

                        //Console.WriteLine("ob2 " + out_byte);

                        write.Write(out_byte);

                        //if(read_bytes > 1 ) System.Environment.Exit(0);


                        key_loop++;
                        name_loop++;

                        read_bytes++;
                    }

                }
            }


            /*
            using (BinaryReader fstr = new BinaryReader(File.OpenRead(args[0])))
            {
                BitmapFile out_bmp_header = new BitmapFile();

                uint unk = fstr.ReadUInt32();
                out_bmp_header.color_palette_c_count = fstr.ReadUInt32();
                out_bmp_header.bpp = (ushort)fstr.ReadUInt32();
                out_bmp_header.width = fstr.ReadInt32();
                out_bmp_header.height = fstr.ReadInt32();

                if(out_bmp_header.color_palette_c_count > 0)
                {
                    out_bmp_header.color_bytes = fstr.ReadBytes((int)out_bmp_header.color_palette_c_count*4); //rgba
                }

                Console.WriteLine("cbytes "+out_bmp_header.color_bytes.Length+" div "+ out_bmp_header.color_bytes.Length/4);

                out_bmp_header.bitmap_data_size = (uint)((out_bmp_header.width * out_bmp_header.height) * out_bmp_header.bpp);

                byte[] data_buffer = fstr.ReadBytes((int)out_bmp_header.bitmap_data_size);

                out_bmp_header.f_size = (uint)fstr.BaseStream.Position;


                using (BinaryWriter wstr = new BinaryWriter(new MemoryStream()))
                {
                    wstr.Write((short)19778);

                    wstr.Write(0);
                    wstr.Write(out_bmp_header.misc1);
                    wstr.Write(out_bmp_header.misc2);

                    wstr.Write(0);

                    wstr.Write(0x28);
                    wstr.Write(out_bmp_header.width);
                    wstr.Write(out_bmp_header.height);

                    
                    wstr.Write(out_bmp_header.color_planes);
                    wstr.Write(out_bmp_header.bpp);
                    wstr.Write(out_bmp_header.compression_method);
                    wstr.Write(out_bmp_header.bitmap_data_size);
                    wstr.Write(out_bmp_header.res_x);
                    wstr.Write(out_bmp_header.res_y);

                    wstr.Write(out_bmp_header.color_bytes.Length / 4);
                    wstr.Write(out_bmp_header.color_palette_imp_c_count);

                    if(out_bmp_header.color_palette_c_count > 0 ) wstr.Write(out_bmp_header.color_bytes);

                    //schizo
                    out_bmp_header.data_offset = (uint)wstr.BaseStream.Position;
                    wstr.BaseStream.Position = 0xA;
                    wstr.Write(out_bmp_header.data_offset);

                    wstr.BaseStream.Position = out_bmp_header.data_offset;

                    wstr.Write(data_buffer);

                    string out_test = args[0] + "_conv.png";

                    Image.FromStream(wstr.BaseStream, true).Save(out_test,System.Drawing.Imaging.ImageFormat.Png);

                }
            }
            */
        }


    }
}
