using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class UserPreference
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;

        // dimension / feature
        [Required]
        public Guid PreferenceId { get; set; }

        [ForeignKey(nameof(PreferenceId))]
        public Preference Preference { get; set; } = null!;
    }
}
