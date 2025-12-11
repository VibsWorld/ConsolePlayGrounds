using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RabbitMqDirect;

public class CustomLogger
{
    private static object lockObject = new object();

    private string? _baseFolderForLogs = string.Empty;
    private string? _baseFileNameForLogs = string.Empty;
    private bool? _requireTimeStamp = false;

    public CustomLogger(
        string? folderName = null,
        string? fileName = null,
        bool? requireTimeStamp = false
    )
    {
        _requireTimeStamp = requireTimeStamp;
        if (string.IsNullOrWhiteSpace(folderName))
        {
            folderName = "Logs";
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            _baseFileNameForLogs =
                "CustomLoggerLogs-" + System.DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
        }

        _baseFolderForLogs = System.IO.Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory,
            folderName
        );
        if (!System.IO.Directory.Exists(_baseFolderForLogs))
            System.IO.Directory.CreateDirectory(_baseFolderForLogs);
        _baseFileNameForLogs = System.IO.Path.Combine(_baseFolderForLogs, _baseFileNameForLogs);
    }

    public void Error(Exception Ex, string? info = null)
    {
        LogException(Ex, info);
    }

    public void Error(string info)
    {
        Log(info);
    }

    public void Log(string message, object? obj = null, bool? requireTimeStamp = null)
    {
        requireTimeStamp = requireTimeStamp.HasValue ? requireTimeStamp.Value : _requireTimeStamp;

        if (requireTimeStamp.HasValue && requireTimeStamp.Value == true)
        {
            message =
                DateTime.Now.ToString(
                    "dd/MM/yyyy HH:MM:ss",
                    System.Globalization.CultureInfo.InvariantCulture
                )
                + ": "
                + message;
        }
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine(message);

        if (obj != null && obj.GetType().IsClass == true)
        {
            try
            {
                sb.AppendLine(JsonSerializer.Serialize(obj));
            }
            catch (Exception Ex)
            {
                sb.AppendLine(
                    "Failed to Deserialzed Object named "
                        + obj.GetType().FullName
                        + Environment.NewLine
                        + Ex.Message
                        + Environment.NewLine
                        + Ex.StackTrace
                );
            }
        }
        else if (obj != null && obj.GetType().GetType() == typeof(string))
        {
            sb.AppendLine(obj.ToString());
        }

        WriteFile(sb.ToString(), _baseFileNameForLogs!);
    }

    public void LogException(Exception exception, string? info = null)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder("***ERROR Starts***");
        if (!string.IsNullOrWhiteSpace(info))
        {
            sb.AppendLine(info);
        }
        do
        {
            sb.AppendLine("\t" + exception.Source + " - " + exception.Message);
            sb.AppendLine("\t" + exception.StackTrace);
            exception = exception.InnerException!;
        } while (exception is not null);
        sb.AppendLine("******Error Ends***********");
        Log(sb.ToString(), null, true);
    }

    public static void WriteFile(string message, string filePath, bool printWriteLine = true)
    {
        lock (lockObject)
        {
            using System.IO.StreamWriter sw = new System.IO.StreamWriter(filePath, true);
            if (printWriteLine)
            {
                sw.WriteLine(message);
            }
            else
            {
                sw.Write(message);
            }
        }
    }
}
