using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using TheLeftExit.TeslaX;

namespace Growtopia_Console_Logger
{
    class Program
    {
        public const long consoleOffset = 0xA7F458;

        static void Main(string[] args)
        {
            var ps = Process.GetProcessesByName("Growtopia");
            Process process = ps.Single();
            ProcessHandle handle = process.GetHandle();

            bool rawText = false;

            Console.Write("How do you want to display console text? (raw/filtered - default): ");

            string input = Console.ReadLine();

            if (input == "raw")
            {
                rawText = true;
            }

            long lastReceivedOffset = 0x0;

            string lastRecievedMessage = "";

            if (!Directory.Exists(@"logs"))
                Directory.CreateDirectory(@"logs");

            string fileName = @"logs\log_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".txt";

            File.AppendAllText(fileName, $"Logging started on {DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")}..." + Environment.NewLine);

            while (true)
            {
                handle.MemoryHandle.ReadOffsets(out long address, handle.BaseAddress + consoleOffset, lastReceivedOffset);

                handle.MemoryHandle.ReadString(address, 300, out string text);

                //fixes program crashing on gt reconnect
                if (string.IsNullOrEmpty(text) || text[0] != '`')
                {
                    lastReceivedOffset = 0x0;

                    Thread.Sleep(1000);

                    continue;
                }

                if (text != lastRecievedMessage)
                {
                    if (rawText)
                        Console.WriteLine(text);
                    else
                        Console.WriteLine(FilterText(text));

                    File.AppendAllText(fileName, text + Environment.NewLine);

                    lastRecievedMessage = text;
                }

                handle.MemoryHandle.ReadOffsets(out long address2, handle.BaseAddress + consoleOffset, lastReceivedOffset + 0x20);

                handle.MemoryHandle.ReadString(address2, 300, out string text2);

                if (!string.IsNullOrEmpty(text2))
                {
                    //checks if console line starts with `
                    //makes sure to not log some random shit that sometimes appear to temporarily be at the next memory address, such as gt resource/filenames
                    if (text2[0] == '`')
                    {
                        lastReceivedOffset += 0x20;
                    }
                }

                Thread.Sleep(1);
            }
        }

        //removes all ` and color codes from the string
        private static string FilterText(string text)
        {
            return System.Text.RegularExpressions.Regex.Replace(text, "`.", "");
        }
    }
}
