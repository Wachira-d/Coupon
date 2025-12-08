using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Coupon.Helpers
{
    /// <summary>
    /// Advanced rate limiting system with memory-based tracking
    /// Prevents brute force attacks and abuse
    /// </summary>
    public static class RateLimiter
    {
        private static readonly object _lock = new object();
        private static Dictionary<string, RateLimitEntry> _attempts = new Dictionary<string, RateLimitEntry>();

        // Configuration
        private const int MAX_ATTEMPTS = 10;
        private const int WINDOW_MINUTES = 60;
        private const int CLEANUP_INTERVAL_MINUTES = 5;
        private static DateTime _lastCleanup = DateTime.Now;

        /// <summary>
        /// Check if request is allowed based on rate limiting
        /// </summary>
        public static bool IsAllowed(string identifier, int maxAttempts = MAX_ATTEMPTS, int windowMinutes = WINDOW_MINUTES)
        {
            lock (_lock)
            {
                // Cleanup old entries periodically
                if (DateTime.Now.Subtract(_lastCleanup).TotalMinutes >= CLEANUP_INTERVAL_MINUTES)
                {
                    CleanupOldEntries();
                }

                if (!_attempts.ContainsKey(identifier))
                {
                    _attempts[identifier] = new RateLimitEntry();
                }

                var entry = _attempts[identifier];
                var cutoffTime = DateTime.Now.AddMinutes(-windowMinutes);

                // Remove attempts outside the time window
                entry.Attempts = entry.Attempts.Where(t => t > cutoffTime).ToList();

                // Check if limit exceeded
                if (entry.Attempts.Count >= maxAttempts)
                {
                    ErrorLogger.LogWarning($"Rate limit exceeded for: {identifier}",
                        $"Attempts: {entry.Attempts.Count}, Window: {windowMinutes} minutes");
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
            lock (_lock)
            {
                if (!_attempts.ContainsKey(identifier))
                {
                    _attempts[identifier] = new RateLimitEntry();
                }

                _attempts[identifier].Attempts.Add(DateTime.Now);
            }
        }

        /// <summary>
        /// Get number of attempts in current window
        /// </summary>
        public static int GetAttemptCount(string identifier, int windowMinutes = WINDOW_MINUTES)
        {
            lock (_lock)
            {
                if (!_attempts.ContainsKey(identifier))
                {
                    return 0;
                }

                var entry = _attempts[identifier];
                var cutoffTime = DateTime.Now.AddMinutes(-windowMinutes);
                return entry.Attempts.Count(t => t > cutoffTime);
            }
        }

        /// <summary>
        /// Reset attempts for an identifier (e.g., after successful login)
        /// </summary>
        public static void Reset(string identifier)
        {
            lock (_lock)
            {
                if (_attempts.ContainsKey(identifier))
                {
                    _attempts.Remove(identifier);
                }
            }
        }

        /// <summary>
        /// Block an identifier temporarily or permanently
        /// </summary>
        public static void Block(string identifier, int durationMinutes = 0)
        {
            lock (_lock)
            {
                if (!_attempts.ContainsKey(identifier))
                {
                    _attempts[identifier] = new RateLimitEntry();
                }

                _attempts[identifier].BlockedUntil = durationMinutes > 0
                    ? DateTime.Now.AddMinutes(durationMinutes)
                    : DateTime.MaxValue; // Permanent block

                ErrorLogger.LogWarning($"Blocked identifier: {identifier}",
                    $"Duration: {(durationMinutes > 0 ? durationMinutes + " minutes" : "Permanent")}");
            }
        }

        /// <summary>
        /// Check if identifier is currently blocked
        /// </summary>
        public static bool IsBlocked(string identifier)
        {
            lock (_lock)
            {
                if (!_attempts.ContainsKey(identifier))
                {
                    return false;
                }

                var entry = _attempts[identifier];
                if (entry.BlockedUntil.HasValue && DateTime.Now < entry.BlockedUntil.Value)
                {
                    return true;
                }

                // Reset block if it's expired
                if (entry.BlockedUntil.HasValue && DateTime.Now >= entry.BlockedUntil.Value)
                {
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
            lock (_lock)
            {
                if (_attempts.ContainsKey(identifier))
                {
                    _attempts[identifier].BlockedUntil = null;
                    ErrorLogger.LogInfo($"Unblocked identifier: {identifier}");
                }
            }
        }

        /// <summary>
        /// Get statistics for an identifier
        /// </summary>
        public static RateLimitStats GetStats(string identifier)
        {
            lock (_lock)
            {
                if (!_attempts.ContainsKey(identifier))
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

                var entry = _attempts[identifier];
                var cutoffTime = DateTime.Now.AddMinutes(-WINDOW_MINUTES);
                var recentAttempts = entry.Attempts.Where(t => t > cutoffTime).ToList();

                return new RateLimitStats
                {
                    Identifier = identifier,
                    AttemptCount = recentAttempts.Count,
                    IsBlocked = IsBlocked(identifier),
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
            var cutoffTime = DateTime.Now.AddMinutes(-WINDOW_MINUTES * 2); // Keep 2x window for safety
            var keysToRemove = new List<string>();

            foreach (var kvp in _attempts)
            {
                // Remove if no recent attempts and not blocked
                if (!kvp.Value.Attempts.Any(t => t > cutoffTime) &&
                    (!kvp.Value.BlockedUntil.HasValue || kvp.Value.BlockedUntil.Value < DateTime.Now))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _attempts.Remove(key);
            }

            _lastCleanup = DateTime.Now;

            if (keysToRemove.Count > 0)
            {
                ErrorLogger.LogInfo($"RateLimiter cleanup completed", $"Removed {keysToRemove.Count} old entries");
            }
        }

        /// <summary>
        /// Clear all rate limit data (use with caution!)
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _attempts.Clear();
                ErrorLogger.LogWarning("RateLimiter: All data cleared");
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
}
