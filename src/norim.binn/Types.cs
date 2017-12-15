namespace norim.binn
{
    public static class Types
    {
        //Standard BINN types

        public const byte List = 0xe0;

        public const byte Object = 0xe2;

        public const byte Blob = 0xc0;

        public const byte Null = 0x00;

        public const byte True = 0x01;

        public const byte False = 0x02;

        public const byte String = 0xa0;

        public const byte UInt8 = 0x20;

        public const byte Int8 = 0x21;

        public const byte UInt16 = 0x40;

        public const byte Int16 = 0x41;

        public const byte UInt32 = 0x60;

        public const byte Int32 = 0x61;

        public const byte UInt64 = 0x80;

        public const byte Int64 = 0x81;

        public const byte Float64 = 0x82;

        //Additional types

        public const byte Guid = 0xc1;

        public const byte DateTime = 0xc2;
    }    
}