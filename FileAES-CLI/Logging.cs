using FAES;
using System;

namespace FileAES_CLI
{
    internal class Logging
    {
        public static void Log(string log, Severity severity = Severity.INFO)
        {
            if (FileAES_Utilities.GetVerboseLogging())
            {
                switch (severity)
                {
                    case Severity.DEBUG:
                        {
                            Console.WriteLine("[DEBUG] {0}", log);
                            break;
                        }
                    case Severity.WARN:
                        {
                            Console.WriteLine("[WARN] {0}", log);
                            break;
                        }
                    case Severity.ERROR:
                        {
                            Console.WriteLine("[ERROR] {0}", log);
                            break;
                        }
                    case Severity.INFO:
                    default:
                        {
                            Console.WriteLine("[INFO] {0}", log);
                            break;
                        }
                }
            }
            else if (severity > 0) Console.WriteLine(log);
        }
    }

    internal enum Severity
    {
        DEBUG,
        INFO,
        WARN,
        ERROR
    };
}
