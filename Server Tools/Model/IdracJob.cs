﻿
namespace Server_Tools.Model    
{
    class IdracJob
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string JobState { get; set; }
        public string Message { get; set; }
        public int PercentComplete { get; set; }
    }
}
