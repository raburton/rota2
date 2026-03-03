using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rota2.Models
{
    public enum SwapStatus { Pending = 0, Accepted = 1, Rejected = 2, Cancelled = 3 }

    public class SwapRequest
    {
        public int Id { get; set; }
        public int FromUserId { get; set; } // requester
        public int ToUserId { get; set; } // nominated person
        public SwapStatus Status { get; set; } = SwapStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // CSV of ShiftAssignment Ids the requester wants to take from the nominated user
        public string RequestedAssignmentIds { get; set; } = string.Empty;
        // CSV of ShiftAssignment Ids the requester offers to the nominated user
        public string OfferedAssignmentIds { get; set; } = string.Empty;

        [NotMapped]
        public IEnumerable<int> RequestedAssignments => ParseCsv(RequestedAssignmentIds);

        [NotMapped]
        public IEnumerable<int> OfferedAssignments => ParseCsv(OfferedAssignmentIds);

        private static IEnumerable<int> ParseCsv(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) yield break;
            foreach (var part in s.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part, out var v)) yield return v;
            }
        }
    }
}
