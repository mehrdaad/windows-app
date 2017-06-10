using System;

namespace wallabag.Models
{
    public class StartPageNavigationParameter
    {
        public Version PreviousVersion { get; set; }
        public bool DatabaseExists { get; set; }
    }
}
