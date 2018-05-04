using System.Collections.Generic;

namespace SimpleCompress
{
    internal class PathList
    {
        public List<string> Paths;
        public byte[] HashData;

        public PathList(byte[] hash, string fullName)
        {
            Paths = new List<string>();
            Paths.Add(fullName);
            HashData = hash;
        }

        public void Add(string fullName)
        {
            Paths.Add(fullName);
        }
    }
}