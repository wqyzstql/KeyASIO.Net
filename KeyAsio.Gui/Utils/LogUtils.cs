﻿using System;
using KeyAsio.Gui.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Sentry;

namespace KeyAsio.Gui.Utils;

internal static class LogUtils
{
    public static readonly ILoggerFactory LoggerFactory =
        Microsoft.Extensions.Logging.LoggerFactory.Create(k => k
            .AddNLog()
            .SetMinimumLevel(LogLevel.Trace));

    public static ILogger GetLogger(string name)
    {
        return LoggerFactory.CreateLogger(name);
    }

    public static ILogger<T> GetLogger<T>()
    {
        return LoggerFactory.CreateLogger<T>();
    }

    public static void LogToSentry(LogLevel logLevel, string content, Exception? exception = null)
    {
        var settings = ConfigurationFactory.GetConfiguration<AppSettings>();
        if (!settings.SendAnonymousLogsToDeveloper) return;
        if (exception != null)
        {
            SentrySdk.CaptureException(exception);
        }
        else
        {
            var sentryLevel = logLevel switch
            {
                LogLevel.Trace => SentryLevel.Debug,
                LogLevel.Debug => SentryLevel.Debug,
                LogLevel.Information => SentryLevel.Info,
                LogLevel.Warning => SentryLevel.Warning,
                LogLevel.Error => SentryLevel.Error,
                LogLevel.Critical => SentryLevel.Fatal,
                LogLevel.None => SentryLevel.Info,
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
            };
            SentrySdk.CaptureMessage(content, sentryLevel);
        }
    }

    public static void DebuggingLog(this ILogger logger, LogLevel logLevel, string content, bool toSentry = false)
    {
        logger.Log(logLevel, "[DEBUGGING] " + content);
        if (toSentry) LogToSentry(logLevel, content);
    }

    public static void DebuggingDebug(this ILogger logger, string content, bool toSentry = false)
    {
        logger.LogDebug("[DEBUGGING] " + content);
        if (toSentry) LogToSentry(LogLevel.Debug, content);
    }

    public static void DebuggingInfo(this ILogger logger, string content, bool toSentry = false)
    {
        logger.LogInformation("[DEBUGGING] " + content);
        if (toSentry) LogToSentry(LogLevel.Information, content);
    }

    public static void DebuggingWarn(this ILogger logger, string content, bool toSentry = false)
    {
        logger.LogWarning("[DEBUGGING] " + content);
        if (toSentry) LogToSentry(LogLevel.Warning, content);
    }

    public static void DebuggingError(this ILogger logger, string content, bool toSentry = false)
    {
        logger.LogError("[DEBUGGING] " + content);
        if (toSentry) LogToSentry(LogLevel.Error, content);
    }

    public static void DebuggingDebug(this ILogger logger, Exception ex, string content, bool toSentry = false)
    {
        logger.LogDebug(ex, "[DEBUGGING] " + content);
        if (toSentry) LogToSentry(LogLevel.Debug, content, ex);
    }

    public static void DebuggingInfo(this ILogger logger, Exception ex, string content, bool toSentry = false)
    {
        logger.LogInformation(ex, "[DEBUGGING] " + content);
        if (toSentry) LogToSentry(LogLevel.Information, content, ex);
    }

    public static void DebuggingWarn(this ILogger logger, Exception ex, string content, bool toSentry = false)
    {
        logger.LogWarning(ex, "[DEBUGGING] " + content);
        if (toSentry) LogToSentry(LogLevel.Warning, content, ex);
    }

    public static void DebuggingError(this ILogger logger, Exception ex, string content, bool toSentry = false)
    {
        logger.LogError(ex, "[DEBUGGING] " + content);
        if (toSentry) LogToSentry(LogLevel.Error, content, ex);
    }
}