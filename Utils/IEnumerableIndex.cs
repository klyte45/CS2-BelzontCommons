﻿using System;

namespace Belzont.Interfaces
{
    public interface IEnumerableIndex<T> where T : Enum
    {
        T Index { get; set; }
    }
}