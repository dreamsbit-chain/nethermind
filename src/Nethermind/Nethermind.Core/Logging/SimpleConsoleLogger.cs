﻿/*
 * Copyright (c) 2018 Demerzel Solutions Limited
 * This file is part of the Nethermind library.
 *
 * The Nethermind library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The Nethermind library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;

namespace Nethermind.Core.Logging
{
    public class SimpleConsoleLogger : ILogger
    {
        public void Log(string text)
        {
            Console.WriteLine($"{DateTime.Now.ToLongTimeString()} [{Thread.CurrentThread.ManagedThreadId}] {text}");
        }

        public void Info(string text)
        {
            Log(text);
        }

        public void Warn(string text)
        {
            Log(text);
        }

        public void Debug(string text)
        {
            Log(text);
        }

        public void Trace(string text)
        {
            Log(text);
        }

        public void Error(string text, Exception ex = null)
        {
            Log(ex != null ? $"{text}, Exception: {ex}" : text);
        }
        
        public bool IsInfoEnabled => true;
        public bool IsWarnEnabled => true;
        public bool IsDebugEnabled => true;
        public bool IsTraceEnabled => true;
        public bool IsErrorEnabled => true;
    }
}