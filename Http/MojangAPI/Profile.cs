namespace MCSkin3D.Http.MojangAPI
{
    public class Profile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        
        public Properties[] Properties { get; set; }
    }
    
    public class Properties
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}