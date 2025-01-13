using FluentValidation;
using System;

namespace PracticeApi.Domain.DTO
{
    // TodoDto Model
    public class TodoDto
    {
        public string Title { get; set; }
        public string Description { get; set; }

        // Force default to false
        private bool _iscomPleted = false;
        public bool IscomPleted
        {
            get => _iscomPleted;
            set => _iscomPleted = false; // Always override to false
        }
    }

    // TodoDto Validator using FluentValidation
    public class TodoDtoValidator : AbstractValidator<TodoDto>
    {
        public TodoDtoValidator()
        {
            // Title must not be empty
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.");

            // Description must not be empty
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.");

            // IscomPleted must always be false
            RuleFor(x => x.IscomPleted)
                .Must(x => x == false).WithMessage("IscomPleted must be false.");
        }
    }

    // Response DTO for Todo
    public class TodoResponseDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IscomPleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? DueDate { get; set; }
    }

    // Update Request DTO for Todo
    public class TodoUpdateRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool? IsCompleted { get; set; }
    }
}
