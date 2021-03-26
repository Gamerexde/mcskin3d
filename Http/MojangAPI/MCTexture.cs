using System;

namespace MCSkin3D.Http.MojangAPI
{
    public class MCTexture
    {
        public Int64 Timestamp { get; set; }
        public string ProfileId { get; set; }
        public string ProfileName { get; set; }
        
        public MCTextureData Textures { get; set; }
    }
    
    public class MCTextureSkin
    {
        public string Url { get; set; }
        public MCTextureMetaData Metadata { get; set; }
    }

    public class MCTextureData
    {
        public MCTextureSkin SKIN { get; set; }
    }
    
    public class MCTextureMetaData
    {
        public string Model { get; set; } 
    }
}