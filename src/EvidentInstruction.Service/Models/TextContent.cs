﻿using EvidentInstruction.Service.Models.Interfaces;

namespace EvidentInstruction.Service.Models
{
    public class TextContent : IContent
    {
        public string Get(object content)
        {
            return ContentTypes.TEXT;
        }
    }
}
