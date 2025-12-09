using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace Coupon.Helpers
{
    /// <summary>
    /// Advanced rate limiting system with memory-based tracking
    /// Prevents brute force attacks and abuse
    /// Includes automatic cleanup to prevent memory leaks
    /// </summary>
    public static class RateLimiter
    {
        private static readonly object _lock = new object();
        private static Dictionary<string, RateLimitEntry> _attempts = new Dictionary<string, RateLimitEntry>();

        // Configuration
        private const int MAX_ATTEMPTS = 10;
        private const int WINDOW_MINUTES = 60;
        private const int CLEANUP_INTERVAL_MINUTES = 5;
        private const int MAX_ENTRIES = 10000; // Maximum entries before forced cleanup
        private const int PERMANENT_BLOCK_HOURS = 24; // Convert "permanent" blocks to 24 hours
        private static DateTime _lastCleanup = DateTime.Now;
        private static Timer _cleanupTimer;
        private static bool _timerInitialized = false;

        /// <summary>
        /// Initialize background cleanup timer (call from Application_Start)
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (!_timerInitialized)
                {
                    // Start background cleanup timer
                    _cleanupTimer = new Timer(
                        BackgroundCleanup,
                        null,
                        TimeSpan.FromMinutes(CLEANUP_INTERVAL_MINUTES),
                        TimeSpan.FromMinutes(CLEANUP_INTERVAL_MINUTES)
                    );
                    _timerInitialized = true;
                }
            }
        }

        /// <summary>
        /// Background cleanup callback
        /// </summary>
        private static void BackgroundCleanup(object state)
        {
            try
            {
                lock (_lock)
                {
                    CleanupOldEntries();
                }
            }
            catch
            {
                // Silently fail - don't crash the app for cleanup
            }
        }

        /// <summary>
        /// Check if request is allowed based on rate limiting
        /// </summary>
        public static bool IsAllowed(string identifier, int maxAttempts = MAX_ATTEMPTS, int windowMinutes = WINDOW_MINUTES)
        {
            if (string.IsNullOrEmpty(identifier))
                return true;

            lock (_lock)
            {
                // Force cleanup if too many entries
                if (_attempts.Count > MAX_ENTRIES)
                {
                    ForceCleanup();
                }

                // Periodic cleanup
                if (DateTime.Now.Subtract(_lastCleanup).TotalMinutes >= CLEANUP_INTERVAL_MINUTES)
                {
                    CleanupOldEntries();
                }

                if (!_attempts.TryGetValue(identifier, out var entry))
                {
                    _attempts[identifier] = new RateLimitEntry();
                    return true;
                }

                var cutoffTime = DateTime.Now.AddMinutes(-windowMinutes);

                // Remove attempts outside the time window (limit to last 100 for memory)
                entry.Attempts = entry.Attempts
                    .Where(t => t > cutoffTime)
                    .OrderByDescending(t => t)
                    .Take(100)
                    .ToList();

                // Check if limit exceeded
                if (entry.Attempts.Count >= maxAttempts)
                {
                    try
                    {
                        ErrorLogger.LogToFileOnly($"Rate limit exceeded for: {identifier}",
                            null, $"Attempts: {entry.Attempts.Count}, Window: {windowMinutes} minutes");
                    }
                    catch { }
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Record an attempt
        /// </summary>
        public static void RecordAttempt(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return;

            lock (_lock)
            {
                if (!_attempts.TryGetValue(identifier, out var entry))
                {
                    entry = new RateLimitEntry();
                    _attempts[identifier] = entry;
                }

                entry.Attempts.Add(DateTime.Now);

                // Trim old attempts to prevent unbounded growth
                if (entry.Attempts.Count > 100)
                {
                    var cutoffTime = DateTime.Now.AddMinutes(-WINDOW_MINUTES);
                    entry.Attempts = entry.Attempts
                        .Where(t => t > cutoffTime)
                        .OrderByDescending(t => t)
                        .Take(50)
                        .ToList();
                }
            }
        }

        /// <summary>
        /// Get number of attempts in current window
        /// </summary>
        public static int GetAttemptCount(string identifier, int windowMinutes = WINDOW_MINUTES)
        {
            if (string.IsNullOrEmpty(identifier))
                return 0;

            lock (_lock)
            {
                if (!_attempts.TryGetValue(identifier, out var entry))
                {
                    return 0;
                }

                var cutoffTime = DateTime.Now.AddMinutes(-windowMinutes);
                return entry.Attempts.Count(t => t > cutoffTime);
            }
        }

        /// <summary>
        /// Reset attempts for an identifier (e.g., after successful login)
        /// </summary>
        public static void Reset(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return;

            lock (_lock)
            {
                _attempts.Remove(identifier);
            }
        }

        /// <summary>
        /// Block an identifier temporarily
        /// Note: "Permanent" blocks are converted to 24-hour blocks to prevent memory leaks
        /// </summary>
        public static void Block(string identifier, int durationMinutes = 0)
        {
            if (string.IsNullOrEmpty(identifier))
                return;

            lock (_lock)
            {
                if (!_attempts.TryGetValue(identifier, out var entry))
                {
                    entry = new RateLimitEntry();
                    _attempts[identifier] = entry;
                }

                // Convert "permanent" to 24 hours to prevent memory leaks
                if (durationMinutes <= 0)
                {
                    durationMinutes = PERMANENT_BLOCK_HOURS * 60;
                }

                entry.BlockedUntil = DateTime.Now.AddMinutes(durationMinutes);

                try
                {
                    ErrorLogger.LogToFileOnly($"Blocked identifier: {identifier}",
                        null, $"Duration: {durationMinutes} minutes");
                }
                catch { }
            }
        }

        /// <summary>
        /// Check if identifier is currently blocked
        /// </summary>
        public static bool IsBlocked(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;

            lock (_lock)
            {
                if (!_attempts.TryGetValue(identifier, out var entry))
                {
                    return false;
                }

                if (entry.BlockedUntil.HasValue)
                {
                    if (DateTime.Now < entry.BlockedUntil.Value)
                    {
                        return true;
                    }

                    // Block expired - clear it
                    entry.BlockedUntil = null;
                }

                return false;
            }
        }

        /// <summary>
        /// Unblock an identifier
        /// </summary>
        public static void Unblock(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return;

            lock (_lock)
            {
                if (_attempts.TryGetValue(identifier, out var entry))
                {
                    entry.BlockedUntil = null;
                    try
                    {
                        ErrorLogger.LogToFileOnly($"Unblocked identifier: {identifier}");
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Get statistics for an identifier
        /// </summary>
        public static RateLimitStats GetStats(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return new RateLimitStats { Identifier = identifier };
            }

            lock (_lock)
            {
                if (!_attempts.TryGetValue(identifier, out var entry))
                {
                    return new RateLimitStats
                    {
                        Identifier = identifier,
                        AttemptCount = 0,
                        IsBlocked = false,
                        FirstAttempt = null,
                        LastAttempt = null
                    };
                }

                var cutoffTime = DateTime.Now.AddMinutes(-WINDOW_MINUTES);
                var recentAttempts = entry.Attempts.Where(t => t > cutoffTime).ToList();

                return new RateLimitStats
                {
                    Identifier = identifier,
                    AttemptCount = recentAttempts.Count,
                    IsBlocked = entry.BlockedUntil.HasValue && DateTime.Now < entry.BlockedUntil.Value,
                    FirstAttempt = recentAttempts.Any() ? recentAttempts.Min() : (DateTime?)null,
                    LastAttempt = recentAttempts.Any() ? recentAttempts.Max() : (DateTime?)null,
                    BlockedUntil = entry.BlockedUntil
                };
            }
        }

        /// <summary>
        /// Cleanup old entries to prevent memory leaks
        /// </summary>
        private static void CleanupOldEntries()
        {
            var cutoffTime = DateTime.Now.AddMinutes(-WINDOW_MINUTES * 2);
            var keysToRemove = new List<string>();

            foreach (var kvp in _attempts)
            {
                // Remove old attempts from entry
                kvp.Value.Attempts = kvp.Value.Attempts
                    .Where(t => t > cutoffTime)
                    .ToList();

                // Check if entry should be removed
                bool hasRecentAttempts = kvp.Value.Attempts.Count > 0;
                bool isCurrentlyBlocked = kvp.Value.BlockedUntil.HasValue && DateTime.Now < kvp.Value.BlockedUntil.Value;

                if (!hasRecentAttempts && !isCurrentlyBlocked)
                {
                    keysToRemove.Add(kvp.Key);
                }

                // Clear expired blocks
                if (kvp.Value.BlockedUntil.HasValue && DateTime.Now >= kvp.Value.BlockedUntil.Value)
                {
                    kvp.Value.BlockedUntil = null;
                }
            }

            foreach (var key in keysToRemove)
            {
                _attempts.Remove(key);
            }

            _lastCleanup = DateTime.Now;

            if (keysToRemove.Count > 0)
            {
                try
                {
                    ErrorLogger.LogToFileOnly($"RateLimiter cleanup completed",
                        null, $"Removed {keysToRemove.Count} old entries, {_attempts.Count} remaining");
                }
                catch { }
            }
        }

        /// <summary>
        /// Force aggressive cleanup when memory is getting full
        /// </summary>
        private static void ForceCleanup()
        {
            var cutoffTime = DateTime.Now.AddMinutes(-WINDOW_MINUTES);
            var keysToRemove = new List<string>();
            var blockedCount = 0;

            foreach (var kvp in _attempts)
            {
                // Only keep entries with active blocks or very recent activity
                bool hasRecentAttempts = kvp.Value.Attempts.Any(t => t > cutoffTime);
                bool isCurrentlyBlocked = kvp.Value.BlockedUntil.HasValue && DateTime.Now < kvp.Value.BlockedUntil.Value;

                if (isCurrentlyBlocked)
                {
                    blockedCount++;
                }

                if (!hasRecentAttempts && !isCurrentlyBlocked)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _attempts.Remove(key);
            }

            _lastCleanup = DateTime.Now;

            try
            {
                ErrorLogger.LogToFileOnly($"RateLimiter FORCE cleanup completed",
                    null, $"Removed {keysToRemove.Count} entries, {_attempts.Count} remaining, {blockedCount} blocked");
            }
            catch { }
        }

        /// <summary>
        /// Clear all rate limit data (use with caution!)
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _attempts.Clear();
                try
                {
                    ErrorLogger.LogToFileOnly("RateLimiter: All data cleared");
                }
                catch { }
            }
        }

        /// <summary>
        /// Get total number of tracked identifiers
        /// </summary>
        public static int GetTrackedCount()
        {
            lock (_lock)
            {
                return _attempts.Count;
            }
        }

        /// <summary>
        /// Get memory statistics
        /// </summary>
        public static RateLimiterMemoryStats GetMemoryStats()
        {
            lock (_lock)
            {
                int totalAttempts = 0;
                int blockedCount = 0;

                foreach (var entry in _attempts.Values)
                {
                    totalAttempts += entry.Attempts.Count;
                    if (entry.BlockedUntil.HasValue && DateTime.Now < entry.BlockedUntil.Value)
                    {
                        blockedCount++;
                    }
                }

                return new RateLimiterMemoryStats
                {
                    TrackedIdentifiers = _attempts.Count,
                    TotalAttempts = totalAttempts,
                    BlockedIdentifiers = blockedCount,
                    LastCleanup = _lastCleanup
                };
            }
        }

        /// <summary>
        /// Dispose cleanup timer (call from Application_End)
        /// </summary>
        public static void Dispose()
        {
            lock (_lock)
            {
                _cleanupTimer?.Dispose();
                _cleanupTimer = null;
                _timerInitialized = false;
            }
        }
    }

    /// <summary>
    /// Internal class to track rate limit attempts
    /// </summary>
    internal class RateLimitEntry
    {
        public List<DateTime> Attempts { get; set; } = new List<DateTime>();
        public DateTime? BlockedUntil { get; set; }
    }

    /// <summary>
    /// Rate limit statistics
    /// </summary>
    public class RateLimitStats
    {
        public string Identifier { get; set; }
        public int AttemptCount { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime? FirstAttempt { get; set; }
        public DateTime? LastAttempt { get; set; }
        public DateTime? BlockedUntil { get; set; }
    }

    /// <summary>
    /// Memory statistics for RateLimiter
    /// </summary>
    public class RateLimiterMemoryStats
    {
        public int TrackedIdentifiers { get; set; }
        public int TotalAttempts { get; set; }
        public int BlockedIdentifiers { get; set; }
        public DateTime LastCleanup { get; set; }

        public override string ToString()
        {
            return $"Identifiers: {TrackedIdentifiers}, Attempts: {TotalAttempts}, Blocked: {BlockedIdentifiers}, Last Cleanup: {LastCleanup}";
        }
    }
}
