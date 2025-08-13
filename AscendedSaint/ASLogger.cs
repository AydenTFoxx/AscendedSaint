using System;
using System.Globalization;
using System.IO;
using BepInEx.Logging;
using static AscendedSaint.AscendedSaintMain.Utils;

namespace AscendedSaint
{
    /// <summary>
    /// A custom logger which sends messages to both the game's and this mod's log files.
    /// </summary>
    /// <remarks>The generated logs for this mod can be found at <c>"%HOMEPATH%\AppData\LocalLow\VideoCult\Rain World\AscendedSaint.log.txt"</c>.</remarks>
    internal static class ASLogger
    {
        private static string _logPath;
        private static string LogPath
        {
            get
            {
                if (string.IsNullOrEmpty(_logPath))
                {
                    _logPath = Path.Combine(Path.GetFullPath(Kittehface.Framework20.UserData.GetPersistentDataPath()), "AscendedSaint.log.txt");
                }

                return _logPath;
            }
        }

        /// <summary>
        /// Logs a message to this mod logger, optionally also sending the same message to Unity's <c>Debug</c> logger.
        /// </summary>
        /// <param name="logLevel">The "importance level" of this log.</param>
        /// <param name="message">The message to be written.</param>
        /// <param name="useUnityLogger">If a copy of this message should be sent to the game's own logger.</param>
        /// <remarks>Note: This function has several specialized variants for ease of use, see below.</remarks>
        public static void Log(LogLevel logLevel, string message, bool useUnityLogger = false)
        {
            WriteToLogFile(FormatMessage(message, logLevel));

            if (useUnityLogger) UnityEngine.Debug.Log(FormatMessage(message, logLevel, addNewLine: false, addDateTime: false));
        }

        /// <summary>
        /// Logs a <c>Debug</c>-level message to the game and this mod's loggers.
        /// </summary>
        /// <param name="message">The message to be written.</param>
        /// <seealso cref="Log"/>
        public static void LogDebug(string message)
        {
            Log(LogLevel.Debug, message, useUnityLogger: true);
        }

        /// <summary>
        /// Logs an <c>Info</c>-level message to the game and this mod's loggers.
        /// </summary>
        /// <param name="message">The message to be written.</param>
        /// <seealso cref="Log"/>
        public static void LogInfo(string message)
        {
            Log(LogLevel.Info, message, useUnityLogger: true);
        }

        /// <summary>
        /// Logs a <c>Message</c>-level message to the game and this mod's loggers.
        /// </summary>
        /// <param name="message">The message to be written.</param>
        /// <seealso cref="Log"/>
        public static void LogMessage(string message)
        {
            Log(LogLevel.Message, message, useUnityLogger: true);
        }

        /// <summary>
        /// Logs a <c>Warning</c>-level message to the game and this mod's loggers.
        /// </summary>
        /// <param name="message">The message to be written.</param>
        /// <seealso cref="Log"/>
        public static void LogWarning(string message)
        {
            Log(LogLevel.Warning, message);

            UnityEngine.Debug.LogWarning(FormatMessage(message, LogLevel.Warning, addNewLine: false, addDateTime: false));
        }

        /// <summary>
        /// Logs an <c>Error</c>-level message to the game and this mod's loggers. Unlike other functions, this is not sent to the game's logs.
        /// </summary>
        /// <param name="message">The message to be written.</param>
        /// <seealso cref="Log"/>
        /// <remarks>Note: This uses a custom format for including exception and stack trace, and should be preferred when handling errors.</remarks>
        public static void LogError(string message, Exception exception)
        {
            Log(LogLevel.Error, $"{message}{Environment.NewLine}-- Exception:{Environment.NewLine}{exception}{Environment.NewLine}-- Stack trace:{Environment.NewLine}{exception.StackTrace}");

            UnityEngine.Debug.LogError(FormatMessage($"{message} (See details at log file)", LogLevel.Error, addNewLine: false, addDateTime: false));
        }

        /// <summary>
        /// Formats and returns the prefix to be used in logs.
        /// </summary>
        /// <param name="acronym">The base acronym to be used.</param>
        /// <param name="isMeadowEnabled">Whether or not the Rain Meadow mod is enabled.</param>
        /// <returns>A new <c>String</c> object with the formatted prefix for usage.</returns>
        /// <remarks>If a mod with special compatibility support is detected, a suffix is also added to the acronym itself.</remarks>
        private static string BuildLogPrefix(string acronym, bool isMeadowEnabled)
        {
            // TODO: Add support for more generalized "isModEnabled" support.
            return $"{acronym + (isMeadowEnabled ? "+M" : "")}";
        }

        /// <summary>
        /// Obtains and formats the current time when the log was created.
        /// </summary>
        /// <returns>The formatted time of when the function was called.</returns>
        private static string GetDateTime()
        {
            return DateTimeOffset.Now.ToString(DateTimeFormatInfo.CurrentInfo.UniversalSortableDateTimePattern);
        }

        /// <summary>
        /// Formats the given message to be written in logs.
        /// </summary>
        /// <param name="message">The message to be formatted.</param>
        /// <param name="logLevel">The log level of this message.</param>
        /// <param name="addNewLine">Whether to add a newline character at the end of the formatted string.</param>
        /// <returns>A new formatted <c>String</c> object ready to be logged.</returns>
        private static string FormatMessage(string message, LogLevel logLevel, bool addNewLine = true, bool addDateTime = true)
        {
            return $"{(addDateTime ? GetDateTime() : "")} [{BuildLogPrefix("AS", IsMeadowEnabled())}: {logLevel}] {message}".Trim() + (addNewLine ? Environment.NewLine : "");
        }

        /// <summary>
        /// Writes the given message to the mod's log file.
        /// </summary>
        /// <param name="contents">The formatted message to be written.</param>
        /// <seealso cref="FormatMessage"/>
        private static void WriteToLogFile(string contents)
        {
            File.AppendAllText(LogPath, contents);
        }

        /// <summary>
        /// Clears the mod's log file.
        /// </summary>
        /// <remarks>This should be called before any other <c>Log</c> function to avoid loss of data.</remarks>
        public static void CleanLogFile()
        {
            File.WriteAllText(LogPath, "");
        }
    }
}