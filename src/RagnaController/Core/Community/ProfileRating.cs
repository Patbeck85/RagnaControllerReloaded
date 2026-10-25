using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RagnaController.Profiles;

namespace RagnaController.Core.Community {
    /// <summary>
    /// TASK-2704: ProfileRating — Modell für eine Community-Bewertung eines Gist-Profils.
    ///
    /// Enthält Sterne-Bewertung (1-5), optionalen Kommentar, Bewerter und Zeitstempel.
    /// Wird vom ProfileReviewService verwaltet und mit dem Profil persistiert.
    /// </summary>
    public sealed class ProfileRating
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ProfileId { get; set; } = "";
        public string ReviewerName { get; set; } = "Anonymous";
        public string ReviewerId { get; set; } = "";
        public int Stars { get; set; } = 5;
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsVerifiedPurchase { get; set; } = false;
        public List<string> HelpfulVoterIds { get; set; } = new();

        public ProfileRating()
        {
        }

        public ProfileRating(string profileId, string reviewerName, int stars, string comment = "")
        {
            ProfileId = profileId;
            ReviewerName = reviewerName;
            Stars = Math.Clamp(stars, 1, 5);
            Comment = comment;
        }

        /// <summary>Durchschnittliche Sterne-Bewertung (auf 2 Nachkommastellen gerundet).</summary>
        public static double AverageStars(IEnumerable<ProfileRating> ratings)
        {
            var list = ratings as IList<ProfileRating> ?? ratings.ToList();
            if (list.Count == 0) return 0.0;
            double sum = 0;
            foreach (var r in list) sum += r.Stars;
            return Math.Round(sum / list.Count, 2);
        }

        /// <summary>Anzahl der Bewertungen.</summary>
        public static int Count(IEnumerable<ProfileRating> ratings)
        {
            var list = ratings as IList<ProfileRating> ?? ratings.ToList();
            return list.Count;
        }

        /// <summary>Verteilungs-Histogramm: Index 0 = 1 Sterne, Index 4 = 5 Sterne.</summary>
        public static int[] StarDistribution(IEnumerable<ProfileRating> ratings)
        {
            var dist = new int[5];
            foreach (var r in ratings)
            {
                var idx = Math.Clamp(r.Stars - 1, 0, 4);
                dist[idx]++;
            }
            return dist;
        }
    }

    /// <summary>
    /// TASK-2704: ProfileReviewService — verwaltet Community-Bewertungen für Gist-Profile.
    ///
    /// Ermöglicht das Hinzufügen, Bearbeiten und Löschen von Bewertungen sowie
    /// die Berechnung von Durchschnittswerten und Sternverteilungen.
    /// </summary>
    public sealed class ProfileReviewService
    {
        private readonly Dictionary<string, List<ProfileRating>> _ratingsByProfile = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Gibt alle Bewertungen für ein Profil zurück.</summary>
        public IReadOnlyList<ProfileRating> GetRatings(string profileId)
        {
            if (_ratingsByProfile.TryGetValue(profileId, out var ratings))
                return ratings.AsReadOnly();
            return new List<ProfileRating>().AsReadOnly();
        }

        /// <summary>Fügt eine neue Bewertung hinzu oder aktualisiert eine bestehende des gleichen Reviewers.</summary>
        public bool AddOrUpdateRating(string profileId, string reviewerName, string reviewerId, int stars, string comment = "")
        {
            if (string.IsNullOrWhiteSpace(profileId)) return false;
            if (string.IsNullOrWhiteSpace(reviewerName)) return false;

            stars = Math.Clamp(stars, 1, 5);

            if (!_ratingsByProfile.ContainsKey(profileId))
                _ratingsByProfile[profileId] = new List<ProfileRating>();

            var ratings = _ratingsByProfile[profileId];

            // Bestehende Bewertung des gleichen Reviewers suchen und aktualisieren
            var existing = ratings.Find(r => r.ReviewerId.Equals(reviewerId, StringComparison.OrdinalIgnoreCase) ||
                                              r.ReviewerName.Equals(reviewerName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Stars = stars;
                existing.Comment = comment;
                existing.UpdatedAt = DateTime.UtcNow;
                return true;
            }

            var rating = new ProfileRating(profileId, reviewerName, stars, comment)
            {
                ReviewerId = reviewerId,
            };
            ratings.Add(rating);
            return true;
        }

        /// <summary>Entfernt eine Bewertung eines Reviewers.</summary>
        public bool RemoveRating(string profileId, string reviewerId)
        {
            if (string.IsNullOrWhiteSpace(profileId) || string.IsNullOrWhiteSpace(reviewerId)) return false;
            if (!_ratingsByProfile.TryGetValue(profileId, out var ratings)) return false;

            var removed = ratings.RemoveAll(r => r.ReviewerId.Equals(reviewerId, StringComparison.OrdinalIgnoreCase));
            return removed > 0;
        }

        /// <summary>Gibt die Durchschnittsbewertung für ein Profil zurück (0.0 wenn keine Bewertungen).</summary>
        public double GetAverageStars(string profileId)
        {
            var ratings = GetRatings(profileId);
            return ProfileRating.AverageStars(ratings);
        }

        /// <summary>Gibt die Anzahl der Bewertungen für ein Profil zurück.</summary>
        public int GetRatingCount(string profileId)
        {
            var ratings = GetRatings(profileId);
            return ProfileRating.Count(ratings);
        }

        /// <summary>Gibt die Sternverteilung für ein Profil zurück (Index 0 = 1 Sterne, ... Index 4 = 5 Sterne).</summary>
        public int[] GetStarDistribution(string profileId)
        {
            var ratings = GetRatings(profileId);
            return ProfileRating.StarDistribution(ratings);
        }

        /// <summary>Fügt einen "Hilfreich"-Vote für eine Bewertung hinzu.</summary>
        public bool AddHelpfulVote(string profileId, string ratingId, string voterId)
        {
            if (string.IsNullOrWhiteSpace(profileId) || string.IsNullOrWhiteSpace(ratingId) || string.IsNullOrWhiteSpace(voterId))
                return false;

            var ratings = GetRatings(profileId);
            var rating = ratings.FirstOrDefault(r => r.Id == ratingId);
            if (rating == null) return false;
            if (rating.HelpfulVoterIds.Contains(voterId)) return false;

            rating.HelpfulVoterIds.Add(voterId);
            return true;
        }

        /// <summary>Entfernt einen "Hilfreich"-Vote.</summary>
        public bool RemoveHelpfulVote(string profileId, string ratingId, string voterId)
        {
            if (string.IsNullOrWhiteSpace(profileId) || string.IsNullOrWhiteSpace(ratingId) || string.IsNullOrWhiteSpace(voterId))
                return false;

            var ratings = GetRatings(profileId);
            var rating = ratings.FirstOrDefault(r => r.Id == ratingId);
            if (rating == null) return false;

            return rating.HelpfulVoterIds.Remove(voterId);
        }

        /// <summary>Löscht alle Bewertungen für ein Profil.</summary>
        public void ClearRatings(string profileId)
        {
            _ratingsByProfile.Remove(profileId);
        }

        /// <summary>Gibt alle Profile-IDs zurück, die mindestens eine Bewertung haben.</summary>
        public IReadOnlyList<string> GetRatedProfileIds()
        {
            var ids = new List<string>(_ratingsByProfile.Keys);
            return ids.AsReadOnly();
        }
    }
}
