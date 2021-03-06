﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.Message
{
    public interface IMessageConsumer
    {
        void Start();
        void Stop();
        string GetStatus();
        decimal MessageCount { get; }
    }
}
