using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using static ModLib.CompatibilityManager;

namespace ModLib;

/// <summary>
/// A custom logger which sends messages to both the game's and this mod's log files.
/// </summary>
/// <remarks>The generated logs for this mod can be found at <c>StreamingAssets\Logs\MOD_NAME.log</c></remarks>
public static class Logger
{
    private static string LogPrefix { get; } = ModPlugin.Assembly.GetModName();

    private static string LogPath
    {
        get
        {
            if (string.IsNullOrEmpty(field))
            {
                char[] bannedChars = [.. Path.GetInvalidFileNameChars(), ' '];
                StringBuilder stringBuilder = new();

                foreach (char c in $"{LogPrefix}.log")
                {
                    if (!bannedChars.Contains(c))
                    {
                        stringBuilder.Append(c);
                    }
                }

                string logsFolder = Path.Combine(Path.GetFullPath(Application.streamingAssetsPath), "Logs");

                if (!Directory.Exists(logsFolder))
                {
                    Directory.CreateDirectory(logsFolder);
                }

                field = Path.Combine(logsFolder, stringBuilder.ToString());
            }

            return field;
        }
    } = "";

    /// <summary>
    /// Clears the mod's log file.
    /// </summary>
    /// <remarks>This should be called before any other <c>Log</c> function to avoid loss of data.</remarks>
    public static void CleanLogFile() => File.WriteAllText(LogPath, "");

    /// <summary>
    /// Logs a message to this mod logger, optionally also sending the same message to Unity's <c>Debug</c> logger.
    /// </summary>
    /// <param name="logLevel">The "importance level" of this log.</param>
    /// <param name="message">The message to be written.</param>
    /// <param name="useUnityLogger">If a copy of this message should be sent to the game's own logger.</param>
    /// <remarks>Note: This function has several specialized variants for ease of use, see below.</remarks>
    public static void Log(LogLevel logLevel, object message, bool useUnityLogger = true, bool traceLogs = false)
    {
        WriteToLogFile(FormatMessage(traceLogs ? AppendStackTrace(message) : message, logLevel));

        if (useUnityLogger)
            Debug.Log(FormatMessage(message, logLevel, addNewLine: false, addDateTime: false));
    }

    /// <summary>
    /// Logs a <c>Debug</c>-level message to the game and this mod's loggers.
    /// </summary>
    /// <param name="message">The message to be written.</param>
    /// <seealso cref="Log"/>
    public static void LogDebug(object message) => Log(LogLevel.Debug, message);

    /// <summary>
    /// Logs an <c>Info</c>-level message to the game and this mod's loggers.
    /// </summary>
    /// <param name="message">The message to be written.</param>
    /// <seealso cref="Log"/>
    public static void LogInfo(object message) => Log(LogLevel.Info, message);

    /// <summary>
    /// Logs a <c>Message</c>-level message to the game and this mod's loggers.
    /// </summary>
    /// <param name="message">The message to be written.</param>
    /// <seealso cref="Log"/>
    public static void LogMessage(object message) => Log(LogLevel.Message, message);

    /// <summary>
    /// Logs a <c>Warning</c>-level message to the game and this mod's loggers.
    /// </summary>
    /// <param name="message">The message to be written.</param>
    /// <seealso cref="Log"/>
    public static void LogWarning(object message)
    {
        Log(LogLevel.Warning, message, useUnityLogger: false);

        Debug.LogWarning(FormatMessage(message, LogLevel.Warning, addNewLine: false, addDateTime: false));
    }

    /// <summary>
    /// Logs an <c>Error</c>-level message to the game and this mod's loggers. Unlike other functions, this is not sent to the game's logs.
    /// </summary>
    /// <param name="message">The message to be written.</param>
    /// <seealso cref="Log"/>
    /// <remarks>
    ///     Note: This uses a custom format for including exception and stack trace, and should be preferred when handling errors.
    ///     If no exception is provided, the stack trace of the method's invocation is used instead.
    /// </remarks>
    public static void LogError(object message, Exception? exception)
    {
        if (exception is not null)
            Log(LogLevel.Error, FormatErrorMessage(message, exception), useUnityLogger: false);
        else
            Log(LogLevel.Error, message, useUnityLogger: false, traceLogs: true);

        Debug.LogError(FormatMessage($"{message} (See details at log file)", LogLevel.Error, addNewLine: false, addDateTime: false));
    }

    /// <summary>
    /// Appends the stack trace of the logging method to the given message.
    /// </summary>
    /// <param name="message">The original message object.</param>
    /// <returns>A <c>String</c> detailing the stack trace of the called <c>Log</c> method.</returns>
    private static string AppendStackTrace(object message) =>
        $"{message}{Environment.NewLine}-- Stack trace:{Environment.NewLine}{Environment.StackTrace}";

    /// <summary>
    /// Formats and returns the prefix to be used in logs.
    /// </summary>
    /// <returns>A new <c>String</c> object with the formatted prefix for usage.</returns>
    /// <remarks>If a mod with special compatibility support is detected, a suffix is also added to the acronym itself.</remarks>
    private static string BuildLogPrefix() => LogPrefix + (IsRainMeadowEnabled() ? "+M" : "");

    /// <summary>
    /// Obtains and formats the current time when the log was created.
    /// </summary>
    /// <returns>The formatted time of when the function was called.</returns>
    private static string GetDateTime() => DateTimeOffset.Now.ToString(DateTimeFormatInfo.CurrentInfo.UniversalSortableDateTimePattern);

    /// <summary>
    /// Formats the given message to be written in logs.
    /// </summary>
    /// <param name="message">The message to be formatted.</param>
    /// <param name="logLevel">The log level of this message.</param>
    /// <param name="addNewLine">Whether to add a newline character at the end of the formatted string.</param>
    /// <returns>A new formatted <c>String</c> object ready to be logged.</returns>
    private static string FormatMessage(object message, LogLevel logLevel, bool addNewLine = true, bool addDateTime = true) =>
        $"{(addDateTime ? GetDateTime() : "")} [{BuildLogPrefix()}: {logLevel}] {message}".Trim() + (addNewLine ? Environment.NewLine : "");

    private static string FormatErrorMessage(object message, Exception exception)
    {
        StringBuilder stringBuilder = new($"{message}{Environment.NewLine}-- Exception:{Environment.NewLine}({exception.HResult}) {exception.GetType()}: {exception.Message}{Environment.NewLine}-- Stack trace:{Environment.NewLine}{exception.StackTrace}");

        Exception? inner = exception;
        while (inner is not null)
        {
            inner = exception.InnerException;

            if (inner is null) break;

            stringBuilder.Append($"{Environment.NewLine}-- Inner exception:{Environment.NewLine}({inner.HResult}) {inner.GetType()}: {inner.Message}{Environment.NewLine}-- Stack trace:{Environment.NewLine}{inner.StackTrace}");
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Writes the given message to the mod's log file.
    /// </summary>
    /// <param name="contents">The formatted message to be written.</param>
    /// <seealso cref="FormatMessage"/>
    private static void WriteToLogFile(string contents) => File.AppendAllText(LogPath, contents);
}